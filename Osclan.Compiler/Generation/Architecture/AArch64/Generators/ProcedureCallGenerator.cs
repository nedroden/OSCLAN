using System;
using System.Collections.Generic;
using System.Linq;
using Osclan.Analytics;
using Osclan.Compiler.Analysis;
using Osclan.Compiler.Exceptions;
using Osclan.Compiler.Generation.Assembly;
using Osclan.Compiler.Meta;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Symbols;

namespace Osclan.Compiler.Generation.Architecture.AArch64.Generators;

public class ProcedureCallGenerator(
    AstNode node,
    Emitter emitter,
    AnalyticsClient<ProcedureCallGenerator> analyticsClient,
    SymbolTable currentScope,
    RegisterTable registerTable) 
    : MemoryManagingGenerator<ProcedureCallGenerator>(registerTable, emitter, analyticsClient)
{
    private readonly Emitter _emitter = emitter;

    public override void Generate()
    {
        var mangled = Mangler.Mangle(node.Value ?? throw new CompilerException("Unable to generate procedure call."));
        var arguments = node.Children.Where(c => c.Type == AstNodeType.Argument).ToList();

        if (arguments.Count != 0)
        {
            PassArguments(arguments);
        }

        _emitter.EmitComment("Procedure call");
        _emitter.EmitOpcode("bl", mangled);

        // If the return value of this procedure is used in an assignment, move it to the appropriate register
        if (node.Meta.TryGetValue(MetaDataKey.VariableName, out var variableName))
        {
            AssignReturnValueToVariable(variableName);
        }
    }

    private void AssignReturnValueToVariable(string variableName)
    {
        var variable = currentScope.ResolveVariable(variableName);

        variable.Register ??= registerTable.Allocate();
        emitter.EmitOpcode("mov", $"{variable.Register.Name}, x0");
    }

    private void PassArguments(List<AstNode> arguments)
    {
        if (arguments.Count > 8)
        {
            throw new NotImplementedException("Argument allocation using the stack is not yet implemented. Hence, only 8 arguments are allowed.");
        }
        
        var paramValues = arguments.Select((a, i) => (i, a.Children.Single()));

        foreach (var (i, argument) in paramValues)
        {
            switch (argument.Type)
            {
                case AstNodeType.String:
                    _ = StoreString(argument.Value ?? string.Empty, registerTable.GetRegister((short)i));
                    break;
                default: throw new NotImplementedException();
            }
        }
    }
}