using Osclan.Compiler.Generation.Architecture;
using Osclan.Compiler.Parsing;

namespace Osclan.Compiler.Generation;

/// <summary>
/// Generates intermediate language code from an abstract syntax tree, based on the specified generation strategy.
/// </summary>
public class Generator
{
    private readonly AstNode _tree;
    private readonly IGenerationStrategy _strategy;

    /// <summary>
    /// Initializes a new instance of the <see cref="Generator"/> class.
    /// </summary>
    /// <param name="tree">The root node of the AST.</param>
    /// <param name="strategy">The type of generator to use (equivalent to the architecture).</param>
    public Generator(AstNode tree, IGenerationStrategy strategy)
    {
        _tree = tree;
        _strategy = strategy;
    }

    public string GenerateIl() => _strategy.GenerateIl(_tree);
}