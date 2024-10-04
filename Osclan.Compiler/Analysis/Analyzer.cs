using System;
using System.Collections.Generic;
using System.Linq;
using Osclan.Compiler.Exceptions;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Symbols;

namespace Osclan.Compiler.Analysis;

public class Analyzer
{
    private readonly AstNode _ast;

    public Dictionary<Guid, SymbolTable> ArchivedSymbolTables { get; } = new();

    private SymbolTable _symbolTable;

    public Analyzer(AstNode ast)
    {
        _ast = ast;

        // Create the root symbol table
        _symbolTable = new SymbolTable(0);
        _symbolTable.AddBuiltInTypes();
    }

    private TypeCompatibility PeformAssignmentCheck(Symbols.Type from, Symbols.Type to)
    {
        if (from.Name == Mangler.Mangle("string") && (to.Name == Mangler.Mangle("int") || to.Name == Mangler.Mangle("uint")))
        {
            return TypeCompatibility.Illegal;
        }

        if (from.SizeInBytes > to.SizeInBytes)
        {
            return TypeCompatibility.LossOfInformation;
        }

        return TypeCompatibility.Ok;
    }

    private bool ValidEntryPointExists() => _ast.Children.Any(c =>
        c.Type == AstNodeType.Procedure
        && Mangler.Mangle(c.RawType?.Name ?? string.Empty) == Mangler.Mangle("uint")
        && c.RawType?.Size == 4
        && c.Modifiers.Contains(Modifier.Public));

    private void AnalyzeStructDeclaration(AstNode node)
    {
        // This field has already been declared through its parent. Or it's an anonymous struct.
        if (!string.IsNullOrWhiteSpace(node.Path) || string.IsNullOrWhiteSpace(node.Value))
        {
            return;
        }

        var structType = new TypeResolver().GetType(_symbolTable, node);

        _symbolTable.AddType(structType);
    }

    public void AnalyzeDeclaration(AstNode node)
    {

    }

    public void AnalyzeArgumentDeclaration(AstNode node)
    {

    }

    public void AnalyzeVariableReference(AstNode node)
    {
        // This might be a declaration. So just ignore it.
        if (string.IsNullOrWhiteSpace(node.Value))
        {
            return;
        }

        if (_symbolTable.VariableInScope(node.Value))
        {
            var variable = _symbolTable.ResolveVariable(node.Value);

            // The variable itself exists. But does the complete path to the field exist?
            if (!string.IsNullOrWhiteSpace(node.Path))
            {
                AnalyzeReferredField(node, variable);
            }

            return;
        }

        throw new Exception($"Unresolved variable with name '{node.Value}'");
    }

    public void AnalyzeReferredField(AstNode node, Variable variable)
    {
        // TODO: Implement this method.
        var varType = _symbolTable.ResolveType(variable.TypeName);
    }

    private void AnalyzeNode(AstNode node, SymbolTable symbolTable)
    {
        switch (node.Type)
        {
            case AstNodeType.Structure:
                AnalyzeStructDeclaration(node);
                break;
            case AstNodeType.Declaration:
                AnalyzeDeclaration(node);
                break;
            case AstNodeType.Argument:
                AnalyzeArgumentDeclaration(node);
                break;
            case AstNodeType.DynOffset:
            case AstNodeType.Variable:
                AnalyzeVariableReference(node);
                break;
        }

        foreach (var child in node.Children)
        {
            var createSubscope = child.Type == AstNodeType.Procedure || child.Type == AstNodeType.Structure;

            if (createSubscope)
            {
                _symbolTable = new SymbolTable(_symbolTable);
            }

            AnalyzeNode(child, _symbolTable);

            // If we have created a subscope, we should preserve it and pop it from the stack.
            if (createSubscope)
            {
                PopScope();
            }
        }
    }

    public AstNode Analyze()
    {
        if (!ValidEntryPointExists())
        {
            throw new Exception("No valid entry point found.");
        }

        AnalyzeNode(_ast, _symbolTable);
        PreserveScope();

        return _ast;
    }

    private void PopScope()
    {
        PreserveScope();
        var nextScope = _symbolTable.Parent;

        // Set the parent to null since at this point we no longer need the parent-child relation of scopes.
        _symbolTable.Parent = null;

        _symbolTable = nextScope ?? throw new Exception("Cannot pop the root symbol table.");
    }

    private void PreserveScope() =>
        ArchivedSymbolTables.Add(_symbolTable.Guid, _symbolTable);
}