namespace Osclan.Compiler.Parsing;

public enum AstNodeType
{
    Root,
    Directive,
    Structure,
    Procedure,
    ProcedureCall,
    Condition,
    Modifier,
    Field,
    Argument,
    Assignment,
    Declaration,
    Variable,
    Allocation,
    Scalar,
    String,
    DynOffset,
    Print,
    Ret
}