namespace Osclan.Compiler.Tokenization;

public record Position
{
    public required string Filename { get; set; }

    public int Offset { get; set; }

    public int Line { get; set; }

    public int Column { get; set; }

    public override string ToString() =>
        $"Line {Line}, column {Column} in {Filename}";
}