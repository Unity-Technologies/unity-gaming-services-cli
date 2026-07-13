using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Unity.Services.Cli.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RegistryTypeAccessibilityAnalyzer : DiagnosticAnalyzer
{
    const string k_CommonInputTypeName = "Unity.Services.Cli.Common.Input.CommonInput";
    const string k_EndpointsTypeName = "Unity.Services.Cli.Common.Networking.NetworkTargetEndpoints";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticRules.CommonInputMustBePublic, DiagnosticRules.EndpointsMustBePublic);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var commonInputType = compilationContext.Compilation.GetTypeByMetadataName(k_CommonInputTypeName);
            var endpointsType = compilationContext.Compilation.GetTypeByMetadataName(k_EndpointsTypeName);

            if (commonInputType == null && endpointsType == null)
                return;

            compilationContext.RegisterSymbolAction(symbolContext =>
            {
                var type = (INamedTypeSymbol)symbolContext.Symbol;

                if (type.IsAbstract || type.TypeKind != TypeKind.Class)
                    return;

                if (IsExternallyAccessible(type))
                    return;

                if (commonInputType != null && InheritsFrom(type, commonInputType))
                {
                    symbolContext.ReportDiagnostic(
                        Diagnostic.Create(DiagnosticRules.CommonInputMustBePublic, type.Locations[0], type.Name));
                    return;
                }

                if (endpointsType != null && InheritsFrom(type, endpointsType))
                {
                    symbolContext.ReportDiagnostic(
                        Diagnostic.Create(DiagnosticRules.EndpointsMustBePublic, type.Locations[0], type.Name));
                }
            }, SymbolKind.NamedType);
        });
    }

    static bool InheritsFrom(INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        var current = type.BaseType;
        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
                return true;
            current = current.BaseType;
        }

        return false;
    }

    static bool IsExternallyAccessible(INamedTypeSymbol type)
    {
        var current = type;
        while (current != null)
        {
            if (current.DeclaredAccessibility != Accessibility.Public)
                return false;
            current = current.ContainingType;
        }

        return true;
    }
}
