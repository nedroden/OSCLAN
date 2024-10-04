namespace Osclan.Compiler.Symbols;

public record Variable
{
    public required string Name { get; set; }

    public required string UnmangledName { get; set; }

    public required string TypeName { get; set; }

    public bool IsPointer { get; set; }

    public uint SizeInBytes { get; set; }
}