using System;
using System.Text.Json;

namespace Osclan.Compiler.Meta;

public static class Developer
{
    public static JsonSerializerOptions SerializationOptions { get; } = new()
    {
        WriteIndented = true
    };

    public static void DumpObject<T>(T obj) where T : class =>
        Console.WriteLine(JsonSerializer.Serialize(obj, SerializationOptions));
}