using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Vestigium.Controls.QueryBar;
using Vestigium.Helpers.Kql;
using Xunit;

namespace Vestigium.Controls.Tests;

public sealed class QueryBarTests
{
    [StaFact]
    public void Template_loads_the_bar_parts()
    {
        var bar = Show(new VestigiumQueryBar());
        Assert.NotNull(Part<TextBox>(bar, "PART_TextBox"));
        Assert.NotNull(Part<Button>(bar, "PART_Clear"));
        Assert.NotNull(Part<ToggleButton>(bar, "PART_Chevron"));
        Close(bar);
    }

    [StaFact]
    public void Clear_empties_text_and_leaves_the_caret_at_the_end()
    {
        var bar = Show(new VestigiumQueryBar { Text = "route.protocol" });
        var cleared = false;
        bar.Cleared += (_, _) => cleared = true;
        Part<Button>(bar, "PART_Clear").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        var box = Part<TextBox>(bar, "PART_TextBox");
        Assert.Equal(string.Empty, bar.Text);
        Assert.Equal(0, box.CaretIndex);
        Assert.True(cleared);
        Close(bar);
    }

    [StaFact]
    public void Saved_apply_writes_text_and_does_not_open_completion()
    {
        var bar = Show(new VestigiumQueryBar { Session = KqlHelper.Create(KqlPack.Route) });
        bar.ApplyQueryCommand.Execute(new Row("route.protocol == tcp"));
        var box = Part<TextBox>(bar, "PART_TextBox");
        Assert.Equal("route.protocol == tcp", box.Text);
        Assert.Equal(box.Text.Length, box.CaretIndex);
        Assert.False(Part<Popup>(bar, "PART_Completion").IsOpen);
        Close(bar);
    }

    [StaFact]
    public void Tab_accept_leaves_the_caret_at_the_end()
    {
        var bar = Show(new VestigiumQueryBar
        {
            Session = KqlHelper.Create(KqlPack.Route),
            CompletionDelay = 1,
            Text = "route.prot"
        });
        var box = Part<TextBox>(bar, "PART_TextBox");
        Pump(80);
        box.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(box)!, 0, Key.Tab)
        {
            RoutedEvent = UIElement.PreviewKeyDownEvent
        });
        Assert.EndsWith(" ", box.Text);
        Assert.Equal(box.Text.Length, box.CaretIndex);
        Close(bar);
    }

    [StaFact]
    public void Limit_zero_hides_the_chevron_and_keeps_the_rows()
    {
        var rows = new[] { new Row("route.protocol == tcp") };
        var bar = Show(new VestigiumQueryBar { Queries = rows, Limit = 0 });
        Assert.Equal(Visibility.Collapsed, Part<ToggleButton>(bar, "PART_Chevron").Visibility);
        Assert.Same(rows, bar.Queries);
        Close(bar);
    }

    private static VestigiumQueryBar Show(VestigiumQueryBar bar)
    {
        var window = new Window { Content = bar, Width = 480, Height = 240, ShowInTaskbar = false };
        window.Show();
        return bar;
    }

    private static void Close(VestigiumQueryBar bar) => Window.GetWindow(bar)?.Close();

    private static T Part<T>(VestigiumQueryBar bar, string name) where T : class =>
        (T)bar.Template.FindName(name, bar)!;

    private static void Pump(int milliseconds)
    {
        var frame = new System.Windows.Threading.DispatcherFrame();
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(milliseconds) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            frame.Continue = false;
        };
        timer.Start();
        System.Windows.Threading.Dispatcher.PushFrame(frame);
    }

    private sealed class Row(string text)
    {
        public string Text { get; } = text;
    }
}
