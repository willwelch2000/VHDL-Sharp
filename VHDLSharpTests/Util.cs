using System.Text;
using System.Text.RegularExpressions;
using VHDLSharp.Behaviors;
using VHDLSharp.Modules;
using VHDLSharp.Signals;

namespace VHDLSharpTests;

internal static partial class Util
{
    private static Module? sampleModule1 = null;
    private static Module? sampleModule2 = null;
    private static Module? andModule = null;
    private static Module? orModule = null;

    internal static Module GetSampleModule1()
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

    internal static Module GetSampleModule2()
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

    internal static Module GetOrModule()
    {
        if (orModule is not null)
            return orModule;

        orModule = new("OrMod");
        Port pIn1 = orModule.AddNewPort("IN1", PortDirection.Input);
        Port pIn2 = orModule.AddNewPort("IN2", PortDirection.Input);
        Port pOut = orModule.AddNewPort("OUT", PortDirection.Output);
        orModule.SignalBehaviors[pOut.Signal] = new LogicBehavior(pIn1.Signal.Or(pIn2.Signal));

        return orModule;
    }

    internal static Module GetAndModule()
    {
        if (andModule is not null)
            return andModule;

        andModule = new("AndMod");
        Port pIn1 = andModule.AddNewPort("IN1", PortDirection.Input);
        Port pIn2 = andModule.AddNewPort("IN2", PortDirection.Input);
        Port pOut = andModule.AddNewPort("OUT", PortDirection.Output);
        andModule.SignalBehaviors[pOut.Signal] = new LogicBehavior(pIn1.Signal.And(pIn2.Signal));

        return andModule;
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

    internal static string GetAndSubcircuitSpice(int numInputs, bool includeModels)
    {
        StringBuilder sb = new();

        sb.AppendLine($".subckt AND{numInputs} {string.Join(' ', Enumerable.Range(1, numInputs).Select(i => $"IN{i}"))} OUT");
        if (includeModels)
        {
            sb.AppendLine($"\t.MODEL NmosMod nmos W=0.0001 L=1E-06");
            sb.AppendLine($"\t.MODEL PmosMod pmos W=0.0001 L=1E-06");
        }
        sb.AppendLine("\tVVDD VDD 0 5");
        for (int i = 1; i <= numInputs; i++)
        {
            // PMOSs go in parallel from VDD to nandSignal
            sb.AppendLine($"\tMpnand{i} nand IN{i} VDD VDD PmosMod");
            // NMOSs go in series from nandSignal to ground
            string nDrain = i == 1 ? "nand" : $"nand{i}";
            string nSource = i == numInputs ? "0" : $"nand{i+1}";
            sb.AppendLine($"\tMnnand{i} {nDrain} IN{i} {nSource} {nSource} NmosMod");
        }
        sb.AppendLine("\tMpnot OUT nand VDD VDD PmosMod");
        sb.AppendLine("\tMnnot OUT nand 0 0 NmosMod");
        sb.AppendLine($".ends AND{numInputs}");

        return sb.ToString();
    }

    internal static string GetOrSubcircuitSpice(int numInputs, bool includeModels)
    {
        StringBuilder sb = new();

        sb.AppendLine($".subckt OR{numInputs} {string.Join(' ', Enumerable.Range(1, numInputs).Select(i => $"IN{i}"))} OUT");
        if (includeModels)
        {
            sb.AppendLine($"\t.MODEL NmosMod nmos W=0.0001 L=1E-06");
            sb.AppendLine($"\t.MODEL PmosMod pmos W=0.0001 L=1E-06");
        }
        sb.AppendLine("\tVVDD VDD 0 5");
        for (int i = 1; i <= numInputs; i++)
        {
            // PMOSs go in series from VDD to norSignal
            string pDrain = i == 1 ? "nor" : $"nor{i}";
            string pSource = i == numInputs ? "VDD" : $"nor{i+1}";
            sb.AppendLine($"\tMpnor{i} {pDrain} IN{i} {pSource} {pSource} PmosMod");
            // NMOSs go in parallel from norSignal to ground
            sb.AppendLine($"\tMnnor{i} nor IN{i} 0 0 NmosMod");
        }
        sb.AppendLine("\tMpnot OUT nor VDD VDD PmosMod");
        sb.AppendLine("\tMnnot OUT nor 0 0 NmosMod");
        sb.AppendLine($".ends OR{numInputs}");

        return sb.ToString();
    }

    internal static string GetNotSubcircuitSpice(bool includeModels)
    {
        StringBuilder sb = new();

        sb.AppendLine($".subckt NOT IN OUT");
        if (includeModels)
        {
            sb.AppendLine($"\t.MODEL NmosMod nmos W=0.0001 L=1E-06");
            sb.AppendLine($"\t.MODEL PmosMod pmos W=0.0001 L=1E-06");
        }
        sb.AppendLine("\tVVDD VDD 0 5");
        sb.AppendLine("\tMp OUT IN VDD VDD PmosMod");
        sb.AppendLine("\tMn OUT IN 0 0 NmosMod");
        sb.AppendLine($".ends NOT");

        return sb.ToString();
    }

    internal static string AddIndentation(this string s, int indents)
    {
        return string.Concat(Enumerable.Repeat("\t", indents)) + s.ReplaceLineEndings($"\n{string.Concat(Enumerable.Repeat("\t", indents))}");
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex MyRegex();
}