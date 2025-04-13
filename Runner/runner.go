package main

import (
	"errors"
	"flag"
	"fmt"
	"github.com/djherbis/times"
	"github.com/fatih/color"
	"os"
	"os/exec"
	"strings"
	"time"
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

	if shouldRecompile, err := r.ShouldRecompileProject(); shouldRecompile {
		if err != nil {
			fmt.Println(err)
		}

		if err = r.CompileFile(); err != nil {
			return err
		}
	}

	return r.RunExecutable()
}

func (r *Runner) CompileFile() error {
	compilationCommand := fmt.Sprintf("dotnet run --project=Osclan.Compiler -- -i -f ./%s", r.Filename)

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

func (r *Runner) ShouldRecompileProject() (bool, error) {
	executableLastUpdated, err := getFileUpdateTime("./a.out")

	if err != nil {
		return true, err
	}

	var sourceFileLastUpdated time.Time
	if sourceFileLastUpdated, err = getFileUpdateTime(r.Filename); err == nil {
		return sourceFileLastUpdated.After(executableLastUpdated), nil
	}

	return true, err
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

func getFileUpdateTime(path string) (time.Time, error) {
	file, err := times.Stat(path)

	if err != nil {
		return time.Time{}, err
	}

	var lastUpdated time.Time

	if file.HasChangeTime() {
		lastUpdated = file.ChangeTime()
	} else if file.HasBirthTime() {
		lastUpdated = file.BirthTime()
	} else {
		return time.Time{}, errors.New("file date check is inconclusive")
	}

	return lastUpdated, nil
}

func writeError(err error) {
	color.Set(color.FgRed)
	defer color.Unset()

	fmt.Println(fmt.Errorf("[ERROR] %s", err.Error()))
}

func main() {
	targetFile := flag.String("t", "", "the path to the file to run")
	flag.Parse()

	runner := Runner{Filename: *targetFile}

	if err := runner.Run(); err != nil {
		writeError(err)
		os.Exit(-1)
	}
}
