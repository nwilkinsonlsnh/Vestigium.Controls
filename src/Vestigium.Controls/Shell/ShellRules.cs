using System.Windows;

namespace Vestigium.Controls.Shell;

public static class ShellRules
{
    public const int DefaultMaxNavDepth = 3;
    public const int MinNavDepth = 1;
    public const int MaxNavDepthCap = 3;
    public const int IndentDip = 20;
    public const int DefaultNavIndent = 20;
    public const int MinNavIndent = 0;
    public const int MaxNavIndent = 80;
    public const int NavIndentStep = 4;

    public const string DefaultHome = "Home";
    public const string DefaultWorkspace = "Workspace";
    public const string DefaultSettings = "Settings";

    public const string PlaceholderSubject = "This area is a placeholder";
    public const string PlaceholderDescription =
        "Replace this item's Content when the module is ready. Title, subject, description, and image are settable on the placeholder.";

    public static readonly string[] DefaultHeaders = [DefaultHome, DefaultWorkspace, DefaultSettings];

    public static int CoerceMaxNavDepth(int value) =>
        value < MinNavDepth ? DefaultMaxNavDepth : Math.Min(value, MaxNavDepthCap);

    public static int CoerceShellDepth(int value) =>
        value < 0 ? 0 : Math.Min(value, MaxNavDepthCap - 1);

    public static int CoerceNavIndent(int value)
    {
        if (value < MinNavIndent) return MinNavIndent;
        if (value > MaxNavIndent) return MaxNavIndent;
        return value;
    }

    public static Thickness NavMargin(int shellDepth, int row, int indent)
    {
        var dip = CoerceNavIndent(indent);
        var left = (CoerceShellDepth(shellDepth) + Math.Max(0, row)) * dip;
        return new Thickness(left, 0, 0, 0);
    }

    public static string NormalizeKey(string? header) =>
        string.IsNullOrWhiteSpace(header) ? string.Empty : header.Trim();
}
