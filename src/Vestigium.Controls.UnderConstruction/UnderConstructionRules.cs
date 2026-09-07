using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Vestigium.Controls.UnderConstruction;

public static class UnderConstructionRules
{
    public const string DefaultTitle = "Under Construction";
    public const string DefaultActionText = "Learn more";
    public const int DefaultTitleMaxLength = 75;
    public const int DefaultSubjectMaxLength = 125;

    public static string CoerceText(string? value) => CoerceSingleLine(value);

    public static string CoerceSingleLine(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        return string.Join(' ',
            value.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n', StringSplitOptions.None)
                .Select(static line => line.Trim())
                .Where(static line => line.Length > 0));
    }

    public static string CoerceMultiline(string? value)
    {
        if (value is null)
            return string.Empty;
        return value.Replace("\r\n", "\n").Replace('\r', '\n');
    }

    public static string Limit(string? value, int maxLength) =>
        Limit(value, maxLength, multiline: false);

    public static string Limit(string? value, int maxLength, bool multiline)
    {
        var text = multiline ? CoerceMultiline(value) : CoerceSingleLine(value);
        if (maxLength <= 0 || text.Length <= maxLength)
            return text;
        return text[..maxLength];
    }

    public static int CoerceMaxLength(int value, int fallback) =>
        value < 0 ? fallback : value;

    public static bool ShowText(string? value) =>
        !string.IsNullOrWhiteSpace(value);

    public static bool ShowAction(ICommand? command) =>
        command is not null;

    public static Visibility ActiveVisibility(bool isActive) =>
        isActive ? Visibility.Visible : Visibility.Collapsed;
}
