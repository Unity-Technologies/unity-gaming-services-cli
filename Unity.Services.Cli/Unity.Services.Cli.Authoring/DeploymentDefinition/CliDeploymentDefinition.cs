using System.Collections.ObjectModel;
using Unity.Services.Deployment.Core.Model;
using IoPath = System.IO.Path;

namespace Unity.Services.Cli.Authoring.Model;

class CliDeploymentDefinition : IDeploymentDefinition
{

    public string Name { get; set; }

    public string Path { get; set; }

    public ObservableCollection<string> ExcludePaths { get; }

    public CliDeploymentDefinition(string path)
    {
        Path = path;
        Name = "";
        ExcludePaths = new ObservableCollection<string>();
    }
}
