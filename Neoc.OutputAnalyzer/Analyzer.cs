using System;
using System.IO;

namespace Neoc.OutputAnalyzer
{
    public class Analyzer
    {
        private readonly AnalyzerOptions _options;

        public Analyzer(AnalyzerOptions options) => _options = options;

        public void Run() => Console.WriteLine($"Running analyzer on path {_options.OutputPath}...");
    }
}