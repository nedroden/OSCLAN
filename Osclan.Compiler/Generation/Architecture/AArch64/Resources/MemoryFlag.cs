using System;

namespace Osclan.Compiler.Generation.Architecture.AArch64.Resources;

[Flags]
public enum MemoryFlag
{
    MapFile = 0x0000,
    MapShared=0x0001,
    MapPrivate = 0x0002,
    MapAnon = 0x1000,
    Map32Bit = 0x0800,
}