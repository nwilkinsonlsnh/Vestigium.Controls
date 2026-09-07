using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace Vestigium.Controls.PropertiesGrid;

internal static class PropertyRules
{
    public const string MiscCategory = "Misc";
    public const int DefaultMaxExpandDepth = 8;
    public const double DefaultNameColumnWidth = 160;

    public static Type Unwrap(Type type) => Nullable.GetUnderlyingType(type) ?? type;

    public static bool IsNumeric(Type type)
    {
        type = Unwrap(type);
        return type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort)
            || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong)
            || type == typeof(float) || type == typeof(double) || type == typeof(decimal);
    }

    public static bool IsUnsigned(Type type)
    {
        type = Unwrap(type);
        return type == typeof(byte) || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong);
    }

    public static bool IsInteger(Type type)
    {
        type = Unwrap(type);
        return type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort)
            || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong);
    }

    public static bool IsColor(Type type)
    {
        type = Unwrap(type);
        return type == typeof(Color);
    }

    public static bool IsDate(Type type)
    {
        type = Unwrap(type);
        return type == typeof(DateTime) || type == typeof(DateTimeOffset);
    }

    public static bool IsCollection(Type type) =>
        type != typeof(string) && typeof(System.Collections.IList).IsAssignableFrom(type);

    public static bool Compatible(Type a, Type b)
    {
        a = Unwrap(a);
        b = Unwrap(b);
        if (a == b) return true;
        if (IsNumeric(a) && IsNumeric(b)) return true;
        return false;
    }

    public static bool IsExpandableType(Type type)
    {
        type = Unwrap(type);
        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
            || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan)
            || type == typeof(Guid) || IsColor(type) || type == typeof(Uri))
            return false;
        if (IsCollection(type)) return false;
        return type.IsClass || type.IsValueType;
    }

    public static VestigiumPropertyEditorKind Classify(Type type, bool readOnly)
    {
        type = Unwrap(type);
        if (type == typeof(bool)) return VestigiumPropertyEditorKind.Boolean;
        if (type.IsEnum) return VestigiumPropertyEditorKind.Enum;
        if (IsNumeric(type)) return VestigiumPropertyEditorKind.Numeric;
        if (IsDate(type)) return VestigiumPropertyEditorKind.DateTime;
        if (IsColor(type)) return VestigiumPropertyEditorKind.Color;
        if (IsCollection(type)) return VestigiumPropertyEditorKind.Collection;
        if (IsExpandableType(type)) return VestigiumPropertyEditorKind.Expandable;
        if (readOnly) return VestigiumPropertyEditorKind.ReadOnly;
        return type == typeof(string) ? VestigiumPropertyEditorKind.Text : VestigiumPropertyEditorKind.Text;
    }

    public static bool IsIndexer(PropertyDescriptor pd)
    {
        var prop = pd.ComponentType.GetProperty(pd.Name);
        return prop?.GetIndexParameters().Length > 0;
    }

    public static bool TryChangeType(object? value, Type target, out object? result)
    {
        target = Unwrap(target);
        result = null;
        if (value is null)
        {
            if (!target.IsValueType || Nullable.GetUnderlyingType(target) is not null || target.IsClass)
            {
                result = null;
                return true;
            }
            return false;
        }

        var sourceType = Unwrap(value.GetType());
        if (target.IsInstanceOfType(value))
        {
            result = value;
            return true;
        }

        try
        {
            if (target.IsEnum)
            {
                if (value is string s)
                {
                    result = Enum.Parse(target, s, ignoreCase: true);
                    return true;
                }
                result = Enum.ToObject(target, value);
                return true;
            }

            if (IsColor(target) && value is string hex)
            {
                if (TryParseColor(hex, out var color))
                {
                    result = color;
                    return true;
                }
                return false;
            }

            if (value is Color c && target == typeof(string))
            {
                result = FormatColor(c);
                return true;
            }

            if (IsNumeric(target))
            {
                var number = Convert.ToDecimal(value, CultureInfo.CurrentCulture);
                result = Convert.ChangeType(number, target, CultureInfo.CurrentCulture);
                return true;
            }

            if (target == typeof(DateTime))
            {
                result = Convert.ToDateTime(value, CultureInfo.CurrentCulture);
                return true;
            }

            var converter = TypeDescriptor.GetConverter(target);
            if (converter.CanConvertFrom(sourceType))
            {
                result = converter.ConvertFrom(null, CultureInfo.CurrentCulture, value);
                return true;
            }

            result = Convert.ChangeType(value, target, CultureInfo.CurrentCulture);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }

    public static bool TryParseColor(string text, out Color color)
    {
        color = default;
        text = text.Trim();
        if (text.StartsWith('#')) text = text[1..];
        try
        {
            if (text.Length == 6)
            {
                color = Color.FromRgb(
                    Convert.ToByte(text[..2], 16),
                    Convert.ToByte(text[2..4], 16),
                    Convert.ToByte(text[4..6], 16));
                return true;
            }
            if (text.Length == 8)
            {
                color = Color.FromArgb(
                    Convert.ToByte(text[..2], 16),
                    Convert.ToByte(text[2..4], 16),
                    Convert.ToByte(text[4..6], 16),
                    Convert.ToByte(text[6..8], 16));
                return true;
            }
        }
        catch
        {
            return false;
        }
        return false;
    }

    public static string FormatColor(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    public static string Format(object? value, VestigiumPropertyEditorKind kind, bool mixed)
    {
        if (mixed) return "Multiple values";
        if (value is null) return kind is VestigiumPropertyEditorKind.Expandable or VestigiumPropertyEditorKind.Collection
            ? "(none)"
            : string.Empty;
        if (kind == VestigiumPropertyEditorKind.Collection && value is System.Collections.IList list)
            return $"Count = {list.Count}";
        if (value is Color color) return FormatColor(color);
        if (value is bool b) return b ? "True" : "False";
        if (value is DateTime dt) return dt.ToString("g", CultureInfo.CurrentCulture);
        return Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
    }

    public static object? CreateElement(Type type)
    {
        type = Unwrap(type);
        if (type == typeof(string)) return string.Empty;
        if (type == typeof(bool)) return false;
        if (IsNumeric(type)) return Convert.ChangeType(0, type, CultureInfo.InvariantCulture);
        if (type.IsEnum)
        {
            var values = Enum.GetValues(type);
            return values.Length > 0 ? values.GetValue(0) : null;
        }
        if (type == typeof(DateTime)) return DateTime.Now;
        if (IsColor(type)) return Color.FromRgb(0x4A, 0x90, 0xC8);
        try
        {
            var ctor = type.GetConstructor(
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);
            if (ctor is not null)
                return ctor.Invoke(null);
            return Activator.CreateInstance(type, nonPublic: true);
        }
        catch
        {
            return null;
        }
    }

    public static Type? ElementType(Type listType, System.Collections.IList? list)
    {
        if (listType.IsArray) return listType.GetElementType();
        if (listType.IsGenericType)
        {
            var args = listType.GetGenericArguments();
            if (args.Length == 1) return args[0];
        }
        foreach (var itf in listType.GetInterfaces())
        {
            if (itf.IsGenericType && itf.GetGenericTypeDefinition() == typeof(IList<>))
                return itf.GetGenericArguments()[0];
        }
        if (list is { Count: > 0 } && list[0] is not null)
            return list[0]!.GetType();
        return typeof(object);
    }

    public static int CoerceMaxDepth(int value) => value <= 0 ? DefaultMaxExpandDepth : value;

    public static Geometry? TryParseGeometry(string? data)
    {
        if (string.IsNullOrWhiteSpace(data)) return null;
        try
        {
            var geometry = Geometry.Parse(data);
            if (geometry.CanFreeze) geometry.Freeze();
            return geometry;
        }
        catch
        {
            return null;
        }
    }

    public static Geometry? TryParseSvg(string? svg)
    {
        if (string.IsNullOrWhiteSpace(svg)) return null;
        var match = Regex.Match(svg, "\\sd\\s*=\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase);
        if (!match.Success)
            match = Regex.Match(svg, "\\sd\\s*=\\s*'([^']+)'", RegexOptions.IgnoreCase);
        return match.Success ? TryParseGeometry(match.Groups[1].Value) : TryParseGeometry(svg);
    }

    public static VestigiumCategoryIcon? FindCategoryIcon(IEnumerable? icons, string category)
    {
        if (icons is null) return null;
        foreach (var item in icons)
        {
            if (item is VestigiumCategoryIcon icon
                && string.Equals(icon.Category, category, StringComparison.OrdinalIgnoreCase))
                return icon;
        }
        return null;
    }

    public static TextAlignment CoerceTextAlignment(TextAlignment value) =>
        value is TextAlignment.Center or TextAlignment.Right or TextAlignment.Justify
            ? value
            : TextAlignment.Left;
}

