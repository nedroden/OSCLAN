using System.Linq;
using NSubstitute;
using Osclan.Compiler.Exceptions;
using Osclan.Compiler.Io.Abstractions;
using Osclan.Compiler.Tokenization;

namespace Osclan.Compiler.Test.Tokenization;

public class TokenizerTests
{
    private readonly IIoService _readerMock;

    public TokenizerTests() => _readerMock = Substitute.For<IIoService>();

    [Fact]
    public void Test_Directive_Is_Tokenized()
    {
        _readerMock.Read("TestFiles/test.osc").Returns(".include \"some-module\"");
        var tokenizer = new Tokenizer(new CompilerOptions { InputFile = "test.osc", TempFilePath = "TestFiles"}, _readerMock);
        var tokens = tokenizer.Tokenize();

        Assert.Equal(3, tokens.Count);
        Assert.Equal(TokenType.Dot, tokens[0].Type);
        Assert.Equal(TokenType.Identifier, tokens[1].Type);
        Assert.Equal(TokenType.String, tokens[2].Type);
    }

    [Fact]
    public void Test_Double_Colon_Is_Tokenized()
    {
        _readerMock.Read("TestFiles/test.osc").Returns("::");
        var tokenizer = new Tokenizer(new CompilerOptions { InputFile = "test.osc", TempFilePath = "TestFiles"}, _readerMock);
        var tokens = tokenizer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.DoubleColon, tokens[0].Type);
    }

    [Fact]
    public void Test_Declare_Is_Tokenized()
    {
        _readerMock.Read("TestFiles/test.osc").Returns("declare");
        var tokenizer = new Tokenizer(new CompilerOptions { InputFile = "test.osc", TempFilePath = "TestFiles"}, _readerMock);
        var tokens = tokenizer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.Declare, tokens[0].Type);
    }

    [Fact]
    public void Test_Random_Identifier_Is_Tokenized()
    {
        _readerMock.Read("TestFiles/test.osc").Returns("some-random-identifier");
        var tokenizer = new Tokenizer(new CompilerOptions { InputFile = "test.osc", TempFilePath = "TestFiles"}, _readerMock);
        var tokens = tokenizer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.Identifier, tokens[0].Type);
        Assert.Equal("some-random-identifier", tokens[0].Value);
    }

    [Fact]
    public void Test_Error_Is_Thrown_When_Identifier_Starts_With_Number()
    {
        _readerMock.Read("TestFiles/test.osc").Returns("1identifier");
        var tokenizer = new Tokenizer(new CompilerOptions { InputFile = "test.osc", TempFilePath = "TestFiles"}, _readerMock);

        Assert.Throws<SourceException>(() => tokenizer.Tokenize());
    }

    [Fact]
    public void Test_Error_Is_Thrown_When_String_Not_Terminated()
    {
        _readerMock.Read("TestFiles/test.osc").Returns("\"some string");
        var tokenizer = new Tokenizer(new CompilerOptions { InputFile = "test.osc", TempFilePath = "TestFiles"}, _readerMock);

        Assert.Throws<SourceException>(() => tokenizer.Tokenize());
    }

    [Theory]
    [InlineData('+', TokenType.Plus)]
    [InlineData('-', TokenType.Minus)]
    [InlineData('.', TokenType.Dot)]
    [InlineData('(', TokenType.Lparen)]
    [InlineData(')', TokenType.Rparen)]
    [InlineData('[', TokenType.Lbracket)]
    [InlineData(']', TokenType.Rbracket)]
    [InlineData('<', TokenType.Lt)]
    [InlineData('>', TokenType.Gt)]
    [InlineData(':', TokenType.Colon)]
    [InlineData(',', TokenType.Comma)]
    [InlineData('=', TokenType.Eq)]
    [InlineData('*', TokenType.Asterisk)]
    [InlineData('~', TokenType.Tilde)]
    public void Test_Special_Character_Is_Tokenized(char character, TokenType tokenType)
    {
        _readerMock.Read("TestFiles/test.osc").Returns(character.ToString());
        var tokenizer = new Tokenizer(new CompilerOptions { InputFile = "test.osc", TempFilePath = "TestFiles"}, _readerMock);
        var tokens = tokenizer.Tokenize();

        Assert.Equal(tokenType, tokens.Single().Type);
    }
}