using NSubstitute;
using Osclan.Compiler.Io;
using Osclan.Compiler.Tokenization;

namespace Osclan.Compiler.Test;

public class TokenizerTests
{
    private readonly IInputFileReader _readerMock;

    public TokenizerTests() => _readerMock = Substitute.For<IInputFileReader>();

    [Fact]
    public void Test_Directive_Is_Tokenized()
    {
        _readerMock.Read("TestFiles/test.osc").Returns(".include \"some-module\"");
        var tokenizer = new Tokenizer(new CompilerOptions(), "TestFiles", "test.osc", _readerMock);
        var tokens = tokenizer.Tokenize();

        Assert.Equal(3, tokens.Count);
        Assert.Equal(TokenType.Dot, tokens[0].Type);
        Assert.Equal(TokenType.Identifier, tokens[1].Type);
        Assert.Equal(TokenType.String, tokens[2].Type);
    }
}