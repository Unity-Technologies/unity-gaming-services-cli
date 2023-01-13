namespace Unity.Services.Cli.Common.Features;

class StaticFeatures: IFeatures
{
    readonly IDictionary<string, bool> m_Features;

    public StaticFeatures(): this(new Dictionary<string, bool>()) {
    }

    public StaticFeatures(IDictionary<string, bool> features) {
        m_Features = features;
    }

    public bool IsEnabled(string flag)
    {
        return m_Features.ContainsKey(flag) && m_Features[flag];
    }
}
