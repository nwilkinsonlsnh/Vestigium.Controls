using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Controls.Shell;
using Vestigium.Controls.UnderConstruction;

namespace Vestigium.Controls.Demo;

public partial class DemoWindowViewModel : VestigiumDefaultWindowViewModel
{
    [RelayCommand]
    private void StageIqSuite()
    {
        if (Shell is null) return;
        Shell.ApplySpec(new VestigiumShellSpec
        {
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
        Status.Message = "Staged PingIQ suite.";
    }

    [RelayCommand]
    private void ReplacePingWithInnerForm()
    {
        if (Shell is null) return;
        var ping = Shell["PingIQ"];
        if (ping is null)
        {
            StageIqSuite();
            ping = Shell["PingIQ"];
        }
        if (ping is null) return;

        var inner = VestigiumShell.Stage(new VestigiumShellSpec
        {
            IsSubShell = true,
            ShowStatusBar = false,
            Items =
            {
                new VestigiumNavItemSpec("Live") { Subject = "Live probes", Description = "Inner form under PingIQ." },
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
        Shell.SelectedItem = ping;
        Status.Message = "PingIQ now hosts a TabControl and an inner shell.";
    }

    [RelayCommand]
    private void ResetDefaults()
    {
        Shell?.SeedDefaults();
        Status.Message = "Restored Home / Workspace / Settings.";
    }
}
