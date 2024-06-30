package core

type Analyzer struct {
	Ast AstTreeNode
}

func InitializeAnalyzer(tree AstTreeNode) (*Analyzer, error) {
	return &Analyzer{
		Ast: tree,
	}, nil
}
