using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Numeric keys may reduce plot space; value axes retain readable type and complete source facts.</summary>
public sealed class AxisReadabilityTests {
    [Theory]
    [InlineData(ChartSeriesKind.Scatter, "color-scale", 800, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.Scatter, "color-scale", 800, VisualThemeMode.Dark)]
    [InlineData(ChartSeriesKind.Bubble, "color-scale", 800, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.Bubble, "color-scale", 800, VisualThemeMode.Dark)]
    [InlineData(ChartSeriesKind.Scatter, "compact-color-dimension", 360, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.Scatter, "compact-color-dimension", 360, VisualThemeMode.Dark)]
    [InlineData(ChartSeriesKind.Bubble, "compact-color-dimension", 360, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.Bubble, "compact-color-dimension", 360, VisualThemeMode.Dark)]
    public void CrowdedNumericKeysKeepReadableValueAxesAndSourceTicks(ChartSeriesKind kind, string variant, int width, VisualThemeMode mode) {
        var chart = V2GalleryModels.Create(kind, variant, mode);
        var context = Context(width, mode);
        var prepared = chart.Prepare(context);
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "axis-y-label").ToArray();
        Assert.True(labels.Length >= 2, "The value axis must retain useful readable captions.");
        Assert.All(labels, label => Assert.Equal(context.Theme.Typography.AxisSize, label.Text.Size));
        var facts = prepared.Regions.Where(region => region.Role == "axis-y-label").ToArray();
        Assert.True(facts.Length >= labels.Length);
        Assert.All(labels, label => Assert.Contains(facts, fact => fact.Label!.StartsWith(string.Join("\n", label.Text.Lines.Select(line => line.Text)) + " (", StringComparison.Ordinal)));
        Assert.Equal(chart.Series.Sum(series => series.AuthoredObservationCount), prepared.Scene.Nodes.OfType<VisualSceneGroup>().Count(node => node.Role == "point"));
        Assert.Contains(XDocument.Parse(prepared.ToSvg()).Descendants(), node => (string?)node.Attribute("data-cfx-role") == "axis-y-label");
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void ExplicitlyAuthoredSmallAxisTypeRemainsAuthoritative() {
        var chart = V2GalleryModels.Create(ChartSeriesKind.Scatter, "color-scale", VisualThemeMode.Light);
        chart.Options.TickLabelStyle.FontSize = 6;
        var prepared = chart.Prepare(Context(800, VisualThemeMode.Light));
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "axis-y-label").ToArray();
        Assert.NotEmpty(labels);
        Assert.All(labels, label => Assert.Equal(6, label.Text.Size));
        Assert.Equal(6, chart.Options.TickLabelStyle.FontSize);
    }

    private static VisualRenderContext Context(int width, VisualThemeMode mode) => new(
        new VisualLayoutOptions(new VisualSize(width, width == 360 ? 360 : 440)), VisualTheme.Graphite(), mode,
        new VisualFrame("Processing time by workload", "Temperature has its own fixed color domain; processing time stays independent"),
        new FontSpec { Family = "Carlito", FilePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf") });
}
