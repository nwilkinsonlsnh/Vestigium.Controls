using System.Windows;
using System.Windows.Input;
using Vestigium.Controls.UnderConstruction;

namespace Vestigium.Controls.Tests;

public class UnderConstructionTests
{
    [Fact]
    public void Default_title()
    {
        Assert.Equal("Under Construction", UnderConstructionRules.DefaultTitle);
        Assert.Equal(75, UnderConstructionRules.DefaultTitleMaxLength);
        Assert.Equal(125, UnderConstructionRules.DefaultSubjectMaxLength);
    }

    [Fact]
    public void Empty_text_is_hidden()
    {
        Assert.False(UnderConstructionRules.ShowText(null));
        Assert.False(UnderConstructionRules.ShowText("   "));
        Assert.True(UnderConstructionRules.ShowText("Scheduled for Q3."));
    }

    [Fact]
    public void Title_truncates_at_default_75()
    {
        var text = new string('A', 90);
        Assert.Equal(75, UnderConstructionRules.Limit(text, 75).Length);
    }

    [Fact]
    public void Subject_truncates_at_default_125()
    {
        var text = new string('B', 200);
        Assert.Equal(125, UnderConstructionRules.Limit(text, 125).Length);
    }

    [Fact]
    public void Max_length_zero_does_not_truncate()
    {
        var text = new string('C', 300);
        Assert.Equal(300, UnderConstructionRules.Limit(text, 0).Length);
    }

    [Fact]
    public void Host_can_raise_the_limit()
    {
        var text = new string('D', 100);
        Assert.Equal(100, UnderConstructionRules.Limit(text, 200).Length);
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
    public void Cone_geometry_parses()
    {
        var geometry = VestigiumUnderConstructionGlyphs.CreateCone();
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
