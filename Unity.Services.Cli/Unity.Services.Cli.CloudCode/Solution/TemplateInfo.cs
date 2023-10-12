using Unity.Services.CloudCode.Authoring.Editor.Core.Solution;

namespace Unity.Services.Cli.CloudCode.Solution;

class TemplateInfo : ITemplateInfo
{
    public string PathSolution => @"Unity.Services.CloudCode.Authoring.Editor.Core.Solution.sln";
    public string PathProject => @"Unity.Services.CloudCode.Authoring.Editor.Core.Project.csproj";
    public string PathExampleClass => @"Unity.Services.CloudCode.Authoring.Editor.Core.Example.cs";
    public string PathConfig => @"Unity.Services.CloudCode.Authoring.Editor.Core.FolderProfile.pubxml";
    public string PathConfigUser => @"Unity.Services.CloudCode.Authoring.Editor.Core.FolderProfile.pubxml.user";
}
