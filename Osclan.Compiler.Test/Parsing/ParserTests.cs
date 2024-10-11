using System.Collections.Generic;
using System.Linq;
using Osclan.Compiler.Exceptions;
using Osclan.Compiler.Parsing;
using Osclan.Compiler.Parsing.Abstractions;
using Osclan.Compiler.Tokenization;

namespace Osclan.Compiler.Test.Parsing;

public class ParserTests
{
    private readonly IParser _parser;

    public ParserTests() => _parser = new Parser();

    [Fact]
    public void Test_Unexpected_Token_Results_In_Error()
    {
        // Identifier token is invalid at root level.
        var tokens = new List<Token> { new(TokenType.Identifier) };

        Assert.Throws<SourceException>(() => _parser.Parse(tokens));
    }

    [Fact]
    public void Test_Root_Node_Is_Correctly_Defined()
    {
        var tokens = new List<Token>();
        var rootToken = _parser.Parse(tokens);

        Assert.Equal(AstNodeType.Root, rootToken.Type);
    }

    [Fact]
    public void Test_Directive_Is_Parsed()
    {
        var tokens = new List<Token>
        {
            new(TokenType.Dot),
            new(TokenType.Identifier) { Value = "import" },
            new(TokenType.String) { Value = "Some.Module" }
        };

        var directiveNode = _parser.Parse(tokens).Children.First();

        Assert.Equal(AstNodeType.Directive, directiveNode.Type);
        Assert.Equal("import", directiveNode.Value);

        var argumentNode = directiveNode.Children.Single();
        Assert.Equal(AstNodeType.Argument, argumentNode.Type);
        Assert.Equal("Some.Module", argumentNode.Value);
    }
}