using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Controls.PropertiesGrid;

namespace Vestigium.Controls.PropertiesGrid.Demo;

public partial class MainViewModel : ObservableObject
{
    public ProbeSettings East { get; } = new() { DisplayName = "East probe", Ttl = 64 };
    public ProbeSettings West { get; } = new() { DisplayName = "West probe", Ttl = 128 };

    public MainViewModel()
    {
        East.Self = East;
        West.Self = West;
        Selected = East;
        SelectedSet.Add(East);
        CategoryIcons = VestigiumCategoryGlyphs.CreateStandard();
        HostItems =
        [
            new VestigiumPropertyItem { Name = "Region", Category = "Host", Description = "Hand-built item. No reflection.", Kind = VestigiumPropertyEditorKind.Text, Value = "us-east" },
            new VestigiumPropertyItem { Name = "Enabled", Category = "Host", Kind = VestigiumPropertyEditorKind.Boolean, Value = true }
        ];
    }

    [ObservableProperty] private ProbeSettings? _selected;
    [ObservableProperty] private bool _isReadOnly;
    [ObservableProperty] private bool _multiSelect;
    [ObservableProperty] private string _log = "Ready.";
    [ObservableProperty] private VestigiumPropertySort _sort = VestigiumPropertySort.Categorized;
    [ObservableProperty] private TextAlignment _editorTextAlignment = TextAlignment.Left;
    [ObservableProperty] private TextAlignment _categoryTextAlignment = TextAlignment.Left;
    [ObservableProperty] private bool _isCategoryBold = true;
    [ObservableProperty] private Orientation _categoryOrientation = Orientation.Horizontal;

    public ObservableCollection<ProbeSettings> SelectedSet { get; } = [];
    public IList<VestigiumPropertyItem> HostItems { get; }
    public VestigiumCategoryIconCollection CategoryIcons { get; }

    public object? GridTarget => MultiSelect ? null : Selected;
    public System.Collections.IList? GridTargets => MultiSelect ? SelectedSet : null;

    partial void OnSelectedChanged(ProbeSettings? value)
    {
        if (MultiSelect) return;
        OnPropertyChanged(nameof(GridTarget));
    }

    partial void OnMultiSelectChanged(bool value)
    {
        SelectedSet.Clear();
        if (value)
        {
            SelectedSet.Add(East);
            SelectedSet.Add(West);
        }
        OnPropertyChanged(nameof(GridTarget));
        OnPropertyChanged(nameof(GridTargets));
        Log = value ? "Two probes selected. Mixed names expected." : "Single probe.";
    }

    [RelayCommand]
    private void SelectEast()
    {
        MultiSelect = false;
        Selected = East;
        Log = "East probe.";
    }

    [RelayCommand]
    private void SelectWest()
    {
        MultiSelect = false;
        Selected = West;
        Log = "West probe.";
    }

    [RelayCommand]
    private void SelectNone()
    {
        MultiSelect = false;
        Selected = null;
        Log = "No object selected.";
    }

    [RelayCommand]
    private void Categorized() => Sort = VestigiumPropertySort.Categorized;

    [RelayCommand]
    private void Alphabetical() => Sort = VestigiumPropertySort.Alphabetical;

    [RelayCommand] private void AlignLeft() => EditorTextAlignment = TextAlignment.Left;
    [RelayCommand] private void AlignCenter() => EditorTextAlignment = TextAlignment.Center;
    [RelayCommand] private void AlignRight() => EditorTextAlignment = TextAlignment.Right;

    [RelayCommand] private void CategoryAlignLeft() => CategoryTextAlignment = TextAlignment.Left;
    [RelayCommand] private void CategoryAlignCenter() => CategoryTextAlignment = TextAlignment.Center;
    [RelayCommand] private void CategoryAlignRight() => CategoryTextAlignment = TextAlignment.Right;
    [RelayCommand] private void CategoryHorizontal() => CategoryOrientation = Orientation.Horizontal;
    [RelayCommand] private void CategoryVertical() => CategoryOrientation = Orientation.Vertical;
}
