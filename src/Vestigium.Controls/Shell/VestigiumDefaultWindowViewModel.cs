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

    [ObservableProperty] private bool _showStatusBar = true;

    public VestigiumShell? Shell { get; set; }

    public VestigiumNavItem? this[string name] => Shell?[name];

    [RelayCommand]
    private void DockStatusBarBottom() => Status.Position = VestigiumStatusBarPosition.Bottom;

    [RelayCommand]
    private void DockStatusBarTop() => Status.Position = VestigiumStatusBarPosition.Top;

    [RelayCommand]
    private void ToggleStatusBar() => ShowStatusBar = !ShowStatusBar;
}
