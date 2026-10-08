using ChartForgeX;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void ComparisonAndIntervalSvgExposeDataMetadata() {
        var error = Chart.Create()
            .AddErrorBar("Confidence", new[] {
                new ChartErrorBar(1, 42, 35, 51),
                new ChartErrorBar(2, 58, 49, 66)
            })
            .ToSvg();
        CartesianMetadata(CartesianPoint(error, 0, 1), ("x", "2"), ("value", "58"), ("lower", "49"), ("upper", "66"));

        var bubble = Chart.Create()
            .AddBubble("Reach", new[] {
                new ChartBubble(1, 42, 12),
                new ChartBubble(2, 58, 24)
            })
            .ToSvg();
        CartesianMetadata(CartesianPoint(bubble, 0, 1), ("x", "2"), ("y", "58"), ("size", "24"));

        var dumbbell = Chart.Create()
            .AddDumbbell("Before/after", new[] {
                new ChartDumbbell(1, 32, 44),
                new ChartDumbbell(2, 38, 58)
            })
            .ToSvg();
        CartesianMetadata(CartesianPoint(dumbbell, 0, 1), ("x", "2"), ("start", "38"), ("end", "58"), ("delta", "20"));

        var rangeBand = Chart.Create()
            .AddRangeBand("Forecast", new[] {
                new ChartRangeBand(1, 32, 44),
                new ChartRangeBand(2, 38, 58),
                new ChartRangeBand(3, 51, 72)
            })
            .ToSvg();
        Assert(System.Xml.Linq.XDocument.Parse(rangeBand).Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "point") == 3, "Range bands should retain all three source intervals.");
        CartesianMetadata(CartesianPoint(rangeBand, 0, 2), ("x", "3"), ("lower", "51"), ("upper", "72"));

        var rangeArea = Chart.Create()
            .AddRangeArea("Prediction", new[] {
                new ChartRangeBand(1, 32, 44),
                new ChartRangeBand(2, 38, 58),
                new ChartRangeBand(3, 51, 72)
            })
            .ToSvg();
        Assert(System.Xml.Linq.XDocument.Parse(rangeArea).Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "point") == 3, "Range areas should retain all three source intervals.");
        CartesianMetadata(CartesianPoint(rangeArea, 0, 2), ("x", "3"), ("lower", "51"), ("upper", "72"));
    }
}
