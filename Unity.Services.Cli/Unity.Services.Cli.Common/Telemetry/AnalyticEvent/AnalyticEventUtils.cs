using System;
using System.CommandLine.Parsing;

namespace Unity.Services.Cli.Common.Telemetry.AnalyticEvent;

public static class AnalyticEventUtils
{
    public static string ConvertSymbolResultToString(SymbolResult symbol)
    {
        List<string> symbolNames = new();
        while (symbol is not null)
        {
            symbolNames.Add(symbol.Symbol.Name);
            symbol = symbol.Parent!;
        }
        symbolNames.Reverse();
        return string.Join("_", symbolNames);
    }
}
