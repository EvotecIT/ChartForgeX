using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;
using static ChartForgeX.Tests.ScatterBubbleColorRenderingTests;

namespace ChartForgeX.Tests;

/// <summary>Per-series keys reuse the matrix scene while preserving independent color formatting and bounded layout.</summary>
public sealed class ScatterBubbleColorLegendTests {
    [Fact]
    public void OffsetPlotKeepsItsMarksAndClippingAboveTheNumericKey() {
        var chart = Chart.Create().WithXAxisBounds(0, 3).WithYAxisBounds(0, 100)
            .AddScatter("Samples", new[] { new ChartPoint(1, 0).WithColorValue(0), new ChartPoint(2, 100).WithColorValue(10) });
        chart.Series[0].WithColorScale(ChartColorScale.Sequential(Low, High));
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 320)));
        var builder = new VisualSceneBuilder(context.Layout.Size, context.Font);
        var plot = new ChartRect(60, 80, 520, 180);
        VisualCartesianCompiler.Build(chart, context, builder, plot, new ChartRect(30, 20, 580, 240));
        var scene = builder.Build();
        var key = scene.Regions.Single(region => region.Id == "color-scale-0");
        var marks = scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "marker").ToArray();
        Assert.Equal(2, marks.Length);
        Assert.All(marks, mark => Assert.True(mark.Cy <= key.Bounds.Top + .000001));
        var clips = scene.Nodes.OfType<VisualSceneGroup>().Where(node => node.Clip.HasValue)
            .Select(node => node.Clip!.Value).Where(bounds => bounds.Top == plot.Top && bounds.Width == plot.Width).ToArray();
        Assert.NotEmpty(clips);
        Assert.All(clips, bounds => Assert.True(bounds.Bottom <= key.Bounds.Top + .000001));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ContinuousAndDiscreteColorCaptionsStayIndependentOfTheYValueFormatter(bool discrete) {
        var chart = Create(false, .0000001, .0000002, .0000003).WithValueFormatter(value => value + " kWh");
        chart.Series[0].WithColorScale(discrete ? ChartColorScale.Discrete(new[] {
            new ChartColorBand(.0000002, Low, "Low"), new ChartColorBand(.0000003, High, "Middle"), new ChartColorBand(null, Empty, "High")
        }) : ChartColorScale.Sequential(Low, High));
        chart.ConfigureLabels(labels => labels.Value = "reading");
        var prepared = Prepare(chart); var document = XDocument.Parse(prepared.ToSvg());
        var key = Role(document, "color-scale-0").Single();
        Assert.DoesNotContain("kWh", key.Value);
        Assert.Contains("kWh", (string)Role(document, "point")[0].Attribute("data-cfx-label")!);
        Assert.Contains("Temperature (°C): 1E-07", prepared.Regions.Single(region => region.Id == "series-0-point-0").Label);
        var steps = Role(document, "color-scale-0-step");
        Assert.Equal(discrete ? 3 : 5, steps.Length);
        Assert.True(steps.Select(step => (string?)step.Attribute("aria-label")).Distinct().Count() > 1);
        if (discrete) {
            Assert.Contains("reading", (string)steps[1].Attribute("data-cfx-label")!);
            Assert.Equal("2E-07", (string?)steps[1].Attribute("data-cfx-lower-bound"));
            Assert.Equal("true", (string?)steps[1].Attribute("data-cfx-upper-exclusive"));
        }
        var matrix = Chart.Create().AddHeatmapRow("Samples", new[] { 1d, 2, 3 })
            .WithHeatmapColorScale(ChartColorScale.Sequential(Low, High)).WithValueFormatter(value => value + " kWh");
        Assert.Contains("kWh", Role(XDocument.Parse(Prepare(matrix).ToSvg()), "heatmap-scale").Single().Value);
    }

    [Theory]
    [InlineData("inferred", false)]
    [InlineData("fixed", true)]
    [InlineData("discrete", true)]
    public void AllMissingColorHasNoInventedInferredExtentButKeepsAuthoredKeys(string mode, bool hasSteps) {
        var chart = Create(true, null, null);
        chart.Series[0].WithColorScale(mode == "discrete" ? ChartColorScale.Discrete(new[] { new ChartColorBand(null, Low, "Any") })
            : mode == "fixed" ? ChartColorScale.Sequential(Low, High).WithValueRange(-10, 10) : ChartColorScale.Sequential(Low, High));
        var prepared = Prepare(chart); var document = XDocument.Parse(prepared.ToSvg());
        var key = Role(document, "color-scale-0").Single();
        Assert.Equal(hasSteps, Role(document, "color-scale-0-step").Length > 0);
        Assert.NotEmpty(Role(document, "color-scale-0-missing-swatch"));
        if (mode == "inferred") {
            Assert.Null(key.Attribute("data-cfx-min-value")); Assert.Null(key.Attribute("data-cfx-max-value"));
            Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.color-scale-overflow");
        }
        chart.Series[0].ReplaceBubbles(Array.Empty<ChartBubble>());
        Assert.Empty(Role(XDocument.Parse(Prepare(chart).ToSvg()), "color-scale-0"));
    }

    [Fact]
    public void KeysRespectFrameAndNumericVisibilityWhilePointLegendUsesMappedPaint() {
        var chart = Create(false, 0, 10).WithPointLegend();
        chart.Series[0].WithColorScale(ChartColorScale.Sequential(Low, High));
        var prepared = Prepare(chart); var document = XDocument.Parse(prepared.ToSvg());
        var swatches = Role(document, "legend-swatch");
        Assert.Equal(new[] { Low.ToHex(), High.ToHex() }, swatches.Select(swatch => (string?)swatch.Attribute("fill")));
        chart.WithLegend(false);
        Assert.Empty(Role(XDocument.Parse(Prepare(chart).ToSvg()), "color-scale-0"));
        chart.WithLegend(); chart.Series[0].ShowInLegend = false;
        Assert.NotEmpty(Role(XDocument.Parse(Prepare(chart).ToSvg()), "color-scale-0"));
        chart.Series[0].ColorDimension.ShowLegend = false;
        Assert.Empty(Role(XDocument.Parse(Prepare(chart).ToSvg()), "color-scale-0"));
        chart.Series[0].ShowInLegend = true;
        chart.Series[0].ColorDimension.ShowLegend = true;
        prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(800, 500)), frame: new VisualFrame(showLegend: false)));
        Assert.Empty(Role(XDocument.Parse(prepared.ToSvg()), "color-scale-0"));
        Assert.NotEmpty(Role(XDocument.Parse(Prepare(chart).ToSvg()), "color-scale-0"));
    }

    [Theory]
    [InlineData("asymmetric", 1d)]
    [InlineData("fractional", 1.28)]
    [InlineData("lower", 0d)]
    [InlineData("upper", 4d)]
    public void CompactKeyMidpointUsesTheCanonicalStopCenterAndNativeSwatchPaint(string mode, double midpointStep) {
        const string family = "CFX Scatter Color Carlito";
        FontRegistry.Register(family, Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"));
        var scale = mode == "asymmetric" ? ChartColorScale.Diverging(new[] { Low }, ChartColor.White, new[] { Empty, High, ChartColor.Black }, 0).WithValueRange(-10, 30)
            : ChartColorScale.Diverging(Low, ChartColor.White, High, mode == "lower" ? -10 : mode == "upper" ? 20 : 3.2).WithValueRange(0, 10);
        var chart = Create(false, 0, 3.2, 10).WithSize(360, 400).WithFontFamily(family);
        chart.Series[0].WithColorScale(scale.WithLabels(null, "Target", null));
        var prepared = Prepare(chart); var document = XDocument.Parse(prepared.ToSvg());
        var swatches = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "color-scale-0-swatch").ToArray();
        var center = swatches[0].Bounds.Left + (midpointStep + .5) * swatches[0].Bounds.Width;
        var caption = prepared.Scene.Nodes.OfType<VisualSceneText>().Single(node => node.Id == "color-scale-0-label-midpoint");
        Assert.Equal(center, caption.X, 6);
        var svgText = document.Descendants().Single(node => (string?)node.Attribute("data-cfx-source-id") == caption.Id)
            .Descendants().Single(node => node.Name.LocalName == "text");
        Assert.InRange(Math.Abs(Number(svgText, "x") + caption.Text.Metrics.Width / 2 - center), 0, .001);
        Assert.True(caption.Text.Size >= 8);
        var key = prepared.Regions.Single(region => region.Id == "color-scale-0");
        var region = prepared.Regions.Single(item => item.Id == caption.Id);
        Assert.True(region.Bounds.Left >= key.Bounds.Left - .000001 && region.Bounds.Right <= key.Bounds.Right + .000001);
        Assert.Contains("Target", region.Label);
        var image = ChartForgeX.Raster.RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        foreach (var swatch in swatches) {
            Assert.Equal(12, swatch.Bounds.Height);
            var box = swatch.Bounds; var offset = ((int)(box.Top + box.Height / 2) * image.Width + (int)(box.Left + box.Width / 2)) * 4;
            Assert.Equal(swatch.Fill!.Value, ChartColor.FromRgba(image.Pixels[offset], image.Pixels[offset + 1], image.Pixels[offset + 2], image.Pixels[offset + 3]));
        }
        Capture(prepared, "scatter-key-" + mode);
    }

    [Fact]
    public void SeveralKeysShareABoundedLaneAndRetainCompleteSemanticDomainsOnOverflow() {
        var chart = Create(false, -10, 10).WithSize(360, 360);
        for (var index = 1; index < 3; index++) chart.AddScatter("Samples " + index, new[] { new ChartPoint(1, index).WithColorValue(index * 100) });
        foreach (var series in chart.Series) series.WithColorScale(ChartColorScale.Sequential(Low, High).WithLabels(null, "Target", null));
        var prepared = Prepare(chart);
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.color-scale-overflow");
        var document = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(3, Role(document, "color-scale-source").Length);
        Assert.Equal("200", (string?)Role(document, "color-scale-2").Single().Attribute("data-cfx-max-value"));
        var top = prepared.Regions.Where(region => region.Role == "color-scale-title").Min(region => region.Bounds.Top);
        var bottom = prepared.Regions.Where(region => region.Role is "color-scale-0" or "color-scale-1" or "color-scale-2").Max(region => region.Bounds.Bottom);
        Assert.InRange(bottom - top, 0, 360 * .35);
    }

    [Theory]
    [InlineData(ChartSeriesKind.Scatter)]
    [InlineData(ChartSeriesKind.Bubble)]
    public void CompactGalleryKeepsReadableCompleteKeysAndRoomForAxisValues(ChartSeriesKind kind) {
        const string family = "CFX Compact Color Key Carlito";
        var fonts = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito");
        FontRegistry.Register(family, Path.Combine(fonts, "Carlito-Regular.ttf"), 400);
        FontRegistry.Register(family, Path.Combine(fonts, "Carlito-Bold.ttf"), 700);
        var chart = V2GalleryModels.Create(kind, "compact-color-scale", VisualThemeMode.Light);
        var subtitle = kind == ChartSeriesKind.Scatter ? "Temperature has its own fixed color domain; processing time stays independent"
            : "Size shows requests; named color bands show temperature";
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(360, 360)), VisualTheme.Graphite(),
            frame: new VisualFrame(V2GalleryModels.Title(kind), subtitle), font: FontSpec.FromFamily(family)));
        var swatches = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "color-scale-0-swatch").ToArray();
        Assert.All(swatches, swatch => Assert.Equal(12, swatch.Bounds.Height));
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code is "cartesian.color-scale-overflow" or "cartesian.axis-title-overflow");
        var captions = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "color-scale-0-label").ToArray();
        Assert.All(captions, caption => {
            Assert.True(caption.Text.Size >= 8);
            Assert.Equal(prepared.Regions.Single(region => region.Id == caption.Id).Label, string.Join("", caption.Text.Lines.Select(line => line.Text)));
        });
        var y = prepared.Scene.Nodes.OfType<VisualSceneLine>().Single(node => node.Role == "axis-y");
        Assert.True(Math.Abs(y.End.Y - y.Start.Y) >= 60);
        Assert.True(prepared.Scene.Nodes.Count(node => node.Role == "axis-y-label") >= 3);
        var document = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(kind == ChartSeriesKind.Scatter ? "-15" : null, (string?)Role(document, "color-scale-0").Single().Attribute("data-cfx-min-value"));
        if (kind == ChartSeriesKind.Scatter) {
            var midpoint = captions.Single(node => node.Id == "color-scale-0-label-midpoint");
            Assert.Equal(swatches[1].Bounds.Left + swatches[1].Bounds.Width / 2, midpoint.X, 6);
            var low = captions.Single(node => node.Id == "color-scale-0-label-low");
            Assert.Equal(low.Baseline, midpoint.Baseline, 6);
            Assert.True(low.X + low.Text.Metrics.Width < midpoint.X - midpoint.Text.Metrics.Width / 2);
        } else Assert.Equal("15", (string?)Role(document, "color-scale-0-step")[1].Attribute("data-cfx-upper-bound"));
        Capture(prepared, "compact-color-key-" + kind.ToString().ToLowerInvariant());
    }

}
