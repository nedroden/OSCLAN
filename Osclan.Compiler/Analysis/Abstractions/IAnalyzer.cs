using System;
using System.Collections.Generic;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Symbols;

namespace Osclan.Compiler.Analysis.Abstractions;

public interface IAnalyzer
{
    Dictionary<Guid, SymbolTable> ArchivedSymbolTables { get; }

    AstNode Analyze(AstNode ast);
}