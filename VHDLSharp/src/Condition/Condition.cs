using VHDLSharp.LogicTree;

namespace VHDLSharp;

public abstract class Condition : ILogicallyCombinable<Condition>
{
    public IEnumerable<Condition> BaseObjects => [this];

    public bool CanCombine(ILogicallyCombinable<Condition> other)
    {
        return true;
    }

    public string ToLogicString()
    {
        throw new NotImplementedException();
    }

    public Module Module => throw new NotImplementedException();
}