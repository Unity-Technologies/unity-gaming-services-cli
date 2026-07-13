using System.Text;

namespace Unity.Services.Cli.Common;

public sealed class CommandDescription
{
    readonly string m_Body;
    string? m_ReturnText;
    string? m_ServiceDocsUrl;
    readonly List<(string Label, string Url)> m_ApiDocs = new();

    public CommandDescription(string body)
    {
        m_Body = body;
    }

    public CommandDescription WithReturn(string returnDescription)
    {
        m_ReturnText = returnDescription;
        return this;
    }

    public CommandDescription WithDocs(string serviceDocsUrl)
    {
        m_ServiceDocsUrl = serviceDocsUrl;
        return this;
    }

    public CommandDescription WithAdminApi(string adminApiDocsUrl)
    {
        m_ApiDocs.Add(("Admin API docs", adminApiDocsUrl));
        return this;
    }

    public CommandDescription WithClientApi(string clientApiDocsUrl)
    {
        m_ApiDocs.Add(("Client API docs", clientApiDocsUrl));
        return this;
    }

    public string Build()
    {
        var sb = new StringBuilder(m_Body);

        if (m_ServiceDocsUrl != null)
        {
            sb.Append($" Service docs: {m_ServiceDocsUrl}.");
        }

        foreach (var (label, url) in m_ApiDocs)
        {
            sb.Append($" {label}: {url}");
        }

        if (m_ReturnText != null)
        {
            sb.Append($" Returns: {m_ReturnText}");
        }

        return sb.ToString();
    }
}
