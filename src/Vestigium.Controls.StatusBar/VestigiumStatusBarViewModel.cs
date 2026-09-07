using CommunityToolkit.Mvvm.ComponentModel;

namespace Vestigium.Controls.StatusBar;

public partial class VestigiumStatusBarViewModel : ObservableObject
{
    [ObservableProperty]
    private VestigiumStatusBarPosition _position = VestigiumStatusBarPosition.Bottom;

    [ObservableProperty]
    private string _message = "Ready";

    [ObservableProperty]
    private string? _trailingText;

    [ObservableProperty]
    private bool _isProgressVisible;

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private bool _isIndeterminate;

    [ObservableProperty]
    private bool _isClockVisible = true;
}
