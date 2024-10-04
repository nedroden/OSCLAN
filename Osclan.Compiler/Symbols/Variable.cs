namespace Osclan.Compiler.Symbols;

public record Variable
{
    public required string Name { get; set; }

    public string UnmangledName { get; set; } = string.Empty;

    public required string TypeName { get; set; }

    public bool IsPointer { get; set; }

    public uint SizeInBytes { get; set; }
}