namespace VHDLSharp.Modules;

/// <summary>
/// Class that should be used to access predfined modules
/// </summary>
public static class ModuleFactory
{
    private readonly static List<Module> fullyDefinedModules = [];

    private readonly static Dictionary<string, Module> otherModules = [];

    /// <summary>
    /// Used to get a predefined module while ensuring that duplicate versions are not created.
    /// Only works for classes that extend <see cref="Module"/> and have a parameterless constructor
    /// </summary>
    /// <typeparam name="T">The type of module to get. Should extend <see cref="Module"/> and implement the default constructor</typeparam>
    /// <returns></returns>
    public static Module Get<T>() where T : Module, new()
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
    public static Module And(int dimension)
    {
        string name = $"and_{dimension}bit";
        if (otherModules.TryGetValue(name, out Module? module))
        {
            return module;
        }
        throw new NotImplementedException();
    }
}