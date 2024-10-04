using Osclan.Compiler.Parsing;

namespace Osclan.Compiler.Generation.Architecture;

public interface IGenerationStrategy
{
    string GenerateIl(AstNode tree);
}