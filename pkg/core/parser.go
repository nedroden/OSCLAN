package core

import (
	"fmt"
	"strings"
)

type Parser struct {
	Tokens            []Token
	CurrentTokenIndex int
	CurrentToken      Token
	NumberOfTokens    int
}

type AstNodeType int64

const (
	Root AstNodeType = iota
	Directive
	DirArgument
)

var nodeStringMapping = map[AstNodeType]string{
	Root:        "Root",
	Directive:   "Directive",
	DirArgument: "DirArgument",
}

func AstNodeTypeToString(nodeType AstNodeType) string {
	if str, found := nodeStringMapping[nodeType]; found {
		return str
	}

	return "Unknown"
}

type AstTreeNode struct {
	Children []AstTreeNode
	Type     AstNodeType
	Value    string
}

func (n AstTreeNode) ToString() string {
	var sb strings.Builder

	sb.WriteString(fmt.Sprintf("<AstNode Type='%s' Value='%s' />", AstNodeTypeToString(n.Type), n.Value))

	for _, child := range n.Children {
		sb.WriteString(fmt.Sprintf("\t%s\n", child.ToString()))
	}

	return sb.String()
}

func InitializeParser(tokens []Token) (*Parser, error) {
	return &Parser{
		Tokens:            tokens,
		CurrentTokenIndex: 0,
		CurrentToken:      tokens[0],
		NumberOfTokens:    len(tokens),
	}, nil
}

func (p *Parser) Consume(tokenType TokenType) (Token, error) {
	if p.CurrentToken.Type != tokenType {
		return Token{}, fmt.Errorf("unexpected token of type '%s'", TokenTypeToString(p.CurrentToken.Type))
	}

	oldToken := p.CurrentToken

	if p.CurrentTokenIndex < p.NumberOfTokens-1 {
		p.CurrentTokenIndex++
		p.CurrentToken = p.Tokens[p.CurrentTokenIndex]
	}

	fmt.Printf("-> Consumed token of type '%s' with value '%s'\n", TokenTypeToString(p.CurrentToken.Type), p.CurrentToken.Value)

	return oldToken, nil
}

func (p *Parser) Lookahead() TokenType {
	return p.Tokens[p.CurrentTokenIndex+1].Type
}

func (p *Parser) ParseDirective(parent *AstTreeNode) (*AstTreeNode, error) {
	var err error
	var token Token

	if _, err = p.Consume(Dot); err != nil {
		return parent, err
	}

	if token, err = p.Consume(Identifier); err != nil {
		return parent, err
	}

	directive := AstTreeNode{
		Type:  Directive,
		Value: token.Value,
	}

	var argument Token

	switch token.Value {
	case "module":
		argument, err = p.Consume(Identifier)

		if err != nil {
			return parent, err
		}

		parent.Children = append(parent.Children, AstTreeNode{Type: DirArgument, Value: argument.Value})

	case "import":
		_, _ = p.Consume(Identifier)

	default:
		return parent, fmt.Errorf("invalid directive '%s'", token.Value)
	}

	parent.Children = append(parent.Children, directive)

	return parent, nil
}

func (p *Parser) GenerateAst() (AstTreeNode, error) {
	rootNode := AstTreeNode{Type: Root}

	for p.CurrentTokenIndex < p.NumberOfTokens {
		switch p.CurrentToken.Type {
		case Dot:
			_, err := p.ParseDirective(&rootNode)

			if err != nil {
				return rootNode, err
			}
		default:
			return AstTreeNode{}, fmt.Errorf("token of type '%s' is missing an implementation", TokenTypeToString(p.CurrentToken.Type))
		}
	}

	return rootNode, nil
}
