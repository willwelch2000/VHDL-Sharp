namespace VHDLSharp.Conditions;

/// <summary>
/// A <see cref="ICondition"/> that can only be true instantaneously. 
/// For example, a rising-edge condition
/// </summary>
public interface IEventDrivenCondition : ICondition
{

}