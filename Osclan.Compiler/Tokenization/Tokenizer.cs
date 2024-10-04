using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Osclan.Compiler.Extensions;
using Osclan.Compiler.Io;

namespace Osclan.Compiler.Tokenization;

public class Tokenizer
{
    private readonly CompilerOptions _options;

    private static readonly Dictionary<string, Token> _sequenceMapping = new()
    {
        { "::", new Token(TokenType.DoubleColon) },
        { "declare", new Token(TokenType.Declare) },
        { "anon", new Token(TokenType.Anon) },
        { "struct", new Token(TokenType.Struct) },
        { "end", new Token(TokenType.End) },
        { "public", new Token(TokenType.Modifier, "public") },
        { "private", new Token(TokenType.Modifier, "private")},
        { "increment", new Token(TokenType.Increment) },
        { "decrement", new Token(TokenType.Decrement) },
        { "print", new Token(TokenType.Print) },
        { "call", new Token(TokenType.Call) },
        { "~=", new Token(TokenType.Neq) },
        { "begin", new Token(TokenType.Begin) },
        { "init", new Token(TokenType.Init) },
        { "ok", new Token(TokenType.Number, "0") },
        { "true", new Token(TokenType.Number, "1") },
        { "false", new Token(TokenType.Number, "0") },
        { "ret", new Token(TokenType.Ret) },
        { "if", new Token(TokenType.If) },
        { "else", new Token(TokenType.Else) },
        { "then", new Token(TokenType.Then) }
    };

    private readonly string _source;
    private readonly string _module;
    private readonly uint _sourceLength;
    private char _currentChar;
    private Position _position;

    public Tokenizer(CompilerOptions options, string directory, string filename, IInputFileReader reader)
    {
        var source = reader.Read(Path.Combine(directory, filename));

        _options = options;
        _source = source;

        _module = directory;
        _sourceLength = (uint)source.Length;

        if (_sourceLength == 0)
        {
            throw new Exception("Source is empty");
        }

        _currentChar = _source[0];
        _position = new Position { Filename = filename, Line = 1, Column = 1 };
    }

    private bool IsAtEol() =>
        _currentChar == '\n';

    private bool IsAtEof() =>
        _position.Offset >= _sourceLength - 1;

    private bool IsAtSpace() =>
        char.IsWhiteSpace(_currentChar);

    private bool IsAtLetter() =>
        char.IsLetter(_currentChar);

    private bool IsAtDigit() =>
        char.IsDigit(_currentChar);

    private void Advance()
    {
        _position = _position with { Offset = _position.Offset + 1, Column = _position.Column + 1 };

        if (_position.Offset < _sourceLength)
        {
            _currentChar = _source[_position.Offset];
        }
    }

    private void AdvanceTimes(uint times)
    {
        for (var i = 0; i < times; i++)
        {
            Advance();
        }
    }

    private void AdvanceSequence(string sequence) =>
        AdvanceTimes((uint)sequence.Length);

    private bool IsAtSequence(string sequence)
    {
        var sequenceLength = sequence.Length;

        if (sequenceLength > _sourceLength - _position.Offset)
        {
            return false;
        }

        for (int i = 0; i < sequenceLength; i++)
        {
            if (_source[_position.Offset + i] != sequence[i])
            {
                return false;
            }
        }

        return true;
    }

    private void SkipComment()
    {
        while (!IsAtEol() && !IsAtEof())
        {
            Advance();
        }

        if (!IsAtEof())
        {
            Advance();
            _position.Line++;
            _position.Column = 1;
        }
    }

    private Token TokenizeIdentifier()
    {
        var originalPosition = _position;
        var stringBuilder = new StringBuilder();

        while (_currentChar.IsIdentifierChar())
        {
            stringBuilder.Append(_currentChar);
            Advance();
        }

        return new Token(TokenType.Identifier, stringBuilder.ToString(), originalPosition);
    }

    private Token TokenizerNumber()
    {
        var originalPosition = _position;
        var stringBuilder = new StringBuilder();

        while (IsAtDigit())
        {
            stringBuilder.Append(_currentChar);
            Advance();
        }

        return new Token(TokenType.Number, stringBuilder.ToString(), originalPosition);
    }

    private Token TokenizeString()
    {
        var originalPosition = _position;
        var stringBuilder = new StringBuilder();

        Advance();

        while (_currentChar != '"')
        {
            stringBuilder.Append(_currentChar);
            Advance();
        }

        if (_currentChar != '"')
        {
            throw new Exception("Unterminated string");
        }

        Advance();

        return new Token(TokenType.String, stringBuilder.ToString(), originalPosition);
    }

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();

        while (!IsAtEof())
        {
            Token? token = null;

            if (IsAtEol())
            {
                _position.Line++;
                Advance();
                _position.Column = 1;
                continue;
            }

            if (IsAtSequence(".*"))
            {
                SkipComment();
                continue;
            }

            if (IsAtSpace())
            {
                Advance();
                continue;
            }

            if (_currentChar == '"')
            {
                tokens.Add(TokenizeString());
                continue;
            }

            foreach (var sequence in _sequenceMapping.Keys)
            {
                if (IsAtSequence(sequence))
                {
                    // Things such as init-list should not be matched
                    if (_sourceLength > _position.Offset + sequence.Length
                        && _source[_position.Offset + sequence.Length].IsIdentifierChar()
                        && sequence != "::")
                    {
                        continue;
                    }

                    token = _sequenceMapping[sequence] with { Position = _position };
                    tokens.Add(token);
                    AdvanceSequence(sequence);
                    break;
                }
            }

            if (token is not null)
            {
                continue;
            }

            if (IsAtLetter() || _currentChar == '-')
            {
                tokens.Add(TokenizeIdentifier());
                continue;
            }

            if (IsAtDigit())
            {
                tokens.Add(TokenizerNumber());
                continue;
            }

            token = _currentChar switch
            {
                '+' => new Token(TokenType.Plus),
                '-' => new Token(TokenType.Minus),
                '.' => new Token(TokenType.Dot),
                '(' => new Token(TokenType.Lparen),
                ')' => new Token(TokenType.Rparen),
                '[' => new Token(TokenType.Lbracket),
                ']' => new Token(TokenType.Rbracket),
                '<' => new Token(TokenType.Lt),
                '>' => new Token(TokenType.Gt),
                ':' => new Token(TokenType.Colon),
                ',' => new Token(TokenType.Comma),
                '=' => new Token(TokenType.Eq),
                '*' => new Token(TokenType.Asterisk),
                '~' => new Token(TokenType.Tilde),
                _ => throw new Exception($"Unexpected character at position {_position}: {_currentChar}")
            };

            tokens.Add(token with { Position = _position });
            Advance();
        }

        return tokens;
    }
}