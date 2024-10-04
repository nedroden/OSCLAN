using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Osclan.Compiler.Exceptions;
using Osclan.Compiler.Meta;

namespace Osclan.Compiler.Symbols;

/// <summary>
/// Represents a symbol table for a given scope.
/// </summary>
public class SymbolTable
{
    private static readonly JsonSerializerOptions serializerOptions = new() { WriteIndented = true };

    /// <summary>
    /// The unique identifier of the symbol table, for identification purposes later in the compilation process.
    /// </summary>
    public Guid Guid { get; } = Guid.NewGuid();

    /// <summary>
    /// The depth of the symbol table, i.e., how many scopes deep it is.
    /// </summary>
    public uint Depth { get; set; }

    /// <summary>
    /// The variables in the symbol table.
    /// </summary>
    public List<Variable> Variables { get; set; } = new();

    /// <summary>
    /// The types in the symbol table.
    /// </summary>
    public List<Type> Types { get; set; } = new();

    /// <summary>
    /// The procedures in the symbol table.
    /// </summary>
    public List<Procedure> Procedures { get; set; } = new();

    /// <summary>
    /// The parent symbol table. If null, this is the global scope.
    /// </summary>
    public SymbolTable? Parent { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SymbolTable"/> class.
    /// </summary>
    /// <param name="depth">The depth of the scope.</param>
    public SymbolTable(uint depth) => Depth = depth;

    /// <summary>
    /// Initializes a new instance of the <see cref="SymbolTable"/> class. The depth is automatically
    /// computed based on the parent.
    /// </summary>
    /// <param name="parent">The parent scope.</param>
    public SymbolTable(SymbolTable parent)
    {
        Parent = parent;
        Depth = parent.Depth + 1;
    }

    /// <summary>
    /// Adds a variable to the symbol table and mangles it.
    /// </summary>
    /// <param name="variable">The variable to store in the table.</param>
    /// <returns>The original value, but with the mangled name.</returns>
    /// <exception cref="CompilerException">Thrown when the variable name is empty.</exception>
    /// <exception cref="SourceException">Thrown then the variable already exists in the current scope.</exception>
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
            throw new SourceException($"Variable with name {variable.Name} already exists in this scope.");
        }

        Variables.Add(variable);

        return variable;
    }

    /// <summary>
    /// Adds a type to the symbol table and mangles its name.
    /// </summary>
    /// <param name="type">The type to store in the table.</param>
    /// <returns>The original type, but with its name mangled.</returns>
    /// <exception cref="CompilerException">Thrown when the type name is empty.</exception>
    /// <exception cref="Exception">Thrown when the type has already been defined in the current scope.</exception>
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
            throw new SourceException($"Type with name {type.Name} already exists in this scope.");
        }

        Types.Add(type);

        return type;
    }

    /// <summary>
    /// Adds a procedure to the symbol table and mangles its name.
    /// </summary>
    /// <param name="procedure">The procedure to store.</param>
    /// <returns>The original procedure, but with its name mangled.</returns>
    /// <exception cref="CompilerException">Thrown when the name is empty.</exception>
    /// <exception cref="SourceException">Thrown when the procedure already exists.</exception>
    public Procedure AddProcedure(Procedure procedure)
    {
        procedure.UnmangledName = procedure.Name;
        procedure.Name = Mangler.Mangle(procedure.Name);

        if (string.IsNullOrWhiteSpace(procedure.Name))
        {
            throw new CompilerException("Procedure name cannot be empty.");
        }

        if (Procedures.Exists(p => p.Name == procedure.Name))
        {
            throw new SourceException($"Procedure with name {procedure.Name} already exists in this scope.");
        }

        Procedures.Add(procedure);

        return procedure;
    }

    /// <summary>
    /// Returns whether a variable is in scope, based on its _unmangled_ name.
    /// </summary>
    /// <param name="name">The name of the variable, unmangled.</param>
    /// <returns>Whether or not the variable lies within the scope hierarchy.</returns>
    public bool VariableInScope(string name) =>
        VariableInCurrentScope(name) || (Parent?.VariableInScope(name) ?? false);

    /// <summary>
    /// Returns whether a variable is in the current scope, based on its _unmangled_ name.
    /// </summary>
    /// <param name="name">The name of the variable, unmangled.</param>
    /// <returns>Whether or not a variable exists in the current scope, with the given name.</returns>
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

    public Type ResolveField(Type type, string remainingPath)
    {
        // TODO: Implement dynamic offsets (and hence remove the line below).
        remainingPath = remainingPath.Replace("$", string.Empty);

        var parts = remainingPath.Split("::");

        // Split element_0 into the field name and the offset.
        var split = parts.First().Split("_");
        var offset = split.Count() > 1 ? uint.Parse(split.Last()) : (uint?)null;
        var fieldName = Mangler.Mangle(split.First());

        // Was the field found at all?
        if (!type.Fields.ContainsKey(fieldName))
        {
            throw new Exception($"Field '{remainingPath}' not found in type {type.UnmangledName}.");
        }

        var field = type.Fields[fieldName];

        // Only arrays can (and must) be indexed.
        if (field.IsArray && offset is null)
        {
            throw new SourceException($"Array fields must be indexed. Type = '{field.UnmangledName}'");
        }
        else if (!field.IsArray && offset is not null)
        {
            throw new SourceException($"Non-array fields cannot be indexed. Type = '{field.UnmangledName}'");
        }

        return parts.Count() > 1 ? ResolveField(type, remainingPath) : field;
    }

    /// <summary>
    /// Adds the built-in types to the symbol table.
    /// </summary>
    public void AddBuiltInTypes() =>
        Types.AddRange(
        [
            new Type { Name = Mangler.Mangle("int"), UnmangledName = "int", SizeInBytes = 4 },
            new Type { Name = Mangler.Mangle("uint"), UnmangledName = "uint", SizeInBytes = 4 },
            new Type { Name = Mangler.Mangle("string"), UnmangledName = "string", SizeInBytes = 4, IsPointer = true }
        ]);

    /// <summary>
    /// Returns a string representation of the symbol table, in the form of JSON.
    /// </summary>
    /// <returns>A string representation of the symbol table.</returns>
    public override string ToString() =>
        JsonSerializer.Serialize(this, serializerOptions);
}