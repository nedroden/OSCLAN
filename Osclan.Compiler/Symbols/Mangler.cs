using System;
using System.Collections.Generic;

namespace Osclan.Compiler.Symbols;

/// <summary>
/// Mangles names to unique identifiers.
/// </summary>
public static class Mangler
{
    /// <summary>
    /// Dictionary of names and their corresponding mangled names, both for caching purposes
    /// and to ensure that identical names are mangled to the same identifier.
    /// </summary>
    public static Dictionary<string, Guid> Guids { get; set; } = new();

    /// <summary>
    /// Mangles a name to a unique identifier.
    /// </summary>
    /// <param name="name">The name that is to be mangled.</param>
    /// <returns>The mangled identifier in the form of a GUID.</returns>
    public static string Mangle(string name)
    {
        name = name.ToLower();

        if (!Guids.TryGetValue(name, out Guid value))
        {
            value = Guid.NewGuid();
            Guids.Add(name, value);
        }

        return value.ToString().Replace("-", "_");
    }
}