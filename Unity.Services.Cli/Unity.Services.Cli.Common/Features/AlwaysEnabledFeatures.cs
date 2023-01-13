namespace Unity.Services.Cli.Common.Features;

class AlwaysEnabledFeatures : IFeatures
{
    public bool IsEnabled(string flag) => true;
}
