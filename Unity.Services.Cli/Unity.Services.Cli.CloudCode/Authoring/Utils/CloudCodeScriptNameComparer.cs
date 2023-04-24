using Unity.Services.CloudCode.Authoring.Editor.Core.Model;

namespace Unity.Services.Cli.CloudCode.Authoring;

class CloudCodeScriptNameComparer : IEqualityComparer<IScript>
{
    public bool Equals(IScript? x, IScript? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        if (ReferenceEquals(x, null)
            || ReferenceEquals(y, null))
        {
            return false;
        }

        return x.Name.Equals(y.Name);
    }

    public int GetHashCode(IScript obj) => obj.Name.GetHashCode();
}
