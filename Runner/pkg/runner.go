package pkg

import (
	"errors"
	"fmt"
	"os"
	"os/exec"
	"strings"
)

type Runner struct {
	Filename string
}

func (r *Runner) Run() error {
	if !strings.HasSuffix(r.Filename, ".osc") {
		return errors.New("file must end with .osc")
	}

	if _, err := os.Stat(r.Filename); os.IsNotExist(err) {
		return errors.New("specified file does not exist")
	}

	if err := r.CompileFile(); err != nil {
		return err
	}

	return r.RunExecutable()
}

func (r *Runner) CompileFile() error {
	// Temporary command, obviously need to make this dynamic
	compilationCommand := "dotnet run --project=Osclan.Compiler -- -i -f examples/hello-world.osc"

	command := exec.Command("bash", "-c", compilationCommand)
	command.Dir = "/Users/robert/Projects/OSCLAN"
	command.Stderr = os.Stderr

	_, err := command.Output()

	if err != nil {
		return fmt.Errorf("could not compile file: %v", err)
	}

	fmt.Println("Compiled successfully")

	return nil
}

func (r *Runner) RunExecutable() error {
	command := exec.Command("bash", "-c", "./a.out")

	command.Dir = "/Users/robert/Projects/OSCLAN"
	command.Stderr = os.Stderr
	command.Stdout = os.Stdout

	if err := command.Run(); err != nil {
		return err
	}

	return nil
}
