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

    private int _navIndent = ShellRules.DefaultNavIndent;

    public int NavIndent
    {
        get => _navIndent;
        set
        {
            var coerced = ShellRules.CoerceNavIndent(value);
            if (!SetProperty(ref _navIndent, coerced))
                return;
            if (RootShell is not null)
                RootShell.NavIndent = coerced;
        }
    }

    public VestigiumShell? RootShell { get; set; }

    public VestigiumNavItem? this[string name] => RootShell?[name];

    [RelayCommand]
    private void DockStatusBarBottom() => Status.Position = VestigiumStatusBarPosition.Bottom;

    [RelayCommand]
    private void DockStatusBarTop() => Status.Position = VestigiumStatusBarPosition.Top;

    [RelayCommand]
    private void ToggleStatusBar() => ShowStatusBar = !ShowStatusBar;

    [RelayCommand]
    private void IncreaseNavIndent() =>
        NavIndent = ShellRules.CoerceNavIndent(NavIndent + ShellRules.NavIndentStep);

    [RelayCommand]
    private void DecreaseNavIndent() =>
        NavIndent = ShellRules.CoerceNavIndent(NavIndent - ShellRules.NavIndentStep);
}
