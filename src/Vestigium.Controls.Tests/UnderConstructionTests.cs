using System.Windows;
using System.Windows.Input;
using Vestigium.Controls.UnderConstruction;

namespace Vestigium.Controls.Tests;

public class UnderConstructionTests
{
    [Fact]
    public void Default_header()
    {
        Assert.Equal("Under Construction", UnderConstructionRules.DefaultHeader);
    }

    [Fact]
    public void Empty_message_is_hidden()
    {
        Assert.False(UnderConstructionRules.ShowMessage(null));
        Assert.False(UnderConstructionRules.ShowMessage("   "));
        Assert.True(UnderConstructionRules.ShowMessage("Scheduled for Q3."));
    }

    [Fact]
    public void Command_null_hides_action()
    {
        Assert.False(UnderConstructionRules.ShowAction(null));
        Assert.True(UnderConstructionRules.ShowAction(new DummyCommand()));
    }

    [Fact]
    public void Inactive_is_collapsed()
    {
        Assert.Equal(Visibility.Visible, UnderConstructionRules.ActiveVisibility(true));
        Assert.Equal(Visibility.Collapsed, UnderConstructionRules.ActiveVisibility(false));
    }

    [Fact]
    public void Whitespace_message_coerces_to_empty()
    {
        Assert.Equal(string.Empty, UnderConstructionRules.CoerceText("  "));
        Assert.Equal("Reports", UnderConstructionRules.CoerceText(" Reports "));
    }

    [Fact]
    public void Default_icon_parses()
    {
        var geometry = VestigiumUnderConstructionGlyphs.CreateDefault();
        Assert.False(geometry.IsEmpty());
        Assert.True(geometry.IsFrozen);
    }

    private sealed class DummyCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) { }
    }
}
