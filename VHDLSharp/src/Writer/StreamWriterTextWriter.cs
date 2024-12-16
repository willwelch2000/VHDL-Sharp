namespace VHDLSharp;

internal class StreamWriterTextWriter(StreamWriter streamWriter) : ITextWriter
{
    public void Write(string s) => streamWriter.Write(s);
}