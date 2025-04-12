using Osclan.Compiler.Exceptions;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Symbols;

namespace Osclan.Compiler.Generation.Architecture.AArch64.Generators;

public class ProcedureCallGenerator(
    AstNode node,
    Emitter emitter) : INodeGenerator
{
    public void Generate()
    {
        var mangled = Mangler.Mangle(node.Value ?? throw new CompilerException("Unable to generate procedure call."));

        emitter.EmitComment("Procedure call");
        emitter.EmitOpcode("bl", mangled);
    }
}