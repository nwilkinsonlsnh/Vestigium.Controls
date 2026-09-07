using System.Windows;
using System.Windows.Controls;

namespace Vestigium.Controls.PropertiesGrid;

public sealed class PropertyEditorSelector : DataTemplateSelector
{
    public DataTemplate? Category { get; set; }
    public DataTemplate? Text { get; set; }
    public DataTemplate? Boolean { get; set; }
    public DataTemplate? Enum { get; set; }
    public DataTemplate? Numeric { get; set; }
    public DataTemplate? DateTime { get; set; }
    public DataTemplate? Color { get; set; }
    public DataTemplate? ReadOnly { get; set; }
    public DataTemplate? Expandable { get; set; }
    public DataTemplate? Collection { get; set; }
    public DataTemplate? Mixed { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is not VestigiumPropertyItem row) return base.SelectTemplate(item, container);
        if (row.IsCategory) return Category;
        if (row.IsMixed && row.Kind != VestigiumPropertyEditorKind.Boolean)
            return Mixed ?? Text;
        return row.Kind switch
        {
            VestigiumPropertyEditorKind.Text => Text,
            VestigiumPropertyEditorKind.Boolean => Boolean,
            VestigiumPropertyEditorKind.Enum => Enum,
            VestigiumPropertyEditorKind.Numeric => Numeric,
            VestigiumPropertyEditorKind.DateTime => DateTime,
            VestigiumPropertyEditorKind.Color => Color,
            VestigiumPropertyEditorKind.ReadOnly => ReadOnly,
            VestigiumPropertyEditorKind.Expandable => Expandable,
            VestigiumPropertyEditorKind.Collection => Collection,
            _ => Text
        };
    }
}
