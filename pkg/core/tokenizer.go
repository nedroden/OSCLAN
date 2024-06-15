package core

import (
	"errors"
	"fmt"
	"os"
	"strings"
	"unicode"
)

type Token struct {
	Position Position
	Type     TokenType
	Value    string
}

func (t *Token) ToString() string {
	return fmt.Sprintf("<Token Type='%s' Value='%s' Position='%s' />", TokenTypeToString(t.Type), t.Value, t.Position.ToString())
}

type Position struct {
	Filename string
	Offset   int
	Line     int
	Column   int
}

func (p *Position) ToString() string {
	return fmt.Sprintf("<Position Line='%d' Column='%d' Filename='%s' />", p.Line, p.Column, p.Filename)
}

type Tokenizer struct {
	Source       []rune
	Module       string
	SourceLength int
	Position     Position
	CurrentChar  rune
}

type TokenType int64

const (
	EOF TokenType = iota
	Plus
	Minus
	Dot
	Identifier
	Lbrace
	Rbrace
	DoubleColon
	Lt
	Gt
	Lparen
	Rparen
	Equals
	Number
	String
	Impl
	Struct
	Comma
)

var tokenStringMapping = map[TokenType]string{
	EOF:         "EOF",
	Plus:        "Plus",
	Minus:       "Minus",
	Dot:         "Dot",
	Identifier:  "Identifier",
	Lbrace:      "Lbrace",
	Rbrace:      "Rbrace",
	DoubleColon: "DoubleColon",
	Lt:          "Lt",
	Gt:          "Gt",
	Lparen:      "Lparen",
	Rparen:      "Rparen",
	Equals:      "Equals",
	Number:      "Number",
	String:      "String",
	Impl:        "Impl",
	Struct:      "Struct",
	Comma:       "Comma",
}

func TokenTypeToString(tokenType TokenType) string {
	if str, found := tokenStringMapping[tokenType]; found {
		return str
	}

	return "Unknown"
}

func InitTokenizer(filename string, directory string) (*Tokenizer, error) {
	bytes, err := os.ReadFile(directory + "/" + filename)
	source := string(bytes)

	if err != nil {
		return nil, err
	}

	return &Tokenizer{
		Module:       directory,
		Position:     Position{Filename: filename, Line: 1, Column: 1, Offset: 0},
		Source:       []rune(source),
		SourceLength: len(source),
		CurrentChar:  rune(source[0]),
	}, nil
}

func (t *Tokenizer) IsAtEol() bool {
	return t.CurrentChar == '\n'
}

func (t *Tokenizer) IsAtEof() bool {
	return t.Position.Offset >= t.SourceLength-1
}

func (t *Tokenizer) IsAtWhiteSpace() bool {
	return t.CurrentChar == ' '
}

func (t *Tokenizer) Advance() {
	t.Position.Offset++
	t.Position.Column++
	t.CurrentChar = t.Source[t.Position.Offset]
}

func (t *Tokenizer) AdvanceTimes(times int) {
	for i := 0; i < times; i++ {
		t.Advance()
	}
}

func (t *Tokenizer) AdvanceSequence(sequence string) {
	t.AdvanceTimes(len(sequence))
}

func (t *Tokenizer) IsAtSequence(sequence string) bool {
	sequenceLength := len(sequence)

	if sequenceLength > t.SourceLength-t.Position.Offset {
		return false
	}

	for i, char := range sequence {
		if char != t.Source[t.Position.Offset+i] {
			return false
		}
	}

	return true
}

func (t *Tokenizer) SkipComment() {
	for !t.IsAtEol() && !t.IsAtEof() {
		t.Advance()
	}

	if !t.IsAtEof() {
		t.Advance()
		t.Position.Line++
	}
}

