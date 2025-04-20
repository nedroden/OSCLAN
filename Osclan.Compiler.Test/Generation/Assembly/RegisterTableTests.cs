using System.Linq;
using Osclan.Analytics;
using Osclan.Compiler.Exceptions;
using Osclan.Compiler.Generation.Assembly;

namespace Osclan.Compiler.Test.Generation.Assembly;

public class RegisterTableTests
{
    private readonly RegisterTable _sut = new(31, new AnalyticsClientFactory().CreateClient<RegisterTable>());

    [Fact]
    public void Test_Register_Table_Is_Initialized_With_Number_Of_Registers() =>
        Assert.Equal(31, _sut.GetRegisters().Length);

    [Fact]
    public void Test_Registers_Are_Initialized_With_Free_State() =>
        Assert.All(_sut.GetRegisters(), register => Assert.Equal(RegisterState.Free, register.State));

    [Fact]
    public void Test_Register_Is_Allocated()
    {
        var register = _sut.Allocate();
        
        Assert.NotNull(register);
        Assert.Equal(0, register.Index);
        Assert.Equal(RegisterState.InUse, register.State);
    }

    [Fact]
    public void Test_Register_Is_Freed()
    {
        var register = _sut.Allocate();
        Assert.Equal(RegisterState.InUse, register.State);
        
        _sut.Free(register.Index);
        Assert.Equal(RegisterState.Free, _sut.GetRegisters()[0].State);
    }

    [Fact]
    public void Test_Second_Register_Is_Allocated()
    {
        var firstRegister = _sut.Allocate();
        Assert.Equal(0, firstRegister.Index);
        
        var secondRegister = _sut.Allocate();
        Assert.Equal(1, secondRegister.Index);
    }

    [Fact]
    public void Test_Allocating_Too_Many_Registers_Throws_Exception()
    {
        var sut = new RegisterTable(1,  new AnalyticsClientFactory().CreateClient<RegisterTable>());
        sut.Allocate();

        Assert.Throws<SourceException>(() => sut.Allocate());
    }

    [Fact]
    public void Test_Register_Is_Reserved()
    {
        _sut.ReserveRegisters(0);

        Assert.True(_sut.GetRegisters()[0].IsReserved);
        Assert.NotEqual(0, _sut.Allocate().Index);
    }

    [Fact]
    public void Test_Protected_Register_Can_Be_Allocated_With_Unsafe_Method()
    {
        _sut.ReserveRegisters(0);
        var register = _sut.UnsafeAllocate(0);

        Assert.NotNull(register);
        Assert.Equal(0, register.Index);
        Assert.Equal(RegisterState.InUse, register.State);
    }
}