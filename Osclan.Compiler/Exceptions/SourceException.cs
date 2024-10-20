using System;

namespace Osclan.Compiler.Exceptions;

/// <summary>
/// Represents an exception that is caused by an error in the input source code.
/// </summary>
public class SourceException(string message) : Exception(message) { }