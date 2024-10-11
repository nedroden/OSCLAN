using Osclan.Compiler.Generation.Abstractions;
using Osclan.Compiler.Generation.Architecture;
using Osclan.Compiler.Parsing;

namespace Osclan.Compiler.Generation;

/// <summary>
/// Generates intermediate language code from an abstract syntax tree, based on the specified generation strategy.
/// </summary>
public class Generator : IGenerator
{
    private readonly IGenerationStrategy _strategy;

    /// <summary>
    /// Initializes a new instance of the <see cref="Generator"/> class.
    /// </summary>
    /// <param name="strategy">The type of generator to use (equivalent to the architecture).</param>
    public Generator(IGenerationStrategy strategy) => _strategy = strategy;

    /// <summary>
    /// Generates intermediate language code for the given AST.
    /// </summary>
    /// <param name="tree">The root node of the AST.</param>
    /// <returns>The IL that corresponds to translating the given AST using the given strategy.</returns>
    public string GenerateIl(AstNode tree) => _strategy.GenerateIl(tree);
}