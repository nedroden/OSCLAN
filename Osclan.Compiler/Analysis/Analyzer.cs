using System;
using System.Collections.Generic;
using System.Linq;
using Osclan.Compiler.Analysis.Abstractions;
using Osclan.Compiler.Exceptions;
using Osclan.Compiler.Meta;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Symbols;

namespace Osclan.Compiler.Analysis;

/// <summary>
/// Performs semantic analysis on the AST.
/// </summary>
public class Analyzer : IAnalyzer
{
    /// <summary>
    /// A dictionary of all the symbol tables that have been created during the analysis. Used
    /// in the code generation process to determine memory allocation for variables.
    /// </summary>
    public Dictionary<Guid, SymbolTable> ArchivedSymbolTables { get; } = new();

    private SymbolTable _symbolTable;

    /// <summary>
    /// Initializes a new instance of the <see cref="Analyzer"/> class.
    /// </summary>
    public Analyzer()
    {
        // Create the root symbol table
        _symbolTable = new SymbolTable(0);
        _symbolTable.AddBuiltInTypes();
    }

    /// <summary>
    /// Checks whether or not an entry point with the signature 'public [uint(4)]::main()' exists.
    /// </summary>
    /// <returns>True if a method exists with the proper signature.</returns>
    private bool ValidEntryPointExists(AstNode ast) => ast.Children.Any(c =>
        c.Type == AstNodeType.Procedure
        && Mangler.Mangle(c.RawType?.Name ?? string.Empty) == Mangler.Mangle("uint")
        && c.RawType?.Size == 4
        && c.Modifiers.Contains(Modifier.Public));

    /// <summary>
    /// Analyzes a struct declaration. If the declaration is valid, then it is registered
    /// as a type.
    /// </summary>
    /// <param name="node">The declaration node.</param>
    private void AnalyzeStructDeclaration(AstNode node)
    {
        // This field has already been declared through its parent. Or it's an anonymous struct.
        if (!string.IsNullOrWhiteSpace(node.Path) || string.IsNullOrWhiteSpace(node.Value))
        {
            return;
        }

        var typeService = new TypeService();
        var structType = typeService.GetType(_symbolTable, node);
        typeService.IndexType(structType);

        // If possible, add the type to the parent symbol table so that it is usable by variables in the same scope.
        (_symbolTable.Parent ?? _symbolTable).AddType(structType);
    }

    /// <summary>
    /// Analyzes the declaration of a variable.
    /// </summary>
    /// <param name="node">The variable node.</param>
    private void AnalyzeVariableDeclaration(AstNode node)
    {
        if (node.Children.Count != 2)
        {
            throw new CompilerException("Expected declaration node to have two children.");
        }

        var variableNode = node.Children[0];
        var valueNode = node.Children[1];

        var type = _symbolTable.ResolveType(variableNode.RawType?.Name ?? string.Empty);

        var typeService = new TypeService();
        var variableType = typeService.GetType(_symbolTable, variableNode);
        var assignmentStatus = typeService.VerifyAssignmentCompatibility(variableType, type);

        // TODO: Improve the error messages, we want a position here.
        switch (assignmentStatus)
        {
            case TypeCompatibility.Illegal:
                throw new SourceException($"Invalid assignment of value to variable '{variableNode.Value}'.");
            case TypeCompatibility.LossOfInformation:
                Console.WriteLine($"Warning: possible loss of information for variable '{variableNode.Value}'.");
                break;
        }

        _symbolTable.AddVariable(new Variable(variableNode.Value ?? throw new CompilerException("Variable name cannot be empty."))
        {
            TypeName = type.Name,
            IsPointer = type.IsPointer,
            SizeInBytes = valueNode.RawType?.Size > 0 ? valueNode.RawType.Size : type.SizeInBytes
        });
    }

    /// <summary>
    /// Analyzes a procedure declaration. If valid, it is added to the symbol table.
    /// </summary>
    /// <param name="node">The procedure node.</param>
    /// <exception cref="CompilerException">Thrown when the procedure name or any argument name is empty.</exception>
    private void AnalyzeProcedureDeclaration(AstNode node)
    {
        var procedure = new Procedure(node.Value ?? throw new CompilerException("Procedure name cannot be empty."))
        {
            ReturnType = node.RawType?.Name != "void" ? _symbolTable.ResolveType(node.RawType?.Name ?? string.Empty) : null
        };

        foreach (var child in node.Children)
        {
            if (child.Type == AstNodeType.Argument)
            {
                var type = _symbolTable.ResolveType(child.RawType?.Name ?? string.Empty);

                procedure.Parameters.Add(new Variable(child.Value ?? throw new CompilerException("Argument name cannot be empty."))
                {
                    TypeName = type.Name,
                    IsPointer = type.IsPointer,
                    SizeInBytes = type.SizeInBytes
                });
            }
        }

        _symbolTable.AddProcedure(procedure);
    }

