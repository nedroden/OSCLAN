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
	ttEOF TokenType = iota
	ttPlus
	ttMinus
	ttDot
	ttIdentifier
	ttString
	ttNumber
	ttDeclare
	ttModifier
	ttStruct
	ttLbracket
	ttRbracket
	ttLparen
	ttRparen
	ttLt
	ttGt
	ttBegin
	ttEnd
	ttDoubleColon
	ttColon
	ttRet
	ttIncrement
	ttAnon
	ttPrint
	ttOk
	ttIf
	ttElse
	ttThen
	ttSpace
	ttZero
	ttComma
	ttEq
	ttNeq
	ttAsterisk
	ttTilde
	ttCall
	ttInit
)

var tokenStringMapping = map[TokenType]string{
	ttEOF:         "Eof",
	ttPlus:        "Plus",
	ttMinus:       "Minus",
	ttDot:         "Dot",
	ttIdentifier:  "Identifier",
	ttString:      "String",
	ttNumber:      "Number",
	ttDeclare:     "Declare",
	ttModifier:    "Modifier",
	ttStruct:      "Struct",
	ttLbracket:    "Lbracket",
	ttRbracket:    "Rbracket",
	ttLparen:      "Lparen",
	ttRparen:      "Rparen",
	ttLt:          "Lt",
	ttGt:          "Gt",
	ttBegin:       "Begin",
	ttEnd:         "End",
	ttDoubleColon: "DoubleColon",
	ttColon:       "Colon",
	ttRet:         "Ret",
	ttIncrement:   "Increment",
	ttAnon:        "Anon",
	ttPrint:       "Print",
	ttOk:          "Ok",
	ttIf:          "If",
	ttElse:        "Else",
	ttThen:        "Then",
	ttSpace:       "Space",
	ttZero:        "Zero",
	ttComma:       "Comma",
	ttEq:          "Eq",
	ttNeq:         "Neq",
	ttAsterisk:    "Asterisk",
	ttTilde:       "Tilde",
	ttCall:        "Call",
	ttInit:        "Init",
}

func TokenTypeToString(tokenType TokenType) string {
	if str, found := tokenStringMapping[tokenType]; found {
		return str
	}

	return "Unknown"
}

// TODO: sort based on usage statistics
var sequences = map[string]Token{
	"declare": {Type: ttDeclare},
	"struct":  {Type: ttStruct},
	"public":  {Type: ttModifier, Value: "public"},
	"private": {Type: ttModifier, Value: "private"},
	"end":     {Type: ttEnd},
	"call":    {Type: ttCall},
	"::":      {Type: ttDoubleColon},
	"~=":      {Type: ttNeq},
	"begin":   {Type: ttBegin},
	"init":    {Type: ttInit},
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
		if char != unicode.ToLower(t.Source[t.Position.Offset+i]) {
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

	for IsIdentifierChar(t.CurrentChar) {
		sb.WriteRune(t.CurrentChar)

		t.Advance()
	}

	return Token{Type: ttIdentifier, Value: sb.String(), Position: originalPosition}
}

func (t *Tokenizer) TokenizeNumber() Token {
	originalPosition := t.Position
	var sb strings.Builder

	for unicode.IsDigit(t.CurrentChar) {
		sb.WriteRune(t.CurrentChar)
		t.Advance()
	}

	return Token{Type: ttNumber, Value: sb.String(), Position: originalPosition}
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

	return Token{Type: ttString, Value: sb.String(), Position: originalPosition}, nil
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

		if t.IsAtSequence(".*") {
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

		for sequence, sequenceToken := range sequences {
			if t.IsAtSequence(sequence) {
				// / Things like init-list should not be matched
				if t.SourceLength > t.Position.Offset+len(sequence) &&
					IsIdentifierChar(t.Source[t.Position.Offset+len(sequence)]) &&
					sequence != "::" {

					continue
				}

				token = sequenceToken
				token.Position = t.Position
				tokens = append(tokens, token)
				t.AdvanceSequence(sequence)
				break
			}
		}

		if token != (Token{}) {
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

		if token == (Token{}) {
			tokenTypes := map[rune]TokenType{
				'.': ttDot,
				'+': ttPlus,
				'-': ttMinus,
				'<': ttLt,
				'>': ttGt,
				'(': ttLparen,
				')': ttRparen,
				'[': ttLbracket,
				']': ttRbracket,
				'=': ttEq,
				':': ttColon,
				',': ttComma,
				'*': ttAsterisk,
				'~': ttTilde,
			}

			tokenType, ok := tokenTypes[t.CurrentChar]

			if !ok {
				return []Token{}, fmt.Errorf("unexpected character '%c' encountered during tokenization process: %s", t.CurrentChar, t.Position.ToString())
			}

			tokens = append(tokens, Token{Type: tokenType, Position: t.Position})
		}

		t.Advance()
	}

	tokens = append(tokens, Token{Position: t.Position, Type: ttEOF})

	return tokens, nil
}

func IsIdentifierChar(char rune) bool {
	return unicode.IsLetter(char) || unicode.IsDigit(char) || char == '-'
}
