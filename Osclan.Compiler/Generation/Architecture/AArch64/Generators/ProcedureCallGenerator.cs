using System;
using System.Collections.Generic;
using System.Linq;
using Osclan.Analytics;
using Osclan.Compiler.Exceptions;
using Osclan.Compiler.Generation.Assembly;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Symbols;

namespace Osclan.Compiler.Generation.Architecture.AArch64.Generators;

public class ProcedureCallGenerator(
    AstNode node,
    Emitter emitter,
    AnalyticsClient<ProcedureCallGenerator> analyticsClient,
    RegisterTable registerTable) 
    : MemoryManagingGenerator<ProcedureCallGenerator>(registerTable, emitter, analyticsClient)
{
    private readonly Emitter _emitter = emitter;

    public override void Generate()
    {
        var mangled = Mangler.Mangle(node.Value ?? throw new CompilerException("Unable to generate procedure call."));
        var arguments = node.Children.Where(c => c.Type == AstNodeType.Argument).ToList();

        _emitter.EmitComment("Procedure call");
        _emitter.EmitOpcode("bl", mangled);

        if (arguments.Count != 0)
        {
            // TODO: zorgen dat parametervariabelen bij het starten van de procedure
            // een register krijgen toegekend (x0-x7)
            PassArguments(arguments);
        }
    }

    private void PassArguments(List<AstNode> arguments)
    {
        if (arguments.Count > 7)
        {
            throw new NotImplementedException("Argument allocation using the stack is not yet implemented. Hence, only 7 arguments are allowed.");
        }
        
        var paramValues = arguments.Select((a, i) => (i, a.Children.Single()));

        foreach (var (i, argument) in paramValues)
        {
            switch (argument.Type)
            {
                case AstNodeType.String:
                    _ = StoreString(argument.Value ?? string.Empty);
                    // TODO: Store in register xi, i in 0..7
                    break;
                default: throw new NotImplementedException();
            }
        }
    }
}