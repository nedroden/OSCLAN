using System;
using System.Collections.Generic;
using System.Linq;
using Osclan.Analytics;
using Osclan.Compiler.Analysis;
using Osclan.Compiler.Exceptions;
using Osclan.Compiler.Generation.Assembly;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Symbols;

namespace Osclan.Compiler.Generation.Architecture.AArch64.Generators;

public class MemoryAllocationGenerator(
    AstNode node,
    Emitter emitter, 
    AnalyticsClient<MemoryAllocationGenerator> analyticsClient,
    Dictionary<Guid, SymbolTable> symbolTables,
    RegisterTable registerTable) : MemoryManagingGenerator<MemoryAllocationGenerator>(registerTable, emitter, analyticsClient)
{
    public override void Generate()
    {
        var type = node.TypeInformation ?? throw new CompilerException("Type information not available.");
        var sizeInBytes = type.SizeInBytes;

        var symbolTableGuid = node.Scope ?? throw new CompilerException("Variable missing symbol table reference.");
        var currentScope = symbolTables.Single(s => s.Key == symbolTableGuid).Value;
        var variable = currentScope.ResolveVariable(node.Meta[MetaDataKey.VariableName]);
            
        AllocateMemory(sizeInBytes, variable.Register ?? throw new CompilerException("Variable was not assigned to a register."));
    }
}