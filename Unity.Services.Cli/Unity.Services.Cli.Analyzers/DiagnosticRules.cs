using Microsoft.CodeAnalysis;

namespace Unity.Services.Cli.Analyzers;

static class DiagnosticRules
{
    public static readonly DiagnosticDescriptor CommonInputMustBePublic = new(
        id: "UGS0001",
        title: "CommonInput subclass must be public",
        messageFormat: "Type '{0}' inherits from CommonInput but is not publicly accessible — the source generator will not discover it",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor EndpointsMustBePublic = new(
        id: "UGS0002",
        title: "NetworkTargetEndpoints subclass must be public",
        messageFormat: "Type '{0}' inherits from NetworkTargetEndpoints but is not publicly accessible — the source generator will not discover it",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
