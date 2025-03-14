using System;

namespace Osclan.Compiler.Extensions;

public static class StringExtensions
{
    public static string Truncate(this string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
    
    public static string Window(this string value, int windowSize, int currentIndex) =>
        value.Length >= currentIndex + windowSize ? value[currentIndex..(currentIndex+windowSize)] : value[currentIndex..value.Length];

    public static string PadWithZeros(this string value, int length) =>
        value.PadRight(length, '0');
}