using System.Collections.Generic;

namespace Osclan.Compiler.Symbols;

/// <summary>
/// Represents a procedure.
/// </summary>
public record Procedure : Symbol
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Procedure"/> class.
    /// </summary>
    /// <param name="name">The name of the procedure.</param>
    public Procedure(string name) : base(name, SymbolType.Procedure)
    {
    }

    /// <summary>
    /// The return type of the procedure. If empty, the return 'type' is void.
    /// </summary>
    public Type? ReturnType { get; set; }

    /// <summary>
    /// The parameters of the procedure, in order.
    /// </summary>
    public List<Variable> Parameters { get; set; } = new();
}