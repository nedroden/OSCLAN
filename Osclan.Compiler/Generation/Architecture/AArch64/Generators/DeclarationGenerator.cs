using System;
using System.Collections.Generic;
using System.Linq;
using Osclan.Analytics;
using Osclan.Compiler.Exceptions;
using Osclan.Compiler.Generation.Assembly;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Symbols;

namespace Osclan.Compiler.Generation.Architecture.AArch64.Generators;

public class DeclarationGenerator(
    AstNode node,
    AnalyticsClient<DeclarationGenerator> analyticsClient,
    Dictionary<Guid, SymbolTable> symbolTables,
    RegisterTable registerTable) : INodeGenerator
{
    public void Generate()
    {
        var variableNode = node.Children.Single(c => c.Type == AstNodeType.Variable);
        var scope = variableNode.Scope ?? throw new CompilerException("No scope defined.");
        var variable = symbolTables[scope].ResolveVariable(variableNode.Value ?? string.Empty);

        // TODO: Check if this entire method has not become redundant
        if (variable.Register is null)
        {
            analyticsClient.LogWarning($"Variable '{variable.UnmangledName}' was not assigned a register");
            variable.Register = registerTable.Allocate();   
        }
    }
}