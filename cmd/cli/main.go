package main

import (
	"fmt"
	"os"

	"github.com/nedroden/nasgo/pkg/core"
)

func writeError(err error) {
	fmt.Println(fmt.Errorf("[ERROR] %s", err.Error()))
}

func main() {
	err := core.GenerateIl(core.CompilerOptions{SaveIntermediate: true})

	if err != nil {
		writeError(err)
		os.Exit(-1)
	}
}
