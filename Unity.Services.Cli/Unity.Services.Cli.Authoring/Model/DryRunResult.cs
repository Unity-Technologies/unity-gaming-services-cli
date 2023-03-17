using System.Text;

namespace Unity.Services.Cli.Authoring.Model;

public class DryRunResult<T>
{
    readonly string m_Legend;
    readonly IEnumerable<T> m_Results;
    readonly Func<T, string> m_ToStringFunc;

    public DryRunResult(string legend, IEnumerable<T> results, Func<T, string> toStringFunc)
    {
        m_Legend = legend;
        m_Results = results;
        m_ToStringFunc = toStringFunc;
    }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.Append($"{m_Legend}{Environment.NewLine}");

        foreach (var result in m_Results)
        {
            stringBuilder.Append($"{m_ToStringFunc(result)}{Environment.NewLine}");
        }

        return stringBuilder.ToString();
    }
}
