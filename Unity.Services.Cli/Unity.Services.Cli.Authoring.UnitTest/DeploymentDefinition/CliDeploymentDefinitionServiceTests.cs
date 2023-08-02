using System.Collections.ObjectModel;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.DeploymentDefinition;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Deployment.Core.Model;

namespace Unity.Services.Cli.Authoring.UnitTest.Service;

[TestFixture]
class CliDeploymentDefinitionServiceTests
{
    Mock<IDeploymentDefinitionFileService> m_MockFileService;
    CliDeploymentDefinitionService m_DdefService;

    List<IDeploymentDefinition> m_InputDdefs = new();
    List<IDeploymentDefinition> m_AllDdefs = new();
    List<string> m_Files = new();
    List<string> m_Extensions = new();

    public CliDeploymentDefinitionServiceTests()
    {
        m_MockFileService = new Mock<IDeploymentDefinitionFileService>();
        m_MockFileService
            .Setup(fs => fs.GetDeploymentDefinitionsForInput(It.IsAny<IEnumerable<string>>()))
            .Returns(() => new DeploymentDefinitionInputResult(m_InputDdefs, m_AllDdefs));
        m_DdefService = new CliDeploymentDefinitionService(m_MockFileService.Object);
    }

    [SetUp]
    public void SetUp()
    {
        m_Files = new List<string>
        {
            "path/to/folder/script.js",
            "path/to/folder/config.rc",
            "path/to/folder/what.ext",
        };

        m_Extensions = new List<string>
        {
            ".js",
            ".rc",
            ".ext",
            ".ec"
        };

        m_InputDdefs.Clear();
        m_AllDdefs.Clear();
    }

    [Test]
    public void GetFilesFromInput_NoDdef()
    {
        var ddefA = CreateMockDdef("path/to/folder/A.ddef");
        m_AllDdefs.Add(ddefA.Object);

        SetupFileService_ForInput(m_Files, m_Extensions);

        var result = m_DdefService.GetFilesFromInput(m_Files, m_Extensions);

        Assert.AreEqual(4, result.DefinitionFiles.FilesByExtension.Count);

        foreach (var (extension, ddefFiles) in result.DefinitionFiles.FilesByExtension)
        {
            foreach (var ddefFile in ddefFiles)
            {
                Assert.IsTrue(m_Files.Contains(ddefFile));
            }

            Assert.IsTrue(m_Extensions.Contains(extension));
        }
    }

    static Mock<IDeploymentDefinition> CreateMockDdef(string path)
    {
        var mockDdef = new Mock<IDeploymentDefinition>();
        mockDdef
            .SetupGet(d => d.Name)
            .Returns(Path.GetFileName(path));
        mockDdef
            .SetupGet(d => d.Path)
            .Returns(path);
        mockDdef
            .SetupGet(d => d.ExcludePaths)
            .Returns(new ObservableCollection<string>());
        return mockDdef;
    }

    static Mock<IDeploymentDefinition> CreateMockDdef(string path, IEnumerable<string> excludes)
    {
        var mockDdef = CreateMockDdef(path);
        mockDdef
            .SetupGet(d => d.ExcludePaths)
            .Returns(new ObservableCollection<string>(excludes));
        return mockDdef;
    }

    void SetupFileService_ForDdef(
        IDeploymentDefinition ddef,
        List<string> files,
        List<string> extensions)
    {
        foreach (var extension in extensions)
        {
            var relevantFiles = files.Where(f => f.EndsWith(extension)).ToList();
            m_MockFileService
                .Setup(
                    fs => fs.ListFilesToDeploy(
                        files,
                        extension,
                        It.IsAny<bool>()))
                .Returns(relevantFiles);
            m_MockFileService
                .Setup(fs => fs.GetFilesForDeploymentDefinition(ddef, extension))
                .Returns(relevantFiles);
        }
    }

    void SetupFileService_ForInput(
        List<string> inputPaths,
        List<string> extensions)
    {
        foreach (var extension in extensions)
        {
            var relevantFiles = inputPaths.Where(f => f.EndsWith(extension)).ToList();
            m_MockFileService
                .Setup(fs => fs.ListFilesToDeploy(inputPaths, extension, It.IsAny<bool>()))
                .Returns(relevantFiles);
        }
    }

    [Test]
    public void GetFilesFromInput_OnlyDdef()
    {
        var ddefA = CreateMockDdef("path/to/folder/A.ddef");
        m_AllDdefs.Add(ddefA.Object);
        m_InputDdefs.Add(ddefA.Object);

        SetupFileService_ForDdef(ddefA.Object, m_Files, m_Extensions);

        var result = m_DdefService.GetFilesFromInput(
            new[]
            {
                ddefA.Object.Path
            },
            m_Extensions);

        Assert.AreEqual(4, result.DefinitionFiles.FilesByExtension.Count);

        foreach (var (extension, ddefFiles) in result.DefinitionFiles.FilesByExtension)
        {
            foreach (var ddefFile in ddefFiles)
            {
                Assert.IsTrue(m_Files.Contains(ddefFile));
            }

            Assert.IsTrue(m_Extensions.Contains(extension));
        }
    }

