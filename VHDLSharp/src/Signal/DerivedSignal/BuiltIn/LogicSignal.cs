using VHDLSharp.Behaviors;
using VHDLSharp.Dimensions;
using VHDLSharp.LogicTree;
using VHDLSharp.Modules;

namespace VHDLSharp.Signals.Derived;

/// <summary>
/// A signal that comes from a <see cref="LogicExpression"/>
/// </summary>
public class LogicSignal : DerivedSignal
{
    /// <summary>
    /// Constructor given an expression and parent module
    /// </summary>
    /// <param name="expression">Expression defining this signal</param>
    /// <param name="parentModule">The module to which this signal belongs. If null, it is extracted from the expression</param>
    /// <exception cref="Exception">
    /// If no parent module is provided and it can't be extracted from the expression,
    /// or if it is provided but doesn't match the expression's module
    /// </exception>
    public LogicSignal(LogicExpression expression, IModule? parentModule = null) : base(parentModule ??
        expression.BaseObjects.OfType<IModuleSpecificSignal>().FirstOrDefault()?.ParentModule ??
        throw new Exception("If no parent module is provided, the provided expression must contain a module-specific signal so that a module can be found"))
    {
        // Exception if expression module doesn't match provided module
        if (parentModule is not null && expression.BaseObjects.OfType<IModuleSpecificSignal>().FirstOrDefault() is IModuleSpecificSignal moduleSpecificSignal
            && !moduleSpecificSignal.ParentModule.Equals(parentModule))
            throw new Exception($"Module of expression ({moduleSpecificSignal.ParentModule}) must equal provided base module ({parentModule})");

        Expression = expression;
        ManageNewSignals(expression.BaseObjects);
    }

    /// <summary>Expression defining this signal</summary>
    public LogicExpression Expression { get; }

    /// <inheritdoc/>
    public override DefiniteDimension Dimension => Expression.Dimension;

    /// <inheritdoc/>
    protected override IEnumerable<IModuleSpecificSignal> InputSignalsWithAssignedModule => Expression.BaseObjects.OfType<IModuleSpecificSignal>();

    /// <inheritdoc/>
    protected override IInstantiation CompileWithoutCheck(string moduleName, string instanceName)
    {
        Module childModule = new(moduleName);
        Dictionary<INamedSignal, ITopLevelNamedSignal> signalMappings = [];
        TransformExpressionOutput output = Expression.GenerateLogicalObject(TransformExpressionOptions, new() { NewParentModule = childModule, SignalMappings = signalMappings });
        int dimension = Expression.Dimension.NonNullValue;
        ITopLevelNamedSignal outputSignal = NamedSignal.GenerateSignalOrVector("Output", childModule, dimension);
        childModule.SignalBehaviors[outputSignal] = new LogicBehavior(output.Expression);

        // Handle ports and port mapping
        Instantiation instantiation = new(childModule, ParentModule, instanceName);
        Port outputPort = childModule.AddNewPort(outputSignal, PortDirection.Output);
        instantiation.PortMapping[outputPort] = GetLinkedSignal();
        foreach ((INamedSignal topLevelSignal, ITopLevelNamedSignal childSignal) in signalMappings)
        {
            Port inputPort = childModule.AddNewPort(childSignal, PortDirection.Input);
            instantiation.PortMapping[inputPort] = topLevelSignal;
        }

        return instantiation;
    }

    // Takes a logic expression and recreates it with signals in another module
    // Derived signals use their linked signals instead
    // The input dictionary is mutated and shared between all calls of the functions
    private CustomLogicObjectOptions<ISignal, TransformExpressionInput, TransformExpressionOutput>? transformExpressionOptions = null;
    private CustomLogicObjectOptions<ISignal, TransformExpressionInput, TransformExpressionOutput> TransformExpressionOptions
    {
        get
        {
            if (transformExpressionOptions is not null)
                return transformExpressionOptions;

            CustomLogicObjectOptions<ISignal, TransformExpressionInput, TransformExpressionOutput> options = new();

            options.AndFunction = TransformExpressionAndFunction;
            options.OrFunction = TransformExpressionOrFunction;
            options.NotFunction = TransformExpressionNotFunction;
            options.BaseFunction = TransformExpressionBaseFunction;

            transformExpressionOptions = options;
            return options;
        }
    }

    private TransformExpressionOutput TransformExpressionBaseFunction(ISignal signal, TransformExpressionInput additionalInput)
    {
        IModule newParentModule = additionalInput.NewParentModule;
        Dictionary<INamedSignal, ITopLevelNamedSignal> signalMappings = additionalInput.SignalMappings;
        // If this is a module-specific signal, get the linked signal directly or indirectly
        if (signal is IModuleSpecificSignal)
        {
            INamedSignal namedSignal = signal switch
            {
                INamedSignal expAsNamedSignal => expAsNamedSignal,
                IDerivedSignal { LinkedSignal: INamedSignal linkedSignal } => linkedSignal,
                IDerivedSignalNode { LinkedSignal: INamedSignal linkedSignal } => linkedSignal,
                _ => throw new Exception("Could not get signal as linked signal--it might be a derived signal without a linked signal"),
            };
            // If already mapped, use that one
            if (signalMappings.TryGetValue(namedSignal, out ITopLevelNamedSignal? mapping))
                return new() { Expression = mapping };
            string name = $"Input{signalMappings.Count}";
            int dimension = namedSignal.Dimension.NonNullValue;
            ITopLevelNamedSignal newMapping = NamedSignal.GenerateSignalOrVector(name, newParentModule, dimension);
            signalMappings[namedSignal] = newMapping;
            return new() { Expression = newMapping };
        }
        if (signal is ISignalWithKnownValue)
            return new() { Expression = signal };
        throw new Exception("Signal is neither of type IModuleSpecificSignal or ISignalWithKnownValue");
    }

    private TransformExpressionOutput TransformExpressionNotFunction(ILogicallyCombinable<ISignal> expression, TransformExpressionInput additionalInput) =>
        new() { Expression = expression.GenerateLogicalObject(TransformExpressionOptions, additionalInput).Expression };

    private TransformExpressionOutput TransformExpressionAndFunction(IEnumerable<ILogicallyCombinable<ISignal>> innerExpressions, TransformExpressionInput additionalInput) => new() { Expression = new And<ISignal>([.. innerExpressions.Select(exp => exp.GenerateLogicalObject(TransformExpressionOptions, additionalInput).Expression)]) };

    private TransformExpressionOutput TransformExpressionOrFunction(IEnumerable<ILogicallyCombinable<ISignal>> innerExpressions, TransformExpressionInput additionalInput) => new() { Expression = new Or<ISignal>([.. innerExpressions.Select(exp => exp.GenerateLogicalObject(TransformExpressionOptions, additionalInput).Expression)]) };

    private class TransformExpressionInput
    {
        public required IModule NewParentModule { get; set; }

        public required Dictionary<INamedSignal, ITopLevelNamedSignal> SignalMappings { get; set; }
    }

    private class TransformExpressionOutput
    {
        public ILogicallyCombinable<ISignal> Expression { get; set; } = null!;
    }
}