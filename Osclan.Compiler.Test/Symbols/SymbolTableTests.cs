using Osclan.Compiler.Symbols;

namespace Osclan.Compiler.Test.Symbols;

public class SymbolTableTests
{
    private readonly SymbolTable _sut = new(0);

    [Fact]
    public void Test_Variable_Is_Declared()
    {
        var variable = new Variable
        {
            Name = "someVariable",
            TypeName = "string"
        };

        _sut.AddVariable(variable);

        Assert.Single(_sut.Variables);

        var symbol = _sut.Variables[0];
        Assert.Equal(Mangler.Mangle("someVariable"), symbol.Name);
        Assert.Equal("someVariable", symbol.UnmangledName);
        Assert.Equal("string", symbol.TypeName);
    }

    [Fact]
    public void Test_Variable_Is_Resolved()
    {
        _sut.AddVariable(new Variable
        {
            Name = "someVariable",
            TypeName = "string"
        });

        Assert.True(_sut.VariableInCurrentScope("someVariable"));
    }

    [Fact]
    public void Test_Type_Is_Declared()
    {
        _sut.AddType(new Osclan.Compiler.Symbols.Type
        {
            Name = "SomeType",
            SizeInBytes = 0
        });

        Assert.Single(_sut.Types);
        var type = _sut.Types[0];

        Assert.Equal(Mangler.Mangle("SomeType"), type.Name);
        Assert.Equal((uint)0, type.SizeInBytes);
    }

    [Fact]
    public void Test_Type_Is_Resolved_In_Current_Scope()
    {
        _sut.AddType(new Osclan.Compiler.Symbols.Type
        {
            Name = "SomeType",
            SizeInBytes = 0
        });

        Assert.True(_sut.TypeInCurrentScope("SomeType"));
    }
}