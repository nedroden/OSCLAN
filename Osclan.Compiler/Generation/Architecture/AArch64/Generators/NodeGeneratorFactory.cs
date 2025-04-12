using System;
using System.Collections.Generic;
using System.Linq;
using Osclan.Analytics;
using Osclan.Compiler.Generation.Assembly;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Symbols;

namespace Osclan.Compiler.Generation.Architecture.AArch64.Generators;

public class NodeGeneratorFactory(
    Emitter emitter, 
    AnalyticsClientFactory analyticsClientFactory,
    RegisterTable registerTable,
    Dictionary<Guid, SymbolTable> symbolTables)
{   
    private readonly AnalyticsClient<NodeGeneratorFactory> _analyticsClient = analyticsClientFactory.CreateClient<NodeGeneratorFactory>();

    public INodeGenerator? CreateGenerator(AstNode node) =>
        node.Type switch
        {
            AstNodeType.Allocation => new MemoryAllocationGenerator(node, emitter, analyticsClientFactory.CreateClient<MemoryAllocationGenerator>(), symbolTables, registerTable),
            AstNodeType.Deallocation => new DeallocationGenerator(node, emitter, analyticsClientFactory.CreateClient<DeallocationGenerator>(), symbolTables, registerTable),
            AstNodeType.Print => new PrintStatementGenerator(node, emitter, analyticsClientFactory.CreateClient<PrintStatementGenerator>(), GetCurrentScope(node.Children[0].Children[0]), registerTable),
            AstNodeType.Ret => new ReturnStatementGenerator(node, emitter, analyticsClientFactory.CreateClient<ReturnStatementGenerator>(), symbolTables, registerTable),
            AstNodeType.Declaration => new DeclarationGenerator(node, analyticsClientFactory.CreateClient<DeclarationGenerator>(), symbolTables, registerTable),
            AstNodeType.ProcedureCall => new ProcedureCallGenerator(node, emitter),
            AstNodeType.Scalar => new ScalarGenerator(node, emitter, analyticsClientFactory.CreateClient<ScalarGenerator>(), GetCurrentScope(node), registerTable),
            _ => null
        };

    private SymbolTable GetCurrentScope(AstNode node)
    {
        if (node.Scope is null)
        {
            _analyticsClient.LogWarning($"Creating empty scope for node of type '{node.Type}'");

            return new SymbolTable(255);
        }

        return symbolTables.Single(s => s.Key == node.Scope).Value;
    }
}