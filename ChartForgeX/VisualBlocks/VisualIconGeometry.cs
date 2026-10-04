using System.Globalization;

namespace ChartForgeX.VisualBlocks;

/// <summary>Curved icon outlines used by both visual-block renderers.</summary>
internal static class VisualIconGeometry {
    internal static string ForkKnife(double x, double y, double size) => "M " + F(x - size * 0.42) + " " + F(y - size * 0.54) + " V " + F(y + size * 0.48) + " M " + F(x - size * 0.66) + " " + F(y - size * 0.56) + " V " + F(y - size * 0.12) + " M " + F(x - size * 0.42) + " " + F(y - size * 0.56) + " V " + F(y - size * 0.12) + " M " + F(x - size * 0.18) + " " + F(y - size * 0.56) + " V " + F(y - size * 0.12) + " M " + F(x - size * 0.66) + " " + F(y - size * 0.12) + " Q " + F(x - size * 0.42) + " " + F(y + size * 0.18) + " " + F(x - size * 0.18) + " " + F(y - size * 0.12) + " M " + F(x + size * 0.34) + " " + F(y + size * 0.48) + " V " + F(y - size * 0.52) + " Q " + F(x + size * 0.70) + " " + F(y - size * 0.24) + " " + F(x + size * 0.40) + " " + F(y + size * 0.04);

    internal static string Flame(double x, double y, double size) => "M " + F(x) + " " + F(y + size * 0.62) + " C " + F(x - size * 0.70) + " " + F(y + size * 0.24) + " " + F(x - size * 0.38) + " " + F(y - size * 0.46) + " " + F(x - size * 0.08) + " " + F(y - size * 0.82) + " C " + F(x + size * 0.04) + " " + F(y - size * 0.30) + " " + F(x + size * 0.52) + " " + F(y - size * 0.24) + " " + F(x + size * 0.38) + " " + F(y - size * 0.88) + " C " + F(x + size * 0.98) + " " + F(y - size * 0.30) + " " + F(x + size * 0.82) + " " + F(y + size * 0.48) + " " + F(x) + " " + F(y + size * 0.62) + " Z";

    internal static string Droplet(double x, double y, double size) => "M " + F(x) + " " + F(y - size * 0.84) + " C " + F(x - size * 0.48) + " " + F(y - size * 0.22) + " " + F(x - size * 0.64) + " " + F(y + size * 0.10) + " " + F(x - size * 0.64) + " " + F(y + size * 0.32) + " C " + F(x - size * 0.64) + " " + F(y + size * 0.78) + " " + F(x - size * 0.30) + " " + F(y + size * 0.98) + " " + F(x) + " " + F(y + size * 0.98) + " C " + F(x + size * 0.30) + " " + F(y + size * 0.98) + " " + F(x + size * 0.64) + " " + F(y + size * 0.78) + " " + F(x + size * 0.64) + " " + F(y + size * 0.32) + " C " + F(x + size * 0.64) + " " + F(y + size * 0.10) + " " + F(x + size * 0.48) + " " + F(y - size * 0.22) + " " + F(x) + " " + F(y - size * 0.84) + " Z";

    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
