using System;
using System.Linq;

namespace Osclan.Compiler.Extensions;

public static class ByteExtensions
{
    public static string ToHex(this byte[] bytes) =>
        $"0x{string.Join("", BitConverter.ToString(bytes).Split('-').Reverse().ToList())}";
}