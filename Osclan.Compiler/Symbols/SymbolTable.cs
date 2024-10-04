using System;
using System.Collections.Generic;
using System.Text.Json;
using Osclan.Compiler.Exceptions;

namespace Osclan.Compiler.Symbols;

public class SymbolTable
{
    private static readonly JsonSerializerOptions serializerOptions = new() { WriteIndented = true };

    public Guid Guid { get; } = Guid.NewGuid();

    public uint Depth { get; set; }

    public List<Variable> Variables { get; set; } = new();

    public List<Type> Types { get; set; } = new();

    public SymbolTable? Parent { get; set; }

    public SymbolTable(uint depth) => Depth = depth;

    public SymbolTable(SymbolTable parent)
    {
        Parent = parent;
        Depth = parent.Depth + 1;
    }

    public Variable AddVariable(Variable variable)
    {
        variable.UnmangledName = variable.Name;
        variable.Name = Mangler.Mangle(variable.Name);

        if (string.IsNullOrWhiteSpace(variable.Name))
        {
            throw new CompilerException("Variable name cannot be empty.");
        }

        if (Variables.Exists(v => v.Name == variable.Name))
        {
            throw new Exception($"Variable with name {variable.Name} already exists in this scope.");
        }

        Variables.Add(variable);

        return variable;
    }

    public Type AddType(Type type)
    {
        type.UnmangledName = type.Name;
        type.Name = Mangler.Mangle(type.Name);

        if (string.IsNullOrWhiteSpace(type.Name))
        {
            throw new CompilerException("Type name cannot be empty.");
        }

        if (Types.Exists(t => t.Name == type.Name))
        {
            throw new Exception($"Type with name {type.Name} already exists in this scope.");
        }

        Types.Add(type);

        return type;
    }

    public bool VariableInScope(string name) =>
        VariableInCurrentScope(name) || (Parent?.VariableInScope(name) ?? false);

    public bool VariableInCurrentScope(string name) =>
        Variables.Exists(v => v.Name == Mangler.Mangle(name));

    public bool TypeInScope(string name) =>
        TypeInCurrentScope(name) || (Parent?.TypeInScope(name) ?? throw new Exception($"Unresolved type {name}."));

    public bool TypeInCurrentScope(string name) =>
        Types.Exists(t => t.Name == Mangler.Mangle(name));

    public Variable ResolveVariable(string name) =>
        Variables.Find(v => v.Name == Mangler.Mangle(name)) ?? Parent?.ResolveVariable(name) ?? throw new Exception($"Unresolved variable {name}.");

    public Type ResolveType(string name) =>
        Types.Find(t => t.Name == Mangler.Mangle(name)) ?? Parent?.ResolveType(name) ?? throw new Exception($"Unresolved type {name}.");

    public Type ResolveTypeByMangledName(string name) =>
        Types.Find(t => t.Name == name) ?? Parent?.ResolveTypeByMangledName(name) ?? throw new Exception($"Unresolved type {name}.");

    public Type ResolveField(Type type, string remainingPath) =>
        throw new NotImplementedException();

    public void AddBuiltInTypes() =>
        Types.AddRange(
        [
            new Type { Name = Mangler.Mangle("int"), UnmangledName = "int", SizeInBytes = 4 },
            new Type { Name = Mangler.Mangle("uint"), UnmangledName = "uint", SizeInBytes = 4 },
            new Type { Name = Mangler.Mangle("string"), UnmangledName = "string", SizeInBytes = 4, IsPointer = true }
        ]);

    public override string ToString() =>
        JsonSerializer.Serialize(this, serializerOptions);
}