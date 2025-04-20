using System.Linq;
using Osclan.Analytics;
using Osclan.Compiler.Exceptions;

namespace Osclan.Compiler.Generation.Assembly;

public class Register(short index, RegisterState state)
{
    public readonly short Index = index;

    public RegisterState State { get; set; } = state;
    
    public bool IsReserved { get; set; }

    public string Name => $"x{Index}";
}

public enum RegisterState
{
    Free,
    InUse
}

public class RegisterTable(int registers, AnalyticsClient<RegisterTable> analyticsClient)
{
    private readonly Register[] _state = Enumerable.Range(0, registers).Select(i => new Register((short)i, RegisterState.Free)).ToArray();

    public void Free(Register register)
    {
        _state[register.Index].State = RegisterState.Free;
        analyticsClient.LogEvent($"Unreserved register '{_state[register.Index].Name}'");
    }

    public void Free(short registerName)
    {
        _state.Single(r => r.Index == registerName).State = RegisterState.Free;
        analyticsClient.LogEvent($"Unreserved register '{registerName}'");
    }

    public Register Allocate()
    {
        var register = _state.FirstOrDefault(r => r is { State: RegisterState.Free, IsReserved: false });

        if (register is null)
        {
            throw new SourceException("No registers are available.");
        }
        
        register.State = RegisterState.InUse;
        analyticsClient.LogEvent($"Reserved register '{register.Name}'");

        return register;
    }

    /// <summary>
    /// Updates the state of a register without any regard for its current state and protection level.
    /// </summary>
    /// <param name="registerId">The id of the register, e.g., '1' for register x1.</param>
    /// <returns>The register, with its updated state.</returns>
    public Register UnsafeAllocate(short registerId)
    {
        var register = _state.First(r => r.Index == registerId);
        
        register.State = RegisterState.InUse;
        analyticsClient.LogEvent($"Reserved register '{register.Name}' in unsafe mode");

        return register;
    }

    public void ReserveRegisters(params short[] registers) => 
        _state.Where(r => registers.Contains(r.Index)).ToList().ForEach(register => register.IsReserved = true);

    public string GetName(short registerIndex) =>
        $"x{registerIndex}";

    public string GetName(Register register) =>
        GetName(register.Index);
    
    public Register GetRegister(short registerIndex) =>
        _state.First(r => r.Index == registerIndex);
    
    public Register[] GetRegisters() => 
        _state;
}