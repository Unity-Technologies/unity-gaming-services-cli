using System.Collections.ObjectModel;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.DeploymentDefinition;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Utils;
using Unity.Services.Deployment.Core.Model;

namespace Unity.Services.Cli.Authoring.UnitTest.Service;

[TestFixture]
class CliDeploymentDefinitionServiceTests
{
    Mock<IFileDiscoveryService> m_MockFileService;
    AuthoringFileService m_DdefService;

    List<IDeploymentDefinition> m_AllDdefs = new();
    List<string> m_Files = new();
    List<string> m_Extensions = new();

    public CliDeploymentDefinitionServiceTests()
    {
        m_MockFileService = new Mock<IFileDiscoveryService>();
        m_MockFileService
            .Setup(fs => fs.DiscoverDeploymentDefinitions(It.IsAny<IEnumerable<string>>()))
            .Returns(() => m_AllDdefs);
        m_DdefService = new AuthoringFileService(m_MockFileService.Object);
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

        m_AllDdefs.Clear();
    }

    [Test]
    public void ResolveAuthoringFiles_NoDdef()
    {
        var ddefA = CreateMockDdef("path/to/folder/A.ddef");
        m_AllDdefs.Add(ddefA.Object);

        SetupFileService_ForEachInput(m_Files, m_Extensions);

        var result = m_DdefService.ResolveAuthoringFiles(m_Files, m_Extensions);

        Assert.AreEqual(4, result.AllFilesByExtension.Count);

        foreach (var (extension, files) in result.AllFilesByExtension)
        {
            foreach (var filePath in files.ToPaths())
            {
                Assert.IsTrue(m_Files.Contains(filePath));
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
            var relevantItems = relevantFiles.Select(f => new AuthoringFile(f, ddef)).ToList();
            m_MockFileService
                .Setup(fs => fs.GetFilesForDeploymentDefinition(ddef, extension))
                .Returns(relevantItems);
        }
    }

    /// <summary>
    /// Sets up mock file service for each individual input path (matching the refactored code
    /// which calls ListFilesToDeploy per input path).
    /// </summary>
    void SetupFileService_ForEachInput(
        List<string> inputPaths,
        List<string> extensions)
    {
        foreach (var inputPath in inputPaths)
        {
            foreach (var extension in extensions)
            {
                var singlePathList = new List<string> { inputPath };
                var relevantFiles = inputPath.EndsWith(extension)
                    ? singlePathList
                    : new List<string>();
                m_MockFileService
                    .Setup(fs => fs.ListFilesToDeploy(
                        It.Is<IReadOnlyList<string>>(l => l.Count == 1 && l[0] == inputPath),
                        extension,
                        It.IsAny<bool>()))
                    .Returns(relevantFiles);
            }
        }
    }

    [Test]
    public void ResolveAuthoringFiles_OnlyDdef()
    {
        var ddefA = CreateMockDdef("path/to/folder/A.ddef");
        m_AllDdefs.Add(ddefA.Object);

        SetupFileService_ForDdef(ddefA.Object, m_Files, m_Extensions);

        var result = m_DdefService.ResolveAuthoringFiles(
            new[]
            {
                ddefA.Object.Path
            },
            m_Extensions);

        Assert.AreEqual(4, result.DefinitionFiles.FilesByExtension.Count);

        foreach (var (extension, ddefFiles) in result.DefinitionFiles.FilesByExtension)
        {
            foreach (var ddefFile in ddefFiles.ToPaths())
            {
                Assert.IsTrue(m_Files.Contains(ddefFile));
            }

            Assert.IsTrue(m_Extensions.Contains(extension));
        }
    }

    [Test]
    public void ResolveAuthoringFiles_DdefAndFiles()
    {
        var otherFiles = new List<string>
        {
            "path/to/otherFolder/otherScript.js",
            "path/to/otherFolder/otherConfig.rc"
        };

        var ddefA = CreateMockDdef("path/to/otherFolder/A.ddef");
        m_AllDdefs.Add(ddefA.Object);

        var input = new List<string>(m_Files)
        {
            ddefA.Object.Path
        };
        SetupFileService_ForDdef(ddefA.Object, otherFiles, m_Extensions);
        SetupFileService_ForEachInput(m_Files, m_Extensions);

        var result = m_DdefService.ResolveAuthoringFiles(input, m_Extensions);

        var flatFilesByExtension = result.AllFilesByExtension
            .SelectMany(kvp => kvp.Value)
            .ToList();
        Assert.AreEqual(5, flatFilesByExtension.Count);
    }

    [Test]
    public void ResolveAuthoringFiles_RespectsNestedDeploymentDefinitions()
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

        // Test with only ddef A as input
        var resultA = m_DdefService.ResolveAuthoringFiles(
            new[] { mockA.Object.Path },
            m_Extensions);

        // Test with only ddef B as input
        var resultB = m_DdefService.ResolveAuthoringFiles(
            new[] { mockB.Object.Path },
            m_Extensions);

        var flatFilesA = resultA.AllFilesByExtension
            .SelectMany(kvp => kvp.Value)
            .ToList();
        Assert.AreEqual(3, flatFilesA.Count);
        Assert.IsFalse(flatFilesA.ToPaths().Any(f => f.Contains("subfolder")));

        var flatFilesB = resultB.AllFilesByExtension
            .SelectMany(kvp => kvp.Value)
            .ToList();
        Assert.AreEqual(2, flatFilesB.Count);
        Assert.IsTrue(flatFilesB.ToPaths().All(f => subfolderFiles.Contains(f)));
    }

