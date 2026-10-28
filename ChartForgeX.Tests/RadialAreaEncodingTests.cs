using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects the visible interval's area, canonical stacks/axes and common native paint.</summary>
public sealed class RadialAreaEncodingTests {
    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, .35)]
    [InlineData(true, .35)]
    public void VisibleSectorAreasKeepValueRatiosAcrossAnnularHolesAndRadiusReversal(bool reversed, double hole) {
        var chart = Create().WithRadialGeometry(Geometry(hole)).AddRadialColumn("Observed", new[] { new ChartPoint(1, 25), new ChartPoint(2, 100) });
        chart.Options.YAxis.Reversed = reversed;
        var scene = NumericRadialSeriesTests.Compile(chart); var marks = NumericRadialSeriesTests.Marks(scene);
        Assert.Equal(2, marks.Length); Assert.Equal(.25, Area(marks[0]) / Area(marks[1]), 9);
        Assert.Equal("25", Facts(scene, 0, 0)["data-cfx-y"]); Assert.Equal("area", Facts(scene, 0, 0)["data-cfx-value-encoding"]);
        Assert.Equal(marks[0].Sweep, marks[1].Sweep, 12);
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(chart.ToVisualArtifact().ToInterchangeJson());
        Assert.Equal("area", envelope.Extensions["chart.radial.valueEncoding"]);
        Capture(chart, "ratio-" + hole.ToString(CultureInfo.InvariantCulture) + "-" + reversed);
    }

    [Fact]
    public void NamedNormalizedStacksAndSecondaryNonzeroDomainsUseTheSameEndpointLaw() {
        var chart = Create().WithRadialGeometry(Geometry(.2)).AddRadialColumn("First", new[] { new ChartPoint(1, 20) })
            .AddRadialColumn("Second", new[] { new ChartPoint(1, 30) }).AddRadialColumn("Temperature", new[] { new ChartPoint(1, 75) });
        chart.Series[0].WithStackGroup("work").WithNormalization(100); chart.Series[1].WithStackGroup("work").WithNormalization(100);
        chart.Series[2].UseSecondaryValueAxis(); chart.Options.SecondaryYAxis.WithBounds(50, 150).WithReversal();
        var scene = NumericRadialSeriesTests.Compile(chart); var marks = NumericRadialSeriesTests.Marks(scene);
        Assert.Equal(2d / 3, Area(marks[0]) / Area(marks[1]), 9);
        Assert.Equal(marks[0].Outer, marks[1].Inner, 9); Assert.Equal(marks[0].Start, marks[1].Start, 12);
        var second = Facts(scene, 1, 0); Assert.Equal("30", second["data-cfx-y"]); Assert.Equal("60", second["data-cfx-rendered-y"]);
        Assert.Equal("40", second["data-cfx-base"]); Assert.Equal("100", second["data-cfx-stack-end"]);
        Assert.Equal("secondary", Facts(scene, 2, 0)["data-cfx-axis"]); Assert.Equal("75", Facts(scene, 2, 0)["data-cfx-y"]);
        Assert.Equal("true", Facts(scene, 2, 0)["data-cfx-clipped"]);
        // Inverting the observable annular interval gives the displayed part of 75 in 50..150, not a rewritten source value.
        var outer = marks.Take(2).Max(mark => mark.Outer); var inner = outer * .2;
        Assert.Equal(.25, (marks[2].Outer * marks[2].Outer - marks[2].Inner * marks[2].Inner) / (outer * outer - inner * inner), 9);
        Capture(chart, "independent-stacked-domains");
    }

    [Fact]
    public void ExplicitValueTickAndMarkEndpointCoincideUnderAreaEncoding() {
        var chart = Create().WithAxes().WithGrid().WithRadialGeometry(Geometry(.2))
            .AddRadialColumn("Observed", new[] { new ChartPoint(1, 25), new ChartPoint(2, 100) });
        chart.Options.YAxis.Labels.Add(new ChartAxisLabel(25, "Quarter"));
        var scene = NumericRadialSeriesTests.Compile(chart); var mark = NumericRadialSeriesTests.Marks(scene)[0];
        Assert.Contains(scene.Nodes.OfType<VisualSceneSlice>(), grid => grid.Role == "radial-value-grid"
            && Math.Abs((grid.Outer + grid.Inner) / 2 - mark.Outer) < 1e-8);
        Assert.Contains(scene.Regions, region => region.Role == "radial-value-label" && region.Label == "Quarter");
        Assert.All(scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "radial-value-axis"),
            group => Assert.Equal("area", group.Metadata["data-cfx-value-encoding"]));
        Capture(chart, "tick-alignment");
    }

    [Fact]
    public void NativePaintAndPreparedFactsStayDetachedFromLaterSourceAndEncodingEdits() {
        var ink = ChartColor.FromHex("#2468AC");
        var series = new ChartSeries(new ChartSeriesId("measured"), "Measured", ChartSeriesKind.RadialColumn,
            new[] { new ChartPoint(1, 25), new ChartPoint(2, 100) }).WithObservationId(0, new ChartObservationId("north")).WithPointColor(0, ink);
        var chart = Create().WithRadialGeometry(Geometry(.1)).AddSeries(series);
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(600, 440))));
        var svg = prepared.ToSvg(); var png = prepared.ToPng(); var mark = NumericRadialSeriesTests.Marks(prepared.Scene)[0];
        var image = VisualSceneRasterRenderer.Render(prepared.Scene); var angle = mark.Start + mark.Sweep / 2; var radius = (mark.Inner + mark.Outer) / 2;
        var x = (int)Math.Round(mark.Cx + Math.Cos(angle) * radius); var y = (int)Math.Round(mark.Cy + Math.Sin(angle) * radius); var offset = (y * image.Width + x) * 4;
        Assert.Equal(ink.R, image.Pixels[offset]); Assert.Equal(ink.G, image.Pixels[offset + 1]); Assert.Equal(ink.B, image.Pixels[offset + 2]);
        Assert.Equal("measured", Facts(prepared.Scene, 0, 0)["data-cfx-series-key"]);
        var source = XDocument.Parse(svg).Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "point"
            && (string?)node.Attribute("data-cfx-series-key") == "measured" && (string?)node.Attribute("data-cfx-point") == "0");
        Assert.Equal("north", (string?)source.Attribute("data-cfx-observation-id")); Assert.Equal("25", (string?)source.Attribute("data-cfx-y"));
        Assert.DoesNotContain("<script", svg, StringComparison.OrdinalIgnoreCase);
        series.Points[0] = new ChartPoint(1, 80); chart.WithRadialGeometry(new()); chart.Options.YAxis.Reversed = true;
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    internal static Chart Create() => Chart.Create().WithSize(600, 440).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false)
        .WithDataLabels(false).WithYAxisBounds(0, 100).WithXLabels("North", "South");
    internal static ChartRadialGeometryOptions Geometry(double hole = 0) => new(-90, 270, hole, 0, 0, valueEncoding: ChartRadialValueEncoding.Area);
    internal static double Area(VisualSceneSlice mark) => (mark.Outer * mark.Outer - mark.Inner * mark.Inner) * mark.Sweep / 2;
    internal static IReadOnlyDictionary<string, string> Facts(VisualScene scene, int series, int point) => scene.Nodes.OfType<VisualSceneGroup>()
        .Single(group => group.Id == "series-" + series + "-point-" + point).Metadata;
    internal static void Capture(Chart chart, string stem) {
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, "radial-area-" + stem + ".svg"), chart.ToSvg());
        File.WriteAllBytes(Path.Combine(directory, "radial-area-" + stem + ".png"), chart.ToPng());
    }
}
