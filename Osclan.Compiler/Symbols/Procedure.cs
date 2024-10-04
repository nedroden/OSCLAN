using System.Collections.Generic;

namespace Osclan.Compiler.Symbols;

/// <summary>
/// Represents a procedure.
/// </summary>
public record Procedure
{
    /// <summary>
    /// The mangled procedure name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The original, unmangled procedure name.
    /// </summary>
    public string UnmangledName { get; set; } = string.Empty;

    /// <summary>
    /// The return type of the procedure. If empty, the return 'type' is void.
    /// </summary>
    public Type? ReturnType { get; set; }

    /// <summary>
    /// The parameters of the procedure, in order.
    /// </summary>
    public List<Variable> Parameters { get; set; } = new();
}