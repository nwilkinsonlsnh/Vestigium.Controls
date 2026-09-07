using Microsoft.Extensions.DependencyInjection;
using Vestigium.Controls.Shell;
using Vestigium.Controls.StatusBar;

namespace Vestigium.Controls.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVestigiumControls(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddTransient<VestigiumStatusBarViewModel>();
        services.AddTransient<VestigiumDefaultWindowViewModel>();
        services.AddTransient<VestigiumShell>();
        return services;
    }
}
