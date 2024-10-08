using System.IO;
using CommandLine;

namespace Osclan.Compiler;

public class CompilerOptions
{
    [Option('v', "verbose", Required = false, HelpText = "Sets the verbosity level")]
    public bool Verbose { get; set; }

    [Option('i', "intermediate", Required = false, HelpText = "Generates intermediate files")]
    public bool GenerateIntermediateFiles { get; set; }

    [Option('f', "file", Required = true, HelpText = "The path to the file to compile")]
    public string InputFile
    {
        get => _inputFile; set
        {
            InputFileName = value;
            _inputFile = Path.GetFullPath(value);
        }
    }
    private string _inputFile = string.Empty;

    [Option('t', "temp", Required = false, Default = "output", HelpText = "The path to to save temp files to")]
    public string TempFilePath { get => _tempFilePath; set => _tempFilePath = Path.GetFullPath(value); }
    private string _tempFilePath = string.Empty;

    public string InputFileName { get; private set; } = string.Empty;

    [Option('o', "output", Required = false, Default = "a.out", HelpText = "The name of the output file")]
    public string OutputPath { get => _outputPath; set => _outputPath = Path.GetFullPath(value); }
    private string _outputPath { get; set; } = string.Empty;
}