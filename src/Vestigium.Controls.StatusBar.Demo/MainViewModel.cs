using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Vestigium.Controls.StatusBar.Demo;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private VestigiumStatusBarPosition _position = VestigiumStatusBarPosition.Bottom;

    [ObservableProperty]
    private string _message = "Ready";

    [RelayCommand]
    private void DockBottom() => Position = VestigiumStatusBarPosition.Bottom;

    [RelayCommand]
    private void DockTop() => Position = VestigiumStatusBarPosition.Top;
}
