namespace Osclan.Compiler.Extensions;

public static class CharExtensions
{
    public static bool IsIdentifierChar(this char c) =>
        char.IsLetterOrDigit(c) || c == '-';
}