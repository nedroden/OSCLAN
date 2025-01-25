using System;
using System.Collections.Generic;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Symbols;

namespace Osclan.Compiler.Generation.Abstractions;

public interface IGenerator
{
    string GenerateIl(AstNode tree, Dictionary<Guid, SymbolTable> symbolTables);
}