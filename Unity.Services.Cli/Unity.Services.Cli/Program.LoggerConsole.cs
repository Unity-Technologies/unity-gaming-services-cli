using System;
using System.CommandLine;
using System.CommandLine.IO;
using System.IO;

namespace Unity.Services.Cli;

public static partial class Program
{
    class LoggerConsole : IConsole
    {
        public LoggerConsole(TextWriter stdout, TextWriter stderr)
        {
            Out = new StandardStreamWriter(stdout ?? Console.Out);
            Error = new StandardStreamWriter(stderr ?? Console.Error);
        }
        public IStandardStreamWriter Out { get; }
        public IStandardStreamWriter Error { get; }
        public bool IsOutputRedirected => Console.IsOutputRedirected;
        public bool IsErrorRedirected => Console.IsOutputRedirected;
        public bool IsInputRedirected => Console.IsInputRedirected;

        class StandardStreamWriter : IStandardStreamWriter
        {
            readonly TextWriter m_Writer;
            public StandardStreamWriter(TextWriter writer) {
                m_Writer = writer;
            }
            public void Write(string value)
            {
                m_Writer.Write(value);
            }
        }
    }
}
