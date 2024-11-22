using VHDLSharp;

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
Signal s2 = new("s2", module2);
Port p1 = new()
{
    Signal = s1, 
    Direction = PortDirection.Input,
};
module1.Ports.Add(p1);
KeyValuePair<Port, Signal> kvp = new(p1, s2);
portMapping.Add(kvp);
portMapping.Remove(kvp);
var a = 5;