using System.Text;

namespace Osclan.Compiler.Generation;

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
    public void EmitOpcode(string opcode, string args) => _stringBuilder.AppendLine($"\t{opcode}\t\t{args}");

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
    /// Gets the result.
    /// </summary>
    /// <returns>The combination of emitted strings.</returns>
    public string GetResult() => _stringBuilder.ToString();
}