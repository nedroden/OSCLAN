using System;
using CommandLine;

namespace Neoc.Analyzer
{
    class Program
    {
        static void Main(string[] args) =>
            Parser.Default.ParseArguments<AnalyzerOptions>(args).WithParsed(o => new Analyzer(o).Run());
    }
}