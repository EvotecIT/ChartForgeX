using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Published native examples preserve the facts and request policies that their figures explain.</summary>
public sealed class WebsiteNativeCapabilityRecipeTests {
    [Theory]
    [InlineData(ChartSeriesKind.Treemap, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.Treemap, VisualThemeMode.Dark)]
    [InlineData(ChartSeriesKind.Sunburst, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.Sunburst, VisualThemeMode.Dark)]
    public void AreaHierarchyFiguresUseOneOriginalSourceForFullAndExistingRootViews(ChartSeriesKind kind, VisualThemeMode mode) {
        var canonical = SunburstRecipe.Create("wide", mode).Series[0].HierarchyItems;
        var chart = NativeHierarchyWebsiteRecipe.Create(kind, mode);
        var original = chart.Series[0].HierarchyItems.Select(item => (item.Id, item.ParentId, item.Value, item.ColorValue)).ToArray();
        Assert.Equal(canonical.Select(item => (item.Id, item.ParentId, item.Value, item.ColorValue)), original);
        var full = NativeHierarchyWebsiteRecipe.Prepare(kind, mode);
        var subtree = NativeHierarchyWebsiteRecipe.Prepare(kind, mode, "engineering");
        Assert.Null(full.HierarchyView);
        Assert.Equal("engineering", subtree.HierarchyView!.RootId);
        var fullText = XDocument.Parse(full.ToSvg("full")).Descendants().Where(item => item.Name.LocalName == "text").Select(item => item.Value).ToArray();
        var subtreeText = XDocument.Parse(subtree.ToSvg("subtree")).Descendants().Where(item => item.Name.LocalName == "text").Select(item => item.Value).ToArray();
        Assert.Contains(fullText, text => text.Contains("Operations", StringComparison.Ordinal));
        Assert.DoesNotContain(subtreeText, text => text.Contains("Operations", StringComparison.Ordinal));
        Assert.Equal(original, chart.Series[0].HierarchyItems.Select(item => (item.Id, item.ParentId, item.Value, item.ColorValue)));
        foreach (var prepared in new[] { full, subtree }) {
            var pixels = RasterImageDecoder.Decode(prepared.ToPng());
            Assert.Equal((760, 480), (pixels.Width, pixels.Height));
        }
    }

    [Theory]
    [InlineData("positive-log")]
    [InlineData("negative-log")]
    [InlineData("ordinal")]
    [InlineData("calendar-month")]
    public void AxisFiguresRetainTheirSourceSignOrUtcInstantAndExplicitNativePolicy(string variant) {
        var chart = NativeAxisWebsiteRecipe.Create(variant);
        var points = chart.Series[0].Points.ToArray();
        if (variant.EndsWith("log", StringComparison.Ordinal)) {
            Assert.Equal(variant == "negative-log" ? new[] { -64d, -8, -1 } : new[] { 1d, 8, 64 }, points.Select(point => point.Y));
            Assert.Equal(ChartScaleKind.Logarithmic, chart.Options.YAxis.Scale);
            Assert.Equal(2, chart.Options.YAxis.LogarithmBase);
            Assert.Equal(variant == "negative-log" ? ChartLogarithmicDomain.Negative : ChartLogarithmicDomain.Positive, chart.Options.YAxis.LogarithmicDomain);
        } else {
            Assert.Equal(new[] { new DateTime(2026, 1, 10).ToOADate(), new DateTime(2026, 3, 30).ToOADate() }, points.Select(point => point.X));
            Assert.Equal(new[] { 18d, 27 }, points.Select(point => point.Y));
            Assert.Equal(ChartScaleKind.Time, chart.Options.XAxis.Scale);
            Assert.NotNull(chart.Options.XAxis.TimeSpacing);
            if (variant == "ordinal") Assert.Same(ChartTimeSpacing.Ordinal, chart.Options.XAxis.TimeSpacing);
            else Assert.Equal(ChartTimeTickUnit.Month, chart.Options.XAxis.TimeSpacing!.CalendarUnit!.Unit);
        }
        var svg = XDocument.Parse(chart.ToSvg("axis-example"));
        Assert.Equal("760", (string?)svg.Root!.Attribute("width"));
        Assert.Equal("440", (string?)svg.Root.Attribute("height"));
        var pixels = RasterImageDecoder.Decode(chart.ToPng());
        Assert.Equal((760, 440), (pixels.Width, pixels.Height));
        Assert.Equal(points, chart.Series[0].Points);
    }
}
