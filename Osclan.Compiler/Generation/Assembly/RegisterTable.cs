using System.Linq;
using Osclan.Compiler.Exceptions;

namespace Osclan.Compiler.Generation.Assembly;

public class Register(short index, RegisterState state)
{
    public readonly short Index = index;

    public RegisterState State { get; set; } = state;
    
    public bool IsReserved { get; set; }
}

public enum RegisterState
{
    Free,
    InUse
}

public class RegisterTable(int registers)
{
    private readonly Register[] _state = Enumerable.Range(0, registers).Select(i => new Register((short)i, RegisterState.Free)).ToArray();
    
    public void Free(short registerName) =>
        _state.Single(r => r.Index == registerName).State = RegisterState.Free;

    public Register Allocate()
    {
        var register = _state.ToList().FirstOrDefault(r => r is { State: RegisterState.Free, IsReserved: false });

        if (register is null)
        {
            throw new SourceException("No registers are available.");
        }
        
        register.State = RegisterState.InUse;

        return register;
    }

    public void ReserveRegisters(params short[] registers) => 
        _state.Where(r => registers.Contains(r.Index)).ToList().ForEach(register => register.IsReserved = true);

    public string GetName(short registerIndex) =>
        $"x{registerIndex}";

    public string GetName(Register register) =>
        GetName(register.Index);
    
    public Register[] GetRegisters() => 
        _state;
}