using VHDLSharp.Behaviors;
using VHDLSharp.LogicTree;
using VHDLSharp.Signals;
using VHDLSharp.Modules;

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
Signal s3 = new("s3", module1);
Port p3 = new()
{
    Signal = s3, 
    Direction = PortDirection.Output,
};
LogicTree<ISignal> expression1 = new And<ISignal>(s1, new Not<ISignal>(s2));
LogicTree<ISignal> expression2 = expression1.And(new Or<ISignal>(s1, s2));
module1.AddNewPort(s1, PortDirection.Input);
module1.AddNewPort(s2, PortDirection.Input);
module1.Ports.Add(p3);
module1.SignalBehaviors[s3] = new LogicBehavior(expression2);

Console.WriteLine(module1.ToSpice());