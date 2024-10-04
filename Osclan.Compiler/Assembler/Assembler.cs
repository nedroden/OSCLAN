using System;
using Osclan.Compiler.Native;

namespace Osclan.Compiler.Assembler;

public class Assembler : IAssembler
{
    private readonly string _inputObjectFile;
    private readonly string _outputPath;

    public Assembler(string inputObjectFile, string outputPath)
    {
        _inputObjectFile = inputObjectFile;
        _outputPath = outputPath;
    }

    public void Assemble()
    {
        var assemblerStep = new ShellCommand("as", $"-o {_inputObjectFile}.o {_inputObjectFile}.s -arch arm64");
        var result = assemblerStep.Start();

        if (result.ExitCode != 0)
        {
            throw new Exception($"Assembler failed with exit code {result.ExitCode}. Stderr: {result.Stderr}");
        }

        var linkerStep = new ShellCommand("ld", $" -arch arm64 {_inputObjectFile}.o -o {_outputPath}");
        result = linkerStep.Start();

        if (result.ExitCode != 0)
        {
            throw new Exception($"Linker failed with exit code {result.ExitCode}. Stderr: {result.Stderr}");
        }
    }
}