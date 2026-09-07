using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Vestigium.Controls.PropertiesGrid;

/// <summary>
/// One category header glyph. Assign Image / ImageUri for PNG, ICO, BMP, or DrawingImage.
/// Assign IconData (WPF path mini-language) or Svg (inline or file contents) for vector.
/// Raster wins over vector when both are set.
/// </summary>
public sealed class VestigiumCategoryIcon
{
    public string Category { get; set; } = string.Empty;

    public ImageSource? Image { get; set; }

    public Uri? ImageUri { get; set; }

    public Geometry? IconGeometry { get; set; }

    public string? IconData { get; set; }

    public string? Svg { get; set; }

    public ImageSource? ResolveImage()
    {
        if (Image is not null) return Image;
        if (ImageUri is null) return null;
        if (IsSvg(ImageUri)) return null;
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = ImageUri;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch
        {
            return null;
        }
    }

    public Geometry? ResolveGeometry()
    {
        if (IconGeometry is not null) return IconGeometry;
        var fromData = PropertyRules.TryParseGeometry(IconData);
        if (fromData is not null) return fromData;
        if (!string.IsNullOrWhiteSpace(Svg))
            return PropertyRules.TryParseSvg(Svg);
        if (ImageUri is not null && IsSvg(ImageUri))
            return PropertyRules.TryParseSvg(TryRead(ImageUri));
        return null;
    }

    public bool HasGlyph => ResolveImage() is not null || ResolveGeometry() is not null;

    private static bool IsSvg(Uri uri) =>
        uri.IsAbsoluteUri && uri.AbsolutePath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
        || uri.OriginalString.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);

    private static string? TryRead(Uri uri)
    {
        try
        {
            if (uri.IsFile) return File.ReadAllText(uri.LocalPath);
            if (uri.Scheme is "http" or "https" or "pack")
            {
                var streamInfo = System.Windows.Application.GetResourceStream(uri);
                if (streamInfo?.Stream is null) return null;
                using var reader = new StreamReader(streamInfo.Stream);
                return reader.ReadToEnd();
            }
        }
        catch
        {
            return null;
        }
        return null;
    }
}

public sealed class VestigiumCategoryIconCollection : ObservableCollection<VestigiumCategoryIcon>
{
}

/// <summary>
/// Optional vector pack the host can assign: <c>CategoryIcons = VestigiumCategoryGlyphs.Standard</c>.
/// Not applied unless the host sets it. Not a theme.
/// </summary>
public static class VestigiumCategoryGlyphs
{
    public static VestigiumCategoryIconCollection CreateStandard() =>
    [
        new() { Category = "General", IconData = General },
        new() { Category = "Timing", IconData = Timing },
        new() { Category = "Display", IconData = Display },
        new() { Category = "Network", IconData = Network },
        new() { Category = "Advanced", IconData = Advanced },
        new() { Category = "Misc", IconData = Misc }
    ];

    public const string General = "M2,3.6 H14 V5.4 H2 Z M2,7.1 H14 V8.9 H2 Z M2,10.6 H10 V12.4 H2 Z";
    public const string Timing = "F0 M8,1.2 A6.8,6.8 0 1 1 7.99,1.2 Z M8,3 A5,5 0 1 0 8.01,3 Z M7.25,4.1 H8.75 V8.15 L11.3,9.7 10.45,10.95 7.25,8.75 Z";
    public const string Display = "M1.4,2.8 H14.6 V10.8 H1.4 Z M6,12 H10 M7.2,10.8 H8.8 V13.2 H7.2 Z";
    public const string Network = "M1.4,6.4 H4.6 V9.6 H1.4 Z M11.4,2.2 H14.6 V5.4 H11.4 Z M11.4,10.6 H14.6 V13.8 H11.4 Z M4.6,8 L11.4,3.8 M4.6,8 L11.4,12.2";
    public const string Advanced = "M8,1.2 L9.85,5.55 H14.4 L10.9,8.2 L12.4,12.7 L8,10.15 L3.6,12.7 L5.1,8.2 L1.6,5.55 H6.15 Z";
    public const string Misc = "M2.2,6.7 H4.8 V9.3 H2.2 Z M6.7,6.7 H9.3 V9.3 H6.7 Z M11.2,6.7 H13.8 V9.3 H11.2 Z";
}
