using System;
using System.Collections.Generic;
using Osclan.Analytics;
using Osclan.Compiler.Generation.Assembly;
using Osclan.Compiler.Meta;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Symbols;

namespace Osclan.Compiler.Generation.Architecture.AArch64.Generators;

public class AssignmentGenerator(
    AstNode node,
    Emitter emitter,
    AnalyticsClient<AssignmentGenerator> analyticsClient,
    Dictionary<Guid, SymbolTable> symbolTables,
    RegisterTable registerTable)
    : MemoryManagingGenerator<AssignmentGenerator>(registerTable, emitter, analyticsClient)
{
    // TODO: Implement. Right now, we need this generator to ensure that the return value
    // of HELLO-WORLD() is assigned to the variable: mov x8, x0, where x8 is the register
    // of the variable and x0 is the return value register of HELLO-WORLD().
    public override void Generate()
    {
        // Temporary, to prevent the compiler from complaining
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(symbolTables);
    }
}