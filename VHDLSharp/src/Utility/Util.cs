using VHDLSharp.LogicTree;

namespace VHDLSharp.Utility;

internal static class Util
{
    internal static double VDD => 5.0;

    internal static double RiseFall => 1e-9;

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

    internal static bool CanCombine<T>(this IEnumerable<ILogicallyCombinable<T>?> expressions) where T : ILogicallyCombinable<T>
    {
        ILogicallyCombinable<T>[] array = expressions.Where(e => e is not null).ToArray()!;
        if (array.Length < 2)
            return true;

        for (int i = 0; i < array.Length-1; i++)
        {
            ILogicallyCombinable<T> first = array[i];
            for (int j = i+1; j < array.Length; j++)
            {
                ILogicallyCombinable<T> second = array[j];
                if (!first.CanCombine(second) || !second.CanCombine(first))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Method to generate Spice node name
    /// </summary>
    /// <param name="uniqueId">Unique id for portion of the circuit</param>
    /// <param name="dimensionIndex">Number given to differentiate duplicates for multi-dimensional signals</param>
    /// <param name="ending">Name given to node to differentiate within portion</param>
    /// <returns></returns>
    internal static string GetSpiceName(string uniqueId, int dimensionIndex, string ending) => $"n{uniqueId}x{dimensionIndex}_{ending}";

    internal static string GetMosfetSpiceLine(string name, string drain, string gate, string source, bool pmos) => $"M{name} {drain} {gate} {source} {source} {(pmos ? "PmosMod" : "NmosMod")} W=100u L=1u\n";
}