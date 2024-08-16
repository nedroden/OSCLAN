package main

import (
	"flag"
	"fmt"
	"os"

	"github.com/fatih/color"
	"github.com/nedroden/OSCLAN/pkg/core"
)

func writeError(err error) {
	color.Set(color.FgRed)
	defer color.Unset()

	fmt.Println(fmt.Errorf("[ERROR] %s", err.Error()))
}

func main() {
	// Command line arguments
	saveIntermediate := flag.Bool("i", false, "save intermediate output")
	flag.Parse()

	err := core.GenerateIl(core.CompilerOptions{SaveIntermediate: *saveIntermediate})

	if err != nil {
		writeError(err)
		os.Exit(-1)
	}
}
