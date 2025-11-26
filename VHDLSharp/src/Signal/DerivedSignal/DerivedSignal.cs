using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using VHDLSharp.Dimensions;
using VHDLSharp.LogicTree;
using VHDLSharp.Modules;
using VHDLSharp.Signals.Derived;
using VHDLSharp.Validation;

namespace VHDLSharp.Signals;

// Look at existing DerivedSignal child classes--all sub-signals must be added during construction,
// ensuring that recursion doesn't happen. If this rule changes, recursion will need to be managed
// like it is for behaviors. 
/// <summary>
/// Interface for a signal that uses more complicated logic (in the form of an <see cref="IInstantiation"/>)
/// to assign a value to a signal. A <see cref="LinkedSignal"/> should be assigned the value determined
/// by the relevant logic. 
/// Any implementing class must be in charge of registering itself in the module that it's in. 
/// Recursion with derived signals is not allowed or checked for, so it should not be allowed by 
/// implementing classes. This can be ensured by not allowing sub-signals to be switched out
/// after construction. 
/// </summary>
public interface IDerivedSignal : IModuleSpecificSignal
{
    /// <summary>
    /// The named signal whose value will be determined by this derived signal.
    /// If unassigned, a temporary signal will be created for necessary functions.
    /// The module might also set this if unassigned before necessary module functions, to avoid duplicate names.  
    /// The module and dimension of the linked signal must match this. 
    /// </summary>
    public INamedSignal? LinkedSignal { get; set; }

    /// <summary>
    /// Get the <see cref="LinkedSignal"/> if it exists, throwing an exception if it doesn't
    /// </summary>
    /// <returns></returns>
    /// <exception cref="UnlinkedDerivedSignalException"></exception>
    public INamedSignal GetLinkedSignal(string? errorMessage = null);

    INamedSignal IModuleSpecificSignal.AsNamedSignal() => GetLinkedSignal();

    /// <summary>
    /// Generate an instantation object that assigns this derived signal to the <see cref="LinkedSignal"/>. 
    /// The instantiation should have the given <paramref name="instanceName"/>. 
    /// <paramref name="moduleName"/> can be used if creating a new module to avoid repeats. 
    /// The <see cref="LinkedSignal"/> must be assigned before compiling. 
    /// It is assigned as an output port of the generated <see cref="IInstantiation"/>
    /// </summary>
    /// <param name="moduleName">Name for a new module, if needed</param>
    /// <param name="instanceName">Name for the returned instantiation</param>
    /// <returns></returns>
    public IInstantiation Compile(string moduleName, string instanceName);

    /// <summary>
    /// All the direct input signals that implement <see cref="IModuleSpecificSignal"/>. 
    /// Used to generate the <see cref="RecursiveInputModuleSignals"/> list.
    /// Not recursive
    /// </summary>
    public abstract IEnumerable<IModuleSpecificSignal> InputModuleSignals { get; }

    /// <summary>
    /// Get all the module-specific signals that are used (recursively through derived signals)
    /// by this, so that they can be included in the module
    /// </summary>
    public IEnumerable<IModuleSpecificSignal> RecursiveInputModuleSignals { get; }

    /// <summary>
    /// Convert to single-node signals, as type <see cref="IDerivedSignalNode"/>
    /// </summary>
    public new IEnumerable<IDerivedSignalNode> ToSingleNodeSignals { get; }

    IEnumerable<ISingleNodeModuleSpecificSignal> IModuleSpecificSignal.ToSingleNodeSignals => ToSingleNodeSignals;

    /// <summary>
    /// Get a slice of this derived signal
    /// </summary>
    /// <param name="range"></param>
    /// <returns></returns>
    public abstract SliceSignal this[Range range] { get; }
}

/// <summary>
/// Abstract base class implementation of <see cref="IDerivedSignal"/>
/// </summary>
public abstract class DerivedSignal : IDerivedSignal, IValidityManagedEntity
{
    private EventHandler? updated;

    private readonly ObservableCollection<object> childEntities;
    
    /// <summary>
    /// Constructor given parent module
    /// </summary>
    /// <param name="parentModule"></param>
    public DerivedSignal(IModule parentModule)
    {
        childEntities = [];
        ValidityManager = new ValidityManager<object>(this, childEntities);
        ParentModule = parentModule;
        parentModule.RegisterDerivedSignal(this);
    }

    /// <summary>Module to which this signal belongs</summary>
    public IModule ParentModule { get; }

