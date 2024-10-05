namespace Osclan.Compiler.Symbols;

public enum SymbolType
{
    Variable,
    Type,
    Procedure
}

public abstract record Symbol
{
    public string Name { get; set; }

    public string UnmangledName { get; set; } = string.Empty;

    public SymbolType Type { get; set; }

    public Symbol(string name, SymbolType type)
    {
        Name = name;
        Type = type;
    }
}