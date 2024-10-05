using System.Collections.Generic;

namespace Osclan.Compiler.Symbols;

public record Type : Symbol
{
    public Type(string name) : base(name, SymbolType.Type)
    {
    }

    public bool IsPointer { get; set; }

    public bool IsArray { get; set; }

    public uint SizeInBytes { get; set; }

    public uint AddressOffset { get; set; }

    public Dictionary<string, Type> Fields { get; set; } = new();
}