using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Tests;

/// <summary>
/// Reads the box of an SVG mark whether it is written as a <c>rect</c> or, for a bar rounded at its data end only
/// (design rule 4), as a <c>path</c>.
/// </summary>
internal static class SvgMarkGeometry {
    private static readonly Regex Number = new(@"-?\d+(?:\.\d+)?(?:[eE][-+]?\d+)?", RegexOptions.CultureInvariant);

    internal static ChartRect Bounds(XElement element) {
        if (element.Name.LocalName == "rect")
            return new ChartRect(Read(element, "x"), Read(element, "y"), Read(element, "width"), Read(element, "height"));
        var values = Number.Matches((string?)element.Attribute("d") ?? string.Empty).Cast<Match>()
            .Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture)).ToArray();
        var xs = values.Where((_, index) => index % 2 == 0).ToArray(); var ys = values.Where((_, index) => index % 2 == 1).ToArray();
        return new ChartRect(xs.Min(), ys.Min(), xs.Max() - xs.Min(), ys.Max() - ys.Min());
    }

    /// <summary>Reads x, y, width or height from either form; other attributes are read verbatim.</summary>
    internal static double Attribute(XElement element, string name) {
        if (element.Name.LocalName == "rect" || name is not ("x" or "y" or "width" or "height")) return Read(element, name);
        var bounds = Bounds(element);
        return name switch { "x" => bounds.X, "y" => bounds.Y, "width" => bounds.Width, _ => bounds.Height };
    }

    private static double Read(XElement element, string name) => double.Parse((string)element.Attribute(name)!, CultureInfo.InvariantCulture);
}