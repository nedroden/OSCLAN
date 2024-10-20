using System;
using CommandLine;
using Osclan.Compiler.Exceptions;

namespace Osclan.Compiler
{
    static class Program
    {
        static void Main(string[] args) =>
            Parser.Default.ParseArguments<CompilerOptions>(args).WithParsed(o =>
            {
                try
                {
                    new Compiler(o).Run();
                }
                catch (SourceException e)
                {
                    Console.Error.WriteLine($"Compilation error: {e.Message}");
                }
            });
    }
}