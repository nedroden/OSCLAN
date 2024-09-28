package core

import (
	"fmt"
	"strings"
)

type Analyzer struct {
	Ast    AstTreeNode
	Scopes ScopeStack
}

func InitializeAnalyzer(tree AstTreeNode) *Analyzer {
	rootScope := CreateScope(0)

	return &Analyzer{
		Ast:    tree,
		Scopes: ScopeStack{*rootScope},
	}
}

func (a *Analyzer) attemptFieldResolution(path string) error {
	var foundType Type
	parts := strings.Split(path, "::")

	if variable, err := a.Scopes.ResolveVariable(parts[0]); err == nil {
		foundType = variable.Type
	} else {
		return fmt.Errorf("could not resolve root type '%s'", parts[0])
	}

	for _, partialPath := range parts[1:] {
		if subType, found := foundType.SubTypes[partialPath]; found {
			foundType = subType
		} else {
			return fmt.Errorf("could not resolve path '%s'", path)
		}
	}

	return nil
}

func (a *Analyzer) analyzeDeclaration(node *AstTreeNode) error {
	var err error

	if len(node.Children) != 2 {
		return fmt.Errorf("expected assignment node to have two children")
	}

	variableChild := node.Children[0]
	valueChild := node.Children[1]

	var variableType Type
	if variableType, err = a.Scopes.ResolveType(variableChild.ValueType); err != nil {
		return err
	}

	// Type of the value assigned to the variable
	var valueType Type
	if valueType, err = GetImplicitType(valueChild); err != nil {
		return err
	}

	// If the variable was declared with an implicit size, use the one from the value
	if variableType.Size == 0 {
		variableType.Size = valueType.Size
	}

	assignmentCompatibility := PerformAssignmentTypeCheck(variableType, valueType)
	if assignmentCompatibility == Illegal {
		return fmt.Errorf("operation %s <- %s is illegal", variableType.ToString(), valueType.ToString())
	} else if assignmentCompatibility == LossOfInformation {
		fmt.Printf("[WARNING]: operation %s <- %s might lead to loss of information\n", variableType.ToString(), valueType.ToString())
	}

	// Add the variable to the current scope
	variable := Variable{
		Name: variableChild.Value,
		Type: variableType,
	}

	if err := a.Scopes.DeclareVariable(variable); err != nil {
		return err
	}

	// fmt.Printf("declared variable '%s' with type '%s' and default value '%s'\n", variable.Name, variableType.ToString(), valueChild.Value)

	return nil
}

// TODO: Declare fields separately
func (a *Analyzer) analyzeStructDeclaration(node *AstTreeNode) error {
	structType, err := GetType(*node)

	if err != nil {
		return err
	}

	// This is an anonymous structure, hence we cannot declare it (nor do we need to).
	if len(strings.TrimSpace(structType.Name)) == 0 {
		return nil
	}

	// This type has already been declared through its parent
	if strings.Contains(node.Path, "::") {
		return nil
	}

	if err := a.Scopes.DeclareType(structType); err != nil {
		return err
	}

	return nil
}

func (a *Analyzer) analyzeArgumentDeclaration(node *AstTreeNode) error {
	var err error
	var variableType Type

	if children := len(node.Children); children != 1 {
		// Too many children to properly interpret what we want
		if children > 1 {
			return fmt.Errorf("invalid argument declaration")
		}

		// If there are no children at all we're talking about a directive
		return nil
	}

	typeChild := node.Children[0]

	// This is an argument being passed rather than declared
	if typeChild.Type != ntType {
		return nil
	}

	if variableType, err = a.Scopes.ResolveType(typeChild.Value); err != nil {
		return err
	}

	variable := Variable{
		Name: node.Value,
		Type: variableType,
	}

	if err := a.Scopes.DeclareVariable(variable); err != nil {
		return err
	}

	return nil
}

func (a *Analyzer) analyzeVariableReference(node *AstTreeNode) error {
	// Check if the variable exists at all
	if _, err := a.Scopes.ResolveVariable(node.Value); err != nil {
		return err
	}

	// E.g., list::name::first-name
	for _, child := range node.Children {
		if err := a.analyzeReferredField(child); err != nil {
			return err
		}
	}

	return nil
}

func (a *Analyzer) analyzeReferredField(node AstTreeNode) error {
	if err := a.attemptFieldResolution(node.Path); err != nil {
		return err
	}

	// Check if the variable exists at all
	for _, child := range node.Children {
		if child.Type != ntField {
			continue
		}

		if err := a.analyzeReferredField(child); err != nil {
			return err
		}
	}

	return nil
}

func (a *Analyzer) RunOnNode(node *AstTreeNode, scope *Scope) error {
	a.Scopes.Push(*scope)
	var err error

	// Variable declaration
	switch node.Type {
	case ntStructure:
		if err = a.analyzeStructDeclaration(node); err != nil {
			return err
		}
	case ntDeclaration:
		if err = a.analyzeDeclaration(node); err != nil {
			return err
		}
	case ntArgument:
		if err := a.analyzeArgumentDeclaration(node); err != nil {
			return err
		}
	case ntVariable:
		if err := a.analyzeVariableReference(node); err != nil {
			return err
		}
	default:
	}

	for _, child := range node.Children {
		subScope := scope

		// Create sub scope if this is a procedure or structure
		if child.Type == ntStructure || child.Type == ntProcedure {
			subScope = CreateScope(scope.Depth + 1)
		}

		if err := a.RunOnNode(&child, subScope); err != nil {
			return err
		}
	}

	// For debugging purposes, display the resulting symbol table
	currentScope := a.Scopes.Peek()
	if (node.Type == ntStructure || node.Type == ntProcedure) && len(currentScope.Variables)+len(currentScope.CustomTypes) > 0 {
		a.Scopes.Pop().PrintTable()
	}

	return nil
}

func (a *Analyzer) Run() (AstTreeNode, error) {
	// TODO: Find a cleaner way to declare built-in types
	_ = a.Scopes.DeclareType(Type{Name: "int", Size: 0})
	_ = a.Scopes.DeclareType(Type{Name: "uint", Size: 0})
	_ = a.Scopes.DeclareType(Type{Name: "string", Size: 0})

	if err := a.RunOnNode(&a.Ast, &a.Scopes[0]); err != nil {
		return a.Ast, err
	}

	return a.Ast, nil
}
