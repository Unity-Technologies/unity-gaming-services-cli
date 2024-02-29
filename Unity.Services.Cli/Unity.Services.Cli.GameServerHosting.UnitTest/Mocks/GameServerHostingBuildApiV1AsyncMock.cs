using System.Net;
using Moq;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Api;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Client;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Mocks;

class GameServerHostingBuildsApiV1Mock
{
    readonly List<string> m_BuildIdsThatCanBeUpdatedCreatedOrDeleted = new()
    {
        BuildWithOneFileId.ToString(),
        BuildWithTwoFilesId.ToString()
    };

    readonly List<string> m_DeletableFiles = new()
    {
        BuildWithOneToBeDeletedFileFileName
    };

    readonly Dictionary<string, BuildFilesList> m_FilesByBuildId = new()
    {
        {
            BuildFilesRequestMockKey(BuildWithOneFileId.ToString(), 100, 0), new BuildFilesList(
                100,
                0,
                new List<BuildFilesListResultsInner>
                {
                    new("", BuildWithOneFileFileName)
                }
            )
        },
        {
            BuildFilesRequestMockKey(BuildWithTwoFilesId.ToString(), 100, 0), new BuildFilesList(
                100,
                0,
                new List<BuildFilesListResultsInner>
                {
                    new("", BuildWithOneFileFileName),
                    new("", BuildWithOneToBeDeletedFileFileName)
                }
            )
        },
        {
            BuildFilesRequestMockKey(BuildWithTwoFilesId.ToString(), 1, 0), new BuildFilesList(
                1,
                0,
                new List<BuildFilesListResultsInner>
                {
                    new("", BuildWithOneFileFileName)
                }
            )
        },
        {
            BuildFilesRequestMockKey(BuildWithTwoFilesId.ToString(), 1, 1), new BuildFilesList(
                1,
                1,
                new List<BuildFilesListResultsInner>
                {
                    new("", BuildWithOneToBeDeletedFileFileName)
                }
            )
        },
        {
            BuildFilesRequestMockKey(BuildWithTwoFilesId.ToString(), 1, 2), new BuildFilesList(
                1,
                2,
                new List<BuildFilesListResultsInner>())
        },
        {
            BuildFilesRequestMockKey(BuildWithOneFileId.ToString(), 2, 0), new BuildFilesList(
                2,
                0,
                new List<BuildFilesListResultsInner>
                {
                    new("", BuildWithOneFileFileName)
                }
            )
        }
    };

    readonly List<BuildListInner1> m_TestBuildInstalls = new()
    {
        new BuildListInner1(
            ValidBuildVersionName,
            new CCDDetails(Guid.Parse(ValidBucketId), Guid.Parse(ValidReleaseId)),
            completedMachines: 1,
            container: new ContainerImage("tag"),
            failures: new List<BuildListInner1FailuresInner>
            {
                new(1234, "failure", DateTime.Now)
            },
            fleetName: "fleet name",
            pendingMachines: 1,
            regions: new List<RegionsInner>
            {
                new(
                    1,
                    1,
                    1,
                    "region name")
            }),
        new BuildListInner1(
            ValidBuildVersionName,
            new CCDDetails(Guid.Parse(ValidBucketId), Guid.Parse(ValidReleaseId)),
            completedMachines: 3,
            container: new ContainerImage("tag"),
            failures: new List<BuildListInner1FailuresInner>
            {
                new(3456, "failure", DateTime.Now)
            },
            fleetName: "another fleet name",
            pendingMachines: 2,
            regions: new List<RegionsInner>
            {
                new(
                    3,
                    1,
                    2,
                    "another region name")
            })
    };

