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
    /// Checks whether an entry point with the signature 'public [uint(4)]::main()' exists.
    /// </summary>
    /// <returns>True if a method exists with the proper signature.</returns>
    private static bool ValidEntryPointExists(AstNode ast) => ast.Children.Exists(c =>
        c.Type == AstNodeType.Procedure
        && Mangler.Mangle(c.RawType?.Name ?? string.Empty) == Mangler.Mangle(BuiltInType.Uint)
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

        var structType = TypeService.GetType(_symbolTable, node);
        TypeService.IndexType(structType);

        // If possible, add the type to the parent symbol table so that it is usable by variables in the same scope.
        (_symbolTable.Parent ?? _symbolTable).AddType(structType with { IsPointer = true });
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

        if (type.SizeInBytes == 8)
        {
            Console.WriteLine($"Type = {type.UnmangledName} with Size = {type.SizeInBytes} bytes");
        }

        var variableType = TypeService.GetType(_symbolTable, variableNode);
        var assignmentStatus = TypeService.VerifyAssignmentCompatibility(variableType, type);

        switch (assignmentStatus)
        {
            case TypeCompatibility.Illegal:
                Developer.DumpObject(variableType);
                Developer.DumpObject(type);
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

        // Both variable and value refer to the same symbol table (for now)
        variableNode.Scope = _symbolTable.Guid;
        valueNode.Scope = _symbolTable.Guid;

        // TODO: check if commented code can indeed be removed
        //if (valueNode.Type == AstNodeType.Allocation)
        //{
        valueNode.Meta[MetaDataKey.VariableName] = variableNode.Value;
        //}
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
            else if (child.Type == AstNodeType.Ret)
            {
                // Variables might not yet have been declared, so check this later.
                child.Meta[MetaDataKey.ProcedureName] = procedure.Name;

                // But we can check if this leads to unreachable code.
                if (node.Children[^1] != child)
                {
                    throw new SourceException($"Unreachable code detected in '{procedure.Name}'");
                }
            }
        }

        // If the return type is void, there must not be a return value.
        if (procedure.ReturnType is null && node.Children.Exists(c => c.Type == AstNodeType.Ret && c.Children.Count > 0))
        {
            throw new SourceException($"Procedure '{procedure.Name}' cannot return a value.");
        }
        
        // Add an implicit return statement, if needed, since all procedures should have an epilog
        if (node.Children.Count == 0 || node.Children[^1].Type != AstNodeType.Ret)
        {
            node.Children.Add(new AstNode { Type = AstNodeType.Ret });
        }

        (_symbolTable.Parent ?? _symbolTable).AddProcedure(procedure);
    }

    /// <summary>
    /// Performs a return type check. This method is called when a procedure is being analyzed and a 'ret' statement is encountered.
    /// </summary>
    /// <param name="returnNode">The return statement node.</param>
    /// <exception cref="SourceException">Thrown when the return value is invalid.</exception>
    private void PerformReturnTypeCheck(AstNode returnNode)
    {
        bool isVoid = returnNode.Children.Count == 0;

        var procedure = _symbolTable.ResolveProcedure(returnNode.Meta[MetaDataKey.ProcedureName]);

        // If the procedure's return type is void and the return statement is void, then we're good.
        if (procedure.ReturnType is null && isVoid)
        {
            return;
        }

        if (procedure.ReturnType is not null && isVoid)
        {
            throw new SourceException($"Procedure '{procedure.UnmangledName}' must return a value.");
        }

        var returnValueNode = returnNode.Children[0];

        // Return value is constant
        if (returnValueNode.RawType?.Name is not null)
        {
            var resolvedType = _symbolTable.ResolveType(returnValueNode.RawType?.Name ?? string.Empty);

            if (isVoid || TypeService.VerifyAssignmentCompatibility(resolvedType, procedure.ReturnType!, true) != TypeCompatibility.Ok)
            {
                throw new SourceException($"Invalid return type for procedure '{procedure.UnmangledName}'.");
            }
        }

        // Return value is a variable
        if (returnValueNode.Children.Count == 1 && returnValueNode.Children[0].Type == AstNodeType.Variable)
        {
            var variable = _symbolTable.ResolveVariable(returnValueNode.Children[0].Value ?? throw new CompilerException("Variable name cannot be empty."));

            if (isVoid || TypeService.VerifyAssignmentCompatibility(_symbolTable.ResolveTypeByMangledName(variable.TypeName), procedure.ReturnType!, true) != TypeCompatibility.Ok)
            {
                throw new SourceException($"Invalid return type for procedure '{procedure.UnmangledName}'.");
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
        if (typeName is null || string.IsNullOrWhiteSpace(typeName.Name) || node.Children.Count > 0)
        {
            return;
        }

        var type = _symbolTable.ResolveType(typeName.Name ?? throw new CompilerException("Type name cannot be empty."));

        var variable = new Variable(node.Value ?? throw new CompilerException("Argument name cannot be empty."))
        {
            TypeName = type.Name,
            IsPointer = type.IsPointer,
            SizeInBytes = type.SizeInBytes,
        };

        _symbolTable.AddVariable(variable);
        node.Scope = _symbolTable.Guid;
    }

    /// <summary>
    /// Checks whether a referred variable actually exists in the scope hierarchy.
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

        if (!_symbolTable.VariableInScope(node.Value))
        {
            throw new SourceException($"Unresolved variable with name '{node.Value}'");
        }

        var variable = _symbolTable.ResolveVariable(node.Value);
        node.Scope = _symbolTable.Guid;

        // The variable itself exists. But does the complete path to the field exist?
        if (!string.IsNullOrWhiteSpace(node.Path))
        {
            AnalyzeReferredField(node, variable);
        }

        // Is this variable being used as a procedure argument?
        if (node.Meta.TryGetValue(MetaDataKey.RequiredTypeMatch, out string? requiredTypeMatch))
        {
            var requiredType = _symbolTable.ResolveTypeByMangledName(requiredTypeMatch);

            if (TypeService.VerifyAssignmentCompatibility(_symbolTable.ResolveTypeByMangledName(variable.TypeName), requiredType, true) != TypeCompatibility.Ok)
            {
                throw new SourceException($"Invalid type for variable '{variable.Name}'. Expected {requiredType.UnmangledName}, got {_symbolTable.ResolveTypeByMangledName(variable.TypeName).UnmangledName}.");
            }
        }
    }

    /// <summary>
    /// Analyzes a referred field. For example, in the case of 'list::elements(0)::first-name : some-value',
    /// elements(0)::first-name represents the referred field. Most of the validation is done in the symbol table.
    /// 
    /// This method (through the symbol table) also ensures that any indexed fields are properly addressed.
    /// </summary>
    /// <param name="node">The input node.</param>
    /// <param name="variable">The variable that should contain the referred fields.</param>
    private void AnalyzeReferredField(AstNode node, Variable variable) =>
        _symbolTable.ResolveField(_symbolTable.ResolveTypeByMangledName(variable.TypeName), node.Path ?? string.Empty);

    /// <summary>
    /// Analyzes a procedure call. This method is called when a procedure call is encountered in the AST.
    /// </summary>
    /// <param name="node">The node representin the procedure call.</param>
    /// <exception cref="CompilerException">Thrown when the procedure call node does not contain a value.</exception>
    /// <exception cref="SourceException">Thrown when the procedure call is invalid.</exception>
    private void AnalyzeProcedureCall(AstNode node)
    {
        var procedure = _symbolTable.ResolveProcedure(node.Value ?? throw new CompilerException("Procedure name cannot be empty."));
        var arguments = node.Children.Where(child => child.Type == AstNodeType.Argument).ToList();

        if (procedure.Parameters.Count != arguments.Count)
        {
            throw new SourceException($"Invalid number of arguments for procedure '{procedure.UnmangledName}'. Expected {procedure.Parameters.Count}, got {arguments.Count}.");
        }

        var pairs = procedure.Parameters.Zip(arguments, (p, a) => new { Parameter = p, Argument = a });
        foreach (var pair in pairs)
        {
            var parameter = pair.Parameter;
            var argument = pair.Argument.Children[0];

            // We don't know what the type is yet. But we know what it should be.
            argument.Meta[MetaDataKey.RequiredTypeMatch] = parameter.TypeName;
        }
    }

    private void AnalyzeString(AstNode node)
    {
        node.TypeInformation = _symbolTable.ResolveType(BuiltInType.String);

        // String is used as procedure argument.
        if (node.Meta.TryGetValue(MetaDataKey.RequiredTypeMatch, out string? requiredTypeMatch))
        {
            var requiredType = _symbolTable.ResolveTypeByMangledName(requiredTypeMatch);

            if (TypeService.VerifyAssignmentCompatibility(node.TypeInformation, requiredType, true) != TypeCompatibility.Ok)
            {
                throw new SourceException($"Expected {requiredType.UnmangledName}, got string.");
            }
        }
    }

    private void AnalyzeScalar(AstNode node)
    {
        node.TypeInformation = _symbolTable.ResolveType(BuiltInType.Int);

        // Scalar is used as procedure argument.
        if (node.Meta.TryGetValue(MetaDataKey.RequiredTypeMatch, out var requiredTypeMatch))
        {
            var requiredType = _symbolTable.ResolveTypeByMangledName(requiredTypeMatch);

            if (TypeService.VerifyAssignmentCompatibility(node.TypeInformation, requiredType, true) != TypeCompatibility.Ok)
            {
                throw new SourceException($"Expected {requiredType.UnmangledName}, got int.");
            }
        }
    }

    /// <summary>
    /// Analyzes an allocation. Basically, it checks the type and updates the node. Since allocations are.
    /// </summary>
    /// <param name="node">The input node.</param>
    private void AnalyzeAllocation(AstNode node) =>
        node.TypeInformation =
            _symbolTable.ResolveType(node.RawType?.Name ?? throw new CompilerException("Type name cannot be empty."))
                with { IsPointer = true };

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
            case AstNodeType.Ret:
                PerformReturnTypeCheck(node);
                break;
            case AstNodeType.ProcedureCall:
                AnalyzeProcedureCall(node);
                break;
            case AstNodeType.String:
                AnalyzeString(node);
                break;
            case AstNodeType.Scalar:
                AnalyzeScalar(node);
                break;
            case AstNodeType.Allocation:
                AnalyzeAllocation(node);
                break;
        }

        foreach (var child in node.Children)
        {
            var createSubScope = child.Type is AstNodeType.Procedure or AstNodeType.Structure;

            // If we have a procedure or a structure, we should create a sub scope, but only if they have any children.
            if (createSubScope && child.Children.Count != 0)
            {
                _symbolTable = new SymbolTable(parent: symbolTable);
            }

            AnalyzeNode(child, _symbolTable);

            // If we have created a sub scope, we should preserve it and pop it from the stack.
            if (createSubScope)
            {
                PopScope();
            }
        }
    }

    /// <summary>
    /// Starts the semantic analysis.
    /// </summary>
    /// <param name="ast">The root node of the AST.</param>
    /// <returns>A processed version of the AST.</returns>
    /// <exception cref="Exception">Thrown when certain conditions, such as the existence of an entry point, are not met.</exception>
    public AnalyzerResult Analyze(AstNode ast)
    {
        if (!ValidEntryPointExists(ast))
        {
            throw new SourceException("No valid entry point found.");
        }

        AnalyzeNode(ast, _symbolTable);
        PreserveScope();

        return new AnalyzerResult(ast, ArchivedSymbolTables);
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