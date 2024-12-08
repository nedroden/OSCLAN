using System;
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
        _emitter.EmitOpcode("mov", $"x16, #{Syscall.Exit}");        // Syscall 1 = exit
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
            switch (child.Type)
            {
                default:
                    continue;
            }
        }

        // Procedure epilog
        _emitter.EmitOpcode("mov", "sp, fp");                       // Restore stack pointer
        _emitter.EmitOpcode("ldp", "lr, fp, [sp], #16");            // Restore FP and LR
        _emitter.EmitOpcode("ret");                                 // Return to caller
        _emitter.EmitNewLine();
    }

    /// <summary>
    /// Allocates memory and saves the address of the allocated memory in x0.
    /// </summary>
    /// <param name="size"></param>
    private void AllocateMemory(int size)
    {
        var protocol = MemoryProtocol.Read | MemoryProtocol.Write;
        var flags = MemoryFlag.MapAnon;

        _emitter.EmitOpcode("mov", "x0, xzr");
        _emitter.EmitOpcode("mov", $"x1, #{size}");
        _emitter.EmitOpcode("mov", $"x2, #{protocol}");
        _emitter.EmitOpcode("mov", $"x3, #{flags}");
        _emitter.EmitOpcode("mov", "x4, #-1");
        _emitter.EmitOpcode("mov", "x5, xzr");
        _emitter.EmitOpcode("mov", $"x16, #{Syscall.Mmap}");
        _emitter.EmitOpcode("svc", "#0x80");
    }

    /// <summary>
    /// Frees memory at the address in x0.
    /// </summary>
    private void FreeMemory()
    {

    }
}