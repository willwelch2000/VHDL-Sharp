using System.Collections.ObjectModel;
using SpiceSharp.Components;
using SpiceSharp.Entities;

namespace VHDLSharp.Utility;

/// <summary>
/// Utility class for Spice stuff
/// </summary>
internal static class SpiceUtil
{
    private static Mosfet1Model? nmosModel = null;
    private static Mosfet1Model? pmosModel = null;
    
    private static readonly Dictionary<SubcircuitDefinition, string> subcircuitNames = [];

    private static double VDD => 5.0;

    /// <summary>
    /// Get singleton NMOS model object
    /// </summary>
    internal static Mosfet1Model NmosModel
    {
        get
        {
            if (nmosModel is not null)
                return nmosModel;

            nmosModel = new("NmosMod");
            nmosModel.Parameters.SetNmos(true);
            return nmosModel;
        }
    }

    /// <summary>
    /// Get singleton PMOS model object
    /// </summary>
    internal static Mosfet1Model PmosModel
    {
        get
        {
            if (pmosModel is not null)
                return pmosModel;

            pmosModel = new("PmosMod");
            pmosModel.Parameters.SetPmos(true);
            return pmosModel;
        }
    }

    /// <summary>
    /// Get singleton VDD voltage source object
    /// </summary>
    internal static VoltageSource VddSource { get; } = new("V_VDD", "VDD", "0", VDD);

    /// <summary>
    /// Entities to be included everywhere
    /// </summary>
    internal static IEnumerable<IEntity> CommonEntities
    {
        get
        {
            yield return NmosModel;
            yield return PmosModel;
            yield return VddSource;
        }
    }

    // TODO commonly-used subcircuits

    /// <summary>
    /// Dictionary mapping Spice# subcircuits to their names to be used in Spice
    /// </summary>
    internal static ReadOnlyDictionary<SubcircuitDefinition, string> SubcircuitNames => new(subcircuitNames);
}