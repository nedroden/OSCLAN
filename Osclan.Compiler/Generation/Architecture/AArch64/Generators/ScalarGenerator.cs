using Osclan.Analytics;
using Osclan.Compiler.Analysis;
using Osclan.Compiler.Generation.Assembly;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Symbols;

namespace Osclan.Compiler.Generation.Architecture.AArch64.Generators;

public class ScalarGenerator(
    AstNode node,
    Emitter emitter, 
    AnalyticsClient<ScalarGenerator> analyticsClient,
    SymbolTable currentScope,
    RegisterTable registerTable) : INodeGenerator
{
    public void Generate()
    {
        // If this is not a variable, ignore for now.
        if (!node.Meta.TryGetValue(MetaDataKey.VariableName, out _))
        {
            analyticsClient.LogWarning("Ignored scalar not assigned to a variable.");
            return;
        }
        
        var variable = currentScope.ResolveVariable(node.Meta[MetaDataKey.VariableName]);

        // TODO: ensure that registers are deallocated nicely
        variable.Register ??= registerTable.Allocate();
            
        emitter.EmitComment("Assigning value to variable");
        emitter.EmitOpcode("mov", $"{variable.Register.Name}, #{node.Value}");
    }
}