using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Common.Persister;

namespace Unity.Services.Cli.Common.UnitTest.Persister;

[TestFixture]
class JsonFilePersisterTests
{
    static readonly IEnumerable<string> k_FilePaths = new[]
    {
        "JsonFilePersisterTests.json",
        "JsonFilePersisterTests/file.json",
        "JsonFilePersisterTests/Parent/file.json",
    };

    [SetUp]
    [TearDown]
    public void DeleteTestFiles()
    {
        foreach (var path in k_FilePaths)
        {
            DeleteAll(path);
        }

        void DeleteAll(string path)
        {
            var rootName = GetRootName(path);
            if (!string.IsNullOrEmpty(rootName)
                && Directory.Exists(rootName))
            {
                Directory.Delete(rootName, true);
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        string? GetRootName(string path)
        {
            var separators = new[]
            {
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar
            };
            var split = path.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            return split.FirstOrDefault();
        }
    }

    [TestCaseSource(nameof(k_FilePaths))]
    public async Task LoadAsyncLoadsDataWhenFileExists(string filePath)
    {
        var persister = new JsonFilePersister<(string foo, bool bar)>(filePath);
        persister.EnsureDirectoryExists();
        (string foo, bool bar) expectedData = ("loadTest", true);
        var json = JsonConvert.SerializeObject(expectedData);
        await File.WriteAllTextAsync(filePath, json);

        var loadedData = await persister.LoadAsync();

        Assert.AreEqual(expectedData, loadedData);
    }

    [TestCaseSource(nameof(k_FilePaths))]
    public async Task LoadAsyncReturnsDefaultWhenFileDoesNotExist(string filePath)
    {
        var persister = new JsonFilePersister<bool>(filePath);
        Assert.IsFalse(File.Exists(filePath));

        var loadedData = await persister.LoadAsync();

        Assert.IsFalse(loadedData);
    }

    [TestCaseSource(nameof(k_FilePaths))]
    public async Task SaveAsyncCreatesFileWithExpectedValueWhenNoneExist(string filePath)
    {
        var persister = new JsonFilePersister<bool>(filePath);

        await persister.SaveAsync(true);

        Assert.IsTrue(File.Exists(filePath));
        await AssertJsonFileEquals(filePath, true);
    }

    [TestCaseSource(nameof(k_FilePaths))]
    public async Task SaveAsyncOverridesExistingFileWithExpectedValue(string filePath)
    {
        var persister = new JsonFilePersister<bool>(filePath);
        persister.EnsureDirectoryExists();
        var json = JsonConvert.SerializeObject(false);
        await File.WriteAllTextAsync(filePath, json);
        await AssertJsonFileEquals(filePath, false);

        await persister.SaveAsync(true);

        Assert.IsTrue(File.Exists(filePath));
        await AssertJsonFileEquals(filePath, true);
    }

    static async Task AssertJsonFileEquals<T>(string filePath, T expected)
    {
        var fileText = await File.ReadAllTextAsync(filePath);
        Assert.AreEqual(expected, JsonConvert.DeserializeObject<T>(fileText));
    }

    [TestCaseSource(nameof(k_FilePaths))]
    public void EnsureDirectoryExistsCreatesDirectory(string filePath)
    {
        var directoryName = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(directoryName))
        {
            return;
        }

        Assert.IsFalse(Directory.Exists(directoryName));

        var persister = new JsonFilePersister<bool>(filePath);
        persister.EnsureDirectoryExists();

        Assert.IsTrue(Directory.Exists(directoryName));
    }

    [TestCaseSource(nameof(k_FilePaths))]
    public async Task DeleteAsyncDeletesFileIfItExists(string filePath)
    {
        var persister = new JsonFilePersister<bool>(filePath);
        persister.EnsureDirectoryExists();
        await File.WriteAllTextAsync(filePath, "{}");
        Assert.IsTrue(File.Exists(filePath));

        await persister.DeleteAsync();

        Assert.IsFalse(File.Exists(filePath));
    }

    [TestCaseSource(nameof(k_FilePaths))]
    public void DeleteAsyncSucceedsWhenFileDoesNotExist(string filePath)
    {
        Assert.IsFalse(File.Exists(filePath));
        var persister = new JsonFilePersister<bool>(filePath);

        Assert.DoesNotThrowAsync(() => persister.DeleteAsync());
    }
}
