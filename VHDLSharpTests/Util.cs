using System.Text.RegularExpressions;
using VHDLSharp.Behaviors;
using VHDLSharp.Modules;
using VHDLSharp.Signals;

namespace VHDLSharpTests;

internal partial class Util
{
    private static Module? sampleModule1 = null;
    private static Module? sampleModule2 = null;

    public static Module GetSampleModule1()
    {
        if (sampleModule1 is not null)
            return sampleModule1;

        sampleModule1 = new("m1");

        Signal s1 = sampleModule1.GenerateSignal("s1");
        Signal s2 = new("s2", sampleModule1);
        Signal s3 = new("s3", sampleModule1);

        sampleModule1.AddNewPort(s1, PortDirection.Input);
        sampleModule1.AddNewPort(s3, PortDirection.Output);

        s2.AssignBehavior(s1.Not());
        sampleModule1.SignalBehaviors[s3] = new LogicBehavior(s2.And(s1));
        return sampleModule1;
    }

    public static Module GetSampleModule2()
    {
        if (sampleModule2 is not null)
            return sampleModule2;

        sampleModule2 = new("m2");

        Signal s1 = sampleModule2.GenerateSignal("s1");
        Signal s2 = new("s2", sampleModule2);
        Vector s3 = new("s3", sampleModule2, 2);

        sampleModule2.AddNewPort(s1, PortDirection.Input);
        sampleModule2.AddNewPort(s3, PortDirection.Output);

        s2.AssignBehavior(s1.Not());
        sampleModule2.SignalBehaviors[s3] = new ValueBehavior(3);
        return sampleModule2;
    }
    
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