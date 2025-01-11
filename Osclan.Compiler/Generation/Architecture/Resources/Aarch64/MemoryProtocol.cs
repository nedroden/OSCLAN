using System;

namespace Osclan.Compiler.Generation.Architecture.Resources.Aarch64;

[Flags]
public enum MemoryProtocol
{
    None = 0x00,
    Read = 0x01,
    Write = 0x02,
    Exec = 0x04
}