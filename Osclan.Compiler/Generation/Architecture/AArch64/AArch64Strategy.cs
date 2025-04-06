using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Osclan.Analytics;
using Osclan.Analytics.Abstractions;
using Osclan.Compiler.Analysis;
using Osclan.Compiler.Exceptions;
using Osclan.Compiler.Extensions;
using Osclan.Compiler.Generation.Architecture.AArch64.Resources;
using Osclan.Compiler.Generation.Assembly;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Symbols;

namespace Osclan.Compiler.Generation.Architecture.AArch64;

public class AArch64Strategy(Emitter emitter, IAnalyticsClientFactory analyticsClientFactory) : IGenerationStrategy
{
    private const string KernelImmediate = "#0x80";

    public string GenerateIl(AstNode tree, Dictionary<Guid, SymbolTable> symbolTables)
    {
        var handler = new Handler(emitter, analyticsClientFactory.CreateClient<Handler>(), tree, symbolTables);
        handler.Handle();

        return emitter.GetResult();
    }

    private sealed class Handler(Emitter emitter, AnalyticsClient<Handler> analyticsClient, AstNode root, Dictionary<Guid, SymbolTable> symbolTables)
    {
        private readonly RegisterTable _registerTable = new(31);

        public void Handle()
        {
            _registerTable.ReserveRegisters(0, 1, 2, 3, 4, 5, 6, 7, 16, 29, 30);

            GenerateRoot();

            foreach (var node in root.Children)
            {
                switch (node.Type)
                {
                    case AstNodeType.Procedure:
                        GenerateProcedureIl(node);
                        break;
                    case AstNodeType.Directive: break;
                    case AstNodeType.Structure: break;
                    default:
                        Console.WriteLine($"Generation for {node.TypeString} is not yet implemented.");
                        break;
                }
            }
        }
        
        private void GenerateRoot()
        {
            analyticsClient.LogEvent("Generating code for the program root");

            emitter.EmitDirect($"; AArch64 code generated at {DateTime.UtcNow}");
            emitter.EmitDirect(".global _main");
            emitter.EmitDirect(".align 8");
            emitter.EmitNewLine();

            emitter.EmitDirect(".include \"output/aarch64_native.s\"");

            emitter.EmitNewLine();

            emitter.EmitDirect("; Program entry point");
            emitter.EmitDirect("_main:"); // Entry point
            emitter.EmitOpcode("bl", $"{Mangler.Mangle("main")}"); // Go to main procedure
            emitter.EmitSyscall(Syscall.Exit);
            emitter.EmitOpcode("svc", KernelImmediate); // macOS supervisor call
            emitter.EmitNewLine();
        }

        private void GenerateProcedureIl(AstNode node)
        {
            analyticsClient.LogEvent($"Generating prolog of procedure with name '{node.Value}'");
            node.Value = Mangler.Mangle(node.Value ?? string.Empty);

            // Procedure prolog
            emitter.EmitDirect("; Procedure prolog");
            emitter.EmitDirect($"{node.Value}:");
            emitter.EmitOpcode("stp", "lr, fp, [sp, #-16]!"); // Save LR and FP on the stack
            emitter.EmitOpcode("mov", "fp, sp"); // Set frame pointer
            emitter.EmitComment("Procedure implementation block");

            foreach (var child in node.Children)
            {
                GenerateIlForBlock(child);
            }
        }

        private void GenerateIlForBlock(AstNode child)
        {
            foreach (var node in child.Children)
            {
                GenerateIlForBlock(node);
            }

            switch (child.Type)
            {
                case AstNodeType.ProcedureCall:
                    GenerateProcedureCall(child);
                    break;
                case AstNodeType.Declaration:
                    GenerateDeclaration(child);
                    break;
                case AstNodeType.Allocation:
                    GenerateMemoryAllocation(child);
                    break;
                case AstNodeType.Deallocation:
                    GenerateVariableDeallocation(child);
                    break;
                case AstNodeType.Print:
                    GeneratePrintStatement(child);
                    break;
                case AstNodeType.Scalar:
                    GenerateScalar(child);
                    break;
                case AstNodeType.Ret:
                    GenerateReturnStatement(child);
                    break;
            }
        }

