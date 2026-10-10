using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NumericRadialLabelTests {
    [Theory]
    [InlineData(ChartSeriesKind.RadialBar, false)]
    [InlineData(ChartSeriesKind.RadialBar, true)]
    [InlineData(ChartSeriesKind.RadialColumn, false)]
    [InlineData(ChartSeriesKind.RadialColumn, true)]
    public void GalleryInsideCaptionsStayWithinTheirPaintedSectorAndRetainEverySourceValue(ChartSeriesKind kind, bool compact) {
        var chart = V2GalleryModels.Create(kind, compact ? "compact" : "wide");
        var scene = Prepare(chart, compact ? 360 : 800, compact ? 360 : 440);
        var marks = NumericRadialSeriesTests.Marks(scene).ToDictionary(mark => mark.Id!);
        foreach (var label in Captions(scene)) {
            var mark = marks[label.Id!.Replace("-label", "-mark", StringComparison.Ordinal)];
            Assert.True(Shape(mark).Contains(Bounds(label)), label.Id + " must fit inside its actual curved mark.");
        }
        Assert.Equal(8, scene.Regions.Count(region => region.Role == "point"));
        Assert.Equal("1050", Point(scene, "series-0-point-3").Metadata["data-cfx-y"]);
        Assert.Contains(scene.Regions, region => region.Id == "series-0-point-3-label" && region.Label == "1050");
        Assert.Contains(scene.Diagnostics, diagnostic => diagnostic.Code == "numeric-radial.label-overflow");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BroadInsideCaptionsKeepTheirFullValueAndResolveTranslucentMarkInk(bool bars) {
        var chart = NumericRadialSeriesTests.Add(Chart.Create(), bars, "Observed", new ChartPoint(1, 500))
            .WithYAxisBounds(0, 1000).WithRadialGeometry(new(-90, 90, .25, 0, 0))
            .WithDataLabels().WithDataLabelPlacement(ChartDataLabelPlacement.Inside);
        var fill = ChartColor.FromHex("#2468AC").WithAlpha(60);
        chart.Series[0].WithPointColor(0, fill);
        chart.Options.ShowAxes = false; chart.Options.ShowGrid = false;
        var scene = Prepare(chart, 600, 440);
        var label = Assert.Single(Captions(scene));
        Assert.Equal("500", string.Join(" ", label.Text.Lines.Select(line => line.Text)));
        Assert.True(Shape(Assert.Single(NumericRadialSeriesTests.Marks(scene))).Contains(Bounds(label)));
        var background = VisualTheme.Graphite().Resolve(VisualThemeMode.Dark).Background;
        var composed = ChartColorMath.Blend(background, ChartColor.FromRgb(fill.R, fill.G, fill.B), fill.A / 255d);
        Assert.True(ChartColorMath.ContrastRatio(composed, label.Color) >= 4.5);

        var authored = ChartColor.FromHex("#D040E0");
        chart.Series[0].ConfigureDataLabelStyle(style => style.WithColor(authored));
        Assert.Equal(authored, Assert.Single(Captions(Prepare(chart, 600, 440))).Color);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ThinMarksOmitUnfittingInsideInkButKeepOutsideCaptionsAndSourceFacts(bool bars) {
        var chart = NumericRadialSeriesTests.Add(Chart.Create(), bars, "Observed", new ChartPoint(1, 500))
            .WithYAxisBounds(0, 1000).WithRadialGeometry(new(-90, 90, .95, 0, 0))
            .WithDataLabels().WithDataLabelPlacement(ChartDataLabelPlacement.Inside);
        chart.Options.ShowAxes = false; chart.Options.ShowGrid = false;
        var inside = Prepare(chart, 600, 440);
        Assert.Empty(Captions(inside));
        Assert.Contains(inside.Regions, region => region.Id == "series-0-point-0-label" && region.Label == "500");
        Assert.Equal("500", Point(inside, "series-0-point-0").Metadata["data-cfx-y"]);
        Assert.Contains(inside.Diagnostics, diagnostic => diagnostic.Code == "numeric-radial.label-overflow");

        chart.WithDataLabelPlacement(ChartDataLabelPlacement.Right);
        var outside = Prepare(chart, 600, 440);
        var label = Assert.Single(Captions(outside));
        Assert.Equal("500", string.Join(" ", label.Text.Lines.Select(line => line.Text)));
        Assert.False(Shape(Assert.Single(NumericRadialSeriesTests.Marks(outside))).Intersects(Bounds(label)));
        Assert.Equal(VisualTheme.Graphite().Resolve(VisualThemeMode.Dark).Foreground, label.Color);
        Assert.Equal("500", Point(outside, "series-0-point-0").Metadata["data-cfx-y"]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ZeroInsideCaptionsRetainTheirDescriptionWithoutInventingPaintedArea(bool bars) {
        var chart = NumericRadialSeriesTests.Add(Chart.Create(), bars, "Observed", new ChartPoint(1, 0))
            .WithYAxisBounds(0, 1000).WithDataLabels().WithDataLabelPlacement(ChartDataLabelPlacement.Inside);
        chart.Options.ShowAxes = false; chart.Options.ShowGrid = false;
        var scene = Prepare(chart, 600, 440);
        Assert.Empty(Captions(scene));
        Assert.Empty(NumericRadialSeriesTests.Marks(scene));
        Assert.Contains(scene.Regions, region => region.Id == "series-0-point-0-label" && region.Label == "0");
        Assert.Equal("0", Point(scene, "series-0-point-0").Metadata["data-cfx-y"]);
    }

    private static VisualScene Prepare(Chart chart, int width, int height) {
        var font = new FontSpec { Family = "Carlito", FilePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf") };
        return chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, height)),
            themeMode: VisualThemeMode.Dark, frame: new VisualFrame(showLegend: false), font: font)).Scene;
    }
    private static VisualSceneText[] Captions(VisualScene scene) => scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "radial-data-label").ToArray();
    private static VisualSceneGroup Point(VisualScene scene, string id) => scene.Nodes.OfType<VisualSceneGroup>().Single(group => group.Id == id);
    private static ChartRect Bounds(VisualSceneText text) => new(text.X, text.Baseline - text.Text.Ascent, text.Text.Metrics.Width, text.Text.Metrics.Height);
    private static LabelMarkShape Shape(VisualSceneSlice mark) => new(VisualSceneGeometry.Flatten(mark, 8), true, 0);
}
