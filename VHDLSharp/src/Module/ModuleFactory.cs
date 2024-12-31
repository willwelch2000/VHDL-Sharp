namespace VHDLSharp.Modules;

/// <summary>
/// Class that should be used to access predfined modules
/// </summary>
public static class ModuleFactory
{
    private readonly static List<Module> modules = [];

    /// <summary>
    /// Used to get a predefined module while ensuring that duplicate versions are not created
    /// </summary>
    /// <typeparam name="T">The type of module to get. Should extend <see cref="Module"/> and implement the default constructor</typeparam>
    /// <returns></returns>
    public static Module Get<T>() where T : Module, new()
    {
        T? existing = modules.FirstOrDefault(m => m is T) as T;
        if (existing is not null)
            return existing;
        T newModule = new();
        modules.Add(newModule);
        return newModule;
    }
}