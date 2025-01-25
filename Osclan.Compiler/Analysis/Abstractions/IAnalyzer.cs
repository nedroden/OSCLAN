using System;
using System.Collections.Generic;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Symbols;

namespace Osclan.Compiler.Analysis.Abstractions;

public record AnalyzerResult(AstNode Root, Dictionary<Guid, SymbolTable> SymbolTables);

public interface IAnalyzer
{
    Dictionary<Guid, SymbolTable> ArchivedSymbolTables { get; }

    AnalyzerResult Analyze(AstNode ast);
}