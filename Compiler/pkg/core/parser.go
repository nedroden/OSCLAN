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
	ntProcedureCall
	ntCondition
	ntType
	ntModifier
	ntField
	ntArgument
	ntAssignment
	ntDeclaration
	ntVariable
	ntAllocation
	ntScalar
	ntDynOffset
	ntPrint
	ntRet
)

var nodeStringMapping = map[AstNodeType]string{
	ntRoot:          "Root",
	ntDirective:     "Directive",
	ntStructure:     "Structure",
	ntProcedure:     "Procedure",
	ntCondition:     "Condition",
	ntType:          "Type",
	ntModifier:      "Modifier",
	ntField:         "Field",
	ntArgument:      "Argument",
	ntAssignment:    "Assignment",
	ntDeclaration:   "Declaration",
	ntVariable:      "Variable",
	ntAllocation:    "Allocation",
	ntScalar:        "Scalar",
	ntDynOffset:     "DynOffset",
	ntPrint:         "Print",
	ntRet:           "Ret",
	ntProcedureCall: "ntProcedureCall",
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
	Name      string
	Offset    uint8
	IsPointer bool
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

func (p *Parser) consume(tokenType TokenType) (Token, error) {
	if p.CurrentToken.Type != tokenType {
		return Token{}, p.unexpectedTokenError(tokenType)
	}

	oldToken := p.CurrentToken

	if p.CurrentTokenIndex < p.NumberOfTokens-1 {
		p.CurrentTokenIndex++
		p.CurrentToken = p.Tokens[p.CurrentTokenIndex]
	}

	return oldToken, nil
}

func (p *Parser) consumeMany(types ...TokenType) error {
	for _, tokenType := range types {
		if _, err := p.consume(tokenType); err != nil {
			return err
		}
	}

	return nil
}

func (p *Parser) isAt(tokenType TokenType) bool {
	return p.Tokens[p.CurrentTokenIndex].Type == tokenType
}

func (p *Parser) isAtSequence(tokenTypes ...TokenType) bool {
	for i, tokenType := range tokenTypes {
		if p.CurrentTokenIndex+i > p.NumberOfTokens {
			return false
		}

		if p.Tokens[p.CurrentTokenIndex+i].Type != tokenType {
			return false
		}
	}

	return true
}

func (p *Parser) parseDirective() (AstTreeNode, error) {
	var err error
	var token Token

	if _, err = p.consume(ttDot); err != nil {
		return AstTreeNode{}, err
	}

	if token, err = p.consume(ttIdentifier); err != nil {
		return AstTreeNode{}, err
	}

	directive := AstTreeNode{
		Type:  ntDirective,
		Value: token.Value,
	}

	var argument Token

	switch strings.ToLower(token.Value) {
	case "module":
		argument, err = p.consume(ttString)

		if err != nil {
			return AstTreeNode{}, err
		}

		directive.Children = append(directive.Children, AstTreeNode{Type: ntArgument, Value: argument.Value})

	case "import":
		argument, err = p.consume(ttString)

		if err != nil {
			return AstTreeNode{}, err
		}

		directive.Children = append(directive.Children, AstTreeNode{Type: ntArgument, Value: argument.Value})

	default:
		return AstTreeNode{}, fmt.Errorf("invalid directive '%s'", token.Value)
	}

	return directive, nil
}

func (p *Parser) parseDeclaration() (AstTreeNode, error) {
	var err error

	if _, err = p.consume(ttDeclare); err != nil {
		return AstTreeNode{}, err
	}

	var modifiers []AstTreeNode

	for p.isAt(ttModifier) {
		var modifier Token

		if modifier, err = p.consume(ttModifier); err != nil {
			return AstTreeNode{}, err
		}

		modifiers = append(modifiers, AstTreeNode{Type: ntModifier, Value: modifier.Value})
	}

	var declaration AstTreeNode
	if p.isAt(ttStruct) {
		declaration, err = p.parseStructure()

		if err != nil {
			return AstTreeNode{}, err
		}
	} else if p.isAt(ttLbracket) {
		declaration, err = p.parseProcedure()

		if err != nil {
			return AstTreeNode{}, err
		}
	} else {
		return AstTreeNode{}, p.unexpectedTokenError(ttIdentifier)
	}

	declaration.Children = append(declaration.Children, modifiers...)

	return declaration, nil
}

func (p *Parser) parseStructure() (AstTreeNode, error) {
	var token Token
	var err error

	node := AstTreeNode{
		Type: ntProcedure,
	}

	if _, err := p.consume(ttStruct); err != nil {
		return AstTreeNode{}, err
	}

	if token, err = p.consume(ttIdentifier); err != nil {
		return AstTreeNode{}, err
	}

	node.Value = token.Value

	for !p.isAt(ttEOF) && !p.isAt(ttEnd) {
		field := AstTreeNode{Type: ntField}
		typeNode, err := p.parseType(&field)

		if err != nil {
			return AstTreeNode{}, err
		}

		field.Children = append(field.Children, *typeNode)

		if typeNode.Type == ntStructure {
			subStructure, err := p.parseImplicitStructure()

			if err != nil {
				return AstTreeNode{}, err
			}

			field.Children = append(field.Children, subStructure)
			continue
		}

		var identifier Token

		if identifier, err = p.consume(ttIdentifier); err != nil {
			return AstTreeNode{}, err
		}

		field.Value = identifier.Value
		node.Children = append(node.Children, field)
	}

	if _, err = p.consume(ttEnd); err != nil {
		return AstTreeNode{}, err
	}

	return node, nil
}

// TODO: Merge with the above function.
func (p *Parser) parseImplicitStructure() (AstTreeNode, error) {
	var node = AstTreeNode{Type: ntStructure}
	var err error

	var identifierToken Token
	if identifierToken, err = p.consume(ttIdentifier); err != nil {
		return AstTreeNode{}, err
	}

	node.Value = identifierToken.Value

	if _, err = p.consume(ttBegin); err != nil {
		return AstTreeNode{}, err
	}

	for !p.isAt(ttEOF) && !p.isAt(ttEnd) {
		field := AstTreeNode{Type: ntField}
		typeNode, err := p.parseType(&field)

		if err != nil {
			return AstTreeNode{}, err
		}

		node.Children = append(node.Children, *typeNode)

		if typeNode.Type == ntStructure {
			subStructure, err := p.parseImplicitStructure()

			if err != nil {
				return AstTreeNode{}, err
			}

			field.Children = append(field.Children, subStructure)
			continue
		}

		var identifier Token

		if identifier, err = p.consume(ttIdentifier); err != nil {
			return AstTreeNode{}, err
		}

		field.Value = identifier.Value
		node.Children = append(node.Children, field)
	}

	if _, err = p.consume(ttEnd); err != nil {
		return AstTreeNode{}, err
	}

	return node, nil
}

func (p *Parser) parseProcedure() (AstTreeNode, error) {
	var node = AstTreeNode{Type: ntProcedure}
	var err error

	// Get the return type of the procedure
	var returnTypeNode *AstTreeNode
	if returnTypeNode, err = p.parseType(&node); err != nil {
		return AstTreeNode{}, err
	}
	node.Children = append(node.Children, *returnTypeNode)

	if _, err = p.consume(ttDoubleColon); err != nil {
		return AstTreeNode{}, err
	}

	// Procedure name
	var identifierToken Token
	if identifierToken, err = p.consume(ttIdentifier); err != nil {
		return AstTreeNode{}, err
	}
	node.Value = identifierToken.Value

	// Arguments
	if _, err = p.consume(ttLparen); err != nil {
		return AstTreeNode{}, err
	}

	for !p.isAt(ttRparen) && !p.isAt(ttEOF) {
		if p.isAt(ttComma) {
			if _, err = p.consume(ttComma); err != nil {
				return AstTreeNode{}, err
			}
		}

		argumentNode := AstTreeNode{Type: ntArgument}

		// Argument type
		var typeNode *AstTreeNode
		if typeNode, err = p.parseType(&argumentNode); err != nil {
			return AstTreeNode{}, err
		}
		argumentNode.ValueType = typeNode.Value

		// Argument name
		var identifierNode Token
		if identifierNode, err = p.consume(ttIdentifier); err != nil {
			return AstTreeNode{}, err
		}
		argumentNode.Value = identifierNode.Value
	}

	if _, err = p.consume(ttRparen); err != nil {
		return AstTreeNode{}, nil
	}

	// Parse body
	var statements []AstTreeNode
	if statements, err = p.parseCompound(); err != nil {
		return AstTreeNode{}, err
	}
	node.Children = append(node.Children, statements...)

	if _, err = p.consume(ttEnd); err != nil {
		return AstTreeNode{}, err
	}

	return node, nil
}

func (p *Parser) parseCompound() ([]AstTreeNode, error) {
	var statements []AstTreeNode

	for !p.isAt(ttEOF) && !p.isAt(ttEnd) {
		switch p.CurrentToken.Type {

		// Variable declaration
		case ttDeclare:
			if node, err := p.parseAssignment(true, false, false); err == nil {
				statements = append(statements, node)
			} else {
				return []AstTreeNode{}, err
			}

		case ttIdentifier:
			if node, err := p.parseAssignment(false, false, false); err == nil {
				statements = append(statements, node)
			} else {
				return []AstTreeNode{}, err
			}

		case ttRet:
			if node, err := p.parseReturnStatement(); err == nil {
				statements = append(statements, node)
			} else {
				return []AstTreeNode{}, err
			}

		case ttPrint:
			if node, err := p.parsePrintStatement(); err == nil {
				statements = append(statements, node)
			} else {
				return []AstTreeNode{}, err
			}

		case ttCall:
			if node, err := p.parseProcedureCall(); err == nil {
				statements = append(statements, node)
			} else {
				return []AstTreeNode{}, err
			}
		// case ttIncrement, ttDecrement:

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

func (p *Parser) parseReturnStatement() (AstTreeNode, error) {
	if _, err := p.consume(ttRet); err != nil {
		return AstTreeNode{}, err
	}

	if variable, err := p.parseAssignment(false, false, true); err == nil {
		return AstTreeNode{Type: ntRet, Children: []AstTreeNode{variable}}, nil
	} else {
		return AstTreeNode{}, err
	}
}

func (p *Parser) parsePrintStatement() (AstTreeNode, error) {
	if _, err := p.consume(ttPrint); err != nil {
		return AstTreeNode{}, err
	}

	if variable, err := p.parseAssignment(false, false, true); err == nil {
		return AstTreeNode{Type: ntPrint, Children: []AstTreeNode{variable}}, nil
	} else {
		return AstTreeNode{}, err
	}
}

func (p *Parser) parseAutoMemoryAllocation() (AstTreeNode, error) {
	var err error

	if _, err = p.consume(ttInit); err != nil {
		return AstTreeNode{}, nil
	}

	node := AstTreeNode{Type: ntAllocation}

	if token, err := p.parseType(&node); err == nil {
		node.ValueType = token.Value
	} else {
		return AstTreeNode{}, err
	}

	return node, nil
}

func (p *Parser) parseAssignment(isDeclaration bool, isAnon bool, skipLeftOperand bool) (AstTreeNode, error) {
	assignmentNode := AstTreeNode{Type: ntAssignment}
	variableNode := AstTreeNode{Type: ntVariable}
	var err error

	if !skipLeftOperand {
		// In case of structures, the left operand is not a variable but rather a field.
		if isAnon {
			variableNode.Type = ntField
		}

		// Variable type. Note that if this is a declaration and the type is missing,
		// an error will be thrown later.
		if isDeclaration {
			if _, err = p.consume(ttDeclare); err != nil {
				return AstTreeNode{}, err
			}

			if typeNode, err := p.parseType(&variableNode); err == nil {
				variableNode.ValueType = typeNode.Value
			} else {
				return AstTreeNode{}, err
			}

			assignmentNode.Type = ntDeclaration
		}

		// Variable name. In case of declarations only a single identifier is allowed
		if isDeclaration {
			if identifierNode, err := p.consume(ttIdentifier); err == nil {
				variableNode.Value = identifierNode.Value
			} else {
				return AstTreeNode{}, err
			}
		} else {
			if identifierNode, err := p.getVariableName(); err == nil {
				variableNode.Value = identifierNode.Value
			} else {
				return AstTreeNode{}, err
			}
		}

		if _, err = p.consume(ttColon); err != nil {
			return AstTreeNode{}, err
		}
	}

	var rightOperand AstTreeNode
	switch p.CurrentToken.Type {
	case ttInit:
		rightOperand, err = p.parseAutoMemoryAllocation()

		if err != nil {
			return AstTreeNode{}, err
		}
	case ttNumber:
		rightOperand = AstTreeNode{Type: ntScalar, ValueType: "number"}

		if token, err := p.consume(ttNumber); err == nil {
			rightOperand.Value = token.Value
		} else {
			return AstTreeNode{}, err
		}
	case ttString:
		rightOperand = AstTreeNode{Type: ntScalar, ValueType: "string"}

		if token, err := p.consume(ttString); err == nil {
			rightOperand.Value = token.Value
			rightOperand.ValueSize = uint8(len(token.Value))
		} else {
			return AstTreeNode{}, err
		}
	case ttIdentifier:
		rightOperand = AstTreeNode{Type: ntVariable, ValueType: "unknown"}

		if token, err := p.consume(ttIdentifier); err == nil {
			rightOperand.Value = token.Value
		} else {
			return AstTreeNode{}, err
		}
	case ttAsterisk:
		rightOperand = AstTreeNode{Type: ntVariable, ValueType: "unknown", IsPointer: true}

		if _, err := p.consume(ttAsterisk); err != nil {
			return AstTreeNode{}, err
		}

		if token, err := p.consume(ttIdentifier); err == nil {
			rightOperand.Value = token.Value
		} else {
			return AstTreeNode{}, err
		}
	case ttDeclare:
		if rightOperand, err = p.parseAnonDeclaration(); err != nil {
			return AstTreeNode{}, err
		}
	case ttCall:
		if rightOperand, err = p.parseProcedureCall(); err != nil {
			return AstTreeNode{}, err
		}
	default:
		return AstTreeNode{}, p.unexpectedTokenError(ttIdentifier)
	}

	// Assign variable as left operand
	assignmentNode.Children = append(assignmentNode.Children, variableNode, rightOperand)

	return assignmentNode, nil
}

func (p *Parser) parseAnonDeclaration() (AstTreeNode, error) {
	declarationNode := AstTreeNode{Type: ntStructure}

	if err := p.consumeMany(ttDeclare, ttAnon); err != nil {
		return AstTreeNode{}, err
	}

	for !p.isAt(ttEOF) && !p.isAt(ttEnd) {
		// Might just be regular variable assignment
		if p.isAtSequence(ttIdentifier, ttColon) {
			if fieldNode, err := p.parseAssignment(false, true, false); err == nil {
				declarationNode.Children = append(declarationNode.Children, fieldNode)
			} else {
				return AstTreeNode{}, err
			}

			continue
		}

		// Shorthand notation, e.g., (first-name : first-name) -> (first-name)
		if p.isAtSequence(ttIdentifier, ttIdentifier) || p.isAtSequence(ttIdentifier, ttEnd) || p.isAtSequence(ttIdentifier, ttEnd) {
			assignmentNode := AstTreeNode{Type: ntAssignment}

			if token, err := p.consume(ttIdentifier); err == nil {
				assignmentNode.Children = append(assignmentNode.Children, AstTreeNode{Type: ntField, Value: token.Value}, AstTreeNode{Type: ntVariable, Value: token.Value})
			} else {
				return AstTreeNode{}, err
			}

			continue
		}

		return AstTreeNode{}, p.unexpectedTokenError(ttColon)
	}

	if _, err := p.consume(ttEnd); err != nil {
		return AstTreeNode{}, err
	}

	return declarationNode, nil
}

func (p *Parser) parseType(parent *AstTreeNode) (*AstTreeNode, error) {
	node := AstTreeNode{
		Type: ntType,
	}

	var err error
	var token Token

	if _, err = p.consume(ttLbracket); err != nil {
		return &AstTreeNode{}, err
	}

	if token, err = p.consume(ttIdentifier); err != nil {
		// We might be dealing with a struct type (which is denoted by a dedicated token)
		var structTypeError error
		if token, structTypeError = p.consume(ttStruct); structTypeError != nil {
			return &AstTreeNode{}, err
		}
	}

	node.Value = token.Value

	if token.Type == ttStruct {
		node.Type = ntStructure
	} else {
		node.Type = ntType
	}

	if p.isAt(ttLparen) {
		if _, err = p.consume(ttLparen); err != nil {
			return &AstTreeNode{}, err
		}

		if token, err = p.consume(ttNumber); err != nil {
			return &AstTreeNode{}, err
		}

		var u64 uint64
		if u64, err = strconv.ParseUint(token.Value, 10, 8); err != nil {
			return &AstTreeNode{}, err
		}

		// TODO: Add check here for math.MaxUint8
		node.ValueSize = uint8(u64)

		if _, err = p.consume(ttRparen); err != nil {
			return &AstTreeNode{}, err
		}
	}

	// Is this a pointer?
	if p.isAt(ttAsterisk) {
		if _, err := p.consume(ttAsterisk); err != nil {
			return &AstTreeNode{}, err
		}

		node.IsPointer = true
	}

	if _, err = p.consume(ttRbracket); err != nil {
		return &AstTreeNode{}, err
	}

	parent.Children = append(parent.Children, node)

	return &node, nil
}

func (p *Parser) getVariableName() (AstTreeNode, error) {
	node := AstTreeNode{Type: ntVariable}

	if variable, err := p.consume(ttIdentifier); err == nil {
		node.Name = variable.Value
	} else {
		return AstTreeNode{}, err
	}

	childNode := node
	for p.isAt(ttDoubleColon) {
		if _, err := p.consume(ttDoubleColon); err != nil {
			return AstTreeNode{}, err
		}

		if partialIdentifier, err := p.consume(ttIdentifier); err == nil {
			childNode.Children = append(childNode.Children, AstTreeNode{Type: ntVariable, Name: partialIdentifier.Value})
		} else {
			return AstTreeNode{}, err
		}

		// Do we have to take into account an offset?
		if p.isAt(ttLparen) {
			if _, err := p.consume(ttLparen); err != nil {
				return AstTreeNode{}, err
			}

			// TODO: Check for overflow
			if offsetToken, err := p.consume(ttNumber); err == nil {
				var u64 uint64
				if u64, err = strconv.ParseUint(offsetToken.Value, 10, 8); err != nil {
					return AstTreeNode{}, err
				}
				childNode.Children[0].Offset = uint8(u64)
			} else if variable, err := p.getVariableName(); err == nil {
				variable.Type = ntDynOffset
				childNode.Children[0].Children = append(childNode.Children[0].Children, variable)
			} else {
				return AstTreeNode{}, err
			}

			if _, err := p.consume(ttRparen); err != nil {
				return AstTreeNode{}, err
			}
		}

		childNode = childNode.Children[0]
	}

	return node, nil
}

func (p *Parser) parseProcedureCall() (AstTreeNode, error) {
	node := AstTreeNode{Type: ntProcedureCall}

	if _, err := p.consume(ttCall); err != nil {
		return AstTreeNode{}, err
	}

	// Name of the procedure
	if token, err := p.consume(ttIdentifier); err == nil {
		node.Name = token.Value
	} else {
		return AstTreeNode{}, err
	}

	// Arguments
	if _, err := p.consume(ttLparen); err != nil {
		return AstTreeNode{}, err
	}

	for !p.isAt(ttRparen) && !p.isAt(ttEOF) {
		// Treat this as an assignment without a left operand.
		if argument, err := p.parseAssignment(false, false, true); err == nil {
			argument.Type = ntArgument
			node.Children = append(node.Children, argument)
		} else {
			return AstTreeNode{}, err
		}

		if !p.isAt(ttRparen) {
			if _, err := p.consume(ttComma); err != nil {
				return AstTreeNode{}, err
			}
		}
	}

	if _, err := p.consume(ttRparen); err != nil {
		return AstTreeNode{}, err
	}

	return node, nil
}

func (p *Parser) GenerateAst() (AstTreeNode, error) {
	rootNode := AstTreeNode{Type: ntRoot}

	for p.CurrentTokenIndex < p.NumberOfTokens-1 {
		switch p.CurrentToken.Type {
		case ttDot:
			if directive, err := p.parseDirective(); err == nil {
				rootNode.Children = append(rootNode.Children, directive)
			} else {
				return rootNode, err
			}
		case ttDeclare:
			if declaration, err := p.parseDeclaration(); err == nil {
				rootNode.Children = append(rootNode.Children, declaration)
			} else {
				return rootNode, err
			}
		case ttEOF:
			if _, err := p.consume(ttEOF); err != nil {
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
