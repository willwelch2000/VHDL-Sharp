namespace VHDLSharp.Modules;

/// <summary>
/// Class that should be used to access predfined modules
/// </summary>
public static class ModuleFactory
{
    private readonly static List<IModule> fullyDefinedModules = [];

    private readonly static Dictionary<string, IModule> otherModules = [];

    /// <summary>
    /// Used to get a predefined module while ensuring that duplicate versions are not created.
    /// Only works for classes that extend <see cref="IModule"/> and have a parameterless constructor
    /// </summary>
    /// <typeparam name="T">The type of module to get. Should extend <see cref="IModule"/> and implement the default constructor</typeparam>
    /// <returns></returns>
    public static IModule Get<T>() where T : class, IModule, new()
    {
        T? existing = fullyDefinedModules.FirstOrDefault(m => m is T) as T;
        if (existing is not null)
            return existing;
        T newModule = new();
        fullyDefinedModules.Add(newModule);
        return newModule;
    }

    /// <summary>
    /// Example function for how this format should work.
    /// Check if name already exists. If so return that. 
    /// Otherwise, make new one
    /// </summary>
    /// <param name="dimension"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static IModule And(int dimension)
    {
        string name = $"and_{dimension}bit";
        if (otherModules.TryGetValue(name, out IModule? module))
        {
            return module;
        }
        throw new NotImplementedException();
    }
}