using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Unity.Services.Cli;

static class AotTypeRegistrations
{
    [ModuleInitializer]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(OpenTelemetry.Context.AsyncLocalRuntimeContextSlot<int>))]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Rooting types for AOT compilation")]
    internal static void EnsureAotTypes()
    {
        _ = new OpenTelemetry.Context.AsyncLocalRuntimeContextSlot<int>("");
    }
}
