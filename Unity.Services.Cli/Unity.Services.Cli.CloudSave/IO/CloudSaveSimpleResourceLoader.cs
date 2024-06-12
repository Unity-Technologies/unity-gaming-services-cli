using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Unity.Services.Cli.CloudSave.Deploy;
using Unity.Services.CloudSave.Authoring.Core.IO;
using Unity.Services.CloudSave.Authoring.Core.Model;

namespace Unity.Services.Cli.CloudSave.IO;

public class CloudSaveSimpleResourceLoader : ICloudSaveSimpleResourceLoader
{
    readonly IFileSystem m_FileSystem;
    readonly JsonSerializerSettings m_SerializerSettings;

    public CloudSaveSimpleResourceLoader(IFileSystem fileSystem)
    {
        m_FileSystem = fileSystem;

        m_SerializerSettings = new JsonSerializerSettings()
        {
            Converters = { new StringEnumConverter() },
            Formatting = Formatting.Indented,
            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
        };
    }

    public async Task<IResourceDeploymentItem> ReadResource(string path, CancellationToken token)
    {
        var fileName = Path.GetFileName(path);
        var deploymentItem = new SimpleResourceDeploymentItem(fileName, path);
        try
        {
            var text = await m_FileSystem.ReadAllText(path, token);
            var model = FromFile(JsonConvert.DeserializeObject<SimpleResourceConfigFile>(text, m_SerializerSettings)!);
            deploymentItem.Resource = model;
            // By default, use filename as the Id, unless overriden.
            // This reduces cognitive complexity in having multiple "ids" for the same file
            // and matches the same behavior as other services (like CloudCode where the name _is_ the ID)
            if (model.Id == null)
                model.Id = Path.GetFileNameWithoutExtension(fileName);
        }
        catch (IOException e)
        {
            deploymentItem.Status = Statuses.GetFailedToLoad(e, deploymentItem.Path);;
        }
        catch (JsonException e)
        {
            deploymentItem.Status = Statuses.GetFailedToRead(e, deploymentItem.Path);
        }

        return deploymentItem;
    }

    public async Task CreateOrUpdateResource(IResourceDeploymentItem deployableItem, CancellationToken token)
    {
        var fileName = Path.GetFileNameWithoutExtension(deployableItem.Path);
        var id = deployableItem.Resource.Id;
        try
        {
            // By default, use filename as the Id, unless overriden.
            // This reduces cognitive complexity in having multiple "ids" for the same file
            var fileModel = ToFile(deployableItem.Resource);
            if (fileModel.Id == fileName)
                fileModel.Id = null;
            var text = JsonConvert.SerializeObject(deployableItem.Resource, m_SerializerSettings);
            await m_FileSystem.WriteAllText(deployableItem.Path, text, token);
        }
        catch (JsonException e)
        {
            deployableItem.Status = Statuses.GetFailedToSerialize(e, deployableItem.Path);
        }
        catch (Exception e)
        {
            deployableItem.Status = Statuses.GetFailedToWrite(e, deployableItem.Path);
        }
    }

    public async Task DeleteResource(IResourceDeploymentItem deploymentItem, CancellationToken token)
    {
        try
        {
            await m_FileSystem.Delete(deploymentItem.Path, token);
        }
        catch (IOException e)
        {
            deploymentItem.Status = Statuses.GetFailedToDelete(e, deploymentItem.Path);
        }
    }

    static SimpleResource FromFile(SimpleResourceConfigFile fileModel)
    {
        //TODO: If your file format does not match your resource model (very likely), you should map from file model to resource model here
        return new SimpleResource()
        {
            Id = fileModel.Id,
            Name = fileModel.Name,
            NestedObj = fileModel.NestedObj,
            AStrValue = fileModel.AStrValue
        };
    }

    static SimpleResourceConfigFile ToFile(IResource fileModel)
    {
        //TODO: If your file format does not match your resource model (very likely), you should map from resource model to file format here
        return new SimpleResourceConfigFile()
        {
            Id = fileModel.Id,
            Name = fileModel.Name,
            NestedObj = fileModel.NestedObj,
            AStrValue = fileModel.AStrValue,
        };
    }
}
