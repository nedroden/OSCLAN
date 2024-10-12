using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Osclan.Compiler.Exceptions;
using Osclan.Compiler.Parsing.Abstractions;
using Osclan.Compiler.Tokenization;

namespace Osclan.Compiler.Parsing;

public class Parser : IParser
{
    private List<Token> _tokens = [];
    private uint _currentTokenIndex;
    private Token CurrentToken => _tokens[(int)_currentTokenIndex];
    private uint _numberOfTokens;

    private void ThrowUnexpectedTokenException(TokenType expected) =>
        throw new SourceException($"Unexpected token '{Enum.GetName(typeof(TokenType), CurrentToken.Type)}'. Expected '{Enum.GetName(typeof(TokenType), expected)}' at {CurrentToken.Position}");

    private Token Consume(TokenType tokenType)
    {
        if (CurrentToken.Type != tokenType)
        {
            ThrowUnexpectedTokenException(tokenType);
        }

        var oldToken = CurrentToken;

        if (_currentTokenIndex < _numberOfTokens)
        {
            _currentTokenIndex++;
        }

        return oldToken;
    }

    private void ConsumeMany(params TokenType[] tokenTypes)
    {
        foreach (var tokenType in tokenTypes)
        {
            Consume(tokenType);
        }
    }

    private Token ConsumeAndGetLast(params TokenType[] tokenTypes)
    {
        Token? last = null;

        foreach (var tokenType in tokenTypes)
        {
            last = Consume(tokenType);
        }

        return last ?? throw new CompilerException("Invalid number of token types.");
    }

    private bool IsAt(TokenType tokenType) =>
        CurrentToken.Type == tokenType;

    private bool IsAtSequence(params TokenType[] tokenTypes)
    {
        for (var i = 0; i < tokenTypes.Length; i++)
        {
            if (_currentTokenIndex + i > _numberOfTokens)
            {
                return false;
            }

            if (_tokens[(int)_currentTokenIndex + i].Type != tokenTypes[i])
            {
                return false;
            }
        }

        return true;
    }

    private AstNode ParseDirective()
    {
        Consume(TokenType.Dot);

        var identifier = Consume(TokenType.Identifier);

        var directive = new AstNode
        {
            Type = AstNodeType.Directive,
            Value = identifier.Value ?? throw new CompilerException("Directive name must have a value.")
        };

        switch (directive.Value)
        {
            case "import":
            case "module":
            case "mangler":
                var argument = Consume(TokenType.String);
                directive.Children.Add(new AstNode { Type = AstNodeType.Argument, Value = argument.Value ?? throw new CompilerException("Argument must have a value.") });
                break;
            default:
                throw new SourceException($"Unknown compiler directive '{directive.Value}'");
        }

        return directive;
    }

    private AstNode ParseDeclaration()
    {
        Consume(TokenType.Declare);

        var modifiers = new List<Modifier>();

        while (IsAt(TokenType.Modifier))
        {
            var modifier = Consume(TokenType.Modifier);

            // Find the enum value belonging to the modifier and add it to the list of modifiers.
            modifier.Value = char.ToUpper(modifier.Value![0]) + modifier.Value[1..];
            modifiers.Add(Enum.Parse<Modifier>(modifier.Value ?? throw new CompilerException("Modifier must have a value.")));
        }

        var declarationNode = CurrentToken switch
        {
            { Type: TokenType.Struct } => ParseStructure(),
            { Type: TokenType.Lbracket } => ParseProcedure(),
            _ => throw new SourceException($"Token type '{CurrentToken.Type}' cannot be used in a declaration.")
        };

        declarationNode.Modifiers.AddRange(modifiers);

        return declarationNode;
    }

    private AstNode ParseStructure() =>
        ParseStructure(implicitStructure: false);

    private AstNode ParseStructure(bool implicitStructure)
    {
        var structure = new AstNode { Type = AstNodeType.Structure };

        if (!implicitStructure)
        {
            Consume(TokenType.Struct);
        }

        var identifier = Consume(TokenType.Identifier);
        structure.Value = identifier.Value ?? throw new CompilerException("Structure name must have a value.");

        if (implicitStructure)
        {
            Consume(TokenType.Begin);
        }

        while (!IsAt(TokenType.Eof) && !IsAt(TokenType.End))
        {
            var field = new AstNode { Type = AstNodeType.Field };
            field.RawType = ParseType();

            if (field.RawType.Name.ToLower() == "struct")
            {
                // Preserve the type information, since we will need it later (for occurrences)
                var type = field.RawType;

                // At this point we always have an implicit structure
                field = ParseStructure(true);
                field.Type = AstNodeType.Field;
                field.RawType = type;
                structure.Children.Add(field);

                continue;
            }

            field.Value = Consume(TokenType.Identifier).Value;
            structure.Children.Add(field);
        }

        Consume(TokenType.End);

        return structure;
    }