    [Test]
    public void ResolveAuthoringFiles_RespectsExclusions()
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

        var result = m_DdefService.ResolveAuthoringFiles(
            new[] { mockA.Object.Path },
            m_Extensions);

        var flatFiles = result.AllFilesByExtension
            .SelectMany(kvp => kvp.Value)
            .ToList();
        var flatExcludes = result.DefinitionFiles.ExcludedFilesByDeploymentDefinition
            .SelectMany(kvp => kvp.Value)
            .ToList();

        Assert.AreEqual(2, flatExcludes.Count);
        Assert.AreEqual(3, flatFiles.Count);
        Assert.IsFalse(flatFiles.ToPaths().Any(f => f.Contains("subfolder")));
        Assert.IsTrue(flatExcludes.ToPaths().All(f => subfolderFiles.Contains(f)));
    }

    [Test]
    public void ResolveAuthoringFiles_NoIntersectionAcrossDdefs()
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

        SetupFileService_ForDdef(mockUgs.Object, m_Files, m_Extensions);
        SetupFileService_ForDdef(mockEc.Object, subfolderFiles, m_Extensions);

        var inputDdefs = new[]
        {
            mockUgs.Object.Path,
            mockEc.Object.Path
        };

        var result = m_DdefService.ResolveAuthoringFiles(inputDdefs, m_Extensions);

        foreach (var ugsFile in result.DefinitionFiles.FilesByDeploymentDefinition[mockUgs.Object])
        {
            Assert.IsFalse(result.DefinitionFiles.FilesByDeploymentDefinition[mockEc.Object].Contains(ugsFile));
        }

        foreach (var ecFile in result.DefinitionFiles.FilesByDeploymentDefinition[mockEc.Object])
        {
            Assert.IsFalse(result.DefinitionFiles.FilesByDeploymentDefinition[mockUgs.Object].Contains(ecFile));
        }
    }

    [Test]
    public void VerifyFileIntersection_IntersectionWithDdefFiles_Throws()
    {
        var inputFiles = new Dictionary<string, IReadOnlyList<AuthoringFile>>
        {
            {
                ".js", new List<AuthoringFile>
                {
                    new("path/to/file.js")
                }
            }
        };
        var ddefFilesByExtension = new Dictionary<string, IReadOnlyList<AuthoringFile>>
        {
            {
                ".js", new List<AuthoringFile>
                {
                    new("path/to/file.js")
                }
            }
        };
        var ddefFilesByDdef = new Dictionary<IDeploymentDefinition, IReadOnlyList<AuthoringFile>>()
        {
            {
                CreateMockDdef("path/to/A.ddef").Object, new List<AuthoringFile>
                {
                    new("path/to/file.js")
                }
            }
        };
        var ddefExcludes = new Dictionary<IDeploymentDefinition, IReadOnlyList<AuthoringFile>>();
        var ddefFiles = new DeploymentDefinitionFiles(ddefFilesByExtension, ddefFilesByDdef, ddefExcludes);
        Assert.Throws<DeploymentDefinitionFileIntersectionException>(() =>
            AuthoringFileService.VerifyFileIntersection(inputFiles, ddefFiles));
    }

    [Test]
    public void VerifyFileIntersection_IntersectionWithDdefExcludes_Throws()
    {
        var inputFiles = new Dictionary<string, IReadOnlyList<AuthoringFile>>
        {
            {
                ".js", new List<AuthoringFile>
                {
                    new("path/to/file.js")
                }
            }
        };
        var ddefFilesByExtension = new Dictionary<string, IReadOnlyList<AuthoringFile>>
        {
            {
                ".js", new List<AuthoringFile>
                {
                    new("path/to/otherFile.js")
                }
            }
        };
        var ddefFilesByDdef = new Dictionary<IDeploymentDefinition, IReadOnlyList<AuthoringFile>>()
        {
            {
                CreateMockDdef("path/to/A.ddef").Object, new List<AuthoringFile>
                {
                    new("path/to/file.js")
                }
            }
        };
        var ddefExcludes = new Dictionary<IDeploymentDefinition, IReadOnlyList<AuthoringFile>>()
        {
            {
                CreateMockDdef(
                        "path/to/A.ddef",
                        new List<string>
                        {
                            "path/to/file.js"
                        })
                    .Object,
                new List<AuthoringFile>
                {
                    new("path/to/file.js")
                }
            }
        };
        var ddefFiles = new DeploymentDefinitionFiles(ddefFilesByExtension, ddefFilesByDdef, ddefExcludes);
        Assert.Throws<DeploymentDefinitionFileIntersectionException>(() =>
            AuthoringFileService.VerifyFileIntersection(inputFiles, ddefFiles));
    }

    [Test]
    public void LogDeploymentDefinitionExclusions_AllExclusionsLogged()
    {
        var ddefResult = new DeploymentDefinitionFilteringResult(
            new DeploymentDefinitionFiles(
                Mock.Of<IReadOnlyDictionary<string, IReadOnlyList<AuthoringFile>>>(),
                Mock.Of<IReadOnlyDictionary<IDeploymentDefinition, IReadOnlyList<AuthoringFile>>>(),
                new Dictionary<IDeploymentDefinition, IReadOnlyList<AuthoringFile>>
                {
                    {
                        CreateMockDdef("path/to/folder/A.ddef").Object, new List<AuthoringFile>
                        {
                            new("path/to/folder/file1.test"),
                            new("path/to/folder/file2.test")
                        }
                    },
                    {
                        CreateMockDdef("path/to/otherFolder/B.ddef").Object, new List<AuthoringFile>
                        {
                            new("path/to/otherFolder/fileY.test"),
                            new("path/to/otherFolder/fileY.test")
                        }
                    }
                }),
            new Dictionary<string, IReadOnlyList<AuthoringFile>>(),
            new Dictionary<string, IDeploymentDefinition?>()
        );


        var message = ddefResult.GetExclusionsLogMessage();

        foreach (var file in ddefResult.DefinitionFiles.ExcludedFilesByDeploymentDefinition.Values.SelectMany(f => f))
        {
            Assert.IsTrue(message.Contains(file.Path));
        }
    }

    [Test]
    public void ResolveAuthoringFiles_WithFilesUnderDdef_AttachesDdefMetadata()
    {
        // Arrange
        var inputPaths = new List<string>
        {
            "configs/file.rc",
            "configs/script.js"
        };
        var ddefPath = "configs/config.ddef";
        var variantTags = new[]
        {
            "ios",
            "mobile"
        };
        var ddef = CreateMockDDefWithVariantTags(ddefPath, variantTags.ToList());
        m_AllDdefs.Add(ddef.Object);

        m_MockFileService
            .Setup(fs => fs.ListFilesToDeploy(
                It.Is<IReadOnlyList<string>>(l => l.Count == 1 && l[0] == "configs/file.rc"),
                ".rc",
                false))
            .Returns(
                new List<string>
                {
                    "configs/file.rc"
                });

        m_MockFileService
            .Setup(fs => fs.ListFilesToDeploy(
                It.Is<IReadOnlyList<string>>(l => l.Count == 1 && l[0] == "configs/script.js"),
                ".js",
                false))
            .Returns(
                new List<string>
                {
                    "configs/script.js"
                });

        // Setup remaining extensions to return empty lists
        foreach (var inputPath in inputPaths)
        {
            foreach (var ext in new[] { ".rc", ".js" })
            {
                // Skip already set up combinations
                if (inputPath == "configs/file.rc" && ext == ".rc") continue;
                if (inputPath == "configs/script.js" && ext == ".js") continue;

                m_MockFileService
                    .Setup(fs => fs.ListFilesToDeploy(
                        It.Is<IReadOnlyList<string>>(l => l.Count == 1 && l[0] == inputPath),
                        ext,
                        false))
                    .Returns(new List<string>());
            }
        }

        // Act
        var result = m_DdefService.ResolveAuthoringFiles(inputPaths, [".rc", ".js"]);

        // Assert
        var allFiles = result.AllFilesByExtension.SelectMany(kvp => kvp.Value).ToList();
        Assert.AreEqual(2, allFiles.Count);
        Assert.IsTrue(allFiles.All(f => f.DeploymentDefinition?.Path == ddefPath));
        Assert.IsTrue(allFiles.All(f => f.VariantTags.SequenceEqual(variantTags)));
    }

    [Test]
    public void ResolveAuthoringFiles_WithFileWithoutDdef_HasNoMetadata()
    {
        // Arrange
        var inputPaths = new List<string>
        {
            "standalone/file.rc"
        };

        m_MockFileService
            .Setup(fs => fs.ListFilesToDeploy(
                It.Is<IReadOnlyList<string>>(l => l.Count == 1 && l[0] == "standalone/file.rc"),
                ".rc",
                false))
            .Returns(
                new List<string>
                {
                    "standalone/file.rc"
                });

        // Act
        var result = m_DdefService.ResolveAuthoringFiles(
            inputPaths,
            [
                ".rc"
            ]);

        // Assert
        var allFiles = result.AllFilesByExtension[".rc"].ToList();
        Assert.AreEqual(1, allFiles.Count);

        var file = allFiles.First();
        Assert.IsNull(file.DeploymentDefinition?.Path);
        Assert.IsEmpty(file.VariantTags);
    }

    [Test]
    public void ResolveAuthoringFiles_WithMixedFiles_OnlySomeHaveMetadata()
    {
        // Arrange
        var inputPaths = new List<string>
        {
            "configs/file1.rc",
            "standalone/file2.rc"
        };
        var ddefPath = "configs/config.ddef";
        var variantTags = new[]
        {
            "ios"
        };
        var ddef = CreateMockDDefWithVariantTags(ddefPath, variantTags.ToList());
        m_AllDdefs.Add(ddef.Object);

        m_MockFileService
            .Setup(fs => fs.ListFilesToDeploy(
                It.Is<IReadOnlyList<string>>(l => l.Count == 1 && l[0] == "configs/file1.rc"),
                ".rc",
                false))
            .Returns(
                new List<string>
                {
                    "configs/file1.rc"
                });

        m_MockFileService
            .Setup(fs => fs.ListFilesToDeploy(
                It.Is<IReadOnlyList<string>>(l => l.Count == 1 && l[0] == "standalone/file2.rc"),
                ".rc",
                false))
            .Returns(
                new List<string>
                {
                    "standalone/file2.rc"
                });

        // Act
        var result = m_DdefService.ResolveAuthoringFiles(
            inputPaths,
            new[]
            {
                ".rc"
            });

        // Assert
        var allFiles = result.AllFilesByExtension[".rc"].ToList();
        Assert.AreEqual(2, allFiles.Count);

        var file1 = allFiles.First(f => f.Path == "configs/file1.rc");
        Assert.AreEqual(ddefPath, file1.DeploymentDefinition?.Path);
        Assert.IsTrue(file1.VariantTags.SequenceEqual(variantTags));

        var file2 = allFiles.First(f => f.Path == "standalone/file2.rc");
        Assert.IsNull(file2.DeploymentDefinition?.Path);
        Assert.IsEmpty(file2.VariantTags);
    }


    [Test]
    public void ResolveAuthoringFiles_PopulatesVariantTagsByInputPath_WithMultiplePaths()
    {
        // Arrange
        var ddefPath1 = "configs/.ddef";
        var ddefPath2 = "other/.ddef";
        var variantTags1 = new List<string> { "ios", "production" };
        var variantTags2 = new List<string> { "android", "staging" };
        var inputPath1 = "configs/file1.rc";
        var inputPath2 = "other/file2.rc";

        var ddef1 = CreateMockDDefWithVariantTags(ddefPath1, variantTags1);
        var ddef2 = CreateMockDDefWithVariantTags(ddefPath2, variantTags2);
        m_AllDdefs.Add(ddef1.Object);
        m_AllDdefs.Add(ddef2.Object);

        m_MockFileService
            .Setup(fs => fs.ListFilesToDeploy(
                It.Is<IReadOnlyList<string>>(l => l.Count == 1 && l[0] == inputPath1),
                ".rc",
                It.IsAny<bool>()))
            .Returns(new List<string> { inputPath1 });

        m_MockFileService
            .Setup(fs => fs.ListFilesToDeploy(
                It.Is<IReadOnlyList<string>>(l => l.Count == 1 && l[0] == inputPath2),
                ".rc",
                It.IsAny<bool>()))
            .Returns(new List<string> { inputPath2 });

        // Act
        var result = m_DdefService.ResolveAuthoringFiles([inputPath1, inputPath2], [".rc"]);

        // Assert
        Assert.IsNotNull(result.DeploymentDefinitionByInputPath);
        Assert.AreEqual(2, result.DeploymentDefinitionByInputPath.Count);

        Assert.IsTrue(result.DeploymentDefinitionByInputPath.ContainsKey(inputPath1));
        Assert.AreEqual(result.DeploymentDefinitionByInputPath[inputPath1]!.Path, ddefPath1);

        Assert.IsTrue(result.DeploymentDefinitionByInputPath.ContainsKey(inputPath2));
        Assert.AreEqual(result.DeploymentDefinitionByInputPath[inputPath2]!.Path, ddefPath2);
    }

    [Test]
    public void ResolveAuthoringFiles_PopulatesEmptyVariantTags_WhenNoParentDDef()
    {
        // Arrange
        var inputPath = "standalone/file.rc";

        m_MockFileService
            .Setup(fs => fs.ListFilesToDeploy(
                It.Is<IReadOnlyList<string>>(l => l.Count == 1 && l[0] == inputPath),
                ".rc",
                It.IsAny<bool>()))
            .Returns(new List<string> { inputPath });

        // Act
        var result = m_DdefService.ResolveAuthoringFiles([inputPath], [".rc"]);

        // Assert
        Assert.IsNotNull(result.DeploymentDefinitionByInputPath);
        Assert.AreEqual(1, result.DeploymentDefinitionByInputPath.Count);
        Assert.IsTrue(result.DeploymentDefinitionByInputPath.ContainsKey(inputPath));
        Assert.IsNull(result.DeploymentDefinitionByInputPath[inputPath]);
    }

    [Test]
    public void ResolveAuthoringFiles_AttachDDef_WhenDDefIsPresent()
    {
        // Arrange
        var ddefPath = "configs/.ddef";
        var inputPath = "configs/file.rc";

        var ddef = CreateMockDdef(ddefPath);
        m_AllDdefs.Add(ddef.Object);

        m_MockFileService
            .Setup(fs => fs.ListFilesToDeploy(
                It.Is<IReadOnlyList<string>>(l => l.Count == 1 && l[0] == inputPath),
                ".rc",
                It.IsAny<bool>()))
            .Returns(new List<string> { inputPath });

        // Act
        var result = m_DdefService.ResolveAuthoringFiles([inputPath], [".rc"]);

        // Assert
        Assert.IsNotNull(result.DeploymentDefinitionByInputPath);
        Assert.AreEqual(1, result.DeploymentDefinitionByInputPath.Count);
        Assert.IsTrue(result.DeploymentDefinitionByInputPath.ContainsKey(inputPath));
        Assert.IsNotNull(result.DeploymentDefinitionByInputPath[inputPath]);
    }

    [Test]
    public void ResolveAuthoringFiles_MixedPathsWithAndDDef()
    {
        // Arrange
        var ddefPath = "configs/.ddef";
        var variantTags = new List<string> { "ios", "production" };
        var inputPath1 = "configs/file1.rc";
        var inputPath2 = "standalone/file2.rc";

        var ddef = CreateMockDDefWithVariantTags(ddefPath, variantTags);
        m_AllDdefs.Add(ddef.Object);

        m_MockFileService
            .Setup(fs => fs.ListFilesToDeploy(
                It.Is<IReadOnlyList<string>>(l => l.Count == 1 && l[0] == inputPath1),
                ".rc",
                It.IsAny<bool>()))
            .Returns(new List<string> { inputPath1 });

        m_MockFileService
            .Setup(fs => fs.ListFilesToDeploy(
                It.Is<IReadOnlyList<string>>(l => l.Count == 1 && l[0] == inputPath2),
                ".rc",
                It.IsAny<bool>()))
            .Returns(new List<string> { inputPath2 });

        // Act
        var result = m_DdefService.ResolveAuthoringFiles([inputPath1, inputPath2], [".rc"]);

        // Assert
        Assert.IsNotNull(result.DeploymentDefinitionByInputPath);
        Assert.AreEqual(2, result.DeploymentDefinitionByInputPath.Count);

        Assert.IsTrue(result.DeploymentDefinitionByInputPath.ContainsKey(inputPath1));
        Assert.AreEqual(result.DeploymentDefinitionByInputPath[inputPath1]!.Path, ddefPath);

        Assert.IsTrue(result.DeploymentDefinitionByInputPath.ContainsKey(inputPath2));
        Assert.IsNull(result.DeploymentDefinitionByInputPath[inputPath2]);
    }

    [Test]
    public void ResolveAuthoringFiles_HandlesEmptyInputPaths()
    {
        // Arrange
        var emptyInputPaths = Array.Empty<string>();

        // Act
        var result = m_DdefService.ResolveAuthoringFiles(emptyInputPaths, [".rc"]);

        // Assert
        Assert.IsNotNull(result.DeploymentDefinitionByInputPath);
        Assert.IsEmpty(result.DeploymentDefinitionByInputPath);
    }

    static Mock<IDeploymentDefinition> CreateMockDDefWithVariantTags(string path, List<string> variantTags)
    {
        var mockDdef = CreateMockDdef(path);
        var additionalProperties = new Dictionary<string, object>
        {
            ["variantTags"] = variantTags
        };
        mockDdef
            .SetupGet(d => d.AdditionalProperties)
            .Returns(new ReadOnlyDictionary<string, object>(additionalProperties));
        return mockDdef;
    }

}
