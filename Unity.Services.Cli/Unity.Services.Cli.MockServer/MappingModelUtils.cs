using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using WireMock.Admin.Mappings;
using WireMock.Extensions;
using WireMock.Net.OpenApiParser;
using WireMock.Net.OpenApiParser.Settings;
using YamlDotNet.Serialization;
using FileMode = System.IO.FileMode;

namespace Unity.Services.Cli.MockServer;

/// <summary>
/// Utilities for MappingModel loading and configuration
/// </summary>
public static class MappingModelUtils
{
    static readonly string k_RepositoryPath = Directory.GetParent(Environment.CurrentDirectory)!.Parent!.Parent!.Parent!.Parent!.ToString();

    /// <summary>
    /// Search path with `{<paramref name="key"/>}/{old-value}` and replace with `{<paramref name="key"/>}/{<paramref name="value"/>}`
    /// </summary>
    /// <param name="model"></param>
    /// <param name="key">key path pattern to search</param>
    /// <param name="value">expected value to replace from `{<paramref name="key"/>}/{old-value}`</param>
    /// <returns>model with replaced value</returns>
    public static MappingModel ConfigMappingPathWithKey(this MappingModel model, string key, string value)
    {
        //We are looking for the following pattern key/{old-value} or and replace with key/{value}
        model.Request.Path = Regex.Replace(model.Request.GetPathAsString()!, $"(?<={key}/).*?(?=/|$)", value);
        return model;
    }

    /// <summary>
    /// Override current BodyAsJson response for the one passed as parameter
    /// </summary>
    /// <param name="model"></param>
    /// <param name="response">response to be return by the model in BodyAsJson</param>
    /// <returns>model with replaced value</returns>
    public static MappingModel ConfigBodyAsJsonResponse(this MappingModel model, object response)
    {
        //We are looking for the following pattern key/{old-value} or and replace with key/{value}
        model.Response.BodyAsJson = response;
        return model;
    }

    /// <summary>
    /// Parse mapping models from openAPI generator config. Make sure configuration inputSpec is downloaded before calling it.
    /// </summary>
    /// <param name="settings">settings to define how to parse Open API doc as request/response model</param>
    /// <param name="generatorConfigFile">Open API generator config file</param>
    /// <returns>MappingModels parsed from Open API doc</returns>
    public static async Task<IEnumerable<MappingModel>> ParseMappingModelsFromGeneratorConfigAsync(string generatorConfigFile, WireMockOpenApiParserSettings settings)
    {
        var inputSpec = await LoadApiInputSpecFromGeneratorAsync(generatorConfigFile);
        return await ParseMappingModelsAsync(inputSpec, settings);
    }

    /// <summary>
    /// Parse mapping models from <paramref name="inputSpec"/>
    /// </summary>
    /// <param name="inputSpec">Specification for Open API doc input source, could be file path or url link</param>
    /// <param name="settings">settings to define how to parse Open API doc as request/response model</param>
    /// <returns>MappingModels parsed from Open API doc</returns>
    /// <exception cref="FileNotFoundException">If input spec is an invalid file, exception will be thrown</exception>
    public static async Task<IEnumerable<MappingModel>> ParseMappingModelsAsync(string inputSpec, WireMockOpenApiParserSettings settings)
    {
        WireMockOpenApiParser parser = new WireMockOpenApiParser();

        IEnumerable<MappingModel> models;
        if (inputSpec.StartsWith("https"))
        {
            await using var stream = await GetOpenApiStream(inputSpec, 3);
            models = parser.FromStream(stream, settings, out var diagnostic);
            foreach (var error in diagnostic.Errors)
            {
                await Console.Error.WriteLineAsync(error.ToString());
            }
        }
        else
        {
            var openApiPath = Path.Combine(k_RepositoryPath, inputSpec);
            if (!File.Exists(openApiPath))
            {
                throw new FileNotFoundException($"Open API source does not exist: {openApiPath}");
            }
            await using var stream = File.Open(openApiPath, FileMode.Open);
            models = parser.FromStream(stream, settings, out var diagnostic);
            foreach (var error in diagnostic.Errors)
            {
                await Console.Error.WriteLineAsync(error.ToString());
            }
        }

        // WireMock.Net have an issue adding an extra "/" at the beginning of the path. We remove it with the following logic
        models = models.Select(m =>
        {
            m.Request.Path = m.Request.GetPathAsString()!.Replace("//", "/");
            return m;
        });
        return models;
    }

    /// <summary>
    /// Load "inputSpec" from <paramref name="generatorConfigFile"/>
    /// </summary>
    /// <param name="generatorConfigFile">Open API generator config file</param>
    /// <returns>input spec string</returns>
    public static async Task<string> LoadApiInputSpecFromGeneratorAsync(string generatorConfigFile)
    {
        string generatorConfigFilePath = Path.Combine(k_RepositoryPath, "OpenApi", generatorConfigFile);
        var openApiConfigString = await File.ReadAllTextAsync(generatorConfigFilePath);
        using var r = new StringReader(openApiConfigString);
        var deserializer = new DeserializerBuilder().Build();
        var yamlObject = deserializer.Deserialize(r);
        var serializer = new SerializerBuilder().JsonCompatible().Build();
        var json = serializer.Serialize(yamlObject!);
        var inputSpec = (string)JObject.Parse(json).SelectToken("inputSpec")!;
        return inputSpec;
    }

    static async Task<Stream> GetOpenApiStream(string url, int retry)
    {
        using var client = new HttpClient();
        HttpRequestException exception = new HttpRequestException();
        for (int i = 0; i < retry; i++)
        {
            try
            {
                return await client.GetStreamAsync(url);
            }
            catch (HttpRequestException ex)
            {
                exception = ex;
            }
        }
        throw exception;
    }
}
