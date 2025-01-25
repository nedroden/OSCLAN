using System;
using System.Collections.Generic;
using System.Linq;
using Osclan.Compiler.Analysis;
using Osclan.Compiler.Exceptions;
using Osclan.Compiler.Generation.Architecture.Resources.Aarch64;
using Osclan.Compiler.Generation.Assembly;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Symbols;

namespace Osclan.Compiler.Generation.Architecture;

public class AArch64Strategy : IGenerationStrategy
{
    private readonly Emitter _emitter;

    public AArch64Strategy(Emitter emitter) => _emitter = emitter;

    public string GenerateIl(AstNode tree, Dictionary<Guid, SymbolTable> symbolTables)
    {
        var handler = new Handler(_emitter, tree, symbolTables);
        handler.Handle();

        return _emitter.GetResult();
    }

    private class Handler(Emitter emitter, AstNode root, Dictionary<Guid, SymbolTable> symbolTables)
    {
        private readonly Emitter _emitter = emitter;
        private readonly AstNode _root = root;
        private readonly Dictionary<Guid, SymbolTable> _symbolTables = symbolTables;

        private readonly RegisterTable _registerTable = new(31);

        public void Handle()
        {
            _registerTable.ReserveRegisters(0, 1, 2, 3, 4, 5, 6, 7, 16, 29, 30);

            GenerateRoot();

            foreach (var node in _root.Children)
            {
                switch (node.Type)
                {
                    case AstNodeType.Procedure:
                        GenerateProcedureIl(node);
                        break;
                    default:
                        Console.WriteLine($"Generation for {node.TypeString} is not yet implemented.");
                        break;
                }
            }
        }
        
        private void GenerateRoot()
        {
            _emitter.EmitDirect($"; AArch64 code generated at {DateTime.UtcNow}");
            _emitter.EmitDirect(".global _main");
            _emitter.EmitDirect(".align 2");
            _emitter.EmitNewLine();

            _emitter.EmitDirect(".include \"output/aarch64_native.s\"");

            _emitter.EmitNewLine();

            _emitter.EmitDirect("_main:"); // Entry point
            _emitter.EmitOpcode("bl", $"{Mangler.Mangle("main")}"); // Go to main procedure
            _emitter.EmitOpcode("mov", "x0, xzr"); // Exit code 0
            _emitter.EmitSyscall(Syscall.Exit);
            _emitter.EmitOpcode("svc", "#0x80"); // macOS supervisor call
            _emitter.EmitNewLine();
        }

        private void GenerateProcedureIl(AstNode node)
        {
            node.Value = Mangler.Mangle(node.Value ?? string.Empty);

            // Procedure prolog
            _emitter.EmitDirect($"{node.Value}:");
            _emitter.EmitOpcode("stp", "lr, fp, [sp, #-16]!"); // Save LR and FP on the stack
            _emitter.EmitOpcode("mov", "fp, sp"); // Set frame pointer

            foreach (var child in node.Children)
            {
                GenerateIlForBlock(child);
            }

            // Procedure epilog
            _emitter.EmitOpcode("mov", "sp, fp"); // Restore stack pointer
            _emitter.EmitOpcode("ldp", "lr, fp, [sp], #16"); // Restore FP and LR
            _emitter.EmitOpcode("ret"); // Return to caller
            _emitter.EmitNewLine();
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
                case AstNodeType.Allocation:
                    GenerateMemoryAllocation(child);
                    break;
            }
        }

        private void GenerateProcedureCall(AstNode child)
        {
            var mangled = Mangler.Mangle(child.Value ?? throw new CompilerException("Unable to generate procedure call."));

            _emitter.EmitOpcode("bl", mangled);
        }

        /// <summary>
        /// Generates code for memory allocation.
        /// </summary>
        /// <param name="node"></param>
        private void GenerateMemoryAllocation(AstNode node)
        {
            var type = node.TypeInformation ?? throw new CompilerException("Type information not available.");
            var sizeInBytes = type.SizeInBytes;

            var register = AllocateMemory(sizeInBytes);

            var symbolTableGuid = node.Scope ?? throw new CompilerException("Variable missing symbol table reference.");
            var currentScope = _symbolTables.Single(s => s.Key == symbolTableGuid).Value;
            var variable = currentScope.ResolveVariable(node.Meta[MetaDataKey.VariableName]);

            variable.Register = register;
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
        private Register AllocateMemory(uint size)
        {
            const MemoryProtocol protocol = MemoryProtocol.Read | MemoryProtocol.Write;
            const MemoryFlag flags = MemoryFlag.MapAnon;

            var register = _registerTable.Allocate();

            _emitter.EmitOpcode("mov", "x0, xzr");
            _emitter.EmitOpcode("mov", $"x1, #{size}");
            _emitter.EmitOpcode("mov", $"x2, #{(int)protocol}");
            _emitter.EmitOpcode("mov", $"x3, #{(int)flags}");
            _emitter.EmitOpcode("mov", "x4, #-1");
            _emitter.EmitOpcode("mov", "x5, xzr");
            _emitter.EmitSyscall(Syscall.Mmap);
            _emitter.EmitOpcode("svc", "#0x80");

            _emitter.EmitOpcode("mov", $"{_registerTable.GetName(register)}, x0");

            return register;
        }

        private void FreeMemory(uint size, Register register)
        {
            _emitter.EmitOpcode("mov", $"x0, {_registerTable.GetName(register)}");
            _emitter.EmitOpcode("mov", $"x1, #{size}");
            _emitter.EmitSyscall(Syscall.Munmap);
            _emitter.EmitOpcode("svc", "#0x80");

            _registerTable.Free(register.Index);
        }
    }
}