    readonly List<CreateBuild200Response> m_TestBuilds = new()
    {
        new CreateBuild200Response(
            ValidBuildIdBucket,
            "build2-bucket-build",
            CreateBuild200Response.BuildTypeEnum.S3,
            buildVersionName: ValidBuildVersionName,
            s3: new AmazonS3Details("s3://bucket-name"),
            osFamily: CreateBuild200Response.OsFamilyEnum.LINUX,
            syncStatus: CreateBuild200Response.SyncStatusEnum.SYNCED,
            updated: new DateTime(2022, 10, 11)),
        new CreateBuild200Response(
            ValidBuildIdContainer,
            "build1-container-build",
            CreateBuild200Response.BuildTypeEnum.CONTAINER,
            buildVersionName: ValidBuildVersionName,
            container: new ContainerImage(ValidContainerTag),
            osFamily: CreateBuild200Response.OsFamilyEnum.LINUX,
            syncStatus: CreateBuild200Response.SyncStatusEnum.SYNCED,
            updated: new DateTime(2022, 10, 11)),
        new CreateBuild200Response(
            ValidBuildIdFileUpload,
            "build3-file-upload-build",
            CreateBuild200Response.BuildTypeEnum.FILEUPLOAD,
            buildVersionName: ValidBuildVersionName,
            ccd: new CCDDetails(Guid.Parse(ValidBucketId), Guid.Parse(ValidReleaseId)),
            osFamily: CreateBuild200Response.OsFamilyEnum.LINUX,
            syncStatus: CreateBuild200Response.SyncStatusEnum.SYNCED,
            updated: new DateTime(2022, 10, 11)),
        new CreateBuild200Response(
            BuildWithOneFileId,
            "Build3 (Build with one file test)",
            CreateBuild200Response.BuildTypeEnum.FILEUPLOAD,
            ValidBuildVersionName,
            new CCDDetails(
                new Guid(ValidBucketId),
                new Guid(ValidReleaseId)),
            osFamily: CreateBuild200Response.OsFamilyEnum.LINUX,
            syncStatus: CreateBuild200Response.SyncStatusEnum.SYNCED,
            updated: new DateTime(2022, 10, 11)),
        new CreateBuild200Response(
            BuildWithTwoFilesId,
            "Build3 (Build with one file test)",
            CreateBuild200Response.BuildTypeEnum.FILEUPLOAD,
            ValidBuildVersionName,
            new CCDDetails(
                new Guid(ValidBucketId),
                new Guid(ValidReleaseId)),
            osFamily: CreateBuild200Response.OsFamilyEnum.LINUX,
            syncStatus: CreateBuild200Response.SyncStatusEnum.SYNCED,
            updated: new DateTime(2022, 10, 11)),
        new CreateBuild200Response(
            SyncingBuildId,
            "Syncing Build",
            CreateBuild200Response.BuildTypeEnum.FILEUPLOAD,
            buildVersionName: ValidBuildVersionName,
            ccd: new CCDDetails(),
            osFamily: CreateBuild200Response.OsFamilyEnum.LINUX,
            syncStatus: CreateBuild200Response.SyncStatusEnum.SYNCING,
            updated: new DateTime(2022, 10, 11)),
    };

    readonly List<BuildListInner> m_TestListBuilds = new()
    {
        new BuildListInner(
            1,
            11,
            "Build1",
            buildVersionName: ValidBuildVersionName,
            ccd: new CCDDetails(
                new Guid(ValidBucketId),
                new Guid(ValidReleaseId)),
            osFamily: BuildListInner.OsFamilyEnum.LINUX,
            syncStatus: BuildListInner.SyncStatusEnum.SYNCED,
            updated: new DateTime(2022, 10, 11)),
        new BuildListInner(
            2,
            22,
            "Build2",
            buildVersionName: ValidBuildVersionName,
            container: new ContainerImage("v1"),
            osFamily: BuildListInner.OsFamilyEnum.LINUX,
            syncStatus: BuildListInner.SyncStatusEnum.SYNCED,
            updated: new DateTime(2022, 10, 11))
    };

    public Mock<IBuildsApi> DefaultBuildsClient = new();

    public List<Guid>? ValidEnvironments;
    public List<Guid>? ValidProjects;


