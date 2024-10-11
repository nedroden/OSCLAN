namespace Osclan.Compiler.Assembler.Abstractions;

public interface IAssembler
{
    void Assemble(string inputObjectFile, string outputPath);
}