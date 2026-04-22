using System.CommandLine;

namespace Unity.Services.Cli.Common.Input;

public class ObfuscatedInputs
{
    public List<Argument> NonObfuscatedArgs { get; } = new List<Argument>();
    public List<Option> NonObfuscatedOptions { get; } = new List<Option>();
    ObfuscatedInputs() { }

    static ObfuscatedInputs? s_Instance;
    public static ObfuscatedInputs Instance
    {
        get { return s_Instance ??= new(); }
    }
}
