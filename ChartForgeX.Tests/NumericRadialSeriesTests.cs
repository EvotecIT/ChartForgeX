using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NumericRadialSeriesTests {
    [Fact]
    public void PublicKindsKeepTheirAssignedSerializedIdentities() {
        Assert.Equal(24, (int)ChartSeriesKind.ProgressRing);
        Assert.Equal(50, (int)ChartSeriesKind.RadialBar);
        Assert.Equal(51, (int)ChartSeriesKind.RadialColumn);
    }

    [Fact]
    public void ColumnValueTicksLeaveTheFirstCategoriesObservationLabelsReadable() {
        var chart = V2GalleryModels.Create(ChartSeriesKind.RadialColumn).WithSize(800, 440)
            .WithTitle(V2GalleryModels.Title(ChartSeriesKind.RadialColumn)).WithSubtitle("Grouped source counts by region");
        var svg = XDocument.Parse(chart.ToSvg());
        Assert.Contains(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "radial-data-label" && element.Value == "1200");
        Assert.Contains(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "radial-data-label" && element.Value == "320");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RotatedHalfTurnRegionsContainThePaintedSector(bool bars) {
        var chart = Add(Chart.Create(), bars, "Observed", new ChartPoint(1, 1200))
            .WithYAxisBounds(0, 1200).WithRadialGeometry(new(-180, 0, .2, 0, 0));
        var scene = Compile(chart); var mark = Assert.Single(Marks(scene));
        var bounds = Assert.Single(scene.Regions, region => region.Id == "series-0-point-0").Bounds;
        Assert.True(bounds.Top <= mark.Cy - mark.Outer + 1e-9);
        Assert.True(bounds.Left <= mark.Cx - mark.Outer + 1e-9);
        Assert.True(bounds.Right >= mark.Cx + mark.Outer - 1e-9);
        Assert.True(bounds.Bottom >= mark.Cy - 1e-9);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GroupedSignedSourceValuesUseNumericDomainsAndSeparatePhysicalSlots(bool bars) {
        var chart = Add(Chart.Create(), bars, "Observed", new(1, 1200), new(2, -400));
        Add(chart, bars, "Expected", new(1, 800), new(2, -200));
        chart.WithYAxisBounds(-500, 1500).WithRadialGeometry(new(-60, 240, .25, .2, .1));
        var scene = Compile(chart); var marks = Marks(scene);
        Assert.Equal(4, marks.Length);
        var first = marks[0]; var peer = marks[2];
        if (bars) Assert.True(first.Outer <= peer.Inner);
        else Assert.True(first.Start + first.Sweep <= peer.Start);
        if (bars) Assert.Equal(1200d / 2000 * Math.PI * 5 / 3, first.Sweep, 9);
        else Assert.Equal(1.5, (first.Outer - first.Inner) / (peer.Outer - peer.Inner), 9);
        var facts = Point(scene, 0, 0);
        Assert.Equal("1200", facts["data-cfx-y"]);
        Assert.Equal("0", facts["data-cfx-base"]);
        Assert.Equal("false", facts["data-cfx-clipped"]);
        Assert.Contains(scene.Regions, region => region.Id == "series-0-point-1" && region.Label!.Contains("-400", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NamedNormalizedStacksShareSlotsAndKeepRawContributionAndSignedTotals(bool bars) {
        var chart = Add(Chart.Create(), bars, "First", new(1, 200), new(2, -150));
        Add(chart, bars, "Second", new(1, 300), new(2, -50));
        Add(chart, bars, "Other", new ChartPoint(1, 700));
        chart.Series[0].WithStackGroup("work").WithNormalization(100);
        chart.Series[1].WithStackGroup("work").WithNormalization(100);
        chart.Series[2].WithStackGroup("other").WithNormalization(100);
        chart.WithYAxisBounds(-100, 100); chart.Options.ShowStackTotals = true;
        var scene = Compile(chart); var marks = Marks(scene);
        var first = marks[0]; var second = marks[2];
        if (bars) { Assert.Equal(first.Inner, second.Inner); Assert.Equal(first.Outer, second.Outer); Assert.Equal(first.Start + first.Sweep, second.Start, 10); }
        else { Assert.Equal(first.Start, second.Start); Assert.Equal(first.Sweep, second.Sweep); Assert.Equal(first.Outer, second.Inner, 10); }
        var facts = Point(scene, 1, 0);
        Assert.Equal("300", facts["data-cfx-y"]); Assert.Equal("60", facts["data-cfx-rendered-y"]);
        Assert.Equal("40", facts["data-cfx-base"]); Assert.Equal("100", facts["data-cfx-stack-end"]);
        Assert.Equal("500", facts["data-cfx-stack-source-total"]); Assert.Equal("work", facts["data-cfx-stack-group"]);
        Assert.Equal("-75", Point(scene, 1, 1)["data-cfx-base"]);
        Assert.Equal("-100", Point(scene, 1, 1)["data-cfx-stack-end"]);
        Assert.Equal(3, scene.Nodes.OfType<VisualSceneGroup>().Count(group => group.Role == "stack-total-source"));
        Assert.Equal(scene.Nodes.Where(node => node.Id != null).Select(node => node.Id).Distinct().Count(), scene.Nodes.Count(node => node.Id != null));
        chart.Series[1].NormalizedTo = 50;
        Assert.Throws<InvalidOperationException>(() => Compile(chart));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FixedNonzeroDomainsClipBaselineAndReversePositionsWithoutChangingSourceFacts(bool bars) {
        var chart = Add(Chart.Create(), bars, "Requests", new(1, 500), new(2, 1000));
        chart.WithYAxisBounds(100, 1100).WithRadialGeometry(new(0, 180, .2, 0, 0));
        var normal = Marks(Compile(chart)); var first = normal[0];
        if (bars) Assert.Equal(.4 * Math.PI, first.Sweep, 10);
        else Assert.Equal(4d / 9, (first.Outer - first.Inner) / (normal[1].Outer - normal[1].Inner), 10);
        chart.Options.YAxis.WithReversal(); chart.Options.XAxis.WithReversal();
        var scene = Compile(chart); var reversed = Marks(scene)[0];
        Assert.Equal(first.Sweep, reversed.Sweep, 10); Assert.Equal(first.Outer - first.Inner, reversed.Outer - reversed.Inner, 10);
        if (bars) { Assert.True(reversed.Start > first.Start); Assert.True(reversed.Inner > first.Inner); }
        else { Assert.True(reversed.Start > first.Start); Assert.True(reversed.Inner > first.Inner); }
        Assert.Equal("500", Point(scene, 0, 0)["data-cfx-y"]); Assert.Equal("true", Point(scene, 0, 0)["data-cfx-clipped"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingCategoriesAndZeroObservationsRemainDistinctFromPaintedValues(bool bars) {
        var chart = Add(Chart.Create(), bars, "Observed", new(1, 0), new(3, 250));
        Add(chart, bars, "Sparse", new ChartPoint(2, 100));
        var scene = Compile(chart);
        Assert.Equal(2, Marks(scene).Length); Assert.Equal(3, scene.Regions.Count(region => region.Role == "point"));
        Assert.Equal("0", Point(scene, 0, 0)["data-cfx-y"]);
        Assert.DoesNotContain(scene.Nodes.OfType<VisualSceneGroup>(), group => group.Id == "series-1-point-1");
        var zeros = Add(Chart.Create(), bars, "Empty contribution", new ChartPoint(1, 0));
        zeros.Series[0].WithStackGroup("zero").WithNormalization(100);
        var zeroScene = Compile(zeros);
        Assert.Empty(Marks(zeroScene)); Assert.Contains(zeroScene.Diagnostics, item => item.Code == "numeric-radial.stack-zero-total");
        Assert.Equal("0", Point(zeroScene, 0, 0)["data-cfx-stack-end"]);
        var empty = Add(Chart.Create(), bars, "Missing", Array.Empty<ChartPoint>());
        Assert.Contains(Compile(empty).Diagnostics, item => item.Code == "numeric-radial.no-data");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativePixelsMatchSvgPaintAndPreparedObservationsRemainDetached(bool bars) {
        var color = ChartColor.FromHex("#2468AC");
        var chart = Add(Chart.Create(), bars, "Authored", new(1, 600), new(2, 900));
        chart.Series[0].StateRole = ChartSeriesState.Danger; chart.Series[0].WithPointColor(0, color).WithInteractionKey("requests");
        chart.Options.ShowAxes = false; chart.Options.ShowGrid = false;
        var scene = Compile(chart); var mark = Marks(scene)[0]; var image = VisualSceneRasterRenderer.Render(scene);
        var angle = mark.Start + mark.Sweep / 2; var radius = (mark.Inner + mark.Outer) / 2;
        var x = (int)Math.Round(mark.Cx + Math.Cos(angle) * radius); var y = (int)Math.Round(mark.Cy + Math.Sin(angle) * radius);
        var offset = (y * image.Width + x) * 4;
        Assert.Equal(color.R, image.Pixels[offset]); Assert.Equal(color.G, image.Pixels[offset + 1]); Assert.Equal(color.B, image.Pixels[offset + 2]);
        var colors = new VisualRenderContext().Theme.Resolve(VisualThemeMode.Light);
        var danger = ChartSeriesColours.State(ChartSeriesState.Danger, colors, color);
        var variables = new SvgColorVariables().Add("--point", color, SvgColorRole.Series).Add("--danger", danger, SvgColorRole.Status);
        var svg = VisualSceneSvgRenderer.Render(scene, options: new VisualSvgOptions(colorVariables: variables));
        var doc = XDocument.Parse(svg); var fills = doc.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == (bars ? "radial-bar" : "radial-column")).Select(element => (string)element.Attribute("fill")!).ToArray();
        Assert.Contains("var(--point,", fills[0]); Assert.Contains("var(--danger,", fills[1]);
        Assert.Equal("requests", Point(scene, 0, 0)["data-cfx-series-key"]);
        chart.Series[0].Points.Clear(); chart.Options.YAxis.WithReversal();
        Assert.Equal(svg, VisualSceneSvgRenderer.Render(scene, options: new VisualSvgOptions(colorVariables: variables)));
        Assert.Equal(image.Pixels, VisualSceneRasterRenderer.Render(scene).Pixels);
    }

    [Fact]
    public void StackAxesAndTickFormattersRemainIndependentWhileSourceLabelsRetainUnits() {
        var chart = Chart.Create().AddRadialBar("First", new[] { new ChartPoint(1, 200) })
            .AddRadialBar("Second", new[] { new ChartPoint(1, 300) }).AddRadialBar("Independent", new[] { new ChartPoint(1, 250) });
        chart.Series[0].WithStackGroup("work").WithNormalization(100);
        chart.Series[1].WithStackGroup("work").WithNormalization(100);
        chart.Series[2].UseSecondaryYAxis().WithStackGroup("work").WithNormalization(10);
        chart.WithYAxisBounds(0, 100); chart.Options.SecondaryYAxis.WithBounds(0, 10);
        var calls = new Dictionary<double, int>();
        chart.Options.YAxis.WithLabelFormatter(value => { calls[value] = calls.TryGetValue(value, out var count) ? count + 1 : 1; return value + " percent"; });
        var scene = Compile(chart);
        Assert.Equal("100", Point(scene, 1, 0)["data-cfx-stack-end"]); Assert.Equal("10", Point(scene, 2, 0)["data-cfx-stack-end"]);
        Assert.Equal("300", Point(scene, 1, 0)["data-cfx-y"]); Assert.All(calls.Values, count => Assert.Equal(1, count));
        Assert.Contains(scene.Regions, region => region.Role == "radial-value-label" && region.Label!.EndsWith(" percent", StringComparison.Ordinal));
        chart.Options.YAxis.WithLabelFormatter(_ => throw new InvalidOperationException("Changed formatter"));
        Assert.NotEmpty(VisualSceneSvgRenderer.Render(scene)); Assert.NotEmpty(VisualSceneRasterRenderer.Render(scene).Pixels);
    }

    [Fact]
    public void GeometryAndValueScaleConstraintsRejectUnsupportedConfigurations() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartRadialGeometryOptions(0, 361));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartRadialGeometryOptions(90, 90));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartRadialGeometryOptions(innerRadiusRatio: 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartRadialGeometryOptions(categorySpacing: double.NaN));
        var options = new ChartRadialGeometryOptions(-60, 240, .3, .2, .1);
        Assert.Equal(-60, options.StartAngleDegrees); Assert.Equal(.3, options.InnerRadiusRatio);
        var chart = Chart.Create().AddRadialBar("Numeric", new[] { new ChartPoint(1, 500) }).WithRadialGeometry(options);
        chart.Options.XAxis.Minimum = 0;
        Assert.Throws<NotSupportedException>(() => Compile(chart)); chart.Options.XAxis.Minimum = null;
        chart.Options.YAxis.Scale = ChartScaleKind.Time;
        Assert.Throws<NotSupportedException>(() => Compile(chart)); chart.Options.YAxis.Scale = ChartScaleKind.Logarithmic;
        chart.Series[0].Points[0] = new ChartPoint(1, 0);
        Assert.Throws<InvalidOperationException>(() => Compile(chart));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartPoint(1, double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => Chart.Create().AddProgressRing("Percent", new[] { new ChartPoint(1, 101) }));
    }

    [Theory]
    [InlineData(ChartScaleKind.Logarithmic, 20, 800)]
    [InlineData(ChartScaleKind.SymmetricLogarithmic, -250, 800)]
    public void FixedNonlinearDomainsKeepEveryValueTickWithinItsDeclaredBounds(ChartScaleKind kind, double minimum, double maximum) {
        var chart = Chart.Create().AddRadialColumn("Observed", new[] { new ChartPoint(1, 100), new ChartPoint(2, 700) }).WithYAxisBounds(minimum, maximum);
        chart.Options.YAxis.WithScale(kind).WithReversal();
        var scale = RadialValueScale.Create(chart.Options.YAxis, new[] { 100d, 700d }, "Numeric radial", true);
        Assert.NotEmpty(scale.Ticks);
        Assert.All(scale.Ticks, value => Assert.InRange(value, minimum, maximum));
        Assert.Equal(minimum, scale.Minimum); Assert.Equal(maximum, scale.Maximum);
        var scene = Compile(chart); var axis = scene.Nodes.OfType<VisualSceneGroup>().Single(group => group.Role == "radial-value-axis");
        Assert.Equal("true", axis.Metadata["data-cfx-reversed"]);
        Assert.Equal(maximum.ToString(System.Globalization.CultureInfo.InvariantCulture), axis.Metadata["data-cfx-max"]);
    }

    internal static Chart Add(Chart chart, bool bars, string name, params ChartPoint[] points) => bars ? chart.AddRadialBar(name, points) : chart.AddRadialColumn(name, points);
    internal static VisualScene Compile(Chart chart) {
        var context = new VisualRenderContext(); var builder = new VisualSceneBuilder(new VisualSize(600, 440), context.Font);
        VisualNumericRadialCompiler.Build(chart, context, builder, new ChartRect(0, 0, 600, 440)); return builder.Build();
    }
    internal static VisualSceneSlice[] Marks(VisualScene scene) => scene.Nodes.OfType<VisualSceneSlice>().Where(mark => mark.Role is "radial-bar" or "radial-column").ToArray();
    private static IReadOnlyDictionary<string, string> Point(VisualScene scene, int series, int point) => scene.Nodes.OfType<VisualSceneGroup>().Single(group => group.Id == $"series-{series}-point-{point}").Metadata;
}
