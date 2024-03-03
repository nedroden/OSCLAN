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
	Modifier
	Structure
	Function
	Type
)

var nodeStringMapping = map[AstNodeType]string{
	Root:        "Root",
	Directive:   "Directive",
	DirArgument: "DirArgument",
	Modifier:    "Modifier",
	Function:    "Function",
	Structure:   "Structure",
	Type:        "Type",
}

func AstNodeTypeToString(nodeType AstNodeType) string {
	if str, found := nodeStringMapping[nodeType]; found {
		return str
	}

	return "Unknown"
}

type AstTreeNode struct {
	Children  []AstTreeNode
	Type      AstNodeType
	Value     string
	ValueType string
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

	fmt.Printf("-> Consumed token of type '%s' with value '%s'\n", TokenTypeToString(oldToken.Type), oldToken.Value)

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
		argument, err = p.Consume(String)

		if err != nil {
			return parent, err
		}

		parent.Children = append(parent.Children, AstTreeNode{Type: DirArgument, Value: argument.Value})

	case "import":
		argument, err = p.Consume(String)

		if err != nil {
			return parent, err
		}

		parent.Children = append(parent.Children, AstTreeNode{Type: DirArgument, Value: argument.Value})

	default:
		return parent, fmt.Errorf("invalid directive '%s'", token.Value)
	}

	parent.Children = append(parent.Children, directive)

	return parent, nil
}

func (p *Parser) ParseFunction(parent *AstTreeNode) (*AstTreeNode, error) {
	var err error
	var visibility string

	if p.CurrentToken.Type == Minus {
		_, err = p.Consume(Minus)
		visibility = "private"
	} else {
		_, err = p.Consume(Plus)
		visibility = "public"
	}

	if err != nil {
		return &AstTreeNode{}, err
	}

	blockNode := AstTreeNode{
		Type:     Function,
		Children: []AstTreeNode{{Type: Modifier, Value: visibility}},
	}

	switch p.CurrentToken.Type {
	case Impl:
		if _, err = p.Consume(Impl); err != nil {
			return &AstTreeNode{}, err
		}

		var returnType Token

		if returnType, err = p.Consume(Identifier); err != nil {
			return &AstTreeNode{}, err
		}

		blockNode.ValueType = returnType.Value
	case Struct:
		if _, err := p.Consume(Struct); err != nil {
			return &AstTreeNode{}, err
		}

		var functionNameToken Token
		if functionNameToken, err = p.Consume(Identifier); err != nil {
			return &AstTreeNode{}, err
		}

		blockNode.Value = functionNameToken.Value

		p.ParseStruct(&blockNode)
	default:
		return &AstTreeNode{}, fmt.Errorf("expected 'impl' or 'struct' token, but got '%s'", TokenTypeToString(p.Lookahead()))
	}

	parent.Children = append(parent.Children, blockNode)

	if _, err = p.Consume(Lbrace); err != nil {
		return &AstTreeNode{}, err
	}

	p.ParseCompound(parent)

	if _, err = p.Consume(Rbrace); err != nil {
		return &AstTreeNode{}, err
	}

	return parent, nil
}

func (p *Parser) ParseStruct(parent *AstTreeNode) (*AstTreeNode, error) {

	return &AstTreeNode{}, nil
}

func (p *Parser) ParseCompound(parent *AstTreeNode) (*AstTreeNode, error) {

	return &AstTreeNode{}, nil
}

// TODO: finish
func (p *Parser) ParseType(parent *AstTreeNode) (*AstTreeNode, error) {
	node := AstTreeNode{
		Type: Type,
	}

	var err error
	var token Token

	if token, err = p.Consume(Identifier); err != nil {
		return &AstTreeNode{}, err
	}

	node.Value = token.Value

	if p.Lookahead() == Lt {
		p.ParseType(&node)

		if _, err := p.Consume(Gt); err != nil {
			return &AstTreeNode{}, err
		}
	}

	parent.Children = append(parent.Children, node)

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
		case Plus:
			if _, err := p.ParseFunction(&rootNode); err != nil {
				return rootNode, err
			}
		case Minus:
			if _, err := p.ParseFunction(&rootNode); err != nil {
				return rootNode, err
			}
		default:
			return AstTreeNode{}, fmt.Errorf("token of type '%s' is missing an implementation (at root level)", TokenTypeToString(p.CurrentToken.Type))
		}
	}

	return rootNode, nil
}
