using System;
using System.Collections.Generic;
using Osclan.Analytics;
using Osclan.Analytics.Abstractions;
using Osclan.Compiler.Generation.Architecture.AArch64.Generators;
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
        var handler = new Handler(emitter, analyticsClientFactory, tree, symbolTables);
        handler.Handle();

        return emitter.GetResult();
    }

    private sealed class Handler(Emitter emitter, IAnalyticsClientFactory analyticsClientFactory, AstNode root, Dictionary<Guid, SymbolTable> symbolTables)
    {
        private readonly RegisterTable _registerTable = new(31, analyticsClientFactory.CreateClient<RegisterTable>());
        private readonly AnalyticsClient<Handler> _analyticsClient = analyticsClientFactory.CreateClient<Handler>();

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
                        _analyticsClient.LogWarning($"Generation for {node.TypeString} is not yet implemented.");
                        break;
                }
            }
        }
        
        private void GenerateRoot()
        {
            emitter.EmitDirect($"; AArch64 code generated at {DateTime.UtcNow}");
            emitter.EmitDirect(".global _main");
            emitter.EmitDirect(".align 8");
            emitter.EmitNewLine();

            emitter.EmitDirect(".include \"output/aarch64_native.s\"");

            emitter.EmitNewLine();

            emitter.EmitDirect("_main:"); // Entry point
            emitter.EmitOpcode("bl", $"{Mangler.Mangle("main")}"); // Go to main procedure
            emitter.EmitSyscall(Syscall.Exit);
            emitter.EmitOpcode("svc", KernelImmediate); // macOS supervisor call
            emitter.EmitNewLine();
        }

        private void GenerateProcedureIl(AstNode node)
        {
            _analyticsClient.LogEvent($"Generating code for procedure with name '{node.Value}'");
            node.Value = Mangler.Mangle(node.Value ?? string.Empty);

            // Procedure prolog
            emitter.EmitDirect($"{node.Value}:");
            emitter.EmitOpcode("stp", "lr, fp, [sp, #-16]!"); // Save LR and FP on the stack
            emitter.EmitOpcode("mov", "fp, sp"); // Set frame pointer

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
            
            new NodeGeneratorFactory(emitter, new AnalyticsClientFactory(), _registerTable, symbolTables)
                .CreateGenerator(child)?
                .Generate();
        }
    }
}