using Osclan.Compiler.Generation.Assembly;

namespace Osclan.Compiler.Symbols;

public record Variable : Symbol
{
    public Variable(string name) : base(name, SymbolType.Variable)
    {
    }

    public required string TypeName { get; set; }

    public bool IsPointer { get; set; }

    public uint SizeInBytes { get; set; }
    
    public Register? Register { get; set; }
}