        private void GenerateScalar(AstNode node)
        {
            analyticsClient.LogEvent("Generating allocation of scalar value");
            
            // If this is not a variable, ignore for now.
            if (!node.Meta.TryGetValue(MetaDataKey.VariableName, out _))
            {
                return;
            }
            
            var symbolTableGuid = node.Scope ?? throw new CompilerException("Variable missing symbol table reference.");
            var currentScope = symbolTables.Single(s => s.Key == symbolTableGuid).Value;
            var variable = currentScope.ResolveVariable(node.Meta[MetaDataKey.VariableName]);

            // TODO: ensure that registers are deallocated nicely
            variable.Register ??= _registerTable.Allocate();
            
            emitter.EmitComment("Assigning value to variable");
            emitter.EmitOpcode("mov", $"{variable.Register.Name}, #{node.Value}");
        }

        private void GenerateDeclaration(AstNode child)
        {
            analyticsClient.LogEvent("Generating code for variable declaration");
            
            var variableNode = child.Children.Single(c => c.Type == AstNodeType.Variable);
            var scope = variableNode.Scope ?? throw new CompilerException("No scope defined.");
            var variable = symbolTables[scope].ResolveVariable(variableNode.Value ?? string.Empty);

            // TODO: Check if this entire method has not become redundant
            if (variable.Register is null)
            {
                analyticsClient.LogWarning($"Variable '{variable.UnmangledName}' was not assigned a register");
                variable.Register = _registerTable.Allocate();   
            }
        }

        private void GenerateReturnStatement(AstNode child)
        {
            analyticsClient.LogEvent("Generating return statement");
            emitter.EmitComment("Return statement");
            
            if (child.Children.Count != 0)
            {
                var childNode = child.Children.Single();
                if (childNode is not { Type: AstNodeType.Assignment })
                {
                    throw new CompilerException("Unable to interpret print statement.");
                }

                var operand = childNode.Children.Single();
                if (operand is not { Type: AstNodeType.Scalar or AstNodeType.Variable, Value: not null })
                {
                    throw new NotImplementedException();
                }

                if (operand.Type == AstNodeType.Variable)
                {
                    // TODO: extract method
                    var scope = operand.Scope ?? throw new CompilerException("No scope defined.");
                    var variable = symbolTables[scope].ResolveVariable(operand.Value);

                    if (variable.TypeName != Mangler.Mangle(BuiltInType.Uint))
                    {
                        throw new NotImplementedException();
                    }

                    // TODO: implement variable declarations
                    if (variable.Register is null)
                    {
                        throw new CompilerException("Variable was not assigned to a register.");
                    }
                    
                    emitter.EmitOpcode("mov", $"x0, {variable.Register?.Name}");
                }
                
                // TODO: support non-scalars
                else
                {
                    emitter.EmitOpcode("mov", $"x0, #{operand.Value}");
                }
            }
            
            // Procedure epilog
            emitter.EmitComment("Procedure epilog");
            emitter.EmitOpcode("mov", "sp, fp");
            emitter.EmitOpcode("ldp", "lr, fp, [sp], #16");
            emitter.EmitOpcode("ret");
            emitter.EmitNewLine();
        }

        /// <summary>
        /// Generates code for the print statement. Syscall 4 is used to write the contents to the screen.
        ///
        /// TODO: unicode support.
        /// </summary>
        /// <param name="child"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void GeneratePrintStatement(AstNode child)
        {
            analyticsClient.LogEvent("Generating print statement");

            var childNode = child.Children.Single();
            if (childNode is not { Type: AstNodeType.Assignment })
            {
                throw new CompilerException("Unable to interpret print statement.");
            }

            var operand = childNode.Children.Single();
            if (operand is not { Type: AstNodeType.String, Value: not null })
            {
                throw new NotImplementedException();
            }

            var operandValue = $"{operand.Value}\n\0";
            var operandLength = operandValue.Length;
            
            // Store string in memory
            var operandRegister = AllocateMemory((uint)operandLength);
            emitter.EmitComment("Store print statement operand in memory");

            var scratchRegister = _registerTable.Allocate();
            for (var i = 0; i < operandLength; i += 2)
            {
                var byteRepr = Encoding.ASCII.GetBytes(operandValue.Window(2, i)).ToHex().PadWithZeros(4);

                emitter.EmitOpcode("mov", $"{scratchRegister.Name}, {byteRepr}");
                emitter.EmitOpcode("str", $"{scratchRegister.Name}, [{operandRegister.Name}, #{i}]");
            }

            // Perform syscall
            emitter.EmitComment("Perform system call 4 (write)");
            emitter.EmitOpcode("mov", $"x0, #{(int)FileDescriptor.Stdout}");
            emitter.EmitOpcode("mov", $"x1, {operandRegister.Name}");
            emitter.EmitOpcode("mov", $"x2, #{operandLength}");
            emitter.EmitSyscall(Syscall.Write);
            emitter.EmitOpcode("svc", KernelImmediate);
            
            // Free registers and deallocate memory
            emitter.EmitOpcode("mov", $"{scratchRegister.Name}, xzr");
            _registerTable.Free(scratchRegister);
            FreeMemory((uint)operandLength, operandRegister);
        }

