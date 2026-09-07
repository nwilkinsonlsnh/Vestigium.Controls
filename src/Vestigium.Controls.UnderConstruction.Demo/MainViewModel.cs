using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Vestigium.Controls.UnderConstruction;

namespace Vestigium.Controls.UnderConstruction.Demo;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private string _title = "Vestigium";
    [ObservableProperty] private string _subject = "Default form client area";
    [ObservableProperty] private string _description = "Feature views land here after their requirements are accepted.";
    [ObservableProperty] private int _titleMaxLength = 75;
    [ObservableProperty] private int _subjectMaxLength = 125;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private bool _attachCommand = true;
    [ObservableProperty] private string _actionLog = "No action yet.";
    [ObservableProperty] private int _liveClicks;
    [ObservableProperty] private Geometry? _iconData;
    [ObservableProperty] private ImageSource? _imageSource;
    [ObservableProperty] private string? _imageUri;

    public ICommand? ActionCommand => AttachCommand ? NotifyMeCommand : null;

    partial void OnAttachCommandChanged(bool value) => OnPropertyChanged(nameof(ActionCommand));

    [RelayCommand]
    private void NotifyMe() => ActionLog = $"Notify Me at {DateTime.Now:T}";

    [RelayCommand]
    private void UseBuiltIn()
    {
        IconData = null;
        ImageSource = null;
        ImageUri = null;
        ActionLog = "Built-in cone and barrier.";
    }

    [RelayCommand]
    private void UseCone()
    {
        ImageSource = null;
        ImageUri = null;
        IconData = VestigiumUnderConstructionGlyphs.CreateCone();
        ActionLog = "Geometry cone.";
    }

    [RelayCommand]
    private void UseBarrier()
    {
        ImageSource = null;
        ImageUri = null;
        IconData = VestigiumUnderConstructionGlyphs.CreateBarrier();
        ActionLog = "Geometry barrier.";
    }

    [RelayCommand]
    private void BrowseImage()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.svg;*.webp|All files|*.*"
        };
        if (dialog.ShowDialog() != true)
            return;
        ImageUri = dialog.FileName;
        ActionLog = $"Image: {dialog.FileName}";
    }

    [RelayCommand]
    private void LiveTile()
    {
        LiveClicks++;
        ActionLog = $"Live tile clicks: {LiveClicks}";
    }
}
