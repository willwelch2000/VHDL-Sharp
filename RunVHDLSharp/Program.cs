using VHDLSharp;
using VHDLSharp.Behaviors;
using VHDLSharp.LogicTree;
using VHDLSharp.Signals;

Module module1 = new()
{
    Name = "m1",
};
Module module2 = new()
{
    Name = "m2",
};
PortMapping portMapping = new(module1, module2);
Signal s1 = new("s1", module1);
Signal s2 = new("s2", module1);
Port p1 = new()
{
    Signal = s1, 
    Direction = PortDirection.Input,
};
LogicTree<ISignal> expression1 = new And<ISignal>(s1, new Not<ISignal>(s2));
expression1.And(new Or<ISignal>(s1, s2));
// LogicExpression expression2 = s1.ToLogicExpression.And(((LogicExpression)s2).Not());
module1.Ports.Add(p1);
module1.SignalBehaviors[s1] = new LogicBehavior(expression1);

Console.WriteLine(module1.ToVhdl());