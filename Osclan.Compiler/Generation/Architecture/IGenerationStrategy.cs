using System;
using System.Collections.Generic;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Symbols;

namespace Osclan.Compiler.Generation.Architecture;

public interface IGenerationStrategy
{
    string GenerateIl(AstNode tree, Dictionary<Guid, SymbolTable> symbolTables);
}