package core

// import (
// 	"testing"
// )

// func initParser(tokens []Token, t *testing.T) *Parser {
// 	p, err := InitializeParser(tokens)

// 	if err != nil {
// 		t.Fatalf("unable to initialize parser: %s", err)
// 	}

// 	return p
// }

// func TestParseTypeList(t *testing.T) {
// 	tokens := []Token{
// 		{Type: Identifier, Value: "List"},
// 		{Type: EOF},
// 	}

// 	parser := initParser(tokens, t)
// 	rootNode := &AstTreeNode{Type: Root}
// 	parser.ParseType(rootNode)

// 	if len(rootNode.Children) != 1 {
// 		t.Fatalf("unable to parse type 'List'")
// 	}
// }

// func TestParseTypeListWithSingleArgument(t *testing.T) {
// 	tokens := []Token{
// 		{Type: Identifier, Value: "List"},
// 		{Type: Lt},
// 		{Type: Identifier, Value: "string"},
// 		{Type: Gt},
// 		{Type: EOF},
// 	}

// 	parser := initParser(tokens, t)
// 	rootNode := &AstTreeNode{Type: Root}
// 	_, err := parser.ParseType(rootNode)

// 	if err != nil {
// 		t.Fatal(err)
// 	}

// 	if len(rootNode.Children) != 1 || len(rootNode.Children[0].Children) != 1 || rootNode.Children[0].Children[0].Value != "string" {
// 		t.Fatalf("unable to parse type 'List<string>'")
// 	}
// }
