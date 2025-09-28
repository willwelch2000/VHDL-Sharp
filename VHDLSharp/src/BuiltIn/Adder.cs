using VHDLSharp.LogicTree;
using VHDLSharp.Modules;
using VHDLSharp.Signals;

namespace VHDLSharp.BuiltIn;

/// <summary>
/// Parameter set for <see cref="Adder"/> module
/// </summary>
/// <param name="Bits">Number of bits in adder</param>
/// <param name="CarryIn">Carry-in pin</param>
/// <param name="CarryOut">Carry-out pin</param>
public record struct AdderParams(int Bits, bool CarryIn = true, bool CarryOut = true) : IEquatable<AdderParams>;

/// <summary>
/// Adder module, with a given number of bits
/// Ports: A, B, CIn (optional), Y, COut (optional)
/// </summary>
public class Adder : ParameterizedModule<AdderParams>
{
    /// <summary>
    /// Constructor given parameter set
    /// </summary>
    /// <param name="options"></param>
    public Adder(AdderParams options) : base(options) { }

    /// <summary>Parameterless constructor</summary>
    public Adder() : base(new()) { }

    /// <summary>
    /// Constructor given parameters
    /// </summary>
    /// <param name="bits">Number of bits in adder</param>
    /// <param name="carryIn">If true, includes carry-in bit (CIn)</param>
    /// <param name="carryOut">If true, includes carry-out bit (COut)</param>
    public Adder(int bits, bool carryIn = true, bool carryOut = true) : base(new(bits, carryIn, carryOut)) { }

    /// <inheritdoc/>
    public override IModule BuildModule(AdderParams options)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(options.Bits, 1, nameof(options.Bits));

        string name = "Adder_" + options.Bits + (options.Bits == 1 ? "bit" : "bits") +
            (options.CarryIn ? "" : "_noCIn") + (options.CarryOut ? "" : "_noCOut");
        Module module = new(name);

        // 1-bit case
        if (options.Bits == 1)
        {
            // Ports
            Signal a = module.GenerateSignal("A");
            Signal b = module.GenerateSignal("B");
            Signal y = module.GenerateSignal("Y");
            module.AddNewPort(a, PortDirection.Input);
            module.AddNewPort(b, PortDirection.Input);
            module.AddNewPort(y, PortDirection.Output);

            Signal? cin = null;
            Signal? aXorB = null;

            if (options.CarryIn)
            {
                cin = module.GenerateSignal("CIn");
                module.AddNewPort(cin, PortDirection.Input);
                aXorB = module.GenerateSignal("aXorB");
                aXorB.AssignBehavior(LogicFunctions.And(a.Or(b), a.And(b).Not()));
                y.AssignBehavior(LogicFunctions.And(aXorB.Or(cin), aXorB.And(cin).Not()));
            }
            else
            {
                y.AssignBehavior(LogicFunctions.And(a.Or(b), a.And(b).Not()));
            }

            if (options.CarryOut)
            {
                Signal cout = module.GenerateSignal("COut");
                module.AddNewPort(cout, PortDirection.Output);
                cout.AssignBehavior(options.CarryIn ? LogicFunctions.Or(aXorB!.And(cin!), a.And(b)) : a.And(b));
            }
        }
        // All other cases
        else
        {
            // Ports
            Vector a = module.GenerateVector("A", options.Bits);
            Vector b = module.GenerateVector("B", options.Bits);
            Vector y = module.GenerateVector("Y", options.Bits);
            module.AddNewPort(a, PortDirection.Input);
            module.AddNewPort(b, PortDirection.Input);
            module.AddNewPort(y, PortDirection.Output);

            Signal? cin = null;
            Signal? cout = null;

            if (options.CarryIn)
            {
                cin = module.GenerateSignal("CIn");
                module.AddNewPort(cin, PortDirection.Input);
            }

            if (options.CarryOut)
            {
                cout = module.GenerateSignal("COut");
                module.AddNewPort(cout, PortDirection.Output);
            }

            // Chain adders together
            IModule adder1Bit = new Adder(1, true);
            IModule adder1BitFirst = options.CarryIn ? adder1Bit : new Adder(1, false, true); // Use no-CIn version for first, if specified
            IModule adder1BitLast = options.CarryOut ? adder1Bit : new Adder(1, true, false); // Use no-COut version for last, if specified
            Signal? currentCarryBit = cin;
            for (int i = 0; i < options.Bits; i++)
            {
                bool firstBit = i == 0;
                bool lastBit = i == options.Bits - 1;
                Instantiation inst = module.AddNewInstantiation(firstBit ? adder1BitFirst : lastBit ? adder1BitLast : adder1Bit, $"Adder{i}");
                inst.PortMapping.SetPort("A", a[i]);
                inst.PortMapping.SetPort("B", b[i]);
                inst.PortMapping.SetPort("Y", y[i]);
                if (currentCarryBit is not null) // True if CarryIn is true, or if we're past first bit
                    inst.PortMapping.SetPort("CIn", currentCarryBit);
                currentCarryBit = (lastBit && cout is not null) ? cout : module.GenerateSignal($"COut{i}");
                // if (options.CarryOut || !lastBit)
                if (options.CarryOut && lastBit)
                    inst.PortMapping.SetPort("COut", currentCarryBit);
            }
        }

        return module;
    }
}