using Unity.Services.CloudSave.Authoring.Core.Model;

namespace Unity.Services.Cli.CloudSave.UnitTest.Core;

public class CloudSaveDeployFetchTestBase
{
    protected static List<SimpleResourceDeploymentItem> GetLocalResources()
    {
        return new List<SimpleResourceDeploymentItem>()
        {
            new("one" + Constants.SimpleFileExtension)
            {
                Resource = new SimpleResource()
                {
                    Id = "ID1"
                }
            },
            new("sub2/two" + Constants.SimpleFileExtension)
            {
                Resource = new SimpleResource()
                {
                    Id = "ID2"
                }
            },
            new("sub2/three" + Constants.SimpleFileExtension)
            {
                Resource = new SimpleResource()
                {
                    Id = "ID3"
                }
            }
        };
    }

    protected static List<SimpleResource> GetRemoteResources()
    {
        return new List<SimpleResource>()
        {
            new()
            {
                Id = "ID3"
            },
            new()
            {
                Id = "ID4"
            },
            new()
            {
                Id = "ID5"
            }
        };
    }
}
