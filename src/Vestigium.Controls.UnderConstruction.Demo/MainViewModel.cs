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
    [ObservableProperty] private int _titleMaxLength = UnderConstructionRules.DefaultTitleMaxLength;
    [ObservableProperty] private int _subjectMaxLength = UnderConstructionRules.DefaultSubjectMaxLength;
    [ObservableProperty] private int _descriptionMaxLength;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private bool _attachCommand = true;
    [ObservableProperty] private string _actionLog = "No action yet.";
    [ObservableProperty] private int _liveClicks;
    [ObservableProperty] private Geometry? _iconData;
    [ObservableProperty] private ImageSource? _imageSource;
    [ObservableProperty] private string? _imageUri;

    public int TitleRemaining => Remaining(Title, TitleMaxLength);
    public int SubjectRemaining => Remaining(Subject, SubjectMaxLength);
    public string TitleCountLabel => CountLabel(Title, TitleMaxLength);
    public string SubjectCountLabel => CountLabel(Subject, SubjectMaxLength);
    public string DescriptionCountLabel => CountLabel(Description, DescriptionMaxLength);

    public ICommand? ActionCommand => AttachCommand ? NotifyMeCommand : null;

    partial void OnAttachCommandChanged(bool value) => OnPropertyChanged(nameof(ActionCommand));

    partial void OnTitleChanged(string value) => ApplyLimit(nameof(Title), value, TitleMaxLength, v => Title = v);
    partial void OnSubjectChanged(string value) => ApplyLimit(nameof(Subject), value, SubjectMaxLength, v => Subject = v);
    partial void OnDescriptionChanged(string value) => ApplyLimit(nameof(Description), value, DescriptionMaxLength, v => Description = v);

    partial void OnTitleMaxLengthChanged(int value)
    {
        if (value < 0)
        {
            TitleMaxLength = UnderConstructionRules.DefaultTitleMaxLength;
            return;
        }
        Title = UnderConstructionRules.Limit(Title, TitleMaxLength);
        RaiseCounts();
    }

    partial void OnSubjectMaxLengthChanged(int value)
    {
        if (value < 0)
        {
            SubjectMaxLength = UnderConstructionRules.DefaultSubjectMaxLength;
            return;
        }
        Subject = UnderConstructionRules.Limit(Subject, SubjectMaxLength);
        RaiseCounts();
    }

    partial void OnDescriptionMaxLengthChanged(int value)
    {
        if (value < 0)
        {
            DescriptionMaxLength = 0;
            return;
        }
        Description = UnderConstructionRules.Limit(Description, DescriptionMaxLength);
        RaiseCounts();
    }

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

    private void ApplyLimit(string property, string value, int max, Action<string> set)
    {
        var limited = UnderConstructionRules.Limit(value, max);
        if (limited != value)
            set(limited);
        RaiseCounts();
    }

    private void RaiseCounts()
    {
        OnPropertyChanged(nameof(TitleCountLabel));
        OnPropertyChanged(nameof(SubjectCountLabel));
        OnPropertyChanged(nameof(DescriptionCountLabel));
        OnPropertyChanged(nameof(TitleRemaining));
        OnPropertyChanged(nameof(SubjectRemaining));
    }

    private static int Remaining(string text, int max) =>
        max <= 0 ? int.MaxValue : Math.Max(0, max - (text?.Length ?? 0));

    private static string CountLabel(string text, int max)
    {
        var length = text?.Length ?? 0;
        return max <= 0 ? $"{length} / ∞" : $"{length} / {max}";
    }
}
