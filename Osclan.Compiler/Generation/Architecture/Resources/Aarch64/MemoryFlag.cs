using System;

namespace Osclan.Compiler.Generation.Architecture.Resources.Aarch64;

[Flags]
public enum MemoryFlag
{
    MapFile = 0x00,
    MapAnon = 0x20,
    Map32Bit = 0x40,
}