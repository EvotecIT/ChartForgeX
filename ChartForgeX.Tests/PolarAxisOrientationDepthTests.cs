using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Dedicated radial text uses the existing physical-axis orientation, footprint and native paint owner.</summary>
public sealed class PolarAxisOrientationDepthTests {
    [Theory]
    [InlineData(true, 360, false)] [InlineData(true, 360, true)] [InlineData(true, 800, false)] [InlineData(true, 800, true)]
    [InlineData(false, 360, false)] [InlineData(false, 360, true)] [InlineData(false, 800, false)] [InlineData(false, 800, true)]
    public void RelativeAndFixedAxisTextShareRotatedNativeFootprintsAndCompleteSourceCaptions(bool radar, int width, bool dark) {
        var chart = Chart.Create().WithSize(width, 420).WithHeader(false).WithLegend(false).WithDataLabels(false).WithYAxisBounds(0, 100)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        var points = Enumerable.Range(0, 4).Select(index => new ChartPoint(radar ? index + 1 : index * Math.PI / 2, 35 + index * 15)).ToArray();
        if (radar) chart.WithXLabels("North", "East", "South", "West").AddRadarLine("Signal", points); else chart.AddPolar("Signal", points);
        chart.Options.PolarRotationDegrees = 25;
        chart.Options.XAxis.LabelOrientation = ChartAxisLabelOrientation.Parallel; chart.Options.XAxis.LabelAngle = 12;
        chart.Options.YAxis.LabelOrientation = ChartAxisLabelOrientation.Perpendicular; chart.Options.YAxis.LabelAngle = -15;
        var prepared = PolarDepthContractTests.Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng(); var document = XDocument.Parse(svg);
        var categoryRole = radar ? "radar-axis-label" : "polar-angle-label";
        var valueRole = radar ? "radar-ring-label" : "polar-radius-label";
        var painted = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == categoryRole || text.Role == valueRole).ToArray();
        Assert.Contains(painted, text => text.Role == categoryRole); Assert.Contains(painted, text => text.Role == valueRole);
        Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Rotation.HasValue);
        Assert.Equal(radar ? 4 : Math.Max(4, Math.Min(12, chart.Options.XAxis.TickCount)), prepared.Regions.Count(region => region.Role == categoryRole));
        var rotated = document.Descendants().Where(node => node.Name.LocalName == "text" && node.Ancestors().Any(parent => ((string?)parent.Attribute("transform"))?.StartsWith("rotate(", StringComparison.Ordinal) == true)).ToArray();
        Assert.NotEmpty(rotated);
        var capture = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture); var name = (radar ? "radar" : "polar") + "-axis-orientation-" + width + "-" + (dark ? "dark" : "light");
            File.WriteAllText(Path.Combine(capture, name + ".svg"), svg); File.WriteAllBytes(Path.Combine(capture, name + ".png"), png);
        }
        chart.Options.XAxis.LabelAngle = -30; chart.Options.XAxis.LabelOrientation = ChartAxisLabelOrientation.Fixed;
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }
}
