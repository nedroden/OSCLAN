using System.Linq;
using Osclan.Compiler.Exceptions;
using Osclan.Compiler.Generation.Assembly;

namespace Osclan.Compiler.Test.Generation.Assembly;

public class RegisterTableTests
{
    private readonly RegisterTable _sut = new(31);

    [Fact]
    public void TestRegisterTableIsInitializedWithNumberOfRegisters() =>
        Assert.Equal(31, _sut.GetRegisters().Length);

    [Fact]
    public void TestRegistersAreInitializedWithFreeState() =>
        Assert.All(_sut.GetRegisters(), register => Assert.Equal(RegisterState.Free, register.State));

    [Fact]
    public void TestRegisterIsAllocated()
    {
        var register = _sut.Allocate();
        
        Assert.NotNull(register);
        Assert.Equal(0, register.Index);
        Assert.Equal(RegisterState.InUse, register.State);
    }

    [Fact]
    public void TestRegisterIsFreed()
    {
        var register = _sut.Allocate();
        Assert.Equal(RegisterState.InUse, register.State);
        
        _sut.Free(register.Index);
        Assert.Equal(RegisterState.Free, _sut.GetRegisters().First().State);
    }

    [Fact]
    public void TestSecondRegisterIsAllocated()
    {
        var firstRegister = _sut.Allocate();
        Assert.Equal(0, firstRegister.Index);
        
        var secondRegister = _sut.Allocate();
        Assert.Equal(1, secondRegister.Index);
    }

    [Fact]
    public void TestAllocatingTooManyRegistersThrowsException()
    {
        var sut = new RegisterTable(1);
        sut.Allocate();

        Assert.Throws<SourceException>(() => sut.Allocate());
    }

    [Fact]
    public void TestRegisterIsReserved()
    {
        _sut.ReserveRegisters(0);

        Assert.True(_sut.GetRegisters().First().IsReserved);
        Assert.NotEqual(0, _sut.Allocate().Index);
    }
}