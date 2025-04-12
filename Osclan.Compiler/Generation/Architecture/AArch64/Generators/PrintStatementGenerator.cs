using System;
using System.Collections.Generic;
using System.Linq;
using Osclan.Analytics;
using Osclan.Compiler.Exceptions;
using Osclan.Compiler.Generation.Architecture.AArch64.Resources;
using Osclan.Compiler.Generation.Assembly;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Symbols;

namespace Osclan.Compiler.Generation.Architecture.AArch64.Generators;

public class PrintStatementGenerator(
    AstNode node,
    Emitter emitter,
    AnalyticsClient<PrintStatementGenerator> analyticsClient,
    Dictionary<Guid, SymbolTable> symbolTables,
    RegisterTable registerTable)
    : MemoryManagingGenerator<PrintStatementGenerator>(registerTable, emitter, analyticsClient)
{
    private readonly Emitter _emitter = emitter;

    public override void Generate()
    {
        var childNode = node.Children.Single();
        if (childNode is not { Type: AstNodeType.Assignment })
        {
            throw new CompilerException("Unable to interpret print statement.");
        }

        var operand = childNode.Children.Single();
        if (operand is not { Type: AstNodeType.String or AstNodeType.Variable or AstNodeType.ProcedureCall, Value: not null })
        {
            throw new NotImplementedException();
        }

        _emitter.EmitComment("Perform system call 4 (write)");
        Register? operandRegister = null;

        var operandValue = $"{operand.Value}\n\0";
        var operandLength = operandValue.Length;

        
        if (operand.Type == AstNodeType.Variable)
        {
            // TODO: extract method
            var symbolTableGuid = operand.Scope ?? throw new CompilerException("Variable missing symbol table reference.");
            var currentScope = symbolTables.Single(s => s.Key == symbolTableGuid).Value;
            var variable = currentScope.ResolveVariable(operand.Value ?? throw new CompilerException("Variable has no name."));

            _emitter.EmitOpcode("mov", $"x2, #{variable.SizeInBytes}");
            _emitter.EmitOpcode("mov", $"x1, {variable.Register?.Name}");
        }
        else if (operand.Type != AstNodeType.ProcedureCall)
        {
            // Store string in memory
            operandRegister = StoreString(operandValue);
            _emitter.EmitOpcode("mov", $"x1, {operandRegister.Name}");
            _emitter.EmitOpcode("mov", $"x2, #{operandLength}");
        }
        else
        {
            // Preserve register and string length, set by the return statement
            _emitter.EmitOpcode("mov", "x2, x1");
            _emitter.EmitOpcode("mov", "x1, x0");
        }

        _emitter.EmitOpcode("mov", $"x0, #{(int)FileDescriptor.Stdout}");
        _emitter.EmitSyscall(Syscall.Write);
        _emitter.EmitOpcode("svc", KernelImmediate);
        
        // Free registers and deallocate memory
        if (operandRegister is not null)
        {
            FreeMemory((uint)operandLength, operandRegister);

            return;
        }

        FreeMemoryUnsafe();
    }
}