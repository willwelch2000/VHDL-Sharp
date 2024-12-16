using System.Text;

namespace VHDLSharp;

internal class StringBuilderTextWriter(StringBuilder stringBuilder) : ITextWriter
{
    public void Write(string s) => stringBuilder.Append(s);
}