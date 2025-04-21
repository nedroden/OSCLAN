using System.Text;
using Osclan.Analytics;
using Osclan.Compiler.Extensions;
using Osclan.Compiler.Generation.Architecture.AArch64.Resources;
using Osclan.Compiler.Generation.Assembly;

namespace Osclan.Compiler.Generation.Architecture.AArch64.Generators;

public abstract class MemoryManagingGenerator<T> : INodeGenerator
{
    protected readonly Emitter emitter;
    protected readonly RegisterTable registerTable;
    protected readonly AnalyticsClient<T> analyticsClient;

    protected MemoryManagingGenerator(RegisterTable registerTable,
        Emitter emitter,
        AnalyticsClient<T> analyticsClient)
    {
        this.emitter = emitter;
        this.registerTable = registerTable;
        this.analyticsClient = analyticsClient;
    }

    protected const string KernelImmediate = "#0x80";

    /// <summary>§
    /// Allocates memory and saves the address of the allocated memory in x0. The address is then moved
    /// to a register.
    /// </summary>
    /// <param name="size"></param>
    protected Register AllocateMemory(uint size) =>
        AllocateMemory(size, registerTable.Allocate());

    protected Register AllocateMemory(uint size, Register register)
    {
        var registerName = registerTable.GetName(register);
        analyticsClient.LogEvent($"Allocating {size} bytes of memory. Address stored in '{registerName}'");
        
        const MemoryProtocol protocol = MemoryProtocol.Read | MemoryProtocol.Write;
        const MemoryFlag flags = MemoryFlag.MapAnon | MemoryFlag.MapPrivate;

        emitter.EmitComment("Memory allocation");
        emitter.EmitOpcode("mov", "x0, xzr");
        emitter.EmitOpcode("mov", $"x1, #{size}");
        emitter.EmitOpcode("mov", $"x2, #{(int)protocol}");
        emitter.EmitOpcode("mov", $"x3, #{(int)flags}");
        emitter.EmitOpcode("mov", "x4, #-1");
        emitter.EmitOpcode("mov", "x5, xzr");
        emitter.EmitSyscall(Syscall.Mmap);
        emitter.EmitOpcode("svc", KernelImmediate);

        emitter.EmitOpcode("mov", $"{registerName}, x0");

        return register;
    }

    protected void FreeMemory(uint size, Register register)
    {
        var registerName = registerTable.GetName(register);
        analyticsClient.LogEvent($"Freeing {size} bytes at address stored in {registerName}");
        
        emitter.EmitComment("Memory deallocation");
        emitter.EmitOpcode("mov", $"x0, {registerName}");
        emitter.EmitOpcode("mov", $"x1, #{size}");
        emitter.EmitSyscall(Syscall.Munmap);
        emitter.EmitOpcode("svc", KernelImmediate);

        emitter.EmitOpcode("mov", $"{register.Name}, xzr");
        registerTable.Free(register.Index);
    }

    /// <summary>
    /// Frees memory based on the values in x0 and x1 (address and length respectively). The register table is NOT
    /// updated, meaning any register determined through the variable table CANNOT be used again.
    ///
    /// Of course, this means that x0 and x1 are set BEFORE calling this method.
    /// </summary>
    protected void FreeMemoryUnsafe()
    {
        analyticsClient.LogEvent("Performing unsafe memory deallocation based on values in x0 and x1");
        
        emitter.EmitComment("Unsafe memory deallocation");
        emitter.EmitSyscall(Syscall.Munmap);
        emitter.EmitOpcode("svc", KernelImmediate);

        emitter.EmitOpcode("mov", "x0, xzr");
        emitter.EmitOpcode("mov", "x1, xzr");
    }

    protected Register StoreString(string value) => 
        StoreString(value, registerTable.Allocate());

    protected Register StoreString(string value, Register register)
    {
        var length = (uint)value.Length;
        _ = AllocateMemory(length, register);

        emitter.EmitComment("Store print statement operand in memory");

        var scratchRegister = registerTable.Allocate();
        for (var i = 0; i < length; i += 2)
        {
            var byteRepr = Encoding.ASCII.GetBytes(value.Window(2, i)).ToHex().PadWithZeros(4);

            emitter.EmitOpcode("mov", $"{scratchRegister.Name}, {byteRepr}");
            emitter.EmitOpcode("str", $"{scratchRegister.Name}, [{register.Name}, #{i}]");
        }

        emitter.EmitOpcode("mov", $"{scratchRegister.Name}, xzr");
        registerTable.Free(scratchRegister);

        return register;   
    }

    public abstract void Generate();
}