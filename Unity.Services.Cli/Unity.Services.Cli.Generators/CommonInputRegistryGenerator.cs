using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unity.Services.Cli.Generators;

/// <summary>
/// Source generator that replaces runtime reflection-based discovery of
/// <c>CommonInput</c> subclasses with a compile-time registry.
///
/// At build time, this generator scans the current compilation and all referenced
/// assemblies whose name starts with <c>Unity.Services.Cli</c> to find every concrete,
/// non-abstract, non-generic class that inherits from <c>CommonInput</c>.
/// It emits a <c>CommonInputRegistry</c> class with a <c>GetAllInputTypes()</c> method
/// that returns a pre-built array of all discovered input types.
///
/// Adding a new <c>CommonInput</c> subclass anywhere in the dependency graph
/// is all that's needed — the generator picks it up automatically on the next build.
/// </summary>
[Generator]
public class CommonInputRegistryGenerator : IIncrementalGenerator
{
    const string k_BaseTypeName = "Unity.Services.Cli.Common.Input.CommonInput";

    /// <summary>
    /// Registers an incremental pipeline that discovers all <c>CommonInput</c>
    /// subclasses and emits <c>CommonInputRegistry.g.cs</c>.
    ///
    /// Local project types are tracked per-file via <c>CreateSyntaxProvider</c>
    /// so only changed files are re-evaluated. Referenced assembly types are
    /// scanned separately via <c>CompilationProvider</c>; since references are
    /// static during editing, the output is stable and effectively cached.
    /// </summary>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Local project types: per-file incremental, only re-evaluates changed files
        var localCandidates = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) => node is ClassDeclarationSyntax { BaseList: { } },
            transform: static (ctx, ct) =>
            {
                if (ctx.SemanticModel.GetDeclaredSymbol(ctx.Node, ct) is not INamedTypeSymbol symbol)
                    return (string)null;

                var baseType = ctx.SemanticModel.Compilation.GetTypeByMetadataName(k_BaseTypeName);
                if (baseType is null)
                    return null;

                if (!IsConcreteInputType(symbol, baseType, ctx.SemanticModel.Compilation.Assembly))
                    return null;

                return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            })
            .Where(static s => s is not null)
            .Collect();

        // Referenced assembly types: produces stable output since references don't change during editing
        var referencedCandidates = context.CompilationProvider.Select(static (compilation, ct) =>
        {
            var baseType = compilation.GetTypeByMetadataName(k_BaseTypeName);
            if (baseType is null)
                return "";

            var compilingAssembly = compilation.Assembly;
            var results = new HashSet<string>();

            foreach (var reference in compilation.SourceModule.ReferencedAssemblySymbols)
            {
                if (!reference.Name.StartsWith("Unity.Services.Cli", StringComparison.OrdinalIgnoreCase))
                    continue;
                CollectTypeNames(reference.GlobalNamespace, baseType, results, compilingAssembly, ct);
            }

            return string.Join("\n", results.OrderBy(s => s, StringComparer.Ordinal));
        });

        var combined = localCandidates.Combine(referencedCandidates);
        context.RegisterSourceOutput(combined, static (spc, pair) =>
        {
            var (local, referencedJoined) = pair;
            var allTypes = new HashSet<string>(StringComparer.Ordinal);

            if (!string.IsNullOrEmpty(referencedJoined))
            {
                foreach (var name in referencedJoined.Split('\n'))
                    allTypes.Add(name);
            }

            foreach (var name in local)
            {
                if (name != null)
                    allTypes.Add(name);
            }

            if (allTypes.Count == 0)
                return;

            var sorted = allTypes.OrderBy(s => s, StringComparer.Ordinal).ToList();
            spc.AddSource("CommonInputRegistry.g.cs", GenerateSource(sorted));
        });
    }

    /// <summary>
    /// Builds the source text for the <c>CommonInputRegistry</c> class containing
    /// a <c>typeof(T)</c> entry for each discovered input type.
    /// </summary>
    static string GenerateSource(List<string> fqns)
    {
        var typeofEntries = string.Join(
            "\n",
            fqns.Select(fqn => $"typeof({fqn}),"));

        var factoryCases = string.Join(
            "\n",
            fqns.Select(fqn =>
                $"            if (type == typeof({fqn})) return new {fqn}();"));

        var dynamicDeps = string.Join(
            "\n",
            fqns.Select(fqn =>
                $"    [global::System.Diagnostics.CodeAnalysis.DynamicDependency(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof({fqn}))]"));

        return $@"// <auto-generated/>
#nullable disable

namespace Unity.Services.Cli;

internal static class CommonInputRegistry
{{
{dynamicDeps}
    internal static global::System.Type[] GetAllInputTypes()
    {{
        return new global::System.Type[]
        {{
{typeofEntries}
        }};
    }}

    internal static object CreateInstance(global::System.Type type)
    {{
{factoryCases}
        throw new global::System.ArgumentException($""Unknown input type: {{type}}"", nameof(type));
    }}
}}
";
    }

    /// <summary>
    /// Recursively walks a namespace tree and collects the fully qualified names
    /// of all concrete <c>CommonInput</c> subclasses into <paramref name="results"/>.
    /// </summary>
    static void CollectTypeNames(
        INamespaceSymbol ns,
        INamedTypeSymbol baseType,
        HashSet<string> results,
        IAssemblySymbol compilingAssembly,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        foreach (var member in ns.GetMembers())
        {
            if (member is INamespaceSymbol childNs)
            {
                CollectTypeNames(childNs, baseType, results, compilingAssembly, ct);
            }
            else if (member is INamedTypeSymbol type)
            {
                if (IsConcreteInputType(type, baseType, compilingAssembly))
                    results.Add(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

                CollectNestedTypeNames(type, baseType, results, compilingAssembly, ct);
            }
        }
    }

    static void CollectNestedTypeNames(
        INamedTypeSymbol parentType,
        INamedTypeSymbol baseType,
        HashSet<string> results,
        IAssemblySymbol compilingAssembly,
        CancellationToken ct)
    {
        foreach (var nested in parentType.GetTypeMembers())
        {
            ct.ThrowIfCancellationRequested();

            if (IsConcreteInputType(nested, baseType, compilingAssembly))
                results.Add(nested.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

            CollectNestedTypeNames(nested, baseType, results, compilingAssembly, ct);
        }
    }

    /// <summary>
    /// Returns <c>true</c> if <paramref name="type"/> is a non-abstract, non-generic
    /// class that inherits (directly or transitively) from <paramref name="baseType"/>.
    /// </summary>
    static bool IsConcreteInputType(INamedTypeSymbol type, INamedTypeSymbol baseType, IAssemblySymbol compilingAssembly)
    {
        if (type.IsAbstract || type.TypeKind != TypeKind.Class || type.TypeParameters.Length > 0)
            return false;

        if (!IsAccessibleFrom(type, compilingAssembly))
            return false;

        // Allow registering CommonInput
        if (SymbolEqualityComparer.Default.Equals(type, baseType))
        {
            return true;
        }

        var current = type.BaseType;
        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
                return true;
            current = current.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Returns <c>true</c> if <paramref name="type"/> and all of its containing types
    /// are accessible from <paramref name="fromAssembly"/>.
    /// </summary>
    static bool IsAccessibleFrom(INamedTypeSymbol type, IAssemblySymbol fromAssembly)
    {
        var current = type;
        while (current != null)
        {
            if (!IsDirectlyAccessible(current, fromAssembly))
                return false;
            current = current.ContainingType;
        }

        return true;
    }

    static bool IsDirectlyAccessible(INamedTypeSymbol type, IAssemblySymbol fromAssembly)
    {
        if (type.DeclaredAccessibility == Accessibility.Public)
            return true;

        if (type.DeclaredAccessibility == Accessibility.Internal
            || type.DeclaredAccessibility == Accessibility.ProtectedOrInternal)
        {
            return SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, fromAssembly);
        }

        return false;
    }
}
