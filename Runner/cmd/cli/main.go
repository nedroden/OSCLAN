package main

import (
	"flag"
	"fmt"
	"os"
	"robertmonden/osclan-runner/pkg"

	"github.com/fatih/color"
)

func writeError(err error) {
	color.Set(color.FgRed)
	defer color.Unset()

	fmt.Println(fmt.Errorf("[ERROR] %s", err.Error()))
}

func main() {
	targetFile := flag.String("t", "", "the path to the file to run")
	flag.Parse()

	runner := pkg.Runner{Filename: *targetFile}

	if err := runner.Run(); err != nil {
		writeError(err)
		os.Exit(-1)
	}
}
