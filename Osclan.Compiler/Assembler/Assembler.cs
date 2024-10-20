using Osclan.Compiler.Assembler.Abstractions;
using Osclan.Compiler.Exceptions;
using Osclan.Compiler.Native;

namespace Osclan.Compiler.Assembler;

public class Assembler : IAssembler
{
    public void Assemble(string inputObjectFile, string outputPath)
    {
        var assemblerStep = new ShellCommand("as", $"-o {inputObjectFile}.o {inputObjectFile}.s -arch arm64");
        var result = assemblerStep.Start();

        if (result.ExitCode != 0)
        {
            throw new SourceException($"Assembler failed with exit code {result.ExitCode}. Stderr: {result.Stderr}");
        }

        var linkerStep = new ShellCommand("ld", $" -arch arm64 {inputObjectFile}.o -o {outputPath}");
        result = linkerStep.Start();

        if (result.ExitCode != 0)
        {
            throw new SourceException($"Linker failed with exit code {result.ExitCode}. Stderr: {result.Stderr}");
        }
    }
}