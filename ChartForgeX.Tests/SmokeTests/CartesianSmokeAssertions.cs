using System.Globalization;
using System.Xml.Linq;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
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
