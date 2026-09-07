using System.Windows;

namespace Vestigium.Controls.PropertiesGrid;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class VestigiumTextAlignmentAttribute : Attribute
{
    public VestigiumTextAlignmentAttribute(TextAlignment alignment) => Alignment = alignment;

    public TextAlignment Alignment { get; }
}
