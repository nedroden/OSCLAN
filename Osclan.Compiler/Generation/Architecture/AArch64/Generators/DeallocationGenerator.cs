using System;
using System.Collections.Generic;
using System.Linq;
using Osclan.Analytics;
using Osclan.Compiler.Exceptions;
using Osclan.Compiler.Generation.Assembly;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Symbols;

namespace Osclan.Compiler.Generation.Architecture.AArch64.Generators;

public class DeallocationGenerator(
    AstNode node,
    Emitter emitter, 
    AnalyticsClient<DeallocationGenerator> analyticsClient,
    Dictionary<Guid, SymbolTable> symbolTables,
    RegisterTable registerTable) : MemoryManagingGenerator<DeallocationGenerator>(registerTable, emitter, analyticsClient)
{
    public override void Generate()
    {
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
}