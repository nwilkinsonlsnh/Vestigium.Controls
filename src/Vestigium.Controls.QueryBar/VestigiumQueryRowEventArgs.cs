using System.Windows;

namespace Vestigium.Controls.QueryBar;

public sealed class VestigiumQueryRowEventArgs : RoutedEventArgs
{
    public VestigiumQueryRowEventArgs(RoutedEvent routedEvent, object source, object? row)
        : base(routedEvent, source)
    {
        Row = row;
    }

    public object? Row { get; }
}
