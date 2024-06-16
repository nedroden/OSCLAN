package core

import (
	"fmt"
	"strconv"
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
	ntRoot AstNodeType = iota
	ntDirective
	ntStructure
	ntProcedure
	ntCondition
	ntType
	ntModifier
	ntField
	ntArgument
	ntAssignment
	ntVariable
)

var nodeStringMapping = map[AstNodeType]string{
	ntRoot:       "Root",
	ntDirective:  "Directive",
	ntStructure:  "Structure",
	ntProcedure:  "Procedure",
	ntCondition:  "Condition",
	ntType:       "Type",
	ntModifier:   "Modifier",
	ntField:      "Field",
	ntArgument:   "Argument",
	ntAssignment: "Assignment",
	ntVariable:   "Variable",
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
	ValueSize uint8
}

func (n AstTreeNode) ToString() string {
	return n.ToStringWithIndentation(1)
}

func (n AstTreeNode) ToStringWithIndentation(level int) string {
	var sb strings.Builder

	sb.WriteString(fmt.Sprintf("<AstNode Type='%s' Value='%s' />\n", AstNodeTypeToString(n.Type), n.Value))

	for _, child := range n.Children {
		sb.WriteString(fmt.Sprintf("%s%s", strings.Repeat("\t", level), child.ToStringWithIndentation(level+1)))
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

func (p *Parser) unexpectedTokenError(tokenType TokenType) error {
	return fmt.Errorf("unexpected token of type '%s', expected '%s'. Position = %s",
		TokenTypeToString(p.CurrentToken.Type),
		TokenTypeToString(tokenType),
		p.CurrentToken.Position.ToString(),
	)
}

func (p *Parser) Consume(tokenType TokenType) (Token, error) {
	if p.CurrentToken.Type != tokenType {
		return Token{}, p.unexpectedTokenError(tokenType)
	}

	oldToken := p.CurrentToken

	if p.CurrentTokenIndex < p.NumberOfTokens-1 {
		p.CurrentTokenIndex++
		p.CurrentToken = p.Tokens[p.CurrentTokenIndex]
	}

	fmt.Printf("-> Consumed token of type '%s' with value '%s'\n", TokenTypeToString(oldToken.Type), oldToken.Value)

	return oldToken, nil
}

func (p *Parser) IsAt(tokenType TokenType) bool {
	return p.Tokens[p.CurrentTokenIndex].Type == tokenType
}

func (p *Parser) Lookahead() TokenType {
	return p.Tokens[p.CurrentTokenIndex+1].Type
}

func (p *Parser) ParseDirective(parent *AstTreeNode) (*AstTreeNode, error) {
	var err error
	var token Token

	if _, err = p.Consume(ttDot); err != nil {
		return parent, err
	}

	if token, err = p.Consume(ttIdentifier); err != nil {
		return parent, err
	}

	directive := AstTreeNode{
		Type:  ntDirective,
		Value: token.Value,
	}

	var argument Token

	switch strings.ToLower(token.Value) {
	case "module":
		argument, err = p.Consume(ttString)

		if err != nil {
			return parent, err
		}

		directive.Children = append(directive.Children, AstTreeNode{Type: ntArgument, Value: argument.Value})

	case "import":
		argument, err = p.Consume(ttString)

		if err != nil {
			return parent, err
		}

		directive.Children = append(directive.Children, AstTreeNode{Type: ntArgument, Value: argument.Value})

	default:
		return parent, fmt.Errorf("invalid directive '%s'", token.Value)
	}

	parent.Children = append(parent.Children, directive)

	return &directive, nil
}

func (p *Parser) ParseDeclaration(parent *AstTreeNode) (*AstTreeNode, error) {
	var err error

	if _, err = p.Consume(ttDeclare); err != nil {
		return &AstTreeNode{}, err
	}

	var modifiers []AstTreeNode

	for p.IsAt(ttModifier) {
		var modifier Token

		if modifier, err = p.Consume(ttModifier); err != nil {
			return &AstTreeNode{}, err
		}

		modifiers = append(modifiers, AstTreeNode{Type: ntModifier, Value: modifier.Value})
	}

	var declaration *AstTreeNode
	if p.IsAt(ttStruct) {
		declaration, err = p.ParseStructure(parent)

		if err != nil {
			return &AstTreeNode{}, err
		}
	} else if p.IsAt(ttLbracket) {
		declaration, err = p.ParseProcedure(parent)

		if err != nil {
			return &AstTreeNode{}, err
		}
	} else {
		return &AstTreeNode{}, p.unexpectedTokenError(ttIdentifier)
	}

	declaration.Children = append(declaration.Children, modifiers...)

	return declaration, nil
}

func (p *Parser) ParseStructure(parent *AstTreeNode) (*AstTreeNode, error) {
	var token Token
	var err error

	node := AstTreeNode{
		Type: ntProcedure,
	}

	if _, err := p.Consume(ttStruct); err != nil {
		return &AstTreeNode{}, err
	}

	if token, err = p.Consume(ttIdentifier); err != nil {
		return &AstTreeNode{}, err
	}

	node.Value = token.Value

	for !p.IsAt(ttEOF) && !p.IsAt(ttEnd) {
		field := AstTreeNode{Type: ntField}
		typeNode, err := p.ParseType(&field)

		if err != nil {
			return &AstTreeNode{}, err
		}

		field.Children = append(field.Children, *typeNode)

		if typeNode.Type == ntStructure {
			subStructure, err := p.ParseImplicitStructure(&field)

			if err != nil {
				return &AstTreeNode{}, err
			}

			field.Children = append(field.Children, *subStructure)
			continue
		}

		var identifier Token

		if identifier, err = p.Consume(ttIdentifier); err != nil {
			return &AstTreeNode{}, err
		}

		field.Value = identifier.Value
		node.Children = append(node.Children, field)
	}

	if _, err = p.Consume(ttEnd); err != nil {
		return &AstTreeNode{}, err
	}

	parent.Children = append(parent.Children, node)

	return &node, nil
}

// TODO: Merge with the above function.
func (p *Parser) ParseImplicitStructure(parent *AstTreeNode) (*AstTreeNode, error) {
	var node = AstTreeNode{Type: ntStructure}
	var err error

	var identifierToken Token
	if identifierToken, err = p.Consume(ttIdentifier); err != nil {
		return &AstTreeNode{}, err
	}

	node.Value = identifierToken.Value

	if _, err = p.Consume(ttBegin); err != nil {
		return &AstTreeNode{}, err
	}

	for !p.IsAt(ttEOF) && !p.IsAt(ttEnd) {
		field := AstTreeNode{Type: ntField}
		typeNode, err := p.ParseType(&field)

		if err != nil {
			return &AstTreeNode{}, err
		}

		node.Children = append(node.Children, *typeNode)

		if typeNode.Type == ntStructure {
			subStructure, err := p.ParseImplicitStructure(&field)

			if err != nil {
				return &AstTreeNode{}, err
			}

			field.Children = append(field.Children, *subStructure)
			continue
		}

		var identifier Token

		if identifier, err = p.Consume(ttIdentifier); err != nil {
			return &AstTreeNode{}, err
		}

		field.Value = identifier.Value
		node.Children = append(node.Children, field)
	}

	if _, err = p.Consume(ttEnd); err != nil {
		return &AstTreeNode{}, err
	}

	return &node, nil
}

func (p *Parser) ParseProcedure(parent *AstTreeNode) (*AstTreeNode, error) {
	var node = AstTreeNode{Type: ntProcedure}
	var err error

	// Get the return type of the procedure
	var returnTypeNode *AstTreeNode
	if returnTypeNode, err = p.ParseType(&node); err != nil {
		return &AstTreeNode{}, err
	}
	node.Children = append(node.Children, *returnTypeNode)

	if _, err = p.Consume(ttDoubleColon); err != nil {
		return &AstTreeNode{}, err
	}

	// Procedure name
	var identifierToken Token
	if identifierToken, err = p.Consume(ttIdentifier); err != nil {
		return &AstTreeNode{}, err
	}
	node.Value = identifierToken.Value

	// Arguments
	if _, err = p.Consume(ttLparen); err != nil {
		return &AstTreeNode{}, err
	}

	for !p.IsAt(ttRparen) && !p.IsAt(ttEOF) {
		if p.IsAt(ttComma) {
			if _, err = p.Consume(ttComma); err != nil {
				return &AstTreeNode{}, err
			}
		}

		argumentNode := AstTreeNode{Type: ntArgument}

		// Argument type
		var typeNode *AstTreeNode
		if typeNode, err = p.ParseType(&argumentNode); err != nil {
			return &AstTreeNode{}, err
		}
		argumentNode.ValueType = typeNode.Value

		// Argument name
		var identifierNode Token
		if identifierNode, err = p.Consume(ttIdentifier); err != nil {
			return &AstTreeNode{}, err
		}
		argumentNode.Value = identifierNode.Value
	}

	// Parse body
	var statements []AstTreeNode
	if statements, err = p.ParseCompound(&node); err != nil {
		return &AstTreeNode{}, err
	}
	node.Children = append(node.Children, statements...)

	if _, err = p.Consume(ttRparen); err != nil {
		return &AstTreeNode{}, err
	}

	return &node, nil
}

func (p *Parser) ParseCompound(parent *AstTreeNode) ([]AstTreeNode, error) {
	var statements []AstTreeNode
	var err error

	for !p.IsAt(ttEOF) {
		switch p.CurrentToken.Type {

		// Variable declaration
		case ttDeclare:
			var node AstTreeNode

			if node, err = p.ParseVariableDeclaration(&node); err != nil {
				return []AstTreeNode{}, err
			}

			statements = append(statements, node)

		default:
			return []AstTreeNode{}, fmt.Errorf(
				"unexpected token of type '%s' at Position = %s",
				TokenTypeToString(p.CurrentToken.Type),
				p.CurrentToken.Position.ToString(),
			)
		}
	}

	return statements, nil
}

func (p *Parser) ParseVariableDeclaration(parent *AstTreeNode) (AstTreeNode, error) {
	assignmentNode := AstTreeNode{Type: ntAssignment}
	variableNode := AstTreeNode{Type: ntVariable}
	var err error

	// Variable type
	var typeNode *AstTreeNode
	if typeNode, err = p.ParseType(&variableNode); err != nil {
		return AstTreeNode{}, err
	}
	variableNode.ValueType = typeNode.Value

	// Variable name
	var identifierNode Token
	if identifierNode, err = p.Consume(ttIdentifier); err != nil {
		return AstTreeNode{}, err
	}
	variableNode.Value = identifierNode.Value

	// Assign variable as left operand
	assignmentNode.Children = append(assignmentNode.Children, variableNode)

	if _, err = p.Consume(ttColon); err != nil {
		return AstTreeNode{}, err
	}

	return assignmentNode, nil
}

func (p *Parser) ParseType(parent *AstTreeNode) (*AstTreeNode, error) {
	node := AstTreeNode{
		Type: ntType,
	}

	var err error
	var token Token

	if _, err = p.Consume(ttLbracket); err != nil {
		return &AstTreeNode{}, err
	}

	if token, err = p.Consume(ttIdentifier); err != nil {
		// We might be dealing with a struct type (which is denoted by a dedicated token)
		var structTypeError error
		if token, structTypeError = p.Consume(ttStruct); structTypeError != nil {
			return &AstTreeNode{}, err
		}
	}

	node.Value = token.Value

	if token.Type == ttStruct {
		node.Type = ntStructure
	} else {
		node.Type = ntType
	}

	if p.IsAt(ttLparen) {
		if token, err = p.Consume(ttLparen); err != nil {
			return &AstTreeNode{}, err
		}

		if token, err = p.Consume(ttNumber); err != nil {
			return &AstTreeNode{}, err
		}

		var u64 uint64
		if u64, err = strconv.ParseUint(token.Value, 10, 8); err != nil {
			return &AstTreeNode{}, err
		}

		// TODO: Add check here for math.MaxUint8
		node.ValueSize = uint8(u64)

		if _, err = p.Consume(ttRparen); err != nil {
			return &AstTreeNode{}, err
		}
	}

	if _, err = p.Consume(ttRbracket); err != nil {
		return &AstTreeNode{}, err
	}

	parent.Children = append(parent.Children, node)

	return &node, nil
}

func (p *Parser) GenerateAst() (AstTreeNode, error) {
	rootNode := AstTreeNode{Type: ntRoot}

	for p.CurrentTokenIndex < p.NumberOfTokens {
		switch p.CurrentToken.Type {
		case ttDot:
			if _, err := p.ParseDirective(&rootNode); err != nil {
				return rootNode, err
			}
		case ttDeclare:
			if _, err := p.ParseDeclaration(&rootNode); err != nil {
				return rootNode, err
			}
		default:
			// For debug purposes
			fmt.Println(rootNode.ToString())
			return AstTreeNode{}, fmt.Errorf("token of type '%s' is missing an implementation (at root level). Position = %s",
				TokenTypeToString(p.CurrentToken.Type),
				p.CurrentToken.Position.ToString(),
			)
		}
	}

	return rootNode, nil
}
