using System;
using System.Collections.Generic;

namespace Osclan.Compiler.Symbols;

public static class Mangler
{
    public static Dictionary<string, Guid> Guids { get; set; } = new();

    public static string Mangle(string name)
    {
        name = name.ToLower();

        if (!Guids.ContainsKey(name))
        {
            Guids.Add(name, Guid.NewGuid());
        }

        return Guids[name].ToString();
    }
}