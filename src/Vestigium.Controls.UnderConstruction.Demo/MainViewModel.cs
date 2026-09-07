using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Controls.UnderConstruction;

namespace Vestigium.Controls.UnderConstruction.Demo;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private string _header = "Under Construction";
    [ObservableProperty] private string _message = "This reporting module is scheduled for Q3.";
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private bool _attachCommand = true;
    [ObservableProperty] private string _actionLog = "No action yet.";
    [ObservableProperty] private int _liveClicks;
    [ObservableProperty] private Geometry _iconData = VestigiumUnderConstructionGlyphs.CreateDefault();

    public ICommand? ActionCommand => AttachCommand ? NotifyMeCommand : null;

    partial void OnAttachCommandChanged(bool value) => OnPropertyChanged(nameof(ActionCommand));

    [RelayCommand]
    private void NotifyMe() => ActionLog = $"Notify Me at {DateTime.Now:T}";


    [RelayCommand]
    private void UseCone() => IconData = VestigiumUnderConstructionGlyphs.CreateDefault();

    [RelayCommand]
    private void UseBarrier() => IconData = VestigiumUnderConstructionGlyphs.CreateBarrier();

    [RelayCommand]
    private void LiveTile()
    {
        LiveClicks++;
        ActionLog = $"Live tile clicks: {LiveClicks}";
    }
}
