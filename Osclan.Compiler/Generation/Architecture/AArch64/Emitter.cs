using System.Text;
using Osclan.Compiler.Generation.Architecture.AArch64.Resources;

namespace Osclan.Compiler.Generation.Architecture.AArch64;

/// <summary>
/// Represents an IL builder.
/// </summary>
public class Emitter
{
    private readonly StringBuilder _stringBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="Emitter"/> class.
    /// </summary>
    public Emitter() => _stringBuilder = new StringBuilder();

    /// <summary>
    /// Initializes a new instance of the <see cref="Emitter"/> class.
    /// </summary>
    /// <param name="stringBuilder">A string builder.</param>
    public Emitter(StringBuilder stringBuilder) => _stringBuilder = stringBuilder;

    /// <summary>
    /// Emits an opcode with arguments.
    /// </summary>
    /// <param name="opcode">The opcode, e.g., "mov".</param>
    /// <param name="args">The arguments, e.g., "x1, xzr"</param>
    public void EmitOpcode(string opcode, string args) => _stringBuilder.AppendLine($"\t{opcode.PadRight(5, ' ')} {args}");

    /// <summary>
    /// Emits an opcode without arguments.
    /// </summary>
    /// <param name="opcode">The opcode, e.g., "ret".</param>
    public void EmitOpcode(string opcode) => _stringBuilder.AppendLine($"\t{opcode}");

    /// <summary>
    /// Emits a new line.
    /// </summary>
    public void EmitNewLine() => _stringBuilder.AppendLine();

    /// <summary>
    /// Emits a string directly.
    /// </summary>
    /// <param name="value">The string to emit.</param>
    public void EmitDirect(string value) => _stringBuilder.AppendLine(value);

    /// <summary>
    /// Emits a comment.
    /// </summary>
    /// <param name="value">The value of the comment.</param>
    public void EmitComment(string value) => _stringBuilder.AppendLine($"\n\t; {value}");
    
    /// <summary>
    /// Emits a syscall, i.e., moves the numeric value of the syscall to register x16.
    /// </summary>
    /// <param name="syscall"></param>
    public void EmitSyscall(Syscall syscall) => EmitOpcode("mov", $"x16, #{(short)syscall}");

    /// <summary>
    /// Gets the result.
    /// </summary>
    /// <returns>The combination of emitted strings.</returns>
    public string GetResult() => _stringBuilder.ToString();
}