using System.Windows;
using System.Windows.Controls;

namespace Vestigium.Controls.QueryBar;

[TemplatePart(Name = "PART_TextBox", Type = typeof(TextBox))]
[TemplatePart(Name = "PART_Clear", Type = typeof(Button))]
[TemplatePart(Name = "PART_Chevron", Type = typeof(System.Windows.Controls.Primitives.ToggleButton))]
public class VestigiumQueryBar : Control
{
    static VestigiumQueryBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(VestigiumQueryBar), new FrameworkPropertyMetadata(typeof(VestigiumQueryBar)));
    }
}
