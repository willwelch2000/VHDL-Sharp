namespace VHDLSharp;

public enum NodeDirection
{
    Input,
    Output,
    Bidirectional
}

public class Node(string Name, NodeDirection Direction)
{

}