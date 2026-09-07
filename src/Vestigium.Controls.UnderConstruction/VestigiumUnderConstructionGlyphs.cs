using System.Windows.Media;

namespace Vestigium.Controls.UnderConstruction;

public static class VestigiumUnderConstructionGlyphs
{
    public const string ConePath =
        "F1 M45,8 L51,8 L67,72 L29,72 Z M24,72 L72,72 L76,86 L20,86 Z";

    public const string BarrierPath =
        "F1 M12,36 L20,36 L16,88 L8,88 Z M76,36 L84,36 L88,88 L80,88 Z M16,40 L80,40 L80,68 L16,68 Z";

    public static Geometry CreateCone()
    {
        var geometry = Geometry.Parse(ConePath);
        geometry.Freeze();
        return geometry;
    }

    public static Geometry CreateBarrier()
    {
        var geometry = Geometry.Parse(BarrierPath);
        geometry.Freeze();
        return geometry;
    }
}
