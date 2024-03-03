package core

import "testing"

// TODO: finish
func TestParseType(t *testing.T) {
	tokens := []Token{
		{Type: Identifier, Value: "List"},
	}

	_, err := InitializeParser(tokens)

	if err != nil {
		t.Fatalf("unable to initialize parser: %s", err)
	}
}
