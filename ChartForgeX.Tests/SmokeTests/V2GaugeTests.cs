using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class V2GaugeTests {
    [Theory]
    [InlineData(ChartGaugeForm.Arc)]
    [InlineData(ChartGaugeForm.Needle)]
    [InlineData(ChartGaugeForm.Linear)]
    public void Forms_RetainRawOutOfRangeValuesAndClampDrawnMarks(ChartGaugeForm form) {
        var chart = Chart.Create().AddGauge("Capacity", 140, 0, 100).WithGauge(options => {
            options.Form = form; options.Target = 75; options.Caption = "Available capacity";
            options.Bands.Add(new ChartGaugeBand(0, 50, ChartSeriesState.Warning));
            options.Bands.Add(new ChartGaugeBand(50, 100, ChartSeriesState.Success));
        });
        var scene = Compile(chart);
        var group = Assert.Single(scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "gauge");
        Assert.Equal("140", group.Metadata["data-cfx-value"]);
        Assert.Equal("1", group.Metadata["data-cfx-percent"]);
        Assert.Contains(scene.Regions, region => region.Role == "gauge-label" && region.Label == "140");
        Assert.Contains(scene.Regions, region => region.Role == "gauge-title" && region.Label == "Available capacity");
        Assert.Contains(scene.Nodes, node => node.Role == "gauge-target");
        Assert.Equal(2, scene.Nodes.Count(node => node.Role == "gauge-band"));
        if (form == ChartGaugeForm.Arc) {
            var arc = Assert.Single(scene.Nodes.OfType<VisualSceneSlice>(), slice => slice.Role == "gauge-value");
            Assert.Equal(Math.PI * 4 / 3, arc.Sweep, 10);
            Assert.Equal(new VisualRenderContext().Theme.Resolve(VisualThemeMode.Light).Status.Pass.Fill, arc.Fill);
        } else if (form == ChartGaugeForm.Needle) Assert.Contains(scene.Nodes, node => node.Role == "gauge-needle");
        else {
            var track = Assert.Single(scene.Nodes.OfType<VisualSceneRectangle>(), rect => rect.Role == "gauge-track");
            var value = Assert.Single(scene.Nodes.OfType<VisualSceneRectangle>(), rect => rect.Role == "gauge-value");
            Assert.Equal(track.Bounds.Width, value.Bounds.Width, 8);
        }
        var svg = XDocument.Parse(VisualSceneSvgRenderer.Render(scene));
        Assert.Contains(svg.Descendants(), element => (string?)element.Attribute("data-cfx-value") == "140");
        var native = VisualSceneRasterRenderer.Render(scene);
        Assert.Equal(420, native.Width); Assert.Equal(320, native.Height);
        Assert.Contains(native.Pixels.Where((value, index) => index % 4 == 3), alpha => alpha > 0);
    }

    [Fact]
    public void ExplicitPointPaintAndLabelsWinWhileFullTextSurvivesFittingAndArtifactHandoff() {
        const string full = "Complete operator-provided measurement label with additional retained context";
        var chart = Chart.Create().AddGauge("Long caption that will not fit a compact surface", 42).WithGauge(options => options.Target = 110);
        var explicitColor = ChartColor.FromHex("#123456");
        chart.Series[0].WithPointColor(0, explicitColor).WithPointLabel(0, full);
        var scene = Compile(chart, new ChartRect(0, 0, 160, 130));
        var value = Assert.Single(scene.Nodes.OfType<VisualSceneSlice>(), slice => slice.Role == "gauge-value");
        Assert.Equal(explicitColor, value.Fill);
        Assert.Contains(scene.Regions, region => region.Role == "gauge-label" && region.Label == full);
        var prepared = new PreparedVisual(scene);
        var artifact = prepared.ToArtifact("gauge", VisualArtifactKind.Chart);
        Assert.Contains(artifact.Regions, region => region.Label == full);
        chart.Series[0].PointLabels[0] = "Changed";
        Assert.Contains(full, artifact.ToSvg());
        Assert.Contains(scene.Regions, region => region.Role == "gauge-target-label" && region.Label == "110");
    }

    [Fact]
    public void OverlappingBandsAndInvalidMutableRangesFailBeforePainting() {
        var chart = Chart.Create().AddGauge("Load", 40).WithGauge(options => {
            options.Bands.Add(new ChartGaugeBand(0, 60, ChartSeriesState.Success));
            options.Bands.Add(new ChartGaugeBand(50, 100, ChartSeriesState.Warning));
        });
        Assert.Contains("overlap", Assert.Throws<InvalidOperationException>(() => Compile(chart)).Message);
        chart.Options.Gauge.Bands.Clear(); chart.Series[0].Points[1] = new ChartPoint(0, 40);
        Assert.Contains("range", Assert.Throws<InvalidOperationException>(() => Compile(chart)).Message);
    }

    [Fact]
    public void LabelSuppressionAndAxisVisibilityAreIndependent() {
        var chart = Chart.Create().AddGauge("Load", -20).WithAxes(false);
        chart.Series[0].ShowDataLabels = false;
        var scene = Compile(chart);
        Assert.DoesNotContain(scene.Nodes, node => node.Role is "gauge-label" or "gauge-title" or "gauge-min-label" or "gauge-max-label");
        Assert.DoesNotContain(scene.Nodes.OfType<VisualSceneSlice>(), slice => slice.Role == "gauge-value");
        Assert.Contains(scene.Regions, region => region.Role == "gauge" && region.Label!.Contains("-20"));
    }

    [Fact]
    public void LinearAxesUseFiveResolvedValuesAndBandsKeepUnclampedSourceRanges() {
        var chart = Chart.Create().AddLinearGauge("Range", 50, 0, 100).WithGauge(options => options.Bands.Add(new ChartGaugeBand(-20, 120, ChartSeriesState.Info)));
        var seen = new List<double>(); chart.Options.ValueFormatter = number => { seen.Add(number); return "V=" + number; };
        var scene = Compile(chart);
        Assert.Equal(5, scene.Nodes.Count(node => node.Role == "gauge-tick"));
        Assert.Contains(25d, seen); Assert.Contains(75d, seen);
        Assert.Contains(scene.Regions, region => region.Role == "gauge-min-label" && region.Label == "V=0");
        Assert.Contains(scene.Regions, region => region.Role == "gauge-max-label" && region.Label == "V=100");
        var band = Assert.Single(scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "gauge-band-source");
        Assert.Equal("-20", band.Metadata["data-cfx-min"]); Assert.Equal("120", band.Metadata["data-cfx-max"]);
        var bounds = Assert.Single(scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "gauge-band").Bounds;
        Assert.True(bounds.Left >= 0 && bounds.Right <= scene.Size.Width);
    }

    private static VisualScene Compile(Chart chart, ChartRect? bounds = null) {
        var plot = bounds ?? new ChartRect(0, 0, 420, 320);
        var context = new VisualRenderContext();
        var builder = new VisualSceneBuilder(new VisualSize(plot.Right, plot.Bottom), context.Font);
        VisualGaugeCompiler.Build(chart, context, builder, plot);
        return builder.Build();
    }
}
