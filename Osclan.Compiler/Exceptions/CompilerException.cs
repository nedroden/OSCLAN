using System;

namespace Osclan.Compiler.Exceptions;

/// <summary>
/// Represents an exception that is caused by an error in the compiler itself.
/// </summary>
public class CompilerException(string message) : Exception(message)
{
}