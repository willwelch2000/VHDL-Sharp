using System.Text.RegularExpressions;

namespace VHDLSharpTests;

internal partial class Util
{
    // From ChatGPT
    internal static bool AreEqualIgnoringWhitespace(string str1, string str2)
    {
        // Normalize whitespace: replace any sequence of whitespace with a single space
        static string Normalize(string input)
        {
            return MyRegex().Replace(input.Trim(), " ");
        }

        // Compare normalized strings
        return Normalize(str1) == Normalize(str2);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex MyRegex();
}