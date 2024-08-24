package core

import (
	"fmt"
	"os"
	"strings"

	"github.com/jedib0t/go-pretty/table"
	"github.com/nedroden/OSCLAN/pkg/util"
)

// TODO: Hier iets mee doen.
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
	ParentScope *Scope
	Depth       int8
}

func CreateScope(parentScope *Scope, depth int8) *Scope {
	return &Scope{
		Variables:   make(map[string]Variable),
		CustomTypes: make(map[string]Type),
		ParentScope: parentScope,
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

func (s Scope) InCurrentScope(name string) bool {
	if _, found := s.Variables[name]; found {
		return true
	}

	return false
}

func (s Scope) InScope(name string) bool {
	if _, found := s.Variables[name]; found {
		fmt.Printf("Found variable with name '%s' in level '%d' scope\n", name, s.Depth)
		return true
	}

	if s.ParentScope == nil {
		return false
	}

	return s.ParentScope.InScope(name)
}

type Variable struct {
	Name          string
	UnmangledName string
	Type          Type
	IsPointer     bool
	SizeInBytes   uint64
}

type Analyzer struct {
	Ast          AstTreeNode
	CurrentScope *Scope
	RootScope    *Scope
}

func InitializeAnalyzer(tree AstTreeNode) *Analyzer {
	rootScope := CreateScope(nil, 0)

	return &Analyzer{
		Ast:          tree,
		CurrentScope: rootScope,
		RootScope:    rootScope,
	}
}

func (a *Analyzer) resolveVariable(name string) (Variable, error) {
	if variable, found := (*a.CurrentScope).Variables[util.Mangle(name)]; found {
		return variable, nil
	}

	return Variable{}, fmt.Errorf("unresolved variable '%s'", name)
}

func (a *Analyzer) resolveTypeInScope(name string, scope Scope) (Type, error) {
	// Type found in current scope?
	if customType, found := scope.CustomTypes[util.Mangle(name)]; found {
		return customType, nil
	}

	// Type found in parent scope?
	if scope.ParentScope != nil {
		if customType, err := a.resolveTypeInScope(name, *scope.ParentScope); err == nil {
			return customType, nil
		}
	}

	// Type not found in any scope
	return Type{}, fmt.Errorf("unresolved type '%s'", name)
}

func (a *Analyzer) resolveType(name string) (Type, error) {
	return a.resolveTypeInScope(name, *a.CurrentScope)
}

func (a *Analyzer) attemptFieldResolution(path string) error {
	var foundType Type
	parts := strings.Split(path, "::")

	if variable, err := a.resolveVariable(parts[0]); err == nil {
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

func (a *Analyzer) declareVariable(variable Variable) error {
	variable.UnmangledName = variable.Name
	variable.Name = util.Mangle(variable.Name)

	if len(strings.TrimSpace(variable.Name)) == 0 {
		return fmt.Errorf("compiler error in variable node creation. variable has no name")
	}

	if a.CurrentScope.InCurrentScope(variable.Name) {
		return fmt.Errorf("cannot redeclare variable '%s'", variable.Name)
	}

	a.CurrentScope.Variables[variable.Name] = variable

	return nil
}

func (a *Analyzer) declareType(s Type) error {
	scope := a.CurrentScope

	// Types declared at root level should be available everywhere
	if a.CurrentScope.Depth == 1 {
		scope = a.RootScope
	}

	if scope.InCurrentScope(s.Name) {
		return fmt.Errorf("cannot redeclare type '%s'", s.Name)
	}

	scope.CustomTypes[util.Mangle(s.Name)] = s

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
	if variableType, err = a.resolveType(variableChild.ValueType); err != nil {
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

	if err := a.declareVariable(variable); err != nil {
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

	if err := a.declareType(structType); err != nil {
		return err
	}

	return nil
}

func (a *Analyzer) analyzeVariableReference(node *AstTreeNode) error {
	// Check if the variable exists at all
	if _, err := a.resolveVariable(node.Value); err != nil {
		return err
	}

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
			subScope = CreateScope(scope, scope.Depth+1)
		}

		if err := a.RunOnNode(&child, subScope); err != nil {
			return err
		}

		a.CurrentScope = subScope
	}

	// For debugging purposes, display the resulting symbol table
	if (node.Type == ntStructure || node.Type == ntProcedure) && len(a.CurrentScope.Variables)+len(a.CurrentScope.CustomTypes) > 0 {
		a.CurrentScope.PrintTable()
	}

	return nil
}

func (a *Analyzer) Run() (AstTreeNode, error) {
	// TODO: Find a cleaner way to declare built-in types
	_ = a.declareType(Type{Name: "int", Size: 0})
	_ = a.declareType(Type{Name: "uint", Size: 0})
	_ = a.declareType(Type{Name: "string", Size: 0})

	if err := a.RunOnNode(&a.Ast, a.CurrentScope); err != nil {
		return a.Ast, err
	}

	return a.Ast, nil
}
