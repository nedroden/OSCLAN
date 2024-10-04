using System;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Symbols;

namespace Osclan.Compiler.Generation.Architecture;

public class AArch64Strategy : IGenerationStrategy
{
    private readonly Emitter _emitter;

    public AArch64Strategy(Emitter emitter) => _emitter = emitter;

    public string GenerateIl(AstNode tree)
    {
        _emitter.EmitDirect($"; AArch64 code generated at {DateTime.UtcNow}");
        GenerateRoot();

        GenerateProcedureProlog(Mangler.Mangle("main"));
        GenerateProcedureEpilog();

        return _emitter.GetResult();
    }

    private void GenerateRoot()
    {
        _emitter.EmitDirect(".global _main");
        _emitter.EmitDirect(".align 2");
        _emitter.EmitNewLine();

        _emitter.EmitDirect("_main:");                              // Entry point
        _emitter.EmitOpcode("bl", $"p_{Mangler.Mangle("main")}");   // Go to main procedure
        _emitter.EmitOpcode("mov", "x0, xzr");                      // Exit code 0
        _emitter.EmitOpcode("mov", "x16, #1");                      // Syscall 1 = exit
        _emitter.EmitOpcode("svc", "#0x80");                        // macOS supervisor call
        _emitter.EmitNewLine();
    }

    private void GenerateProcedureProlog(string procedureName)
    {
        _emitter.EmitDirect($"p_{procedureName}:");
        _emitter.EmitOpcode("stp", "lr, fp, [sp, #-16]!");          // Save LR and FP on the stack
        _emitter.EmitOpcode("mov", "fp, sp");                       // Set frame pointer
    }

    private void GenerateProcedureEpilog()
    {
        _emitter.EmitOpcode("mov", "sp, fp");                       // Restore stack pointer
        _emitter.EmitOpcode("ldp", "lr, fp, [sp], #16");            // Restore FP and LR
        _emitter.EmitOpcode("ret");                                 // Return to caller
        _emitter.EmitNewLine();
    }
}