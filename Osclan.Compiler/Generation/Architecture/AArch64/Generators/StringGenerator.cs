using Osclan.Analytics;
using Osclan.Compiler.Analysis;
using Osclan.Compiler.Generation.Assembly;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Symbols;

namespace Osclan.Compiler.Generation.Architecture.AArch64.Generators;

public class StringGenerator(
    AstNode node,
    Emitter emitter,
    AnalyticsClient<StringGenerator> analyticsClient,
    SymbolTable currentScope,
    RegisterTable registerTable)
    : MemoryManagingGenerator<StringGenerator>(registerTable, emitter, analyticsClient)
{
    public override void Generate()
    {
        // If this is not a variable, ignore for now.
        if (!node.Meta.TryGetValue(MetaDataKey.VariableName, out _))
        {
            analyticsClient.LogWarning("Ignored string not assigned to a variable.");
            return;
        }
        
        var variable = currentScope.ResolveVariable(node.Meta[MetaDataKey.VariableName]);

        // TODO: ensure that registers are deallocated nicely
        emitter.EmitComment("Assigning value to variable");
        variable.Register ??= StoreString(node.Value!);
    }
}