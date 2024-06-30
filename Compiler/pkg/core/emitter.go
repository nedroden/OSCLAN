package core

import (
	"fmt"
	"strings"
	"time"
)

type Emitter struct {
	Builder *strings.Builder
}

func InitEmitter() *Emitter {
	builder := strings.Builder{}

	builder.WriteString(fmt.Sprintf("; Generated file on %s\n\n", time.Now().UTC().Format("Mon Jan _2 15:04:05 MST 2006")))

	return &Emitter{
		Builder: &builder,
	}
}

func (e *Emitter) EmitDirect(line string) {
	e.Builder.WriteString(fmt.Sprintf("%s\n", line))
}

func (e *Emitter) EmitNewLine() {
	e.Builder.WriteString("\n")
}

func (e *Emitter) EmitOpcode(operands ...string) {
	for i, operand := range operands {
		if i != 0 {
			e.Builder.WriteString("\t")
		}

		e.Builder.WriteString(fmt.Sprintf("\t%s", operand))
	}

	e.Builder.WriteString("\n")
}

func (e *Emitter) GetResult() string {
	return e.Builder.String()
}
