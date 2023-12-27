
public static class APIAssembly
{
    static readonly List<IMapEndPoints> registeredApis = new List<IMapEndPoints>();
    public static void GetAssemblies()
    {
        var interfaceType = typeof(IMapEndPoints);
        var _iMapEndPoints = AppDomain.CurrentDomain.GetAssemblies()
          .SelectMany(x => x.GetTypes())
          .Where(x => interfaceType.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
          .Select(x => Activator.CreateInstance(x));
        foreach (IMapEndPoints i in _iMapEndPoints)
        {
            registeredApis.Add(i);
        }
    }
    #region Private Methods
    /// <summary>
    /// This method will return all the classes which implement IModule interface
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<IMapEndPoints> DiscoverModules(Assembly[] assemblies)
    {
        var type = typeof(IMapEndPoints);
        var types = assemblies
            .SelectMany(s => s.GetTypes())
            .Where(p => type.IsAssignableFrom(p) && p.IsClass)
            .Select(Activator.CreateInstance)
            .Cast<IMapEndPoints>();

        return types;
    }

    #endregion
    public static void RegisterApis(this WebApplicationBuilder builder, IEnumerable<IMapEndPoints> modules)
    {
        foreach (var module in modules)
        {
            registeredApis.Add(module);
        }
    }
    public static void MapEndpoints(this WebApplication app)
    {
        foreach (var module in registeredApis)
        {
            module.MapEndpoints(app);
        }
    }

}

