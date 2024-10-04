using System;

namespace Osclan.Compiler.Tokenization;

public record Token
{
    public string? Value { get; set; }

    public TokenType Type { get; set; }

    public string TokenType => Enum.GetName(typeof(TokenType), Type) ?? "Unknown";

    public Position? Position { get; set; }

    public Token(TokenType tokenType) => Type = tokenType;

    public Token(TokenType tokenType, string value) : this(tokenType) => Value = value;

    public Token(TokenType tokenType, string value, Position position) : this(tokenType, value) => Position = position;
}