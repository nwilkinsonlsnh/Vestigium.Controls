using Microsoft.Extensions.DependencyInjection;
using Vestigium.Controls.DependencyInjection;
using Vestigium.Controls.Shell;
using Vestigium.Controls.StatusBar;

namespace Vestigium.Controls.Tests;

public class SkeletonTests
{
    [Fact]
    public void StatusBarViewModel_Defaults_AreBottomAndReady()
    {
        var vm = new VestigiumStatusBarViewModel();
        Assert.Equal(VestigiumStatusBarPosition.Bottom, vm.Position);
        Assert.Equal("Ready", vm.Message);
        Assert.True(vm.IsClockVisible);
    }

    [Fact]
    public void AddVestigiumControls_ResolvesDefaultWindowViewModel()
    {
        var services = new ServiceCollection();
        services.AddVestigiumControls();
        using var provider = services.BuildServiceProvider();
        var vm = provider.GetRequiredService<VestigiumDefaultWindowViewModel>();
        Assert.Equal(VestigiumStatusBarPosition.Bottom, vm.Status.Position);
        Assert.Equal("Ready", vm.Status.Message);
    }

    [Fact]
    public void DefaultWindowViewModel_DockCommands_ChangePosition()
    {
        var vm = new VestigiumDefaultWindowViewModel();
        Assert.True(vm.DockStatusBarTopCommand.CanExecute(null));
        vm.DockStatusBarTopCommand.Execute(null);
        Assert.Equal(VestigiumStatusBarPosition.Top, vm.Status.Position);
        vm.DockStatusBarBottomCommand.Execute(null);
        Assert.Equal(VestigiumStatusBarPosition.Bottom, vm.Status.Position);
    }
}
