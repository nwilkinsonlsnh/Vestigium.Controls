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
        Services = collection.BuildServiceProvider();

        base.OnStartup(e);

        var window = new VestigiumDefaultWindow(
            Services.GetRequiredService<VestigiumDefaultWindowViewModel>())
        {
            Title = "Vestigium.Controls — default form"
        };
        window.Show();
    }
}
