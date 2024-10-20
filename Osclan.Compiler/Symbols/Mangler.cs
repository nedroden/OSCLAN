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
    public static Dictionary<string, string> Cache { get; set; } = [];

    /// <summary>
    /// Mangles a name to a unique identifier.
    /// </summary>
    /// <param name="name">The name that is to be mangled.</param>
    /// <returns>The mangled identifier.</returns>
    public static string Mangle(string name)
    {
        name = name.ToLower();

        if (!Cache.TryGetValue(name, out var value))
        {
            value = $"__u{name.ToLower().Replace("-", "_")}";
            Cache.Add(name, value);
        }

        return value;
    }
}