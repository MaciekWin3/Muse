using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Terminal.Gui.ViewBase;

namespace Muse.Utils;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTerminalGuiViews(
        this IServiceCollection services,
        Assembly? assembly = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        assembly ??= Assembly.GetCallingAssembly();

        var viewTypes = assembly.GetTypes()
            .Where(t =>
                typeof(View).IsAssignableFrom(t) &&   // must inherit View
                t is { IsClass: true, IsAbstract: false });

        foreach (var type in viewTypes)
        {
            services.Add(new ServiceDescriptor(type, type, lifetime));
        }

        return services;
    }
}