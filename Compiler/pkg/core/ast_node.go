package core

import (
	"fmt"
	"strings"
)

type AstNodeType int64

const (
	ntRoot AstNodeType = iota
	ntDirective
	ntStructure
	ntProcedure
	ntProcedureCall
	ntCondition
	ntType
	ntModifier
	ntField
	ntArgument
	ntAssignment
	ntDeclaration
	ntVariable
	ntAllocation
	ntScalar
	ntString
	ntDynOffset
	ntPrint
	ntRet
)

var nodeStringMapping = map[AstNodeType]string{
	ntRoot:          "Root",
	ntDirective:     "Directive",
	ntStructure:     "Structure",
	ntProcedure:     "Procedure",
	ntCondition:     "Condition",
	ntType:          "Type",
	ntModifier:      "Modifier",
	ntField:         "Field",
	ntArgument:      "Argument",
	ntAssignment:    "Assignment",
	ntDeclaration:   "Declaration",
	ntVariable:      "Variable",
	ntAllocation:    "Allocation",
	ntScalar:        "Scalar",
	ntString:        "String",
	ntDynOffset:     "DynOffset",
	ntPrint:         "Print",
	ntRet:           "Ret",
	ntProcedureCall: "ProcedureCall",
}

func AstNodeTypeToString(nodeType AstNodeType) string {
	if str, found := nodeStringMapping[nodeType]; found {
		return str
	}

	return "Unknown"
}

type AstTreeNode struct {
	NodeType  AstNodeType
	Value     string
	ValueType string
	ValueSize uint64
	Offset    uint8
	IsPointer bool
	Path      string
	Children  []AstTreeNode
}

func (n AstTreeNode) ToString() string {
	return n.ToStringWithIndentation(1)
}

func (n AstTreeNode) ToStringWithIndentation(level int) string {
	var sb strings.Builder

	sb.WriteString(fmt.Sprintf("[%s] '%s'\n", AstNodeTypeToString(n.NodeType), n.Value))

	for _, child := range n.Children {
		sb.WriteString(fmt.Sprintf("%s%s", strings.Repeat("\t", level), child.ToStringWithIndentation(level+1)))
	}

	return sb.String()
}
