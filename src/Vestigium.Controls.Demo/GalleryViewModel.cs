using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Controls.Shell;
using Vestigium.Controls.StatusBar;
using Vestigium.Controls.UnderConstruction;

namespace Vestigium.Controls.Demo;

public partial class GalleryViewModel : VestigiumDefaultWindowViewModel
{
    public GalleryViewModel()
        : base(new VestigiumStatusBarViewModel())
    {
        Status.Message = "Ready. Home includes nested Overview / Live / Shortcuts.";
    }

    private bool _isSubShell;
    private string _scenario = "Default seed";
    private int _slotCount = 3;

    public bool IsSubShell
    {
        get => _isSubShell;
        set
        {
            if (!SetProperty(ref _isSubShell, value))
                return;
            if (RootShell is null) return;
            RootShell.IsSubShell = value;
            RootShell.ShellDepth = value ? 1 : 0;
            if (value)
                ShowStatusBar = false;
            Status.Message = value
                ? "Sub-shell: radios indent, File/View stay on this window, status bar off."
                : "Root form: status bar available, no extra indent.";
        }
    }

    public string Scenario
    {
        get => _scenario;
        set => SetProperty(ref _scenario, value);
    }

    public int SlotCount
    {
        get => _slotCount;
        set => SetProperty(ref _slotCount, value);
    }

    public bool IsDockedBottom
    {
        get => Status.Position == VestigiumStatusBarPosition.Bottom;
        set
        {
            if (value)
                Status.Position = VestigiumStatusBarPosition.Bottom;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDockedTop));
        }
    }

    public bool IsDockedTop
    {
        get => Status.Position == VestigiumStatusBarPosition.Top;
        set
        {
            if (value)
                Status.Position = VestigiumStatusBarPosition.Top;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDockedBottom));
        }
    }

    [RelayCommand]
    private void StageIqSuite()
    {
        if (RootShell is null) return;
        RootShell.ApplySpec(new VestigiumShellSpec
        {
            IsSubShell = IsSubShell,
            ShowStatusBar = ShowStatusBar,
            NavIndent = NavIndent,
            Items =
            {
                new VestigiumNavItemSpec("PingIQ")
                {
                    Title = "PingIQ",
                    Subject = "ICMP engine",
                    Description = "Live ping, history, and thresholds land here."
                },
                new VestigiumNavItemSpec("TraceIQ")
                {
                    Title = "TraceIQ",
                    Subject = "Path discovery",
                    Description = "Traceroute and hop analysis."
                },
                new VestigiumNavItemSpec("DnsIQ")
                {
                    Title = "DnsIQ",
                    Subject = "Resolver lab",
                    Description = "Query, cache, and record inspection."
                },
                new VestigiumNavItemSpec("CertIQ")
                {
                    Title = "CertIQ",
                    Subject = "Certificate watch",
                    Description = "Expiry, chain, and host coverage."
                },
                new VestigiumNavItemSpec("Settings")
                {
                    Title = "Settings",
                    Subject = "Host preferences",
                    Description = "Theme is assigned by the parent application, not this shell."
                }
            }
        });
        SlotCount = RootShell.NavItems.Count;
        Scenario = "PingIQ suite";
        Status.Message = "Staged PingIQ, TraceIQ, DnsIQ, CertIQ, Settings.";
    }

    [RelayCommand]
    private void ReplacePingWithInnerForm()
    {
        if (RootShell is null) return;
        if (RootShell["PingIQ"] is null)
            StageIqSuite();
        var ping = RootShell["PingIQ"];
        if (ping is null) return;

        var inner = VestigiumShell.Stage(new VestigiumShellSpec
        {
            IsSubShell = true,
            ShowStatusBar = false,
            NavIndent = NavIndent,
            Items =
            {
                new VestigiumNavItemSpec("Live") { Subject = "Live probes", Description = "Inner form under PingIQ. No File menu." },
                new VestigiumNavItemSpec("History") { Subject = "Result history" },
                new VestigiumNavItemSpec("Thresholds") { Subject = "Warn / critical" }
            }
        });

        ping.Content = new TabControl
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Items =
            {
                new TabItem { Header = "Workspace", Content = inner },
                new TabItem
                {
                    Header = "Notes",
                    Content = new VestigiumUnderConstruction
                    {
                        Title = "Notes",
                        Subject = "Tab inside PingIQ",
                        Description = "A TabControl replaced the construction page. This tab is still a placeholder."
                    }
                }
            }
        };
        RootShell.SelectedItem = ping;
        Scenario = "PingIQ inner form";
        Status.Message = "PingIQ now hosts a TabControl and an inner shell.";
    }

    [RelayCommand]
    private void ResetDefaults()
    {
        IsSubShell = false;
        ShowStatusBar = true;
        NavIndent = ShellRules.DefaultNavIndent;
        Status.Position = VestigiumStatusBarPosition.Bottom;
        RootShell?.SeedDefaults();
        SlotCount = RootShell?.NavItems.Count ?? 3;
        Scenario = "Default seed";
        Status.Message = "Restored Home / Workspace / Settings with nested Home pages.";
        OnPropertyChanged(nameof(IsDockedBottom));
        OnPropertyChanged(nameof(IsDockedTop));
    }

    [RelayCommand]
    private void RestoreSelectedPlaceholder()
    {
        RootShell?.SelectedItem?.RestorePlaceholder();
        Status.Message = "Selected slot is a construction page again.";
    }
}
