using System.Windows;
using System.Windows.Input;

namespace Vestigium.Controls.UnderConstruction;

public static class UnderConstructionRules
{
    public const string DefaultTitle = "Under Construction";
    public const string DefaultActionText = "Learn more";
    public const int DefaultTitleMaxLength = 75;
    public const int DefaultSubjectMaxLength = 125;

    public static string CoerceText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    public static string Limit(string? value, int maxLength)
    {
        var text = CoerceText(value);
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
