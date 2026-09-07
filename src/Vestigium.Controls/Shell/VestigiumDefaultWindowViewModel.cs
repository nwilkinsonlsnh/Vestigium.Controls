using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Controls.StatusBar;

namespace Vestigium.Controls.Shell;

public partial class VestigiumDefaultWindowViewModel : ObservableObject
{
    public VestigiumDefaultWindowViewModel()
        : this(new VestigiumStatusBarViewModel())
    {
    }

    public VestigiumDefaultWindowViewModel(VestigiumStatusBarViewModel status)
    {
        Status = status;
        Status.Message = "Ready";
        Status.Position = VestigiumStatusBarPosition.Bottom;
    }

    public VestigiumStatusBarViewModel Status { get; }

    [RelayCommand]
    private void DockStatusBarBottom() => Status.Position = VestigiumStatusBarPosition.Bottom;

    [RelayCommand]
    private void DockStatusBarTop() => Status.Position = VestigiumStatusBarPosition.Top;
}
