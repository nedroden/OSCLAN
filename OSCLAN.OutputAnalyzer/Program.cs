using System;
using CommandLine;

namespace OSCLAN.OutputAnalyzer
{
    class Program
    {
        static void Main(string[] args) =>
            Parser.Default.ParseArguments<AnalyzerOptions>(args).WithParsed(o => new Analyzer(o).Run());
    }
}