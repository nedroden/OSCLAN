using Osclan.Compiler.Parsing;

namespace Osclan.Compiler.Generation.Abstractions;

public interface IGenerator
{
    string GenerateIl(AstNode tree);
}