    [Test]
    public void GetFilesFromInput_DdefAndFiles()
    {
        var otherFiles = new List<string>
        {
            "path/to/otherFolder/otherScript.js",
            "path/to/otherFolder/otherConfig.rc"
        };

        var ddefA = CreateMockDdef("path/to/otherFolder/A.ddef");
        m_AllDdefs.Add(ddefA.Object);
        m_InputDdefs.Add(ddefA.Object);

        var input = new List<string>(m_Files) { ddefA.Object.Path };
        SetupFileService_ForDdef(ddefA.Object, otherFiles, m_Extensions);
        SetupFileService_ForInput(input, m_Extensions);

        var result = m_DdefService.GetFilesFromInput(input, m_Extensions);

        var flatFilesByExtension = result.AllFilesByExtension
            .SelectMany(kvp => kvp.Value)
            .ToList();
        Assert.AreEqual(5, flatFilesByExtension.Count);
    }

    [Test]
    public void GetDeploymentDefinitionFiles_RespectsNestedDeploymentDefinitions()
    {
        var mockA = CreateMockDdef("path/to/folder/A.ddef", new List<string>());
        var mockB = CreateMockDdef("path/to/folder/subfolder/B.ddef", new List<string>());

        m_AllDdefs.Add(mockA.Object);
        m_AllDdefs.Add(mockB.Object);

        var subfolderFiles = new[]
        {
            "path/to/folder/subfolder/script2.js",
            "path/to/folder/subfolder/config2.rc"
        };
        m_Files.AddRange(subfolderFiles);

        SetupFileService_ForDdef(mockA.Object, m_Files, m_Extensions);
        SetupFileService_ForDdef(mockB.Object, m_Files, m_Extensions);

        m_InputDdefs.Add(mockA.Object);
        var ddefFilesA = m_DdefService.GetDeploymentDefinitionFiles(
            new[]
            {
                mockA.Object.Path
            },
            m_Extensions);

        m_InputDdefs.Remove(mockA.Object);
        m_InputDdefs.Add(mockB.Object);
        var ddefFilesB = m_DdefService.GetDeploymentDefinitionFiles(
            new[]
            {
                mockB.Object.Path
            },
            m_Extensions);

        var flatFiles = ddefFilesA.FilesByExtension
            .SelectMany(kvp => kvp.Value)
            .ToList();
        Assert.AreEqual(3, flatFiles.Count);
        Assert.IsFalse(flatFiles.Any(f => f.Contains("subfolder")));

        flatFiles = ddefFilesB.FilesByExtension
            .SelectMany(kvp => kvp.Value)
            .ToList();
        Assert.AreEqual(2, flatFiles.Count);
        Assert.IsTrue(flatFiles.All(f => subfolderFiles.Contains(f)));
    }

    [Test]
    public void GetDeploymentDefinitionFiles_RespectsExclusions()
    {
        var subfolderFiles = new[]
        {
            "path/to/folder/subfolder/script2.js",
            "path/to/folder/subfolder/config2.rc"
        };
        m_Files.AddRange(subfolderFiles);

        var mockA = CreateMockDdef("path/to/folder/A.ddef", subfolderFiles);
        m_AllDdefs.Add(mockA.Object);

        SetupFileService_ForDdef(mockA.Object, m_Files, m_Extensions);

        m_InputDdefs.Add(mockA.Object);
        var ddefFilesA = m_DdefService.GetDeploymentDefinitionFiles(
            new[]
            {
                mockA.Object.Path
            },
            m_Extensions);

        var flatFiles = ddefFilesA.FilesByExtension
            .SelectMany(kvp => kvp.Value)
            .ToList();
        var flatExcludes = ddefFilesA.ExcludedFilesByDeploymentDefinition
            .SelectMany(kvp => kvp.Value)
            .ToList();

        Assert.AreEqual(2, flatExcludes.Count);
        Assert.AreEqual(3, flatFiles.Count);
        Assert.IsFalse(flatFiles.Any(f => f.Contains("subfolder")));
        Assert.IsTrue(flatExcludes.All(f => subfolderFiles.Contains(f)));
    }

