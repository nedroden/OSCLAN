using System;
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
    SymbolTable currentScope,
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

        var operandValue = $"{operand.Value}\n\0";
        var operandLength = operandValue.Length;
        
        var operandRegister = SetParameters(operand, operandValue, operandLength);

        _emitter.EmitOpcode("mov", $"x0, #{(int)FileDescriptor.Stdout}");
        _emitter.EmitSyscall(Syscall.Write);
        _emitter.EmitOpcode("svc", KernelImmediate);
        
        // Free registers and deallocate memory
        if (operandRegister is not null)
        {
            FreeMemory((uint)operandLength, operandRegister);

            return;
        }
        
        // The address of the string is in x1, and the length is in x2. FreeMemoryUnsafe() expects these to be in x0 and x1
        _emitter.EmitOpcode("mov", "x0, x1");
        _emitter.EmitOpcode("mov", "x1, x2");
        FreeMemoryUnsafe();
    }

    private Register? SetParameters(AstNode operand, string operandValue, int operandLength)
    {
        switch (operand.Type)
        {
            case AstNodeType.Variable: HandleVariableOperand(operand); break;
            case AstNodeType.ProcedureCall: HandleProcedureCallOperand(); break;
            default: return HandleStringOperand(operandValue, operandLength);
        }

        return null;
    }

    private void HandleProcedureCallOperand()
    {
        // Preserve register and string length, set by the return statement
        _emitter.EmitOpcode("mov", "x2, x1");
        _emitter.EmitOpcode("mov", "x1, x0");
    }

    private Register HandleStringOperand(string operandValue, int operandLength)
    {   
        var stringRegister = StoreString(operandValue);

        _emitter.EmitOpcode("mov", $"x1, {stringRegister.Name}");
        _emitter.EmitOpcode("mov", $"x2, #{operandLength}");

        return stringRegister;
    }

    private void HandleVariableOperand(AstNode operand)
    {
        var variable = currentScope.ResolveVariable(operand.Value ?? throw new CompilerException("Variable has no name."));

        _emitter.EmitOpcode("mov", $"x2, #{variable.SizeInBytes}");
        _emitter.EmitOpcode("mov", $"x1, {variable.Register?.Name}");
    }
}