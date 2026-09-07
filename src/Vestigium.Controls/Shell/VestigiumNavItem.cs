using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Vestigium.Controls.UnderConstruction;

namespace Vestigium.Controls.Shell;

public partial class VestigiumNavItem : ObservableObject
{
    public VestigiumNavItem()
        : this(ShellRules.DefaultHome)
    {
    }

    public VestigiumNavItem(string header)
    {
        var name = ShellRules.NormalizeKey(header);
        if (name.Length == 0)
            name = ShellRules.DefaultHome;
        Key = name;
        _header = name;
        Placeholder = new VestigiumUnderConstruction
        {
            Title = name,
            Subject = ShellRules.PlaceholderSubject,
            Description = ShellRules.PlaceholderDescription
        };
        Children = new ObservableCollection<VestigiumNavItem>();
    }

    public string Key { get; set; }

    public VestigiumUnderConstruction Placeholder { get; }

    public ObservableCollection<VestigiumNavItem> Children { get; }

    [ObservableProperty] private string _header = ShellRules.DefaultHome;
    [ObservableProperty] private object? _content;
    [ObservableProperty] private ICommand? _command;
    [ObservableProperty] private bool _isEnabled = true;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _titleOverridden;

    public object DisplayContent => Content ?? Placeholder;

    public bool HasChildren => Children.Count > 0;

    partial void OnHeaderChanged(string oldValue, string newValue)
    {
        var name = ShellRules.NormalizeKey(newValue);
        if (name.Length == 0)
            return;
        if (string.Equals(Key, oldValue, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(Key))
            Key = name;
        if (!TitleOverridden)
            Placeholder.Title = name;
    }

    partial void OnContentChanged(object? value) => OnPropertyChanged(nameof(DisplayContent));

    public void SetPlaceholder(
        string? title = null,
        string? subject = null,
        string? description = null,
        ImageSource? image = null,
        string? imageUri = null)
    {
        if (title is not null)
        {
            Placeholder.Title = title;
            TitleOverridden = true;
        }
        if (subject is not null)
            Placeholder.Subject = subject;
        if (description is not null)
            Placeholder.Description = description;
        if (image is not null)
            Placeholder.ImageSource = image;
        if (imageUri is not null)
            Placeholder.ImageUri = imageUri;
        OnPropertyChanged(nameof(DisplayContent));
    }

    public void RestorePlaceholder()
    {
        Content = null;
    }
}