        private void GenerateVariableDeallocation(AstNode node)
        {
            analyticsClient.LogEvent("Generating variable deallocation");
            
            var variableToDeallocate = node.Children.Single();
            
            var symbolTableGuid = variableToDeallocate.Scope ?? throw new CompilerException("Variable missing symbol table reference.");
            var currentScope = symbolTables.Single(s => s.Key == symbolTableGuid).Value;
            var variable = currentScope.ResolveVariable(variableToDeallocate.Value ?? throw new CompilerException("Variable has no name."));

            if (variable.Register is null)
            {
                throw new SourceException($"Variable with identifier '{variable.UnmangledName}' is not currently allocated.");
            }

            if (!variable.IsPointer)
            {
                throw new SourceException($"Invalid free operation: non-pointer '{variable.UnmangledName}' cannot be used as an operand.");
            }
            
            FreeMemory(variable.SizeInBytes, variable.Register);
        }

        private void GenerateProcedureCall(AstNode child)
        {
            var mangled = Mangler.Mangle(child.Value ?? throw new CompilerException("Unable to generate procedure call."));
            analyticsClient.LogEvent($"Generating code for call to procedure '{child.Value}'");

            emitter.EmitComment("Procedure call");
            emitter.EmitOpcode("bl", mangled);
        }

        /// <summary>
        /// Generates code for memory allocation.
        /// </summary>
        /// <param name="node"></param>
        private void GenerateMemoryAllocation(AstNode node)
        {
            analyticsClient.LogEvent("Generating code for variable initialization of fixed size");

            var type = node.TypeInformation ?? throw new CompilerException("Type information not available.");
            var sizeInBytes = type.SizeInBytes;

            var symbolTableGuid = node.Scope ?? throw new CompilerException("Variable missing symbol table reference.");
            var currentScope = symbolTables.Single(s => s.Key == symbolTableGuid).Value;
            var variable = currentScope.ResolveVariable(node.Meta[MetaDataKey.VariableName]);
            
            AllocateMemory(sizeInBytes, variable.Register ?? throw new CompilerException("Variable was not assigned to a register."));
        }

        /// <summary>
        /// At the end of a scope, frees any allocated memory.
        /// </summary>
        /// <param name="node"></param>
        private void FreeMemoryAtEndOfScope(AstNode node) => throw new NotImplementedException();

        /// <summary>§
        /// Allocates memory and saves the address of the allocated memory in x0. The address is then moved
        /// to a register.
        /// </summary>
        /// <param name="size"></param>
        private Register AllocateMemory(uint size) =>
            AllocateMemory(size, _registerTable.Allocate());

        public Register AllocateMemory(uint size, Register register)
        {
            var registerName = _registerTable.GetName(register);
            analyticsClient.LogEvent($"Allocating {size} bytes of memory. Address stored in '{registerName}'");
            
            const MemoryProtocol protocol = MemoryProtocol.Read | MemoryProtocol.Write;
            const MemoryFlag flags = MemoryFlag.MapAnon | MemoryFlag.MapPrivate;

            emitter.EmitComment("Memory allocation");
            emitter.EmitOpcode("mov", "x0, xzr");
            emitter.EmitOpcode("mov", $"x1, #{size}");
            emitter.EmitOpcode("mov", $"x2, #{(int)protocol}");
            emitter.EmitOpcode("mov", $"x3, #{(int)flags}");
            emitter.EmitOpcode("mov", "x4, #-1");
            emitter.EmitOpcode("mov", "x5, xzr");
            emitter.EmitSyscall(Syscall.Mmap);
            emitter.EmitOpcode("svc", KernelImmediate);

            emitter.EmitOpcode("mov", $"{registerName}, x0");

            return register;
        }

        private void FreeMemory(uint size, Register register)
        {
            var registerName = _registerTable.GetName(register);
            analyticsClient.LogEvent($"Freeing {size} bytes at address stored in {registerName}");
            
            emitter.EmitComment("Memory deallocation");
            emitter.EmitOpcode("mov", $"x0, {registerName}");
            emitter.EmitOpcode("mov", $"x1, #{size}");
            emitter.EmitSyscall(Syscall.Munmap);
            emitter.EmitOpcode("svc", KernelImmediate);

            emitter.EmitOpcode("mov", $"{register.Name}, xzr");
            _registerTable.Free(register.Index);
        }
    }
}