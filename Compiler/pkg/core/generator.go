package core

import "fmt"

type Generator struct {
	Ast     AstTreeNode
	Emitter *Emitter
}

func InitGenerator(tree AstTreeNode) (*Generator, error) {
	return &Generator{
		Ast:     tree,
		Emitter: InitEmitter(),
	}, nil
}

func (g *Generator) generateRoot() {
	g.Emitter.EmitDirect(".global _start")
	g.Emitter.EmitDirect(".align 2")
	g.Emitter.EmitNewLine()

	g.Emitter.EmitDirect("_start:")        // Entry point
	g.Emitter.EmitOpcode("bl", "_main")    // Go to main procedure
	g.Emitter.EmitOpcode("mov", "x16, #1") // Syscall 1 = exit
	g.Emitter.EmitOpcode("svc", "#0x80")   // macOS supervisor call
	g.Emitter.EmitNewLine()
}

func (g *Generator) generateProcProlog(procName string) {
	g.Emitter.EmitDirect(fmt.Sprintf("%s:", procName))
	g.Emitter.EmitOpcode("stp", "lr, fp, [sp, #-16]!") // Store FP and LR on the stack
	g.Emitter.EmitOpcode("mov", "fp, sp")              // Set frame pointer

}

func (g *Generator) generateProcEpilog() {
	g.Emitter.EmitOpcode("mov", "sp, fp")            // Reset stack pointer
	g.Emitter.EmitOpcode("ldp", "lr, fp, [sp], #16") // Restore FP and LR
	g.Emitter.EmitOpcode("bx", "lr")                 // Return to caller
	g.Emitter.EmitNewLine()
}

func (g *Generator) GenerateIl() string {
	g.generateRoot()

	g.generateProcProlog("_main")
	g.generateProcEpilog()

	return g.Emitter.GetResult()
}
