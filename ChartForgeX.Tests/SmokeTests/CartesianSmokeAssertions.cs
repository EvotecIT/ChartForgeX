using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static ChartRect CartesianPlot(string svg) {
        var document = XDocument.Parse(svg);
        var line = document.Descendants().First(element => (string?)element.Attribute("data-cfx-role") == "line");
        var reference = (string)line.Ancestors().First(element => element.Attribute("clip-path") != null).Attribute("clip-path")!;
        var clipId = reference.Substring(5, reference.Length - 6);
        var bounds = document.Descendants().Single(element => element.Name.LocalName == "clipPath" && (string?)element.Attribute("id") == clipId).Elements().Single();
        double Number(string attribute) => double.Parse((string)bounds.Attribute(attribute)!, CultureInfo.InvariantCulture);
        return new ChartRect(Number("x"), Number("y"), Number("width"), Number("height"));
    }
    private static XElement CartesianPoint(string svg, int series, int point) => XDocument.Parse(svg).Descendants().Single(element =>
        (string?)element.Attribute("data-cfx-role") == "point" && (string?)element.Attribute("data-cfx-series") == series.ToString(CultureInfo.InvariantCulture)
        && (string?)element.Attribute("data-cfx-point") == point.ToString(CultureInfo.InvariantCulture));

    private static double CartesianMarkAttribute(string svg, int series, int point, string role, string attribute) =>
        double.Parse((string)CartesianPoint(svg, series, point).Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == role).Attribute(attribute)!, CultureInfo.InvariantCulture);

    private static void CartesianMetadata(XElement element, params (string Name, string Value)[] values) {
        foreach (var value in values) Assert((string?)element.Attribute("data-cfx-" + value.Name) == value.Value,
            "The source observation must retain " + value.Name + "=" + value.Value + ".");
    }
}
