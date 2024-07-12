package core

import (
	"fmt"

	"github.com/nedroden/nasgo/pkg/util"
)

type Scope struct {
	variables   map[string]Variable
	customTypes map[string]CompositeType
	parentScope *Scope
	depth       int8
}

func CreateScope(parentScope *Scope, depth int8) *Scope {
	return &Scope{
		variables:   make(map[string]Variable),
		customTypes: make(map[string]CompositeType),
		parentScope: parentScope,
		depth:       depth,
	}
}

func (s Scope) InCurrentScope(name string) bool {
	if _, found := s.variables[name]; found {
		return true
	}

	return false
}

func (s Scope) InScope(name string) bool {
	if _, found := s.variables[name]; found {
		fmt.Printf("Found variable with name '%s' in level '%d' scope\n", name, s.depth)
		return true
	}

	if s.parentScope == nil {
		return false
	}

	return s.parentScope.InScope(name)
}

type Variable struct {
	Name          string
	UnmangledName string
	Type          ElementaryType
	IsPointer     bool
	SizeInBytes   uint64
}

type Analyzer struct {
	Ast          AstTreeNode
	CurrentScope *Scope
}

func InitializeAnalyzer(tree AstTreeNode) *Analyzer {
	rootScope := CreateScope(nil, 0)

	return &Analyzer{
		Ast:          tree,
		CurrentScope: rootScope,
	}
}

func (a *Analyzer) resolveVariable(name string) (Variable, error) {
	if variable, found := (*a.CurrentScope).variables[util.Mangle(name)]; found {
		return variable, nil
	}

	return Variable{}, fmt.Errorf("unresolved variable '%s'", name)
}

func (a *Analyzer) declareVariable(variable Variable) error {
	variable.UnmangledName = variable.Name
	variable.Name = util.Mangle(variable.Name)

	if a.CurrentScope.InCurrentScope(variable.Name) {
		return fmt.Errorf("cannot redeclare variable '%s'", variable.Name)
	}

	a.CurrentScope.variables[variable.Name] = variable

	return nil
}

func (a *Analyzer) declareType(s CompositeType) error {
	if a.CurrentScope.InCurrentScope(s.Name) {
		return fmt.Errorf("cannot redeclare type '%s'", s.Name)
	}

	a.CurrentScope.customTypes[s.Name] = s

	return nil
}

func (a *Analyzer) analyzeDeclaration(node *AstTreeNode) error {
	var err error

	if len(node.Children) != 2 {
		return fmt.Errorf("expected assignment node to have two children")
	}

	variableChild := node.Children[0]
	valueChild := node.Children[1]

	variableType := GetElementaryType(variableChild.ValueType, variableChild.ValueSize)

	// Type of the value assigned to the variable
	var valueType ElementaryType
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

	if err := a.declareVariable(variable); err != nil {
		return err
	}

	fmt.Printf("declared variable '%s' with type '%s' and default value '%s'\n", variable.Name, variableType.ToString(), valueChild.Value)

	return nil
}

func (a *Analyzer) analyzeStructDeclaration(node *AstTreeNode) error {
	structType, err := GetCompositeType(*node)

	if err != nil {
		return err
	}

	if err := a.declareType(structType); err != nil {
		return err
	}

	fmt.Printf("declared type '%s' with size '%d'\n", structType.Name, structType.GetSize())

	return nil
}

func (a *Analyzer) RunOnNode(node *AstTreeNode, scope *Scope) error {
	a.CurrentScope = scope
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
	}
	// else if node.Type == ntVariable {
	// 	if variable, err := a.resolveVariable(node.Value); err == nil {
	// 		fmt.Printf("Successfully resolved variable with name '%s'\n", variable.UnmangledName)
	// 	} else {
	// 		return err
	// 	}
	// }

	for _, child := range node.Children {
		if err := a.RunOnNode(&child, CreateScope(scope, scope.depth+1)); err != nil {
			return err
		}
	}

	return nil
}

func (a *Analyzer) Run() (AstTreeNode, error) {
	if err := a.RunOnNode(&a.Ast, CreateScope(nil, 0)); err != nil {
		return a.Ast, err
	}

	return a.Ast, nil
}
