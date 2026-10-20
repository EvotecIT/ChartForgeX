using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SourceAnnotationTests {
    [Theory]
    [InlineData(390, VisualThemeMode.Light, false)]
    [InlineData(390, VisualThemeMode.Dark, true)]
    [InlineData(900, VisualThemeMode.Light, true)]
    [InlineData(900, VisualThemeMode.Dark, false)]
    public void SourceDrawingsFollowNativeMarksWithoutChangingDomainsAndKeepDetachedExports(int width, VisualThemeMode mode, bool reversed) {
        var chart = Fixture(); chart.Options.XAxis.Reversed = reversed;
        var context = Context(width, mode);
        var before = chart.Prepare(context);
        chart.AddNote(new ChartPoint(3, 40), "Review", id: new ChartAnnotationId("review"))
            .AddLineSegment(new ChartPoint(3, 40), new ChartPoint(8, 70), "Plan", id: new ChartAnnotationId("plan"))
            .AddParallelChannel(new ChartPoint(1, 60), new ChartPoint(8, 80), 12, "Target", id: new ChartAnnotationId("target"));
        var prepared = chart.Prepare(context);
        Assert.Equal(before.XProjection!.FullMinimum, prepared.XProjection!.FullMinimum);
        Assert.Equal(before.XProjection.FullMaximum, prepared.XProjection.FullMaximum);
        Assert.Equal(before.Scene.DataProjection!.Y!.Minimum, prepared.Scene.DataProjection!.Y!.Minimum);
        Assert.Equal(before.Scene.DataProjection.Y.Maximum, prepared.Scene.DataProjection.Y.Maximum);
        var point = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Single(node => node.Role == "marker" && Math.Abs(node.Cx - prepared.XProjection.Project(3)) < 1e-7);
        var anchor = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneEllipse>(), node => node.Role == "annotation-anchor");
        Assert.Equal(point.Cx, anchor.Cx, 7); Assert.Equal(point.Cy, anchor.Cy, 7);
        Assert.Equal(3, prepared.Scene.Annotations.Count);
        Assert.Equal(new[] { "plan", "review", "target" }, prepared.Scene.Annotations.Select(annotation => annotation.AnnotationId).OrderBy(id => id, StringComparer.Ordinal));
        Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "annotation-channel");
        Assert.Equal(3, prepared.Scene.Nodes.OfType<VisualSceneLine>().Count(node => node.Role == "annotation-source-line"));
        Assert.Contains("data-cfx-source-end-y=\"80\"", prepared.ToSvg());
        Assert.Contains("data-cfx-channel-offset=\"12\"", prepared.ToSvg());
        var svg = prepared.ToSvg(); var png = prepared.ToPng(new VisualRenderOptions(supersampling: 1));
        Assert.False(before.ToPng(new VisualRenderOptions(supersampling: 1)).SequenceEqual(png));
        chart.Annotations.Clear(); chart.Series[0].Points[1] = new ChartPoint(3, 1);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.Contains("annotation-id-", prepared.ToArtifact("drawings", VisualArtifactKind.Chart).ToSvg());
        ScatterBubbleColorRenderingTests.Capture(prepared, $"source-annotations-{width}-{mode}-{reversed}");
    }

    [Fact]
    public void SourceDrawingsClipToNativeWindowsWithoutClampingAuthoredEndpointsAndUseTheDeclaredValueAxis() {
        var chart = Fixture().AddLine("Ratio", new[] { new ChartPoint(0, .2), new ChartPoint(3, .5), new ChartPoint(10, .8) });
        chart.Series[1].UseSecondaryYAxis();
        chart.Series[1].ConfigureMarkers(options => options.Enabled = true);
        chart.Options.SecondaryYAxis.WithBounds(0, 1);
        chart.AddNote(new ChartPoint(3, .5), "Ratio review", id: new ChartAnnotationId("ratio"), yAxis: ChartAxisSide.Secondary)
            .AddLineSegment(new ChartPoint(-10, 50), new ChartPoint(20, 50), id: new ChartAnnotationId("crossing"))
            .AddParallelChannel(new ChartPoint(-10, 0), new ChartPoint(20, 600), 10, id: new ChartAnnotationId("outside"));
        var full = chart.Prepare(Context(900, VisualThemeMode.Light));
        var anchor = Assert.Single(full.Scene.Nodes.OfType<VisualSceneEllipse>(), node => node.Role == "annotation-anchor");
        Assert.Contains(full.Scene.Nodes.OfType<VisualSceneEllipse>(), node => node.Role == "marker" && Math.Abs(node.Cx - anchor.Cx) < 1e-7 && Math.Abs(node.Cy - anchor.Cy) < 1e-7);
        var window = chart.Prepare(Context(390, VisualThemeMode.Dark), new ChartXViewport(4, 7));
        Assert.DoesNotContain(window.Scene.Nodes, node => node.Role == "annotation-anchor");
        var note = window.Scene.Nodes.OfType<VisualSceneGroup>().Single(group => group.Metadata.TryGetValue("data-cfx-annotation-id", out var id) && id == "ratio");
        Assert.Equal("false", note.Metadata["data-cfx-visible"]);
        var line = window.Scene.Nodes.OfType<VisualSceneGroup>().Single(group => group.Metadata.TryGetValue("data-cfx-annotation-id", out var id) && id == "crossing");
        Assert.Equal("-10", line.Metadata["data-cfx-source-x"]); Assert.Equal("20", line.Metadata["data-cfx-source-end-x"]);
        Assert.Single(window.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "annotation-source-line");
        var outside = window.Scene.Nodes.OfType<VisualSceneGroup>().Single(group => group.Metadata.TryGetValue("data-cfx-annotation-id", out var id) && id == "outside");
        Assert.Equal("false", outside.Metadata["data-cfx-visible"]);
        Assert.DoesNotContain(window.Scene.Nodes, node => node.Role == "annotation-channel");
        Assert.NotEmpty(window.ToPng());
    }

    [Fact]
    public void InvalidGeometryAndUnsupportedCoordinateOwnersFailBeforePainting() {
        var ink = ChartColor.FromHex("#3874CB");
        Assert.Throws<ArgumentException>(() => ChartAnnotation.LineSegment(new ChartPoint(1, 2), new ChartPoint(1, 2), "", ink));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartAnnotation.ParallelChannel(new ChartPoint(1, 2), new ChartPoint(2, 3), 0, "", ink));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartAnnotation.Note(new ChartPoint(double.NaN, 1), "", ink));
        var duplicate = Fixture().AddNote(new ChartPoint(1, 2), "One", id: new ChartAnnotationId("same"));
        Assert.Throws<InvalidOperationException>(() => duplicate.AddLineSegment(new ChartPoint(1, 2), new ChartPoint(2, 3), id: new ChartAnnotationId("same")));
        var noSecondary = Fixture().AddNote(new ChartPoint(1, 2), "", yAxis: ChartAxisSide.Secondary);
        Assert.Throws<InvalidOperationException>(() => noSecondary.Prepare(Context(390, VisualThemeMode.Light)));
        var logarithmic = Fixture().AddParallelChannel(new ChartPoint(1, 2), new ChartPoint(2, 3), -3);
        logarithmic.Options.YAxis.WithBounds(1, 100);
        logarithmic.Options.YAxis.Scale = ChartScaleKind.Logarithmic;
        Assert.Throws<InvalidOperationException>(() => logarithmic.Prepare(Context(390, VisualThemeMode.Light)));
        var horizontal = Chart.Create().AddHorizontalBar("Values", new[] { new ChartPoint(1, 1), new ChartPoint(2, 2), new ChartPoint(3, 3) }).AddNote(new ChartPoint(1, 2), "");
        Assert.Throws<NotSupportedException>(() => horizontal.Prepare(Context(390, VisualThemeMode.Light)));
    }

    private static Chart Fixture() {
        var chart = Chart.Create().AddLine("Values", new[] { new ChartPoint(0, 10), new ChartPoint(3, 40), new ChartPoint(10, 70) });
        chart.Series[0].ConfigureMarkers(options => options.Enabled = true);
        chart.Options.XAxis.WithBounds(0, 10); chart.Options.YAxis.WithBounds(0, 100); return chart;
    }
    private static VisualRenderContext Context(int width, VisualThemeMode mode) => new(new VisualLayoutOptions(new VisualSize(width, 420), 14),
        VisualTheme.Graphite(), mode, new VisualFrame("Source drawings", "", showLegend: true),
        FontSpec.FromFile(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf")));
}
