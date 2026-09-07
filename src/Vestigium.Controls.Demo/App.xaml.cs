using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Vestigium.Controls.DependencyInjection;

namespace Vestigium.Controls.Demo;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = default!;

    protected override void OnStartup(StartupEventArgs e)
    {
        var collection = new ServiceCollection();
        collection.AddVestigiumControls();
        collection.AddTransient<GalleryViewModel>();
        Services = collection.BuildServiceProvider();

        base.OnStartup(e);

        var vm = Services.GetRequiredService<GalleryViewModel>();
        var window = new MainWindow(vm);
        window.Show();
    }
}
