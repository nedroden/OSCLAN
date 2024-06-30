package core

import (
	"encoding/json"
	"errors"
	"fmt"
	"os"
)

type CompilerOptions struct {
	SaveIntermediate bool
}

func GenerateIl(options CompilerOptions) error {
	var tokenizer *Tokenizer
	var parser *Parser
	var err error

	if len(os.Args) < 2 {
		return errors.New("target file not specified")
	}

	target := os.Args[1]

	if tokenizer, err = InitTokenizer(target, "Examples"); err != nil {
		return err
	}

	os.RemoveAll("output")
	os.Mkdir("output", 0755)

	var tokens []Token
	if tokens, err = tokenizer.GetTokens(); err != nil {
		return err
	}

	if bytes, err := json.MarshalIndent(tokens, "", "\t"); err == nil {
		os.WriteFile(fmt.Sprintf("output/%s.tokens.json", target), bytes, 0644)
	} else {
		return err
	}

	if parser, err = InitializeParser(tokens); err != nil {
		return err
	}

	var ast AstTreeNode
	if ast, err = parser.GenerateAst(); err != nil {
		return err
	}

	if bytes, err := json.MarshalIndent(ast, "", "\t"); err == nil {
		os.WriteFile(fmt.Sprintf("output/%s.ast.json", target), bytes, 0644)
	} else {
		return err
	}

	return nil
}
