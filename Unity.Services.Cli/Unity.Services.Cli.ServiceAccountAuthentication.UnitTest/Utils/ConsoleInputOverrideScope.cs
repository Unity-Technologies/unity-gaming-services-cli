namespace Unity.Services.Cli.Authentication.UnitTest;

struct ConsoleInputOverrideScope : IDisposable
{
    public TextReader PreviousInput { get; } = Console.In;

    public TextReader OverrideInput { get; }

    bool m_IsDisposed;

    public ConsoleInputOverrideScope(TextReader input)
    {
        PreviousInput = Console.In;
        OverrideInput = input;
        m_IsDisposed = false;

        Console.SetIn(OverrideInput);
    }

    public void Dispose()
    {
        if (m_IsDisposed)
            return;

        Console.SetIn(PreviousInput);
        m_IsDisposed = true;
    }
}
