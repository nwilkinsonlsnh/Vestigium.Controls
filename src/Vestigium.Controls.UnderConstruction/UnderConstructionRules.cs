using System.Windows;
using System.Windows.Input;

namespace Vestigium.Controls.UnderConstruction;

public static class UnderConstructionRules
{
    public const string DefaultHeader = "Under Construction";
    public const string DefaultActionText = "Learn more";

    public static string CoerceText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    public static bool ShowMessage(string? message) =>
        !string.IsNullOrWhiteSpace(message);

    public static bool ShowAction(ICommand? command) =>
        command is not null;

    public static Visibility ActiveVisibility(bool isActive) =>
        isActive ? Visibility.Visible : Visibility.Collapsed;
}
