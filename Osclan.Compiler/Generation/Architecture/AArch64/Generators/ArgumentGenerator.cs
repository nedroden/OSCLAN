using Osclan.Analytics;
using Osclan.Compiler.Analysis;
using Osclan.Compiler.Generation.Assembly;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Symbols;

namespace Osclan.Compiler.Generation.Architecture.AArch64.Generators;

public class ArgumentGenerator(
    AstNode node,
    AnalyticsClient<ArgumentGenerator> analyticsClient,
    SymbolTable currentScope,
    RegisterTable registerTable) : INodeGenerator
{
    public void Generate()
    {
        // This is not a procedure argument, hence we don't need to do anything
        if (!node.Meta.TryGetValue(MetaDataKey.ProcedureName, out _))
        {
            return;
        }
        
        var variable = currentScope.ResolveVariable(node.Value ?? string.Empty);

        // TODO: We should be getting this warning every time since a register should be assigned
        // as soon as the argument is passed, thus making this entire class redundant.
        if (variable.Register is not null)
        {
            analyticsClient.LogWarning("Argument was already assigned a register");

            return;
        }

        variable.Register = registerTable.Allocate();   
    }
}