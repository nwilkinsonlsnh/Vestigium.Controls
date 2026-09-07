using System.Globalization;
using System.Windows.Data;
using Vestigium.Controls.NumericUpDown;

namespace Vestigium.Controls.PropertiesGrid;

public sealed class SignModeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? VestigiumNumericSignMode.Unsigned : VestigiumNumericSignMode.Signed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

public sealed class InputModeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? VestigiumNumericInputMode.ReadOnly : VestigiumNumericInputMode.Full;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
