package core

type TokenType int64

const (
	ttEOF TokenType = iota
	ttPlus
	ttMinus
	ttDot
	ttIdentifier
	ttString
	ttNumber
	ttDeclare
	ttModifier
	ttStruct
	ttLbracket
	ttRbracket
	ttLparen
	ttRparen
	ttLt
	ttGt
	ttBegin
	ttEnd
	ttDoubleColon
	ttColon
	ttRet
	ttIncrement
	ttDecrement
	ttAnon
	ttPrint
	ttIf
	ttElse
	ttThen
	ttSpace
	ttZero
	ttComma
	ttEq
	ttNeq
	ttAsterisk
	ttTilde
	ttCall
	ttInit
)

var tokenStringMapping = map[TokenType]string{
	ttEOF:         "Eof",
	ttPlus:        "Plus",
	ttMinus:       "Minus",
	ttDot:         "Dot",
	ttIdentifier:  "Identifier",
	ttString:      "String",
	ttNumber:      "Number",
	ttDeclare:     "Declare",
	ttModifier:    "Modifier",
	ttStruct:      "Struct",
	ttLbracket:    "Lbracket",
	ttRbracket:    "Rbracket",
	ttLparen:      "Lparen",
	ttRparen:      "Rparen",
	ttLt:          "Lt",
	ttGt:          "Gt",
	ttBegin:       "Begin",
	ttEnd:         "End",
	ttDoubleColon: "DoubleColon",
	ttColon:       "Colon",
	ttRet:         "Ret",
	ttIncrement:   "Increment",
	ttDecrement:   "Decrement",
	ttAnon:        "Anon",
	ttPrint:       "Print",
	ttIf:          "If",
	ttElse:        "Else",
	ttThen:        "Then",
	ttSpace:       "Space",
	ttZero:        "Zero",
	ttComma:       "Comma",
	ttEq:          "Eq",
	ttNeq:         "Neq",
	ttAsterisk:    "Asterisk",
	ttTilde:       "Tilde",
	ttCall:        "Call",
	ttInit:        "Init",
}

func TokenTypeToString(tokenType TokenType) string {
	if str, found := tokenStringMapping[tokenType]; found {
		return str
	}

	return "Unknown"
}

// TODO: sort based on usage statistics
var sequences = map[string]Token{
	"::":        {Type: ttDoubleColon},
	"declare":   {Type: ttDeclare},
	"anon":      {Type: ttAnon},
	"struct":    {Type: ttStruct},
	"end":       {Type: ttEnd},
	"public":    {Type: ttModifier, Value: "public"},
	"private":   {Type: ttModifier, Value: "private"},
	"increment": {Type: ttIncrement},
	"decrement": {Type: ttDecrement},
	"print":     {Type: ttPrint},
	"call":      {Type: ttCall},
	"~=":        {Type: ttNeq},
	"begin":     {Type: ttBegin},
	"init":      {Type: ttInit},
	"ok":        {Type: ttNumber, Value: "0"},
	"true":      {Type: ttNumber, Value: "1"},
	"false":     {Type: ttNumber, Value: "0"},
	"ret":       {Type: ttRet},
	"if":        {Type: ttIf},
	"else":      {Type: ttElse},
	"then":      {Type: ttThen},
}
