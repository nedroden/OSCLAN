package core

type VariableScope struct {
	childScopes []VariableScope
	variables   map[string]Variable
	parentScope *VariableScope
	depth       int8
}

func (s VariableScope) InScope(name string) bool {
	if _, found := s.variables[name]; found {
		return true
	}

	if s.parentScope == nil {
		return false
	}

	return s.parentScope.InScope(name)
}

type Variable struct {
	Name        string
	Type        string
	IsPointer   bool
	SizeInBytes uint8
}

type Analyzer struct {
	Ast AstTreeNode
}

func InitializeAnalyzer(tree AstTreeNode) (*Analyzer, error) {
	// rootScope := VariableScope{
	// 	childScopes: []VariableScope{},
	// 	parentScope: nil,
	// 	depth:       0,
	// }

	return &Analyzer{
		Ast: tree,
	}, nil
}