    /// <summary>
    /// Performs a return type check. This method is called when a procedure is being analyzed and a 'ret' statement is encountered.
    /// </summary>
    /// <param name="procedure">The procedure in which the return statement is located.</param>
    /// <param name="returnNode">The return statement node.</param>
    /// <exception cref="SourceException">Thrown when the return value is invalid.</exception>
    private void PerformReturnTypeCheck(Procedure procedure, AstNode returnNode)
    {
        bool isVoid = returnNode.Children.Count == 0;

        // If the procedure's return type is void and the return statement is void, then we're good.
        if (procedure.ReturnType is null && isVoid)
        {
            return;
        }

        var typeService = new TypeService();
        var returnValueNode = returnNode.Children.First();

        // Return value is constant
        if (returnValueNode.RawType?.Name is not null)
        {
            var resolvedType = _symbolTable.ResolveType(returnValueNode.RawType?.Name ?? string.Empty);

            if (isVoid || typeService.VerifyAssignmentCompatibility(resolvedType, procedure.ReturnType!) != TypeCompatibility.Ok)
            {
                throw new SourceException("Invalid return type.");
            }
        }

        // Return value is a variable
        if (returnValueNode.Children.Count == 1 && returnValueNode.Children[0].Type == AstNodeType.Variable)
        {
            var variable = _symbolTable.ResolveVariable(returnValueNode.Children[0].Value ?? throw new CompilerException("Variable name cannot be empty."));

            if (isVoid || typeService.VerifyAssignmentCompatibility(_symbolTable.ResolveType(variable.TypeName), procedure.ReturnType!) != TypeCompatibility.Ok)
            {
                throw new SourceException("Invalid return type.");
            }
        }
    }

    /// <summary>
    /// Analyzes an argument declaration. This method is called when a procedure is being analyzed and
    /// ensures that the arguments of a procedure are properly declared. Arguments are declared as variables,
    /// since, in practice, they are.
    /// </summary>
    /// <param name="node">The argument node.</param>
    private void AnalyzeArgumentDeclaration(AstNode node)
    {
        var typeName = node.RawType;

        // This is an argument being passed rather than declared, or it's a directive argument.
        if (typeName is null || string.IsNullOrWhiteSpace(typeName?.Name) || node.Children.Count > 0)
        {
            return;
        }

        var type = _symbolTable.ResolveType(typeName?.Name ?? throw new CompilerException("Type name cannot be empty."));

        var variable = new Variable(node.Value ?? throw new CompilerException("Argument name cannot be empty."))
        {
            TypeName = type.Name,
            IsPointer = type.IsPointer,
            SizeInBytes = type.SizeInBytes
        };

        _symbolTable.AddVariable(variable);
    }

    /// <summary>
    /// Checks whether not not a referred variable actually exists in the scope hierarchy.
    /// </summary>
    /// <param name="node">The variable node.</param>
    /// <exception cref="Exception">Thrown when the variable does not exist in the scope hierarchy.</exception>
    private void AnalyzeVariableReference(AstNode node)
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

        throw new SourceException($"Unresolved variable with name '{node.Value}'");
    }

    /// <summary>
    /// Analyzes a referred field. For example, in the case of 'list::elements(0)::first-name : some-value',
    /// elements(0)::first-name represents the referred field. Most of the validation is done in the symbol table.
    /// 
    /// This method (through the symbol table) also ensures that any indexed fields are properly addressed.
    /// </summary>
    /// <param name="node">The input node.</param>
    /// <param name="variable">The variable that should contain the referred fields.</param>
    private void AnalyzeReferredField(AstNode node, Variable variable)
    {
        var varType = _symbolTable.ResolveTypeByMangledName(variable.TypeName);
        _symbolTable.ResolveField(varType, node.Path ?? string.Empty);
    }

    /// <summary>
    /// Analyzes a single node. This method is called recursively for each node in the AST.
    /// </summary>
    /// <param name="node">The input node.</param>
    /// <param name="symbolTable">A symbol table representing the current scope.</param>
    private void AnalyzeNode(AstNode node, SymbolTable symbolTable)
    {
        switch (node.Type)
        {
            case AstNodeType.Structure:
                AnalyzeStructDeclaration(node);
                break;
            case AstNodeType.Declaration:
                AnalyzeVariableDeclaration(node);
                break;
            case AstNodeType.Argument:
                AnalyzeArgumentDeclaration(node);
                break;
            case AstNodeType.Procedure:
                AnalyzeProcedureDeclaration(node);
                break;
            case AstNodeType.DynOffset:
            case AstNodeType.Variable:
                AnalyzeVariableReference(node);
                break;
        }

        foreach (var child in node.Children)
        {
            var createSubscope = child.Type == AstNodeType.Procedure || child.Type == AstNodeType.Structure;

            // If we have a procedure or a structure, we should create a subscope, but only if they have any children.
            if (createSubscope && child.Children.Any())
            {
                _symbolTable = new SymbolTable(parent: symbolTable);
            }

            AnalyzeNode(child, _symbolTable);

            // If we have created a subscope, we should preserve it and pop it from the stack.
            if (createSubscope)
            {
                PopScope();
            }
        }
    }

    /// <summary>
    /// Starts the semantic analysis.
    /// </summary>
    /// 
    /// <returns>A processed version of the AST.</returns>
    /// <exception cref="Exception">Thrown when certain conditions, such as the existence of an entry point, are not met.</exception>
    public AstNode Analyze(AstNode ast)
    {
        if (!ValidEntryPointExists(ast))
        {
            throw new SourceException("No valid entry point found.");
        }

        AnalyzeNode(ast, _symbolTable);
        PreserveScope();

        return ast;
    }

    /// <summary>
    /// Pops the current scope and removes the association with its parent.
    /// </summary>
    /// <exception cref="Exception">Thrown when the scope being popped has no parent (= is a root scope).</exception>
    private void PopScope()
    {
        PreserveScope();
        var nextScope = _symbolTable.Parent;

        // Set the parent to null since at this point we no longer need the parent-child relation of scopes.
        _symbolTable.Parent = null;

        _symbolTable = nextScope ?? throw new CompilerException("Cannot pop the root symbol table.");
    }

    /// <summary>
    /// Preserves the current scope by adding it to the dictionary of archived symbol tables. Its
    /// guid is used as the key.
    /// </summary>
    private void PreserveScope() =>
        ArchivedSymbolTables.Add(_symbolTable.Guid, _symbolTable);
}