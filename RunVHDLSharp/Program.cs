using VHDLSharp;
using VHDLSharp.LogicTree;

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
LogicTree<IBaseSignal> expression1 = new And<IBaseSignal>(s1, new Not<IBaseSignal>(s2));
expression1.And(new Or<IBaseSignal>(s1, s2));
// LogicExpression expression2 = s1.ToLogicExpression.And(((LogicExpression)s2).Not());
module1.Ports.Add(p1);
module1.SignalBehaviors[s1] = new LogicBehavior(expression1);

Console.WriteLine(module1.ToVhdl());