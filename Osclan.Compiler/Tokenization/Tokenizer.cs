using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Osclan.Compiler.Exceptions;
using Osclan.Compiler.Extensions;
using Osclan.Compiler.Io;
using Osclan.Compiler.Io.Abstractions;
using Osclan.Compiler.Tokenization.Abstractions;

namespace Osclan.Compiler.Tokenization;

/// <summary>
/// Given an input source file, converts the contents of that file into a series of tokens.
/// </summary>
public class Tokenizer : ITokenizer
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
        { "failed", new Token(TokenType.Number, "255") },
        { "true", new Token(TokenType.Number, "1") },
        { "false", new Token(TokenType.Number, "0") },
        { "ret", new Token(TokenType.Ret) },
        { "if", new Token(TokenType.If) },
        { "else", new Token(TokenType.Else) },
        { "then", new Token(TokenType.Then) },
        { "free", new Token(TokenType.Free) }
    };

    private readonly string _source; private readonly uint _sourceLength;
    private char _currentChar;
    private Position _position;

    /// <summary>
    /// Initializes a new instance of the <see cref="Tokenizer"/> class.
    /// </summary>
    /// <param name="options">Compilation options.</param>
    /// <param name="directory">The input directory.</param>
    /// <param name="filename">The input filename.</param>
    /// <param name="ioService">A file reader.</param>
    /// <exception cref="SourceException">Thrown when the source file is empty.</exception>
    public Tokenizer(CompilerOptions options, string directory, string filename, IIoService ioService)
    {
        var source = ioService.Read(Path.Combine(directory, filename));

        _options = options;
        _source = source;

        _sourceLength = (uint)source.Length;

        if (_sourceLength == 0)
        {
            throw new SourceException("Source is empty");
        }

        _currentChar = _source[0];
        _position = new Position { Filename = filename, Line = 1, Column = 1 };
    }

    private char? PeakNext() =>
        _position.Offset + 1 < _sourceLength ? _source[_position.Offset + 1] : null;

    /// <summary>
    /// Determines whether or not the current character is at the end of a line.
    /// </summary>
    /// <returns>True if the current character is the final character in its line.</returns>
    private bool IsAtEol() =>
        _currentChar == '\n';

    /// <summary>
    /// Determines whether or not the tokenizer has reached the end of the source code.
    /// </summary>
    /// <returns>True if the current index no longer points to an existing character in the source code.</returns>
    private bool IsAtEof() =>
        _position.Offset >= _sourceLength;

    /// <summary>
    /// Determines whether or not the current character is whitespace.
    /// </summary>
    /// <returns>True if the current character represents whitespace.</returns>
    private bool IsAtSpace() =>
        char.IsWhiteSpace(_currentChar);

    /// <summary>
    /// Determines whether or not the current character is a letter.
    /// </summary>
    /// <returns>True if the current character is a letter.</returns>
    private bool IsAtLetter() =>
        char.IsLetter(_currentChar);

    /// <summary>
    /// Determines whether or not the current character is a digit.
    /// </summary>
    /// <returns>True if the current character is a digit.</returns>
    private bool IsAtDigit() =>
        char.IsDigit(_currentChar);

    /// <summary>
    /// Advances the position in the source code by one character.
    /// </summary>
    private void Advance()
    {
        _position = _position with { Offset = _position.Offset + 1, Column = _position.Column + 1 };

        if (_position.Offset < _sourceLength)
        {
            _currentChar = _source[_position.Offset];
        }
    }

    /// <summary>
    /// Advances the position in the source code by the given number.
    /// </summary>
    /// <param name="times"></param>
    private void AdvanceTimes(uint times)
    {
        for (var i = 0; i < times; i++)
        {
            Advance();
        }
    }

    /// <summary>
    /// Advances the position in the source code by the length of the given sequence.
    /// </summary>
    /// <param name="sequence">The input sequence.</param>
    private void AdvanceSequence(string sequence) =>
        AdvanceTimes((uint)sequence.Length);

    /// <summary>
    /// Determines whether or not the upcoming sequence of characters in the source code matches the given sequence.
    /// E.g.,:
    ///    IsAtSequence("begin") will return true if the next five characters in the source code are "begin".
    /// 
    /// The current character is considered to be the first character in the sequence, hence if the first letter of the sequence
    /// is not the current character, then this method will return false.
    /// </summary>
    /// <param name="sequence">The sequence to check for.</param>
    /// <returns>True if there is a match.</returns>
    private bool IsAtSequence(string sequence)
    {
        var sequenceLength = sequence.Length;

        if (sequenceLength > _sourceLength - _position.Offset)
        {
            return false;
        }

        for (int i = 0; i < sequenceLength; i++)
        {
            if (char.ToLower(_source[_position.Offset + i]) != char.ToLower(sequence[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Skips a comment, represented by the .* characters.
    /// </summary>
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

    /// <summary>
    /// Tokenizes an identifier, which is a sequence of alphanumeric characters and dashes, starting with a letter. An
    /// identifier can represent, i.a., a variable name, a procedure name, or a type name.
    /// </summary>
    /// <returns>An identifier token.</returns>
    private Token TokenizeIdentifier()
    {
        var originalPosition = _position;
        var stringBuilder = new StringBuilder();

        while (_currentChar.IsIdentifierChar() && !IsAtEof())
        {
            stringBuilder.Append(_currentChar);
            Advance();
        }

        return new Token(TokenType.Identifier, stringBuilder.ToString(), originalPosition);
    }

    /// <summary>
    /// Tokenizes a number. For now no distinction is made between integers and floats.
    /// </summary>
    /// <returns>A number token.</returns>
    private Token TokenizerNumber()
    {
        var originalPosition = _position;
        var stringBuilder = new StringBuilder();

        while (IsAtDigit() && !IsAtEof())
        {
            stringBuilder.Append(_currentChar);
            Advance();
        }

        if (_currentChar.IsIdentifierChar())
        {
            throw new SourceException($"Unexpected character at position {_position}: {_currentChar}");
        }

        return new Token(TokenType.Number, stringBuilder.ToString(), originalPosition);
    }

    /// <summary>
    /// Tokenizes a string, which is a sequence of alphanumeric characters and symbols enclosed in double quotes.
    /// </summary>
    /// <returns>A string token.</returns>
    /// <exception cref="SourceException">Thrown if the string is not properly terminated by a double quote.</exception>
    private Token TokenizeString()
    {
        var originalPosition = _position;
        var stringBuilder = new StringBuilder();

        Advance();

        while (_currentChar != '"' && !IsAtEof())
        {
            stringBuilder.Append(_currentChar);
            Advance();
        }

        if (_currentChar != '"')
        {
            throw new SourceException("Unterminated string");
        }

        Advance();

        return new Token(TokenType.String, stringBuilder.ToString(), originalPosition);
    }

    /// <summary>
    /// Tokenizes the source code.
    /// </summary>
    /// <returns>A list of tokens.</returns>
    /// <exception cref="SourceException">Thrown if there is a syntax error in the source file.</exception>
    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();

        while (!IsAtEof())
        {
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

            var token = FindSequenceToken();
            if (token is not null)
            {
                tokens.Add(token);
                continue;
            }

            if (IsAtLetter() || (_currentChar == '-' && PeakNext() is not null && !PeakNext()!.Value.IsIdentifierChar()))
            {
                tokens.Add(TokenizeIdentifier());
                continue;
            }

            if (IsAtDigit())
            {
                tokens.Add(TokenizerNumber());
                continue;
            }

            tokens.Add(GetSingleCharacterToken() with { Position = _position });
            Advance();
        }

        return tokens;
    }

    private Token GetSingleCharacterToken() => _currentChar switch
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
        _ => throw new SourceException($"Unexpected character at position {_position}: {_currentChar}")
    };

    private Token? FindSequenceToken()
    {
        foreach (var sequence in _sequenceMapping.Keys.Where(IsAtSequence))
        {
            // Things such as init-list should not be matched
            if (_sourceLength > _position.Offset + sequence.Length
                && _source[_position.Offset + sequence.Length].IsIdentifierChar()
                && sequence != "::")
            {
                continue;
            }

            AdvanceSequence(sequence);

            return _sequenceMapping[sequence] with { Position = _position };
        }

        return null;
    }
}