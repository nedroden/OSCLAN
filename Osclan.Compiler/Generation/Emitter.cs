using System.Text;

namespace Osclan.Compiler.Generation;

public class Emitter
{
    private readonly StringBuilder _stringBuilder;

    public Emitter() => _stringBuilder = new StringBuilder();

    public Emitter(StringBuilder stringBuilder) => _stringBuilder = stringBuilder;

    public void EmitOpcode(string opcode, string args) => _stringBuilder.AppendLine($"\t{opcode}\t\t{args}");

    public void EmitOpcode(string opcode) => _stringBuilder.AppendLine($"\t{opcode}");

    public void EmitNewLine() => _stringBuilder.AppendLine();

    public void EmitDirect(string value) => _stringBuilder.AppendLine(value);

    public string GetResult() => _stringBuilder.ToString();
}