using System.CommandLine.Binding;
using Microsoft.Extensions.DependencyInjection;

namespace Unity.Services.Cli.Common;

/// <summary>
/// Helper to retrieve a service from a binding context when declaring a command handler.
/// </summary>
/// <typeparam name="T">
/// Any type that has been provided to the binding context.
/// </typeparam>
public sealed class ServiceBinder<T> : BinderBase<T>
{
    public static ServiceBinder<T> Instance { get; } = new();

    ServiceBinder() { }

    protected override T GetBoundValue(BindingContext bindingContext)
        => (T)bindingContext.GetRequiredService(typeof(T));
}
