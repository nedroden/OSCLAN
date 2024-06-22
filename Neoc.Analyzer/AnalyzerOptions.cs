using System.IO;
using CommandLine;

namespace Neoc.Analyzer;

public class AnalyzerOptions
{
    [Option('v', "verbose", Required = false, HelpText = "Sets the verbosity level")]
    public bool Verbose { get; set; }

    [Option('p', "path", Required = true, HelpText = "The path to the directory that contains the output files")]
    public string OutputPath { get => _outputPath; set => _outputPath = Path.GetFullPath(value); }
    private string _outputPath = string.Empty;
}