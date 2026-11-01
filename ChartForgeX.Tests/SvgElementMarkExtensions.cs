using System.Globalization;
using System.Text.RegularExpressions;
using ChartForgeX.Svg;

namespace ChartForgeX.Tests;

/// <summary>Reads rect and data-end-rounded path marks alike through the internal SVG reader.</summary>
internal static class SvgElementMarkExtensions {
    private static readonly Regex Number = new(@"-?\d+(?:\.\d+)?(?:[eE][-+]?\d+)?", RegexOptions.CultureInvariant);

    /// <summary>Rect and path marks (a bar rounded at its data end only is written as a path).</summary>
    internal static IEnumerable<SvgElement> FindMarks(this SvgElement root) => root.DescendantsAndSelf().Where(element => element.Name is "rect" or "path");

    internal static double MarkNumber(this SvgElement element, string name) {
        if (element.GetAttribute(name) is { } direct) return double.Parse(direct, CultureInfo.InvariantCulture);
        var values = Number.Matches(element.GetAttribute("d") ?? string.Empty).Cast<Match>().Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture)).ToArray();
        var xs = values.Where((_, index) => index % 2 == 0).ToArray(); var ys = values.Where((_, index) => index % 2 == 1).ToArray();
        return name switch { "x" => xs.Min(), "y" => ys.Min(), "width" => xs.Max() - xs.Min(), "height" => ys.Max() - ys.Min(), _ => throw new ArgumentException(name) };
    }
}