using System;
using System.Collections.Generic;
using System.Linq;
using Osclan.Analytics;
using Osclan.Compiler.Exceptions;
using Osclan.Compiler.Generation.Assembly;
using Osclan.Compiler.Meta;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Symbols;

namespace Osclan.Compiler.Generation.Architecture.AArch64.Generators;

public class ReturnStatementGenerator(
    AstNode node,
    Emitter emitter,
    AnalyticsClient<ReturnStatementGenerator> analyticsClient,
    Dictionary<Guid, SymbolTable> symbolTables,
    RegisterTable registerTable)
    : MemoryManagingGenerator<ReturnStatementGenerator>(registerTable, emitter, analyticsClient)
{
    private readonly Emitter _emitter = emitter;
    private readonly RegisterTable _registerTable = registerTable;

    public override void Generate()
    {
        _emitter.EmitComment("Return statement");
        
        if (node.Children.Count != 0)
        {
            var childNode = node.Children.Single();
            if (childNode is not { Type: AstNodeType.Assignment })
            {
                throw new CompilerException("Unable to interpret print statement.");
            }

            var operand = childNode.Children.Single();

            switch (operand.Type)
            {
                case AstNodeType.Variable:
                    HandleVariableOperand(operand);
                    break;
                case AstNodeType.String:
                    HandleStringOperand(operand);
                    break;
                case AstNodeType.Scalar:
                    HandleScalarOperand(operand);
                    break;
                default: throw new NotImplementedException(operand.Type.ToString());
            }
        }
        
        // Procedure epilog
        _emitter.EmitComment("Procedure epilog");
        _emitter.EmitOpcode("mov", "sp, fp");
        _emitter.EmitOpcode("ldp", "lr, fp, [sp], #16");
        _emitter.EmitOpcode("ret");
        _emitter.EmitNewLine();
    }

    private void HandleVariableOperand(AstNode operand)
    {
        ArgumentNullException.ThrowIfNull(operand.Value);

        var scope = operand.Scope ?? throw new CompilerException("No scope defined.");
        var variable = symbolTables[scope].ResolveVariable(operand.Value);

        if (variable.Register is null)
        {
            throw new CompilerException("Variable was not assigned to a register.");
        }
                
        _emitter.EmitOpcode("mov", $"x0, {variable.Register?.Name}");
    }

    private void HandleScalarOperand(AstNode operand)
    {
        ArgumentNullException.ThrowIfNull(operand.Value);

        _emitter.EmitOpcode("mov", $"x0, #{operand.Value}");
    }

    private void HandleStringOperand(AstNode operand)
    {
        ArgumentNullException.ThrowIfNull(operand.Value);

        // TODO: Free memory later on
        var location = StoreString(operand.Value);
        _emitter.EmitOpcode("mov", $"x0, {location.Name}");
        _emitter.EmitOpcode("mov", $"x1, #{operand.Value.Length}");
        _registerTable.Free(location);
    }
}