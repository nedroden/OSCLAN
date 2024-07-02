package core

import (
	"fmt"

	"github.com/nedroden/nasgo/pkg/util"
)

type Scope struct {
	variables   map[string]Variable
	parentScope *Scope
	depth       int8
}

func CreateScope(parentScope *Scope, depth int8) *Scope {
	return &Scope{
		variables:   make(map[string]Variable),
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
	Type          string
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

func (a *Analyzer) RunOnNode(node *AstTreeNode) error {
	// Variable declaration
	if node.Type == ntDeclaration {
		if len(node.Children) != 2 {
			return fmt.Errorf("expected assignment node to have two children")
		}

		variableChild := node.Children[0]
		valueChild := node.Children[1]

		variable := Variable{Name: variableChild.Value}

		if err := a.declareVariable(variable); err != nil {
			return err
		}

		fmt.Printf("assigning variable '%s' a default value of '%s'\n", variableChild.Value, valueChild.Value)
	} else if node.Type == ntVariable {
		if variable, err := a.resolveVariable(node.Value); err == nil {
			fmt.Printf("Successfully resolved variable with name '%s'\n", variable.UnmangledName)
		} else {
			return err
		}
	}

	for _, child := range node.Children {
		if err := a.RunOnNode(&child); err != nil {
			return err
		}
	}

	return nil
}

func (a *Analyzer) Run() (AstTreeNode, error) {
	if err := a.RunOnNode(&a.Ast); err != nil {
		return a.Ast, err
	}

	return a.Ast, nil
}