    /// <summary>
    ///     Sets up an extremely lightweight implementation of GameServerHosting Build service logic in order to mock each API
    ///     response.
    /// </summary>
    public void SetUp()
    {
        DefaultBuildsClient = new Mock<IBuildsApi>();

        DefaultBuildsClient.Setup(a => a.Configuration)
            .Returns(new Configuration());

        DefaultBuildsClient.Setup(
                a =>
                    a.CreateBuildAsync(
                        It.IsAny<Guid>(), // projectId
                        It.IsAny<Guid>(), // environmentId
                        It.IsAny<CreateBuildRequest>(), // build
                        0,
                        CancellationToken.None
                    ))
            .Returns(
                (Guid projectId, Guid environmentId, CreateBuildRequest req, int _, CancellationToken _) =>
                {
                    var validated = ValidateProjectEnvironment(projectId, environmentId);
                    if (!validated) throw new HttpRequestException();


                    var buildExists = m_TestListBuilds.Find(b => b.BuildName == req.BuildName) != null;
                    if (buildExists) throw new ApiException();

                    if (req.BuildVersionName is InValidBuildVersionName)
                    {
                        throw new ApiException(
                            (int)HttpStatusCode.BadRequest,
                            "Bad request",
                            "{\"Detail\":\"Invalid build version name\"}"
                        );
                    }

                    var osFamily = req.OsFamily switch
                    {
                        CreateBuildRequest.OsFamilyEnum.LINUX => CreateBuild200Response.OsFamilyEnum.LINUX,
                        _ => throw new ApiException()
                    };

                    var build = req.BuildType switch
                    {
                        CreateBuildRequest.BuildTypeEnum.CONTAINER => new CreateBuild200Response(
                            1,
                            req.BuildName,
                            CreateBuild200Response.BuildTypeEnum.CONTAINER,
                            buildVersionName: ValidBuildVersionName,
                            cfv: 5,
                            osFamily: osFamily,
                            updated: DateTime.Now,
                            container: new ContainerImage("v1")
                        ),
                        CreateBuildRequest.BuildTypeEnum.FILEUPLOAD => new CreateBuild200Response(
                            1,
                            req.BuildName,
                            CreateBuild200Response.BuildTypeEnum.FILEUPLOAD,
                            buildVersionName: ValidBuildVersionName,
                            cfv: 5,
                            osFamily:
                            osFamily,
                            updated: DateTime.Now,
                            ccd: new CCDDetails(new Guid(ValidBucketId), new Guid(ValidReleaseId))
                        ),
                        CreateBuildRequest.BuildTypeEnum.S3 => new CreateBuild200Response(
                            1,
                            req.BuildName,
                            CreateBuild200Response.BuildTypeEnum.FILEUPLOAD,
                            buildVersionName: ValidBuildVersionName,
                            cfv: 5,
                            osFamily: osFamily,
                            updated: DateTime.Now,
                            ccd: new CCDDetails(new Guid(ValidBucketId), new Guid(ValidReleaseId))),
                        _ => throw new ApiException()
                    };

                    return Task.FromResult(build);
                });

        DefaultBuildsClient.Setup(
                a =>
                    a.ListBuildsAsync(
                        It.IsAny<Guid>(), // projectId
                        It.IsAny<Guid>(), // environmentId
                        It.IsAny<string>(), // limit
                        It.IsAny<Guid?>(), // lastVal
                        It.IsAny<Guid?>(), // lastId
                        It.IsAny<string>(), // sortBy
                        It.IsAny<string>(), // sortDir
                        It.IsAny<string>(), // partialFilter
                        0,
                        CancellationToken.None
                    ))
            .Returns(
                (
                    Guid projectId,
                    Guid environmentId,
                    string _,
                    Guid? _,
                    Guid? _,
                    string _,
                    string _,
                    string _,
                    int _,
                    CancellationToken _
                ) =>
                {
                    var validated = ValidateProjectEnvironment(projectId, environmentId);
                    if (!validated) throw new HttpRequestException();
                    return Task.FromResult(m_TestListBuilds);
                });

        DefaultBuildsClient.Setup(
                a =>
                    a.GetBuildAsync(
                        It.IsAny<Guid>(), // projectId
                        It.IsAny<Guid>(), // environmentId
                        It.IsAny<long>(), // buildId
                        0,
                        CancellationToken.None
                    ))
            .Returns(
                (Guid projectId, Guid environmentId, long buildId, int _, CancellationToken _) =>
                {
                    var validated = ValidateProjectEnvironment(projectId, environmentId);
                    if (!validated) throw new HttpRequestException();

                    var build = m_TestBuilds.Find(b => b.BuildID == buildId);
                    if (build == null) throw new ApiException();

                    return Task.FromResult(build);
                });
        DefaultBuildsClient.Setup(
                a =>
                    a.DeleteBuildAsync(
                        It.IsAny<Guid>(), // projectId
                        It.IsAny<Guid>(), // environmentId
                        It.IsAny<long>(), // buildId
                        null, // Dry Run
                        0,
                        CancellationToken.None
                    ))
            .Returns(
                (Guid projectId, Guid environmentId, long buildId, bool _, int _, CancellationToken _) =>
                {
                    var validated = ValidateProjectEnvironment(projectId, environmentId);
                    if (!validated) throw new HttpRequestException();

                    var build = GetBuildById(buildId);
                    if (build is null) throw new HttpRequestException();

                    return Task.CompletedTask;
                });
        DefaultBuildsClient.Setup(
                a =>
                    a.UpdateBuildAsync(
                        It.IsAny<Guid>(), // projectId
                        It.IsAny<Guid>(), // environmentId
                        It.IsAny<long>(), // buildId
                        It.IsAny<UpdateBuildRequest>(), // update build request
                        0,
                        CancellationToken.None
                    ))
            .Returns(
                (
                    Guid projectId,
                    Guid environmentId,
                    long buildId,
                    UpdateBuildRequest _,
                    int _,
                    CancellationToken _
                ) =>
                {
                    var validated = ValidateProjectEnvironment(projectId, environmentId);
                    if (!validated) throw new HttpRequestException();

                    var build = m_TestBuilds.Find(b => b.BuildID == buildId);
                    if (build == null) throw new ApiException();

                    return Task.FromResult(build);
                });

        DefaultBuildsClient.Setup(
                a =>
                    a.CreateNewBuildVersionAsync(
                        It.IsAny<Guid>(), // projectId
                        It.IsAny<Guid>(), // environmentId
                        It.IsAny<long>(), // buildId
                        It.IsAny<CreateNewBuildVersionRequest>(), // buildVersion
                        0,
                        CancellationToken.None
                    ))
            .Returns(
                (
                    Guid projectId,
                    Guid environmentId,
                    long buildId,
                    CreateNewBuildVersionRequest request,
                    int _,
                    CancellationToken _) =>
                {
                    var validated = ValidateProjectEnvironment(projectId, environmentId);
                    if (!validated) throw new HttpRequestException();

                    if (request.BuildVersionName is InValidBuildVersionName)
                    {
                        throw new ApiException(
                            (int)HttpStatusCode.BadRequest,
                            "Bad request",
                            "{\"Detail\":\"Invalid build version name\"}"
                        );
                    }

                    var build = m_TestBuilds.Find(b => b.BuildID == buildId);
                    if (build == null) throw new ApiException();

                    return Task.FromResult(new object());
                });

        DefaultBuildsClient.Setup(
                a => a.GetBuildFilesAsync(
                    It.IsAny<Guid>(), // projectId
                    It.IsAny<Guid>(), // environmentId
                    It.IsAny<long>(), // BuildId
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    0,
                    CancellationToken.None
                ))
            .Returns(
                (Guid projectId, Guid environmentId, long buildId, int limit, int offset, int _, CancellationToken _) =>
                {
                    var validated = ValidateProjectEnvironment(projectId, environmentId);
                    if (!validated) throw new HttpRequestException();

                    var buildFilesRequestKey = BuildFilesRequestMockKey(buildId.ToString(), limit, offset);

                    if (!m_FilesByBuildId.ContainsKey(buildFilesRequestKey)) throw new ApiException();

                    return Task.FromResult(m_FilesByBuildId[buildFilesRequestKey]);
                });


        DefaultBuildsClient.Setup(
                a => a.CreateOrUpdateBuildFileAsync(
                    It.IsAny<Guid>(), // projectId
                    It.IsAny<Guid>(), // environmentId
                    It.IsAny<long>(), // Bui
                    It.IsAny<CreateOrUpdateBuildFileRequest?>(),
                    0,
                    CancellationToken.None
                ))
            .Returns(
                (
                    Guid projectId,
                    Guid environmentId,
                    long buildId,
                    CreateOrUpdateBuildFileRequest request,
                    int _,
                    CancellationToken _) =>
                {
                    ValidateProjectEnvironmentThrowsException(projectId, environmentId);
                    ValidateIsBuildWithFilesThrowsException(buildId);

                    var fileResponse = new BuildFilesListResultsInner(
                        "hashvalue",
                        "path",
                        "http://www.fakesite.io/signedUrl/" + request.Path);

                    if (request.Path.Contains("uploaded"))
                    {
                        fileResponse.Uploaded = true;
                        return Task.FromResult(fileResponse);
                    }

                    if (request.Path.Contains("fails")) throw new ApiException();

                    return Task.FromResult(fileResponse);
                }
            );


        DefaultBuildsClient.Setup(
                a => a.DeleteBuildFileByPathAsync(
                    It.IsAny<Guid>(), // projectId
                    It.IsAny<Guid>(), // environmentId
                    It.IsAny<long>(), // Bui
                    It.IsAny<string>(),
                    0,
                    CancellationToken.None
                ))
            .Returns(
                (Guid projectId, Guid environmentId, long buildId, string filepath, int _, CancellationToken _) =>
                {
                    ValidateProjectEnvironmentThrowsException(projectId, environmentId);
                    ValidateIsBuildWithFilesThrowsException(buildId);

                    if (!m_DeletableFiles.Contains(filepath))
                        throw new ApiException(400, $"filepath: {filepath} is not in the allowed deletable files list");


                    return Task.FromResult(new object());
                }
            );

        DefaultBuildsClient.Setup(
                a =>
                    a.GetBuildInstallsAsync(
                        It.IsAny<Guid>(), // projectId
                        It.IsAny<Guid>(), // environmentId
                        It.IsAny<long>(), // BuildId
                        0,
                        CancellationToken.None
                    ))
            .Returns(
                (Guid projectId, Guid environmentId, long buildId, int _, CancellationToken _) =>
                {
                    var validated = ValidateProjectEnvironment(projectId, environmentId);
                    if (!validated) throw new HttpRequestException();

                    var buildExists = m_TestListBuilds.Find(b => b.BuildID == buildId) != null;
                    if (!buildExists) throw new HttpRequestException();

                    return Task.FromResult(m_TestBuildInstalls);
                });
    }

    bool ValidateProjectEnvironment(Guid projectId, Guid environmentId)
    {
        if (ValidProjects != null && !ValidProjects.Contains(projectId)) return false;
        if (ValidEnvironments != null && !ValidEnvironments.Contains(environmentId)) return false;
        return true;
    }

    void ValidateProjectEnvironmentThrowsException(Guid projectId, Guid environmentId)
    {
        var validated = ValidateProjectEnvironment(projectId, environmentId);
        if (!validated) throw new HttpRequestException();
    }

    void ValidateIsBuildWithFilesThrowsException(long buildId)
    {
        if (!m_BuildIdsThatCanBeUpdatedCreatedOrDeleted.Contains(buildId.ToString()))
            throw new ApiException(404, $"buildId: {buildId} not found in mock data");
    }

    CreateBuild200Response? GetBuildById(long id)
    {
        return m_TestBuilds.FirstOrDefault(b => b.BuildID.Equals(id));
    }

    static string BuildFilesRequestMockKey(string buildId, int limit, int offset)
    {
        return $"{buildId}:{limit}:{offset}";
    }
}
