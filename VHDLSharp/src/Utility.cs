namespace VHDLSharp;

internal static class Utility
{
    internal static string AddIndentation(this string s, int indents)
    {
        return string.Concat(Enumerable.Repeat("\t", indents)) + s.ReplaceLineEndings($"\n{string.Concat(Enumerable.Repeat("\t", indents))}");
    }

    internal static string ToBinaryString(this int i, int digits)
    {
        if (i < 0 || i > (1<<digits)-1)
            throw new ArgumentException("i must be between 0 and 2^digits");
        
        string conversion = Convert.ToString(i, 2);
        if (conversion.Length < digits)
            conversion = string.Concat(Enumerable.Repeat('0', digits - conversion.Length)) + conversion;

        return conversion;
    }
}