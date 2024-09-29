package core

import (
	"errors"
	"flag"
	"fmt"
	"os"
	"strings"

	"github.com/nedroden/OSCLAN/pkg/util"
)

type CompilerOptions struct {
	SaveIntermediate bool
}

func GenerateIl(options CompilerOptions) error {
	var tokenizer *Tokenizer
	var parser *Parser
	var err error

	if len(flag.Args()) < 1 {
		return errors.New("target file not specified")
	}

	target := flag.Args()[0]

	if tokenizer, err = InitTokenizer(target, "Examples"); err != nil {
		return err
	}

	os.RemoveAll("output")
	os.Mkdir("output", 0755)

	// Step 1: Tokenization
	var tokens []Token
	if tokens, err = tokenizer.GetTokens(); err != nil {
		return err
	}

	if bytes, err := util.SerializeJson(tokens); err == nil && options.SaveIntermediate {
		os.WriteFile(fmt.Sprintf("output/%s.tokens.json", target), bytes, 0755)
	} else {
		return err
	}

	// Step 2: Parsing
	if parser, err = InitializeParser(tokens); err != nil {
		return err
	}

	var ast AstTreeNode
	if ast, err = parser.GenerateAst(); err != nil {
		return err
	}

	if bytes, err := util.SerializeJson(ast); err == nil && options.SaveIntermediate {
		os.WriteFile(fmt.Sprintf("output/%s.ast-pre-sa.json", target), bytes, 0755)
	} else {
		return err
	}
	os.WriteFile(fmt.Sprintf("output/%s.ast-pre-sa.txt", target), []byte(ast.ToString()), 0755)

	// Step 3: Semantic analysis
	analyzer := InitializeAnalyzer(ast)
	if ast, err = analyzer.Run(); err != nil {
		return err
	}

	if bytes, err := util.SerializeJson(ast); err == nil && options.SaveIntermediate {
		os.WriteFile(fmt.Sprintf("output/%s.ast-post-sa.json", target), bytes, 0755)
	} else {
		return err
	}

	// Step 4: Optimization

	// Step 5: Code generation
	generator, _ := InitGenerator(ast)
	os.WriteFile(fmt.Sprintf("output/%s.s", strings.TrimSuffix(target, ".oscl")), []byte(generator.GenerateIl()), 0755)

	return nil
}
