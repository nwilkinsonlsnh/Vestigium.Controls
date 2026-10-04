using System.Windows;
using System.Windows.Controls;

namespace Vestigium.Controls.QueryBar;

public class VestigiumQueryBar : Control
{
    static VestigiumQueryBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(VestigiumQueryBar), new FrameworkPropertyMetadata(typeof(VestigiumQueryBar)));
    }
}
