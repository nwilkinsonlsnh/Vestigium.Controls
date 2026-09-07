using CommunityToolkit.Mvvm.ComponentModel;

namespace Vestigium.Controls.StatusBar;

public partial class StatusBarColumn : ObservableObject
{
    [ObservableProperty] private string? _key;
    [ObservableProperty] private StatusBarColumnKind _kind = StatusBarColumnKind.Text;
    [ObservableProperty] private StatusBarColumnWidth _width = StatusBarColumnWidth.Auto;
    [ObservableProperty] private double _widthDip = 120;
    [ObservableProperty] private string _text = string.Empty;
    [ObservableProperty] private string? _toolTip;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private bool _isIndeterminate;
    [ObservableProperty] private bool _isProgressVisible;
    [ObservableProperty] private StatusBarIconKind _icon = StatusBarIconKind.None;
    [ObservableProperty] private string _idleText = StatusBarDefaults.IdleText;
    [ObservableProperty] private int _idleTimeoutMs;
    [ObservableProperty] private bool _isLiveRegion;
    [ObservableProperty] private bool _isIdle;
    [ObservableProperty] private StatusBarSlot _slot = StatusBarSlot.Left;
    [ObservableProperty] private bool _isSeparatorVisible;

    public bool ShowProgress => Kind == StatusBarColumnKind.Progress && IsProgressVisible;
    public bool ShowText => Kind is StatusBarColumnKind.Text or StatusBarColumnKind.Clock;
    public bool ShowIconOnly => Kind == StatusBarColumnKind.Icon;

    partial void OnKindChanged(StatusBarColumnKind value) => NotifyLayout();
    partial void OnIsProgressVisibleChanged(bool value) => NotifyLayout();
    partial void OnIconChanged(StatusBarIconKind value) => NotifyLayout();
    partial void OnIsIdleChanged(bool value) => OnPropertyChanged(nameof(DisplayForegroundIdle));

    public bool DisplayForegroundIdle => IsIdle;

    private void NotifyLayout()
    {
        OnPropertyChanged(nameof(ShowProgress));
        OnPropertyChanged(nameof(ShowText));
        OnPropertyChanged(nameof(ShowIconOnly));
    }
}
