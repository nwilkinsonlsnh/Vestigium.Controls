using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Vestigium.Controls.DependencyInjection;
using Vestigium.Controls.Shell;

namespace Vestigium.Controls.Demo;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = default!;

    protected override void OnStartup(StartupEventArgs e)
    {
        var collection = new ServiceCollection();
        collection.AddVestigiumControls();
        collection.AddTransient<DemoWindowViewModel>();
        Services = collection.BuildServiceProvider();

        base.OnStartup(e);

        var vm = Services.GetRequiredService<DemoWindowViewModel>();
        var window = new VestigiumDefaultWindow(vm)
        {
            Title = "Vestigium.Controls — default form"
        };
        window.Shell.Status.Message = "Ready. Home / Workspace / Settings are placeholders.";
        window.Show();
    }
}
