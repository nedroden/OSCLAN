package core

import (
	"errors"
	"fmt"
	"os"
	"strings"
)

type CompilerOptions struct {
	SaveIntermediate bool
}

func saveOutput(filename string, body string) error {
	os.RemoveAll("output")
	os.Mkdir("output", 0755)

	if err := os.WriteFile(fmt.Sprintf("output/%s", filename), []byte(body), 0755); err != nil {
		return err
	}

	return nil
}

func GenerateIl(options CompilerOptions) error {
	var tokenizer *Tokenizer
	var parser *Parser
	var err error

	if len(os.Args) < 2 {
		return errors.New("target file not specified")
	}

	target := os.Args[1]

	if tokenizer, err = InitTokenizer(target, "examples"); err != nil {
		return err
	}

	var tokens []Token
	if tokens, err = tokenizer.GetTokens(); err != nil {
		return err
	}

	if parser, err = InitializeParser(tokens); err != nil {
		return err
	}

	if err = saveTokens(tokens, target); err != nil {
		return err
	}

	var ast AstTreeNode
	if _, err = parser.GenerateAst(); err != nil {
		return err
	}

	if err := saveOutput(fmt.Sprintf("%s_ast", target), ast.ToString()); err != nil {
		return err
	}

	return nil
}

func saveTokens(tokens []Token, target string) error {
	var sb strings.Builder

	for _, token := range tokens {
		sb.WriteString(fmt.Sprintf("%s\n", token.ToString()))
	}

	if err := saveOutput(fmt.Sprintf("%s_tokens", target), sb.String()); err != nil {
		return err
	}

	return nil
}
