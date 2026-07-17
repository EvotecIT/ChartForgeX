using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void ZeroMarkerRadiusSuppressesOptionalLineMarkers() {
        var color = ChartColor.FromRgb(37, 99, 235);
        var points = new[] { new ChartPoint(1, 10), new ChartPoint(2, 30), new ChartPoint(3, 20) };
        var referenceSvg = Chart.Create()
            .WithSize(320, 200)
            .WithTheme(theme => theme.WithMarkerRadius(4))
            .AddLine("Values", points, color)
            .ToSvg();
        var referenceMarkers = SvgDocument.Parse(referenceSvg).Root
            .FindByTag("circle")
            .Where(element => element.GetAttribute("data-cfx-role") == "line-marker")
            .ToArray();
        Assert(referenceMarkers.Length == points.Length, "Positive marker radii should preserve one SVG marker per line point.");

        var markerless = Chart.Create()
            .WithSize(320, 200)
            .WithTheme(theme => theme.WithMarkerRadius(0))
            .AddLine("Values", points, color);
        var markerlessSvg = markerless.ToSvg();
        Assert(!markerlessSvg.Contains("data-cfx-role=\"line-marker\"", StringComparison.Ordinal), "A zero marker radius should omit optional SVG line markers.");
        Assert(!SvgDocument.Parse(markerlessSvg).Root.FindByTag("circle").Any(), "A markerless line should not advertise a point marker in its SVG legend.");

        var pixels = ReadPngRgba(markerless.ToPng(), out var width, out _);
        var middleX = double.Parse(referenceMarkers[1].GetAttribute("cx")!, CultureInfo.InvariantCulture);
        var middleY = double.Parse(referenceMarkers[1].GetAttribute("cy")!, CultureInfo.InvariantCulture);
        var nextX = double.Parse(referenceMarkers[2].GetAttribute("cx")!, CultureInfo.InvariantCulture);
        var nextY = double.Parse(referenceMarkers[2].GetAttribute("cy")!, CultureInfo.InvariantCulture);
        var length = Math.Sqrt(Math.Pow(nextX - middleX, 2) + Math.Pow(nextY - middleY, 2));
        var sampleX = (int)Math.Round(middleX + (nextX - middleX) / length);
        var sampleY = (int)Math.Round(middleY + (nextY - middleY) / length);
        var offset = (sampleY * width + sampleX) * 4;
        Assert(Math.Abs(pixels[offset] - color.R) <= 48 && Math.Abs(pixels[offset + 1] - color.G) <= 48 && Math.Abs(pixels[offset + 2] - color.B) <= 48,
            "A zero marker radius should not punch marker-outline holes through PNG line paths.");

        var markedArea = Chart.Create()
            .WithSize(320, 200)
            .WithTheme(theme => theme.WithMarkerRadius(4))
            .AddArea("Area", points, color);
        var markerlessArea = Chart.Create()
            .WithSize(320, 200)
            .WithTheme(theme => theme.WithMarkerRadius(0))
            .AddArea("Area", points, color);
        Assert(!SvgDocument.Parse(markerlessArea.ToSvg()).Root.FindByTag("circle").Any(), "A markerless area should not advertise a point marker in its SVG legend.");
        var markedAreaPixels = ReadPngRgba(markedArea.ToPng(), out var areaWidth, out var areaHeight);
        var markerlessAreaPixels = ReadPngRgba(markerlessArea.ToPng(), out _, out _);
        var legendTop = areaHeight * 3 / 4;
        var markedLegendInk = CountNearColorInRect(markedAreaPixels, areaWidth, 0, legendTop, areaWidth, areaHeight - legendTop, color.R, color.G, color.B, 24);
        var markerlessLegendInk = CountNearColorInRect(markerlessAreaPixels, areaWidth, 0, legendTop, areaWidth, areaHeight - legendTop, color.R, color.G, color.B, 24);
        Assert(markerlessLegendInk < markedLegendInk, "A markerless area should omit the PNG legend marker while retaining its line symbol.");
    }
}
