namespace Osclan.Compiler.Parsing;

/// <summary>
/// Represents a variable's type before the type is properly resolved.
/// </summary>
public record RawType
{
    public required string Name { get; set; }

    public uint Size { get; set; }

    public bool IsPointer { get; set; }

    /// <summary>
    /// Represents a field, e.g. 'elements' in 'List::elements'
    /// </summary>
    public RawType? SubType { get; set; }
}