func (t *Tokenizer) TokenizeIdentifier() Token {
	originalPosition := t.Position
	var sb strings.Builder

	for unicode.IsLetter(t.CurrentChar) || unicode.IsDigit(t.CurrentChar) || t.CurrentChar == '_' {
		sb.WriteRune(t.CurrentChar)

		t.Advance()
	}

	return Token{Type: Identifier, Value: sb.String(), Position: originalPosition}
}

func (t *Tokenizer) TokenizeNumber() Token {
	originalPosition := t.Position
	var sb strings.Builder

	for unicode.IsDigit(t.CurrentChar) {
		sb.WriteRune(t.CurrentChar)
		t.Advance()
	}

	return Token{Type: Number, Value: sb.String(), Position: originalPosition}
}

func (t *Tokenizer) TokenizeString() (Token, error) {
	originalPosition := t.Position
	var sb strings.Builder

	t.Advance()

	for !t.IsAtEof() && t.CurrentChar != '"' {
		sb.WriteRune(t.CurrentChar)
		t.Advance()
	}

	if t.CurrentChar != '"' {
		return Token{}, errors.New("string literal not terminated")
	}

	t.Advance()

	return Token{Type: String, Value: sb.String(), Position: originalPosition}, nil
}

func (t *Tokenizer) GetTokens() ([]Token, error) {
	var tokens []Token

	for !t.IsAtEof() {
		var token Token
		var err error

		if t.IsAtEol() {
			t.Position.Line++
			t.Advance()
			t.Position.Column = 1
			continue
		}

		if t.IsAtSequence("//") {
			t.SkipComment()
			continue
		}

		if t.IsAtWhiteSpace() {
			t.Advance()
			continue
		}

		if t.CurrentChar == '"' {
			token, err = t.TokenizeString()

			if err != nil {
				return []Token{}, err
			}

			tokens = append(tokens, token)
			continue
		}

		if t.IsAtSequence("struct") {
			tokens = append(tokens, Token{Type: Struct, Position: t.Position})
			t.AdvanceSequence("struct")
			continue
		}

		if t.IsAtSequence("impl") {
			tokens = append(tokens, Token{Type: Impl, Position: t.Position})
			t.AdvanceSequence("impl")
			continue
		}

		if unicode.IsLetter(t.CurrentChar) {
			token = t.TokenizeIdentifier()
			tokens = append(tokens, token)
			continue
		}

		if unicode.IsDigit(t.CurrentChar) {
			token = t.TokenizeNumber()
			tokens = append(tokens, token)
			continue
		}

		if t.IsAtSequence("::") {
			tokens = append(tokens, Token{Type: DoubleColon, Position: t.Position})
			t.AdvanceSequence("::")
			continue
		}

		if token == (Token{}) {
			switch t.CurrentChar {
			case '.':
				tokens = append(tokens, Token{Type: Dot, Position: t.Position})
			case '+':
				tokens = append(tokens, Token{Type: Plus, Position: t.Position})
			case '-':
				tokens = append(tokens, Token{Type: Minus, Position: t.Position})
			case '{':
				tokens = append(tokens, Token{Type: Lbrace, Position: t.Position})
			case '}':
				tokens = append(tokens, Token{Type: Rbrace, Position: t.Position})
			case '<':
				tokens = append(tokens, Token{Type: Lt, Position: t.Position})
			case '>':
				tokens = append(tokens, Token{Type: Gt, Position: t.Position})
			case '(':
				tokens = append(tokens, Token{Type: Lparen, Position: t.Position})
			case ')':
				tokens = append(tokens, Token{Type: Rparen, Position: t.Position})
			case '=':
				tokens = append(tokens, Token{Type: Equals, Position: t.Position})
			case ',':
				tokens = append(tokens, Token{Type: Comma, Position: t.Position})
			default:
				return []Token{}, fmt.Errorf("unexpected character '%c' encountered during tokenization process: %s", t.CurrentChar, t.Position.ToString())
			}
		}

		t.Advance()
	}

	tokens = append(tokens, Token{Position: t.Position, Type: EOF})

	return tokens, nil
}
