using NSubstitute;
using Osclan.Compiler.Exceptions;
using Osclan.Compiler.Io;
using Osclan.Compiler.Tokenization;

namespace Osclan.Compiler.Test.Tokenization;

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

    [Fact]
    public void Test_Double_Colon_Is_Tokenized()
    {
        _readerMock.Read("TestFiles/test.osc").Returns("::");
        var tokenizer = new Tokenizer(new CompilerOptions(), "TestFiles", "test.osc", _readerMock);
        var tokens = tokenizer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.DoubleColon, tokens[0].Type);
    }

    [Fact]
    public void Test_Declare_Is_Tokenized()
    {
        _readerMock.Read("TestFiles/test.osc").Returns("declare");
        var tokenizer = new Tokenizer(new CompilerOptions(), "TestFiles", "test.osc", _readerMock);
        var tokens = tokenizer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.Declare, tokens[0].Type);
    }

    // [Fact]
    // public void Test_Random_Identifier_Is_Tokenized()
    // {
    //     _readerMock.Read("TestFiles/test.osc").Returns("some-random-identifier");
    //     var tokenizer = new Tokenizer(new CompilerOptions(), "TestFiles", "test.osc", _readerMock);
    //     var tokens = tokenizer.Tokenize();

    //     Assert.Single(tokens);
    //     Assert.Equal(TokenType.Identifier, tokens[0].Type);
    // }

    // [Fact]
    // public void Test_Error_Is_Thrown_When_Identifier_Starts_With_Number()
    // {
    //     _readerMock.Read("TestFiles/test.osc").Returns("1identifier");
    //     var tokenizer = new Tokenizer(new CompilerOptions(), "TestFiles", "test.osc", _readerMock);

    //     Assert.Throws<SourceException>(() => tokenizer.Tokenize());
    // }

    [Fact]
    public void Test_Error_Is_Thrown_When_String_Not_Terminated()
    {
        _readerMock.Read("TestFiles/test.osc").Returns("\"some string");
        var tokenizer = new Tokenizer(new CompilerOptions(), "TestFiles", "test.osc", _readerMock);

        Assert.Throws<SourceException>(() => tokenizer.Tokenize());
    }
}