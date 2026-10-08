using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class V2PolarTests {
    [Fact]
    public void PolarMapsRadiansAndExplicitValueDomainWithSourceColorsAndFullFormatting() {
        var chart = Chart.Create().AddPolar("Signal", new[] { new ChartPoint(0, 100), new ChartPoint(Math.PI / 2, 50), new ChartPoint(Math.PI, 25) }).WithDataLabels();
        chart.Options.YAxis.Minimum = 0; chart.Options.YAxis.Maximum = 100;
        chart.Options.ValueFormatter = value => value.ToString("0.0", CultureInfo.GetCultureInfo("pl-PL")) + " units";
        var color = ChartColor.FromHex("#123456"); chart.Series[0].WithPointColor(1, color).WithMarkerRadius(6);
        var scene = Compile(chart);
        var points = scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "polar-point").ToArray();
        var cx = 220d; var cy = 165d;
        Assert.Equal(cy, points[0].Cy, 8); Assert.True(points[0].Cx > cx);
        Assert.Equal(cx, points[1].Cx, 8); Assert.True(points[1].Cy < cy);
        Assert.Equal((points[0].Cx - cx) / 2, cy - points[1].Cy, 8);
        Assert.Equal(color, points[1].Fill); Assert.Equal(6, points[1].Rx);
        Assert.Contains(scene.Regions, region => region.Id == "series-0-point-1" && region.Label!.Contains("50,0 units"));
        var svg = XDocument.Parse(VisualSceneSvgRenderer.Render(scene));
        Assert.Contains(svg.Descendants(), element => (string?)element.Attribute("data-cfx-angle") == (Math.PI / 2).ToString("R", CultureInfo.InvariantCulture));
        Assert.Contains(VisualSceneRasterRenderer.Render(scene).Pixels.Where((_, index) => index % 4 == 3), alpha => alpha > 0);
    }

    [Fact]
    public void RadarAlignsCategoriesAndRetainsMissingObservationsWithoutInventingSourcePointIds() {
        var chart = Chart.Create().AddRadar("First", new[] { new ChartPoint(1, 80), new ChartPoint(2, 60), new ChartPoint(3, 40) })
            .AddRadar("Second", new[] { new ChartPoint(1, 30), new ChartPoint(3, 70), new ChartPoint(4, 50) }).WithXLabels("North", "East", "South", "West");
        chart.Series[1].StateRole = ChartSeriesState.Warning;
        var scene = Compile(chart);
        Assert.Equal(2, scene.Nodes.Count(node => node.Role == "radar-area"));
        Assert.Equal(8, scene.Nodes.Count(node => node.Role == "radar-point"));
        var missing = scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "radar-point-source" && group.Metadata["data-cfx-missing"] == "true").ToArray();
        Assert.Equal(2, missing.Length); Assert.All(missing, group => Assert.Equal("-1", group.Metadata["data-cfx-point"]));
        Assert.Contains(scene.Regions, region => region.Id == "series-1-missing-category-1" && region.Label == "East: 0");
        var secondArea = scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == "radar-area").Last();
        Assert.Equal(ChartColorMath.WithOpacity(new VisualRenderContext().Theme.Resolve(VisualThemeMode.Light).Status.Medium.Fill, new VisualRenderContext().Theme.AreaOpacity), secondArea.Fill);
        chart.Options.YAxis.Scale = ChartScaleKind.Logarithmic;
        Assert.NotEmpty(Compile(chart).Nodes);
    }

    [Fact]
    public void PolarAreaUsesEqualAnglesSquareRootRadiiAndVisibleZeroSlotsWithPatterns() {
        var chart = Chart.Create().AddPolarArea("Distribution", new[] { new ChartPoint(1, 100), new ChartPoint(2, 25), new ChartPoint(3, 0) }).WithXLabels("Large", "Small", "Zero");
        chart.Series[0].WithPointFillPattern(1, ChartFillPattern.Crosshatch);
        var scene = Compile(chart);
        var segments = scene.Nodes.OfType<VisualSceneSlice>().Where(slice => slice.Role == "polar-area-segment").ToArray();
        Assert.Equal(2, segments.Length); Assert.Equal(Math.PI * 2 / 3, segments[0].Sweep, 8);
        Assert.Equal(segments[0].Outer / 2, segments[1].Outer, 8);
        var zero = Assert.Single(scene.Nodes.OfType<VisualSceneSlice>(), slice => slice.Role == "polar-area-zero-slot");
        Assert.True(zero.Inner > 0); Assert.Equal(segments[0].Outer, zero.Outer, 8);
        Assert.Contains(scene.Nodes, node => node.Role == "polar-area-pattern");
        Assert.Contains(scene.Regions, region => region.Label == "Zero: 0");
        Assert.Equal(3, VisualPolarCompiler.LegendEntries(chart, new VisualRenderContext().Theme.Resolve(VisualThemeMode.Light)).Count);
        chart.Series[0].Points[0] = new ChartPoint(1, 0); chart.Series[0].Points[1] = new ChartPoint(2, 0);
        Assert.Equal(3, Compile(chart).Nodes.Count(node => node.Role == "polar-area-zero-slot"));
        Assert.Contains(Compile(chart).Diagnostics, diagnostic => diagnostic.Code == "polar-area.all-zero");
    }

    [Theory]
    [InlineData(140, 100)]
    [InlineData(700, 180)]
    public void MeasuredAxesFitFixedViewportAndRetainCompleteLabelsInArtifacts(int width, int height) {
        const string full = "The complete category label remains available after fitting a compact polar viewport";
        var chart = Chart.Create().AddRadar("Categories", new[] { new ChartPoint(1, 20), new ChartPoint(2, 40), new ChartPoint(3, 60) }).WithXLabels(full, full + " B", full + " C");
        var scene = Compile(chart, new ChartRect(0, 0, width, height));
        var artifact = new PreparedVisual(scene).ToArtifact("radar", VisualArtifactKind.Chart);
        Assert.Contains(artifact.Regions, region => region.Label == full);
        Assert.Contains(full, artifact.ToSvg());
        foreach (var point in scene.Nodes.OfType<VisualSceneEllipse>().Where(point => point.Role == "radar-point")) {
            Assert.InRange(point.Cx - point.Rx, 0, width); Assert.InRange(point.Cx + point.Rx, 0, width);
            Assert.InRange(point.Cy - point.Ry, 0, height); Assert.InRange(point.Cy + point.Ry, 0, height);
        }
        foreach (var text in scene.Nodes.OfType<VisualSceneText>()) {
            Assert.True(text.X >= 0 && text.X + text.Text.Metrics.Width <= width + .001);
            Assert.True(text.Baseline - text.Text.Ascent >= -.001 && text.Baseline - text.Text.Ascent + text.Text.Metrics.Height <= height + .001);
        }
        Assert.NotEmpty(VisualSceneRasterRenderer.Render(scene).Pixels);
    }

    [Fact]
    public void InvalidLogValuesFailAndGridAndAxesVisibilityRemainIndependent() {
        var chart = Chart.Create().AddPolar("Invalid", new[] { new ChartPoint(0, 0), new ChartPoint(1, 2) });
        chart.Options.YAxis.Scale = ChartScaleKind.Logarithmic;
        Assert.Throws<InvalidOperationException>(() => Compile(chart));
        chart.Options.YAxis.Scale = ChartScaleKind.Linear;
        chart.Options.ShowAxes = false;
        var scene = Compile(chart);
        Assert.Contains(scene.Nodes, node => node.Role == "polar-ring");
        Assert.DoesNotContain(scene.Nodes, node => node.Role is "polar-angle-label" or "polar-radius-label");
        chart.Options.ShowGrid = false;
        Assert.DoesNotContain(Compile(chart).Nodes, node => node.Role is "polar-ring" or "polar-spoke");
    }

    [Fact]
    public void RadarPointOverridesFollowSourceOrderAfterCategoriesAreSorted() {
        var chart = Chart.Create().AddRadar("Unsorted", new[] { new ChartPoint(3, 90), new ChartPoint(1, 70), new ChartPoint(2, 80) }).WithDataLabels();
        chart.Options.ShowAxes = false;
        chart.Series[0].WithPointLabel(0, "kept").WithPointDataLabelStyle(0, style => style.WithFontSize(18).WithTextCase(TextCaseTransform.Uppercase));
        var scene = Compile(chart);
        var label = Assert.Single(scene.Nodes.OfType<VisualSceneText>(), text => text.Id == "series-0-point-0-label");
        Assert.Equal("KEPT", Assert.Single(label.Text.Lines).Text); Assert.Equal(18, label.Text.Size);
        var source = Assert.Single(scene.Nodes.OfType<VisualSceneGroup>(), group => group.Id == "series-0-point-0");
        Assert.Equal("3", source.Metadata["data-cfx-category"]);
    }

    private static VisualScene Compile(Chart chart, ChartRect? bounds = null) {
        var plot = bounds ?? new ChartRect(0, 0, 440, 330); var context = new VisualRenderContext();
        var builder = new VisualSceneBuilder(new VisualSize(plot.Right, plot.Bottom), context.Font);
        VisualPolarCompiler.Build(chart, context, builder, plot); return builder.Build();
    }
}
