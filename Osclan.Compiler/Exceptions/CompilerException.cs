using System;

namespace Osclan.Compiler.Exceptions;

public class CompilerException : Exception
{
    public CompilerException(string message) : base(message) { }
}