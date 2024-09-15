package core

import (
	"fmt"
	"os"
	"strings"

	"github.com/jedib0t/go-pretty/table"
	"github.com/nedroden/OSCLAN/pkg/util"
)

type Variable struct {
	Name          string
	UnmangledName string
	Type          Type
	IsPointer     bool
	SizeInBytes   uint64
}

type ScopeStack []Scope

func (s *ScopeStack) Peek() Scope {
	if len(*s) == 0 {
		return Scope{}
	}

	return (*s)[len(*s)-1]
}

func (s *ScopeStack) Pop() Scope {
	if len(*s) == 0 {
		return Scope{}
	}

	top := s.Peek()
	*s = (*s)[1:]
	return top
}

func (s *ScopeStack) Push(scope Scope) {
	*s = append(*s, scope)
}

type Scope struct {
	Variables   map[string]Variable
	CustomTypes map[string]Type
	Depth       int8
}

func CreateScope(depth int8) *Scope {
	return &Scope{
		Variables:   make(map[string]Variable),
		CustomTypes: make(map[string]Type),
		Depth:       depth,
	}
}

func (s Scope) PrintTable() {
	writer := table.NewWriter()
	writer.SetOutputMirror(os.Stdout)

	writer.AppendHeader(table.Row{"Key", "Name (unmangled)", "Symbol type", "Type", "Size"})

	for _, variable := range s.Variables {
		writer.AppendRow(table.Row{variable.Name, variable.UnmangledName, "Variable", variable.Type.ToString(), variable.Type.Size})
	}

	for _, vType := range s.CustomTypes {
		writer.AppendRow(table.Row{vType.Name, "-", "Type", "-", vType.GetSize()})
	}

	fmt.Printf("Current scope (level %d):\n:", s.Depth)
	writer.Render()
}

func (s ScopeStack) InCurrentScope(name string) bool {
	if _, found := s.Peek().Variables[name]; found {
		return true
	}

	return false
}

func (s ScopeStack) InScope(name string) bool {
	for i := range len(s) {
		scope := s[i]

		if _, found := scope.Variables[name]; found {
			fmt.Printf("Found variable with name '%s' in level '%d' scope\n", name, scope.Depth)
			return true
		}
	}

	return false
}

func (s ScopeStack) ResolveVariable(name string) (Variable, error) {
	for i := range len(s) {
		scope := s[i]

		// Variable found in current scope?
		if variable, found := scope.Variables[util.Mangle(name)]; found {
			return variable, nil
		}
	}

	// Variable not found in any scope
	return Variable{}, fmt.Errorf("unresolved variable '%s'", name)
}

func (s ScopeStack) ResolveType(name string) (Type, error) {
	for i := range len(s) {
		scope := s[i]

		// Type found in current scope?
		if customType, found := scope.CustomTypes[util.Mangle(name)]; found {
			return customType, nil
		}
	}

	// Type not found in any scope
	return Type{}, fmt.Errorf("unresolved type '%s'", name)
}

func (s ScopeStack) DeclareVariable(variable Variable) error {
	variable.UnmangledName = variable.Name
	variable.Name = util.Mangle(variable.Name)

	currentScope := s.Peek()

	if len(strings.TrimSpace(variable.UnmangledName)) == 0 {
		return fmt.Errorf("compiler error in variable node creation. variable has no name")
	}

	if s.InCurrentScope(variable.Name) {
		return fmt.Errorf("cannot redeclare variable '%s'", variable.Name)
	}

	currentScope.Variables[variable.Name] = variable

	return nil
}

func (s ScopeStack) DeclareType(t Type) error {
	scope := s.Peek()

	// Types declared at root level should be available everywhere
	if scope.Depth == 1 {
		scope = s[0]
	}

	if s.InScope(t.Name) {
		return fmt.Errorf("cannot redeclare type '%s'", t.Name)
	}

	scope.CustomTypes[util.Mangle(t.Name)] = t

	return nil
}
