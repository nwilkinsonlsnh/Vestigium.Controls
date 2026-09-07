using Vestigium.Controls.Shell;
using Vestigium.Controls.UnderConstruction;

namespace Vestigium.Controls.Tests;

public class ShellTests
{
    [StaFact]
    public void Stage_without_spec_seeds_home_workspace_settings()
    {
        var shell = VestigiumShell.Stage();
        Assert.Equal(3, shell.NavItems.Count);
        Assert.NotNull(shell["Home"]);
        Assert.NotNull(shell["Workspace"]);
        Assert.NotNull(shell["Settings"]);
        Assert.NotSame(shell["Home"]!.Placeholder, shell["Workspace"]!.Placeholder);
        Assert.Equal(3, shell["Home"]!.Children.Count);
        Assert.NotNull(shell["Overview"]);
        Assert.NotNull(shell["Shortcuts"]);
        Assert.Equal(2, shell["Shortcuts"]!.Children.Count);
        Assert.NotNull(shell["Favorites"]);
    }

    [StaFact]
    public void Each_seed_uses_its_own_construction_page()
    {
        var shell = VestigiumShell.Stage();
        shell["Home"]!.Placeholder.Subject = "Home only";
        Assert.NotEqual("Home only", shell["Workspace"]!.Placeholder.Subject);
        Assert.Equal(ShellRules.PlaceholderSubject, shell["Workspace"]!.Placeholder.Subject);
    }

    [StaFact]
    public void Stage_named_items_and_indexer_are_case_insensitive()
    {
        var shell = VestigiumShell.Stage(new VestigiumShellSpec
        {
            Items =
            {
                new VestigiumNavItemSpec("PingIQ") { Subject = "ICMP engine" },
                new VestigiumNavItemSpec("TraceIQ"),
                new VestigiumNavItemSpec("DnsIQ"),
                new VestigiumNavItemSpec("CertIQ"),
                new VestigiumNavItemSpec("Settings")
            }
        });
        Assert.Equal(5, shell.NavItems.Count);
        var ping = shell["pingiq"];
        Assert.NotNull(ping);
        Assert.Equal("ICMP engine", ping!.Placeholder.Subject);
        Assert.IsType<VestigiumUnderConstruction>(ping.DisplayContent);
    }

    [StaFact]
    public void Replacing_content_hides_placeholder_until_restored()
    {
        var shell = VestigiumShell.Stage();
        var home = shell["Home"]!;
        var marker = new object();
        home.Content = marker;
        Assert.Same(marker, home.DisplayContent);
        home.RestorePlaceholder();
        Assert.Same(home.Placeholder, home.DisplayContent);
    }

    [StaFact]
    public void Header_rename_updates_placeholder_title_until_overridden()
    {
        var item = new VestigiumNavItem("Home");
        item.Header = "Probes";
        Assert.Equal("Probes", item.Placeholder.Title);
        item.SetPlaceholder(title: "Probe Host");
        item.Header = "PingIQ";
        Assert.Equal("Probe Host", item.Placeholder.Title);
        Assert.True(item.TitleOverridden);
    }

    [StaFact]
    public void Sub_shell_hides_status_bar_until_host_sets_it()
    {
        var nested = VestigiumShell.Stage(new VestigiumShellSpec { IsSubShell = true });
        Assert.True(nested.IsSubShell);
        Assert.False(nested.ShowStatusBar);
        Assert.Equal(1, nested.ShellDepth);

        var shown = VestigiumShell.Stage(new VestigiumShellSpec { IsSubShell = true, ShowStatusBar = true });
        Assert.True(shown.ShowStatusBar);
    }

    [StaFact]
    public void Max_nav_depth_clips_fourth_level()
    {
        var spec = new VestigiumShellSpec
        {
            Items =
            {
                new VestigiumNavItemSpec("One")
                {
                    Children =
                    {
                        new VestigiumNavItemSpec("Two")
                        {
                            Children =
                            {
                                new VestigiumNavItemSpec("Three")
                                {
                                    Children = { new VestigiumNavItemSpec("Four") }
                                }
                            }
                        }
                    }
                }
            }
        };
        var shell = VestigiumShell.Stage(spec);
        var three = shell["Three"];
        Assert.NotNull(three);
        Assert.Empty(three!.Children);
        Assert.Null(shell["Four"]);
    }

    [StaFact]
    public void Pinned_content_wins_over_selected_item()
    {
        var shell = VestigiumShell.Stage();
        var pin = new object();
        shell.Content = pin;
        Assert.Same(pin, shell.DisplayContent);
    }

    [StaFact]
    public void Nav_indent_is_settable_and_coerced()
    {
        var shell = VestigiumShell.Stage();
        Assert.Equal(20, shell.NavIndent);
        Assert.Equal(0, shell.Level0Margin.Left);
        Assert.Equal(20, shell.Level1Margin.Left);

        shell.NavIndent = 8;
        Assert.Equal(8, shell.Level1Margin.Left);
        Assert.Equal(16, shell.Level2Margin.Left);

        shell.NavIndent = -10;
        Assert.Equal(0, shell.NavIndent);
        shell.NavIndent = 400;
        Assert.Equal(80, shell.NavIndent);
        Assert.Equal(80, shell.Level1Margin.Left);
    }

    [StaFact]
    public void Default_window_exposes_shell()
    {
        var window = new VestigiumDefaultWindow();
        Assert.NotNull(window.HostShell);
        Assert.Equal(3, window.HostShell.NavItems.Count);
        Assert.True(window.ViewModel.ShowStatusBar);
    }
}
