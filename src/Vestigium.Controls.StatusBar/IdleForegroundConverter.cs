using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Vestigium.Controls.StatusBar;

public sealed class IdleForegroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true
            ? new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8))
            : new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class IconVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is StatusBarIconKind kind && kind != StatusBarIconKind.None
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
