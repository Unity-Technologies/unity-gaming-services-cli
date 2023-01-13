namespace Unity.Services.Cli.Common.Features;

public interface IFeatures
{
    bool IsEnabled(string flag);
}
