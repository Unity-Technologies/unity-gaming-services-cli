using System.CommandLine;

namespace Unity.Services.Cli.Common;

/// <summary>
/// Helpers to simplify command handler declaration.
/// </summary>
public static class HandlerHelper
{
    public static void SetHandler<T1>(this Command self, Action<T1> handler)
    {
        self.SetHandler(handler, ServiceBinder<T1>.Instance);
    }

    public static void SetHandler<T1, T2>(this Command self, Action<T1, T2> handler)
    {
        self.SetHandler(handler, ServiceBinder<T1>.Instance, ServiceBinder<T2>.Instance);
    }

    public static void SetHandler<T1, T2, T3>(this Command self, Action<T1, T2, T3> handler)
    {
        self.SetHandler(handler, ServiceBinder<T1>.Instance, ServiceBinder<T2>.Instance, ServiceBinder<T3>.Instance);
    }

    public static void SetHandler<T1, T2, T3, T4>(this Command self, Action<T1, T2, T3, T4> handler)
    {
        self.SetHandler(
            handler,
            ServiceBinder<T1>.Instance,
            ServiceBinder<T2>.Instance,
            ServiceBinder<T3>.Instance,
            ServiceBinder<T4>.Instance);
    }

    public static void SetHandler<T1, T2, T3, T4, T5>(
        this Command self, Action<T1, T2, T3, T4, T5> handler)
    {
        self.SetHandler(
            handler,
            ServiceBinder<T1>.Instance,
            ServiceBinder<T2>.Instance,
            ServiceBinder<T3>.Instance,
            ServiceBinder<T4>.Instance,
            ServiceBinder<T5>.Instance);
    }

    public static void SetHandler<T1, T2, T3, T4, T5, T6>(
        this Command self, Action<T1, T2, T3, T4, T5, T6> handler)
    {
        self.SetHandler(
            handler,
            ServiceBinder<T1>.Instance,
            ServiceBinder<T2>.Instance,
            ServiceBinder<T3>.Instance,
            ServiceBinder<T4>.Instance,
            ServiceBinder<T5>.Instance,
            ServiceBinder<T6>.Instance);
    }

    public static void SetHandler<T1, T2, T3, T4, T5, T6, T7>(
        this Command self, Action<T1, T2, T3, T4, T5, T6, T7> handler)
    {
        self.SetHandler(
            handler,
            ServiceBinder<T1>.Instance,
            ServiceBinder<T2>.Instance,
            ServiceBinder<T3>.Instance,
            ServiceBinder<T4>.Instance,
            ServiceBinder<T5>.Instance,
            ServiceBinder<T6>.Instance,
            ServiceBinder<T7>.Instance);
    }

    public static void SetHandler<T1, T2, T3, T4, T5, T6, T7, T8>(
        this Command self, Action<T1, T2, T3, T4, T5, T6, T7, T8> handler)
    {
        self.SetHandler(
            handler,
            ServiceBinder<T1>.Instance,
            ServiceBinder<T2>.Instance,
            ServiceBinder<T3>.Instance,
            ServiceBinder<T4>.Instance,
            ServiceBinder<T5>.Instance,
            ServiceBinder<T6>.Instance,
            ServiceBinder<T7>.Instance,
            ServiceBinder<T8>.Instance);
    }

    public static void SetHandler<T1>(this Command self, Func<T1, Task> handler)
    {
        self.SetHandler(handler, ServiceBinder<T1>.Instance);
    }

    public static void SetHandler<T1, T2>(this Command self, Func<T1, T2, Task> handler)
    {
        self.SetHandler(handler, ServiceBinder<T1>.Instance, ServiceBinder<T2>.Instance);
    }

    public static void SetHandler<T1, T2, T3>(this Command self, Func<T1, T2, T3, Task> handler)
    {
        self.SetHandler(handler, ServiceBinder<T1>.Instance, ServiceBinder<T2>.Instance, ServiceBinder<T3>.Instance);
    }

    public static void SetHandler<T1, T2, T3, T4>(
        this Command self, Func<T1, T2, T3, T4, Task> handler)
    {
        self.SetHandler(
            handler,
            ServiceBinder<T1>.Instance,
            ServiceBinder<T2>.Instance,
            ServiceBinder<T3>.Instance,
            ServiceBinder<T4>.Instance);
    }

    public static void SetHandler<T1, T2, T3, T4, T5>(
        this Command self, Func<T1, T2, T3, T4, T5, Task> handler)
    {
        self.SetHandler(
            handler,
            ServiceBinder<T1>.Instance,
            ServiceBinder<T2>.Instance,
            ServiceBinder<T3>.Instance,
            ServiceBinder<T4>.Instance,
            ServiceBinder<T5>.Instance);
    }

    public static void SetHandler<T1, T2, T3, T4, T5, T6>(
        this Command self, Func<T1, T2, T3, T4, T5, T6, Task> handler)
    {
        self.SetHandler(
            handler,
            ServiceBinder<T1>.Instance,
            ServiceBinder<T2>.Instance,
            ServiceBinder<T3>.Instance,
            ServiceBinder<T4>.Instance,
            ServiceBinder<T5>.Instance,
            ServiceBinder<T6>.Instance);
    }

    public static void SetHandler<T1, T2, T3, T4, T5, T6, T7>(
        this Command self, Func<T1, T2, T3, T4, T5, T6, T7, Task> handler)
    {
        self.SetHandler(
            handler,
            ServiceBinder<T1>.Instance,
            ServiceBinder<T2>.Instance,
            ServiceBinder<T3>.Instance,
            ServiceBinder<T4>.Instance,
            ServiceBinder<T5>.Instance,
            ServiceBinder<T6>.Instance,
            ServiceBinder<T7>.Instance);
    }

    public static void SetHandler<T1, T2, T3, T4, T5, T6, T7, T8>(
        this Command self, Func<T1, T2, T3, T4, T5, T6, T7, T8, Task> handler)
    {
        self.SetHandler(
            handler,
            ServiceBinder<T1>.Instance,
            ServiceBinder<T2>.Instance,
            ServiceBinder<T3>.Instance,
            ServiceBinder<T4>.Instance,
            ServiceBinder<T5>.Instance,
            ServiceBinder<T6>.Instance,
            ServiceBinder<T7>.Instance,
            ServiceBinder<T8>.Instance);
    }
}