    private AstNode ParseProcedure()
    {
        var node = new AstNode { Type = AstNodeType.Procedure };

        // The raw type of a procedure is its return type
        var returnType = ParseType();
        node.RawType = returnType;

        Consume(TokenType.DoubleColon);

        // Procedure name
        node.Value = Consume(TokenType.Identifier).Value;

        // Arguments
        Consume(TokenType.Lparen);

        while (!IsAt(TokenType.Rparen) && !IsAt(TokenType.Eof))
        {
            if (IsAt(TokenType.Comma))
            {
                Consume(TokenType.Comma);
            }

            var argument = new AstNode { Type = AstNodeType.Argument };

            // Argument type and name
            argument.RawType = ParseType();
            argument.Value = Consume(TokenType.Identifier).Value;

            node.Children.Add(argument);
        }

        Consume(TokenType.Rparen);

        // Procedure body
        node.Children.AddRange(ParseCompound());

        Consume(TokenType.End);

        return node;
    }

    private List<AstNode> ParseCompound()
    {
        var statements = new List<AstNode>();

        while (!IsAt(TokenType.Eof) && !IsAt(TokenType.End))
        {
            switch (CurrentToken.Type)
            {
                case TokenType.Declare:
                    statements.Add(ParseAssignment(isDeclaration: true, isAnon: false, skipLeftOperand: false));
                    break;
                case TokenType.Ret:
                    statements.Add(ParseReturnStatement());
                    break;
                case TokenType.Print:
                    statements.Add(ParsePrintStatement());
                    break;
                case TokenType.Identifier:
                    statements.Add(ParseAssignment(isDeclaration: false, isAnon: false, skipLeftOperand: false));
                    break;
                case TokenType.Call:
                    statements.Add(ParseProcedureCall());
                    break;
                default:
                    throw new SourceException($"Unexpected token '{CurrentToken.Value}' at {CurrentToken.Position}");
            }
        }

        return statements;
    }

    private AstNode ParseReturnStatement()
    {
        Consume(TokenType.Ret);

        var children = new List<AstNode>();

        if (CurrentToken is { Type: TokenType.Identifier or TokenType.String or TokenType.Number })
        {
            children.AddRange([ParseAssignment(false, false, true)]);
        }

        return new AstNode
        {
            Type = AstNodeType.Ret,
            Children = children
        };
    }

    private AstNode ParsePrintStatement()
    {
        Consume(TokenType.Print);

        return new AstNode
        {
            Type = AstNodeType.Print,
            Children = { ParseAssignment(false, false, true) }
        };
    }

    private AstNode ParseAutoMemoryAllocation()
    {
        Consume(TokenType.Init);

        var allocationNode = new AstNode
        {
            Type = AstNodeType.Allocation,
            RawType = ParseType()
        };

        return allocationNode;
    }

    private AstNode ParseAssignment(bool isDeclaration, bool isAnon, bool skipLeftOperand)
    {
        var assignmentNode = new AstNode { Type = AstNodeType.Assignment };

        if (!skipLeftOperand)
        {
            var leftOperand = new AstNode { Type = AstNodeType.Variable };

            if (isDeclaration)
            {
                Consume(TokenType.Declare);

                // Variable type. Note that if this is a declaration and the type is missing, an
                // exception will be thrown later.
                var type = ParseType();
                leftOperand.RawType = type;
                assignmentNode.Type = AstNodeType.Declaration;

                // The name of the variable
                leftOperand.Value = Consume(TokenType.Identifier).Value;
            }
            else
            {
                leftOperand = ParseVariable();
            }

            if (isAnon)
            {
                leftOperand.Type = AstNodeType.Field;
            }

            assignmentNode.Children.Add(leftOperand);
            Consume(TokenType.Colon);
        }

        AstNode rightOperand = CurrentToken.Type switch
        {
            TokenType.Init => ParseAutoMemoryAllocation(),
            TokenType.Number => new AstNode { Type = AstNodeType.Scalar, Value = Consume(TokenType.Number).Value },
            TokenType.String => new AstNode { Type = AstNodeType.String, Value = Consume(TokenType.String).Value },
            TokenType.Identifier => new AstNode { Type = AstNodeType.Variable, Value = Consume(TokenType.Identifier).Value },
            TokenType.Asterisk => new AstNode { Type = AstNodeType.Variable, Value = ConsumeAndGetLast(TokenType.Asterisk, TokenType.Identifier).Value, IsDereferenced = true },
            TokenType.Declare => ParseAnonDeclaration(),
            TokenType.Call => ParseProcedureCall(),
            _ => throw new SourceException($"Unexpected token '{CurrentToken.Value}' at {CurrentToken.Position}")
        };

        assignmentNode.Children.Add(rightOperand);

        return assignmentNode;
    }

