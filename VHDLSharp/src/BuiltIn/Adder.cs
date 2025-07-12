using VHDLSharp.LogicTree;
using VHDLSharp.Modules;
using VHDLSharp.Signals;

namespace VHDLSharp.BuiltIn;

/// <summary>
/// Adder module, with a given number of bits
/// Ports: A, B, CIn, Y, COut
/// </summary>
/// <param name="bits"></param>
public class Adder(int bits) : ParameterizedModule<int>(bits)
{
    /// <inheritdoc/>
    public override IModule BuildModule(int bits)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(bits, 1, nameof(bits));

        Module module = new($"Adder_{bits}bits");

        // 1-bit case
        if (bits == 1)
        {
            // Ports
            Signal a = module.GenerateSignal("A");
            Signal b = module.GenerateSignal("B");
            Signal cin = module.GenerateSignal("CIn");
            Signal y = module.GenerateSignal("Y");
            Signal cout = module.GenerateSignal("COut");
            module.AddNewPort(a, PortDirection.Input);
            module.AddNewPort(b, PortDirection.Input);
            module.AddNewPort(cin, PortDirection.Input);
            module.AddNewPort(y, PortDirection.Output);
            module.AddNewPort(cout, PortDirection.Output);

            // Implement circuit
            Signal aXorB = module.GenerateSignal("aXorB");
            aXorB.AssignBehavior(LogicFunctions.And(a.Or(b), a.And(b).Not()));
            y.AssignBehavior(LogicFunctions.And(aXorB.Or(cin), aXorB.And(cin).Not()));
            cout.AssignBehavior(LogicFunctions.Or(aXorB.And(cin), a.And(b)));
        }
        // All other cases
        else
        {
            // Ports
            Vector a = module.GenerateVector("A", bits);
            Vector b = module.GenerateVector("B", bits);
            Signal cin = module.GenerateSignal("CIn");
            Vector y = module.GenerateVector("Y", bits);
            Signal cout = module.GenerateSignal("COut");
            module.AddNewPort(a, PortDirection.Input);
            module.AddNewPort(b, PortDirection.Input);
            module.AddNewPort(cin, PortDirection.Input);
            module.AddNewPort(y, PortDirection.Output);
            module.AddNewPort(cout, PortDirection.Output);

            // Chain adders together
            IModule adder1Bit = new Adder(1);
            Signal previousCin = cin;
            for (int i = 0; i < bits; i++)
            {
                Instantiation instantiation = module.AddNewInstantiation(adder1Bit, $"Adder{i}");
                instantiation.PortMapping.SetPort("A", a[i]);
                instantiation.PortMapping.SetPort("B", b[i]);
                instantiation.PortMapping.SetPort("Y", y[i]);
                instantiation.PortMapping.SetPort("CIn", previousCin);
                Signal nextCout = (i == bits - 1) ? cout : module.GenerateSignal($"COut{i}");
                instantiation.PortMapping.SetPort("COut", nextCout);
            }
        }

        return module;
    }
}