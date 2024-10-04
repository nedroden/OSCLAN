using System.Collections.Generic;

namespace Osclan.Compiler.Symbols;

public record Type
{
    public required string Name { get; set; }

    public string UnmangledName { get; set; } = string.Empty;

    public uint SizeInBytes { get; set; }

    public bool IsPointer { get; set; }

    public Dictionary<string, Type> Fields { get; set; } = new();

    public bool IsArray { get; set; }
}