    [Test]
    public void GetDeploymentDefinitionFiles_NoIntersectionAcrossDdefs()
    {
        m_Files = new List<string>
        {
            "UGS/cc/script1.js",
            "UGS/cc/script2.js",
            "UGS/rc/config.rc"
        };

        var subfolderFiles = new List<string>()
        {
            "UGS/ec/file1.ec",
            "UGS/ec/file2.ec"
        };
        m_Files.AddRange(subfolderFiles);

        var mockUgs = CreateMockDdef("UGS/UGS.ddef");
        m_AllDdefs.Add(mockUgs.Object);
        var mockEc = CreateMockDdef("UGS/ec/EC.ddef");
        m_AllDdefs.Add(mockEc.Object);

        m_InputDdefs.Add(mockUgs.Object);
        m_InputDdefs.Add(mockEc.Object);

        SetupFileService_ForDdef(mockUgs.Object, m_Files, m_Extensions);
        SetupFileService_ForDdef(mockEc.Object, subfolderFiles, m_Extensions);

        var inputDdefs = new[]
        {
            mockUgs.Object.Path,
            mockEc.Object.Path
        };

        var ddefFiles = m_DdefService.GetDeploymentDefinitionFiles(inputDdefs, m_Extensions);

        foreach (var ugsFile in ddefFiles.FilesByDeploymentDefinition[mockUgs.Object])
        {
            Assert.IsFalse(ddefFiles.FilesByDeploymentDefinition[mockEc.Object].Contains(ugsFile));
        }

        foreach (var ecFile in ddefFiles.FilesByDeploymentDefinition[mockEc.Object])
        {
            Assert.IsFalse(ddefFiles.FilesByDeploymentDefinition[mockUgs.Object].Contains(ecFile));
        }
    }

    [Test]
    public void VerifyFileIntersection_IntersectionWithDdefFiles_Throws()
    {
        var inputFiles = new Dictionary<string, IReadOnlyList<string>>
        {
            {
                ".js", new List<string>
                {
                    "path/to/file.js"
                }
            }
        };
        var ddefFilesByExtension = new Dictionary<string, IReadOnlyList<string>>
        {
            {
                ".js", new List<string>
                {
                    "path/to/file.js"
                }
            }
        };
        var ddefFilesByDdef = new Dictionary<IDeploymentDefinition, IReadOnlyList<string>>()
        {
            {
                CreateMockDdef("path/to/A.ddef").Object, new List<string>
                {
                    "path/to/file.js"
                }
            }
        };
        var ddefExcludes = new Dictionary<IDeploymentDefinition, IReadOnlyList<string>>();
        var ddefFiles = new DeploymentDefinitionFiles(ddefFilesByExtension, ddefFilesByDdef, ddefExcludes);
        Assert.Throws<DeploymentDefinitionFileIntersectionException>(
            () => CliDeploymentDefinitionService.VerifyFileIntersection(inputFiles, ddefFiles));
    }

    [Test]
    public void VerifyFileIntersection_IntersectionWithDdefExcludes_Throws()
    {
        var inputFiles = new Dictionary<string, IReadOnlyList<string>>
        {
            {
                ".js", new List<string>
                {
                    "path/to/file.js"
                }
            }
        };
        var ddefFilesByExtension = new Dictionary<string, IReadOnlyList<string>>
        {
            {
                ".js", new List<string>
                {
                    "path/to/otherFile.js"
                }
            }
        };
        var ddefFilesByDdef = new Dictionary<IDeploymentDefinition, IReadOnlyList<string>>()
        {
            {
                CreateMockDdef("path/to/A.ddef").Object, new List<string>
                {
                    "path/to/file.js"
                }
            }
        };
        var ddefExcludes = new Dictionary<IDeploymentDefinition, IReadOnlyList<string>>()
        {
            {
                CreateMockDdef(
                        "path/to/A.ddef",
                        new List<string>
                        {
                            "path/to/file.js"
                        })
                    .Object,
                new List<string>
                {
                    "path/to/file.js"
                }
            }
        };
        var ddefFiles = new DeploymentDefinitionFiles(ddefFilesByExtension, ddefFilesByDdef, ddefExcludes);
        Assert.Throws<DeploymentDefinitionFileIntersectionException>(
            () => CliDeploymentDefinitionService.VerifyFileIntersection(inputFiles, ddefFiles));
    }

    [Test]
    public void LogDeploymentDefinitionExclusions_AllExclusionsLogged()
    {
        var ddefResult = new DeploymentDefinitionFilteringResult(
            new DeploymentDefinitionFiles(
                Mock.Of<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                Mock.Of<IReadOnlyDictionary<IDeploymentDefinition, IReadOnlyList<string>>>(),
                new Dictionary<IDeploymentDefinition, IReadOnlyList<string>>
                {
                    {
                        CreateMockDdef("path/to/folder/A.ddef").Object, new List<string>
                        {
                            "path/to/folder/file1.test",
                            "path/to/folder/file2.test"
                        }
                    },
                    {
                        CreateMockDdef("path/to/otherFolder/B.ddef").Object, new List<string>
                        {
                            "path/to/otherFolder/fileY.test",
                            "path/to/otherFolder/fileZ.test"
                        }
                    }
                }),
            new Dictionary<string, IReadOnlyList<string>>());


        var message = ddefResult.GetExclusionsLogMessage();

        foreach (var file in ddefResult.DefinitionFiles.ExcludedFilesByDeploymentDefinition.Values.SelectMany(f => f))
        {
            Assert.IsTrue(message.Contains(file));
        }
    }
}