    private INamedSignal? linkedSignal;
    /// <inheritdoc/>
    public INamedSignal? LinkedSignal
    {
        get => linkedSignal;
        set
        {
            if (value is not null && !value.CanCombine(this))
                throw new Exception($"Linked signal ({value}) is not compatible");
            linkedSignal = value;
            updated?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <inheritdoc/>
    public IInstantiation Compile(string moduleName, string instanceName) => ValidityManager.IsValid(out Exception? issue) ?
        CompileWithoutCheck(moduleName, instanceName) :
        throw new InvalidException("Derived signal must be valid to compile", issue);

    /// <inheritdoc/>
    public INamedSignal GetLinkedSignal(string? errorMessage = null) => LinkedSignal ??
        throw (errorMessage is null ? new UnlinkedDerivedSignalException() : new UnlinkedDerivedSignalException(errorMessage));

    /// <summary>
    /// Compile instantiation without checking for validity first
    /// </summary>
    /// <param name="moduleName">Name for a new module, if needed</param>
    /// <param name="instanceName">Name for the returned instantiation</param>
    /// <returns></returns>
    protected abstract IInstantiation CompileWithoutCheck(string moduleName, string instanceName);

    /// <inheritdoc/>
    public IEnumerable<IModuleSpecificSignal> RecursiveInputModuleSignals
    {
        get
        {
            HashSet<IModuleSpecificSignal> foundSignals = [];
            foreach (IModuleSpecificSignal signal in InputModuleSignals)
            {
                if (foundSignals.Contains(signal)) // Skip if already found
                    continue;
                yield return signal; // Return the signal itself
                // If it's a derived signal or derived signal node, return all its used signals recursively as well
                IDerivedSignal? derivedSignal = signal as IDerivedSignal ?? (signal as IDerivedSignalNode)?.DerivedSignal;
                if (derivedSignal is not null)
                    foreach (IModuleSpecificSignal subNamedSignal in derivedSignal.RecursiveInputModuleSignals)
                        if (!foundSignals.Contains(subNamedSignal))
                            yield return subNamedSignal;
                // Remember signal to avoid returning it again
                foundSignals.Add(signal);
            }
        }
    }

    /// <inheritdoc/>
    public abstract IEnumerable<IModuleSpecificSignal> InputModuleSignals { get; }

    /// <inheritdoc/>
    public abstract DefiniteDimension Dimension { get; }

    /// <inheritdoc/>
    public IModuleSpecificSignal? ParentSignal => null;

    /// <inheritdoc/>
    public ISingleNodeSignal this[Index index]
    {
        get
        {
            int actualIndex = index.IsFromEnd ? Dimension.NonNullValue - index.Value : index.Value; // From ChatGPT
            if (actualIndex < 0 || actualIndex >= Dimension.NonNullValue)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index ({actualIndex}) must refer to a node between 0 and {Dimension.NonNullValue - 1}");
            return new DerivedSignalNode(this, actualIndex);
        }
    }

    /// <inheritdoc/>
    public IEnumerable<ISignal> ChildSignals => ToSingleNodeSignals;

    /// <inheritdoc/>
    public IEnumerable<IDerivedSignalNode> ToSingleNodeSignals => Enumerable.Range(0, Dimension.NonNullValue)
        .Select(i => new DerivedSignalNode(this, i));

    /// <inheritdoc/>
    public string GetVhdlName() => LinkedSignal is null ?
        throw new Exception("Must have a linked assigned signal to get the VHDL name") : LinkedSignal.GetVhdlName();

    /// <inheritdoc/>
    public bool CanCombine(ILogicallyCombinable<ISignal> other) => ISignal.CanCombineSignals(this, other);

    /// <inheritdoc/>
    public bool CanCombine(IEnumerable<ILogicallyCombinable<ISignal>> others) => ISignal.CanCombineSignals([this, .. others]);

    /// <inheritdoc/>
    public string ToLogicString() => LinkedSignal is null ?
        throw new Exception("Must have a linked assigned signal to get the logic string") : LinkedSignal.ToLogicString();

    /// <inheritdoc/>
    public string ToLogicString(LogicStringOptions options) => LinkedSignal is null ?
        throw new Exception("Must have a linked assigned signal to get the logic string") : LinkedSignal.ToLogicString(options);

    /// <inheritdoc/>
    public ValidityManager ValidityManager { get; }

    /// <inheritdoc/>
    public SliceSignal this[Range range]
    {
        get
        {
            int dimension = Dimension.NonNullValue;
            return new(this, range.Start.GetOffset(dimension), range.End.GetOffset(dimension));
        }
    }

    /// <inheritdoc/>
    public event EventHandler? Updated
    {
        add
        {
            updated -= value; // remove if already present
            updated += value;
        }
        remove => updated -= value;
    }

    /// <inheritdoc/>
    public bool CheckTopLevelValidity([MaybeNullWhen(true)] out Exception exception)
    {
        // TODO parent-module check might be redundant with check in LinkedSignal setter
        exception = LinkedSignal is INamedSignal namedLinkedSignal && !namedLinkedSignal.ParentModule.Equals(ParentModule) ?
            new Exception($"Linked signal ({namedLinkedSignal.Name}) must share a parent module ({ParentModule.Name}) with this") :
            RecursiveInputModuleSignals.Any(s => s.ParentModule != ParentModule) ? new Exception($"All used module-specific signals must belong to the correct module ({ParentModule.Name})") : null;
        return exception is null;
    }

    /// <summary>
    /// Call to raise updated event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void InvokeUpdated(object? sender, EventArgs e)
    {
        updated?.Invoke(sender, e);
    }

    /// <summary>
    /// Should be called by child classes whenever new signals are added. 
    /// Finds the derived signals and tracks them
    /// </summary>
    /// <param name="newSignals"></param>
    protected void ManageNewSignals(IEnumerable<ISignal> newSignals)
    {
        // Doesn't need to unpack derived signals--just the direct children are followed
        foreach (IDerivedSignal derivedSignal in newSignals.OfType<IDerivedSignal>().Concat(newSignals.OfType<IDerivedSignalNode>().Select(s => s.DerivedSignal)))
            childEntities.Add(derivedSignal);
    }
    
    /// <summary>
    /// Should be called by child classes whenever signals are removed. 
    /// Finds the derived signals and untracks them
    /// </summary>
    /// <param name="removedSignals"></param>
    protected void ManageRemovedSignals(IEnumerable<ISignal> removedSignals)
    {
        // The ValidityManager class is built to track the net add/remove count, so it should work to 
        // remove these even if they are used multiple times
        foreach (IDerivedSignal derivedSignal in removedSignals.OfType<IDerivedSignal>().Concat(removedSignals.OfType<IDerivedSignalNode>().Select(s => s.DerivedSignal)))
            childEntities.Remove(derivedSignal);
    }
}