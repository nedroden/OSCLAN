using System;
using Osclan.Compiler.Exceptions;
using Osclan.Compiler.Generation.Architecture.Resources.Aarch64;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Symbols;

namespace Osclan.Compiler.Generation.Architecture;

public class AArch64Strategy(Emitter emitter) : IGenerationStrategy
{
    private readonly Emitter _emitter = emitter;

    public string GenerateIl(AstNode tree)
    {
        _emitter.EmitDirect($"; AArch64 code generated at {DateTime.UtcNow}");
        GenerateRoot();

        foreach (var node in tree.Children)
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

        return _emitter.GetResult();
    }

    private void GenerateRoot()
    {
        _emitter.EmitDirect(".global _main");
        _emitter.EmitDirect(".align 2");
        _emitter.EmitNewLine();

        _emitter.EmitDirect(".include \"output/aarch64_native.s\"");

        _emitter.EmitNewLine();

        _emitter.EmitDirect("_main:");                              // Entry point
        _emitter.EmitOpcode("bl", $"{Mangler.Mangle("main")}");     // Go to main procedure
        _emitter.EmitOpcode("mov", "x0, xzr");                      // Exit code 0
        _emitter.EmitSyscall(Syscall.Exit);
        _emitter.EmitOpcode("svc", "#0x80");                        // macOS supervisor call
        _emitter.EmitNewLine();
    }

    private void GenerateProcedureIl(AstNode node)
    {
        node.Value = Mangler.Mangle(node.Value ?? string.Empty);

        // Procedure prolog
        _emitter.EmitDirect($"{node.Value}:");
        _emitter.EmitOpcode("stp", "lr, fp, [sp, #-16]!");          // Save LR and FP on the stack
        _emitter.EmitOpcode("mov", "fp, sp");                       // Set frame pointer

        foreach (var child in node.Children)
        {
            GenerateIlForBlock(child);
        }

        // Procedure epilog
        _emitter.EmitOpcode("mov", "sp, fp");                       // Restore stack pointer
        _emitter.EmitOpcode("ldp", "lr, fp, [sp], #16");            // Restore FP and LR
        _emitter.EmitOpcode("ret");                                 // Return to caller
        _emitter.EmitNewLine();
    }

    private void GenerateIlForBlock(AstNode child)
    {
        switch (child.Type)
        {
            case AstNodeType.ProcedureCall:
                GenerateProcedureCall(child);
                break;
            // case AstNodeType.Allocation:
            //     GenerateMemoryAllocation(node);
            //     break;
        }

        if (child.Children.Count <= 0)
        {
            return;
        }
        
        foreach (var node in child.Children)
        {
            GenerateIlForBlock(node);
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
    private void GenerateMemoryAllocation(AstNode node) => throw new NotImplementedException();

    /// <summary>
    /// At the end of a scope, frees any allocated memory.
    /// </summary>
    /// <param name="node"></param>
    private void FreeMemoryAtEndOfScope(AstNode node) => throw new NotImplementedException();

    /// <summary>§
    /// Allocates memory and saves the address of the allocated memory in x0.
    /// </summary>
    /// <param name="size"></param>
    private void AllocateMemory(int size)
    {
        const MemoryProtocol protocol = MemoryProtocol.Read | MemoryProtocol.Write;
        const MemoryFlag flags = MemoryFlag.MapAnon;

        _emitter.EmitOpcode("mov", "x0, xzr");
        _emitter.EmitOpcode("mov", $"x1, #{size}");
        _emitter.EmitOpcode("mov", $"x2, #{protocol}");
        _emitter.EmitOpcode("mov", $"x3, #{flags}");
        _emitter.EmitOpcode("mov", "x4, #-1");
        _emitter.EmitOpcode("mov", "x5, xzr");
        _emitter.EmitSyscall(Syscall.Mmap);
        _emitter.EmitOpcode("svc", "#0x80");
    }

    /// <summary>
    /// Frees memory at the address in x0. Input:
    /// x0: The address.
    /// </summary>
    private void FreeMemory(int size)
    {
        _emitter.EmitOpcode("mov", $"#{size}");
        _emitter.EmitSyscall(Syscall.Munmap);
        _emitter.EmitOpcode("svc", "#0x80");
    }
}