using System.Windows.Media;

namespace Vestigium.Controls.UnderConstruction;

public static class VestigiumUnderConstructionGlyphs
{
    /// <summary>Traffic cone, 96×96 design box.</summary>
    public const string DefaultPath =
        "F1 M48,6 L58,6 L78,70 L18,70 Z M14,70 L82,70 L88,88 L8,88 Z M36,28 L60,28 L62,40 L34,40 Z M32,46 L64,46 L66,58 L30,58 Z";

    public static Geometry CreateDefault()
    {
        var geometry = Geometry.Parse(DefaultPath);
        geometry.Freeze();
        return geometry;
    }

    public static Geometry CreateBarrier()
    {
        var geometry = Geometry.Parse(
            "F1 M8,36 L88,36 L88,60 L8,60 Z M16,20 L28,20 L28,76 L16,76 Z M68,20 L80,20 L80,76 L68,76 Z M8,42 L88,42 L88,50 L8,50 Z");
        geometry.Freeze();
        return geometry;
    }
}
