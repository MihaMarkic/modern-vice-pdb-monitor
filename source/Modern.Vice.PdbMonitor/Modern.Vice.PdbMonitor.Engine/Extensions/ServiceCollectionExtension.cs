namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtension
{
    public static void AddFactoryTransient<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        services.AddTransient<TService, TImplementation>();
        services.AddSingleton<Func<TService>>(x => () => x.GetRequiredService<TService>());
    }
    public static void AddFactoryTransient<TService>(this IServiceCollection services)
        where TService : class
    {
        services.AddTransient<TService>();
        services.AddSingleton<Func<TService>>(x => () => x.GetRequiredService<TService>());
    }
    public static void AddFactoryScoped<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        services.AddScoped<TService, TImplementation>();
        services.AddSingleton<Func<TService>>(x => () => x.GetRequiredService<TService>());
    }
    public static void AddFactoryScoped<TService>(this IServiceCollection services)
        where TService : class
    {
        services.AddScoped<TService>();
        services.AddSingleton<Func<TService>>(x => () => x.GetRequiredService<TService>());
    }
}