    private AstNode ParseProcedureCall()
    {
        var node = new AstNode { Type = AstNodeType.ProcedureCall };

        Consume(TokenType.Call);
        node.Value = Consume(TokenType.Identifier).Value;

        // Arguments
        Consume(TokenType.Lparen);

        while (!IsAt(TokenType.Rparen) && !IsAt(TokenType.Eof))
        {
            var argument = ParseAssignment(false, false, true);
            argument.Type = AstNodeType.Argument;
            node.Children.Add(argument);

            if (!IsAt(TokenType.Rparen))
            {
                Consume(TokenType.Comma);
            }
        }

        Consume(TokenType.Rparen);

        return node;
    }

    private AstNode ParseAnonDeclaration()
    {
        var declarationNode = new AstNode { Type = AstNodeType.Structure };

        ConsumeMany(TokenType.Declare, TokenType.Anon);

        while (!IsAt(TokenType.Eof) && !IsAt(TokenType.End))
        {
            // Regular variable assignment
            if (IsAtSequence(TokenType.Identifier, TokenType.Colon))
            {
                declarationNode.Children.Add(ParseAssignment(isDeclaration: false, isAnon: true, skipLeftOperand: false));
                continue;
            }

            // Shorthand notation, e.g., (first-name : first-name) -> (first-name)
            if (IsAtSequence(TokenType.Identifier, TokenType.Identifier) || IsAtSequence(TokenType.Identifier, TokenType.End))
            {
                var assignmentNode = new AstNode { Type = AstNodeType.Assignment };
                var identifier = Consume(TokenType.Identifier);

                assignmentNode.Children.AddRange(new List<AstNode>
                {
                    new AstNode { Type = AstNodeType.Field, Value = identifier.Value },
                    new AstNode { Type = AstNodeType.Variable, Value = identifier.Value }
                });
                declarationNode.Children.Add(assignmentNode);

                continue;
            }

            ThrowUnexpectedTokenException(TokenType.Identifier);
        }

        Consume(TokenType.End);

        return declarationNode;
    }

    private RawType ParseType()
    {
        Consume(TokenType.Lbracket);

        if (!IsAt(TokenType.Identifier) && !IsAt(TokenType.Struct))
        {
            throw new SourceException("Expected identifier after '['");
        }

        var name = IsAt(TokenType.Identifier) ? Consume(TokenType.Identifier).Value : "struct";
        var type = new RawType { Name = name ?? throw new CompilerException("Type name must have a value: " + CurrentToken.Position) };

        if (IsAt(TokenType.Struct))
        {
            Consume(TokenType.Struct);
        }

        // Length of the type (N as in string(N))
        if (IsAt(TokenType.Lparen))
        {
            Consume(TokenType.Lparen);
            var length = Consume(TokenType.Number);
            type.Size = uint.Parse(length.Value ?? throw new CompilerException("Type length must have a value."));
            Consume(TokenType.Rparen);
        }

        // Do we already know that this is a pointer?
        if (IsAt(TokenType.Asterisk))
        {
            Consume(TokenType.Asterisk);
            type.IsPointer = true;
        }

        Consume(TokenType.Rbracket);

        return type;
    }

    private AstNode ParseVariable()
    {
        var variable = new AstNode { Type = AstNodeType.Variable };
        var sb = new StringBuilder();

        var identifier = Consume(TokenType.Identifier);
        variable.Value = identifier.Value ?? throw new CompilerException("Variable name must have a value.");

        while (IsAt(TokenType.DoubleColon))
        {
            Consume(TokenType.DoubleColon);
            var field = Consume(TokenType.Identifier);
            sb.Append(sb.Length == 0 ? field.Value : $"::{field.Value}");

            if (IsAt(TokenType.Lparen))
            {
                Consume(TokenType.Lparen);

                // Is this an offset?
                if (IsAt(TokenType.Number))
                {
                    var offset = Consume(TokenType.Number);
                    sb.Append($"_{offset.Value}");
                }
                else if (IsAt(TokenType.Identifier))
                {
                    // In case of dynamic offsets we add the offset as a child node
                    var numberOfParams = variable.Children.Count(x => x.Type == AstNodeType.DynOffset);
                    sb.AppendFormat("_${0}", numberOfParams);

                    var offset = ParseVariable();
                    offset.Type = AstNodeType.DynOffset;
                    variable.Children.Add(offset);
                }
                else
                {
                    ThrowUnexpectedTokenException(TokenType.Number);
                }

                Consume(TokenType.Rparen);
            }
        }

        variable.Path = sb.ToString();

        return variable;
    }

    public AstNode Parse(List<Token> tokens)
    {
        _tokens = tokens;
        _numberOfTokens = (uint)tokens.Count;

        var root = new AstNode { Type = AstNodeType.Root };

        while (_currentTokenIndex < _numberOfTokens)
        {
            switch (CurrentToken.Type)
            {
                case TokenType.Dot: root.Children.Add(ParseDirective()); break;
                case TokenType.Declare: root.Children.Add(ParseDeclaration()); break;
                case TokenType.Eof: return root;
                default:
                    throw new SourceException($"Unexpected token '{Enum.GetName(typeof(TokenType), CurrentToken.Type)}' at {CurrentToken.Position}");
            }
        }

        return root;
    }
}