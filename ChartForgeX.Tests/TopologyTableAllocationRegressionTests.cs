using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyTableAllocationRegressionTests {
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void AutomaticTableWidthFitsUnequalFlexibleContentAndPreservesAuthoredFixedColumns(bool fixedColumn) {
        var chart = Source(null);
        var table = TableArtifact.Create("owners");
        if (fixedColumn) table.AddColumn("id", "ID", width: 80);
        table.AddColumn("owner", "Exception handling ownership").AddColumn("count", "N");
        if (fixedColumn) table.AddRow("/release", "R7", "Release coordination", "0");
        else table.AddRow("/release", "Release coordination", "0");
        chart.Nodes[0].TableContent = table;
        chart.Nodes[0].Width = 40; chart.Nodes[0].Height = 40;
        var prepared = chart.Prepare().Visual;
        var words = prepared.Scene.Nodes.OfType<VisualSceneText>().SelectMany(text => text.Text.Lines.Select(line => line.Text)).ToArray();
        foreach (var text in new[] { "Exception handling ownership", "Release coordination", "N", "0" }) Assert.Contains(text, words);
        Assert.DoesNotContain(prepared.Scene.Diagnostics, diagnostic => diagnostic.Code == "topology.table-cell-truncated");
        var cells = prepared.Scene.Regions.Where(region => region.Role == "table-cell").OrderBy(region => region.Bounds.Left).ToArray();
        Assert.Equal(fixedColumn ? 3 : 2, cells.Length);
        // The public table policy shares available space equally; its preferred body must fit the widest flexible cell.
        Assert.Equal(cells[^1].Bounds.Width, cells[^2].Bounds.Width, 8);
        if (fixedColumn) Assert.Equal(80d, cells[0].Bounds.Width, 8);
        Assert.Equal(40d, chart.Nodes[0].Width); Assert.Equal("0", table.Rows[0].Cells[^1].DisplayText);
        Capture(prepared, "table-width-" + (fixedColumn ? "fixed-and-flex" : "unequal-flex"));
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(null)] [InlineData(8d)] [InlineData(18d)]
    public void CapturedTablePaddingSubtitleFooterAndWidthPolicyApplyOnceAcrossLoadAndFreshLayout(double? fontSize) {
        var chart = Source(fontSize); var node = chart.Nodes[0];
        node.Subtitle = "Ready"; node.Badge = "Retained"; node.ShowStatusBadge = true;
        node.ContentPadding = 18; node.MaximumTextWidth = 320;
        var raw = chart.Prepare().Visual;
        var fitted = chart.Prepare(Context(390, 420), Options());
        var saved = fitted.ToArtifact().ToInterchangeEnvelope(); var portable = saved.Nodes.Single(item => item.Id == "facts");
        var scale = portable.Topology!.TableContent!.PresentationScale;
        Assert.InRange(scale, .001, .999);
        Assert.Equal(18d, portable.Topology.ContentPadding); Assert.Equal(320d, portable.Topology.MaximumTextWidth);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart();
        // CardSubtitleMode is an authored host option, not a carried portable graph field.
        // Supply the same Chip policy when comparing the actual prepared plates.
        var loaded = restored.Prepare(CapturedOptions()).Visual; var repeated = restored.Prepare(CapturedOptions()).Visual;
        Assert.Equal(TableText(fitted), TableText(loaded)); Assert.Equal(TableText(loaded), TableText(repeated));
        Assert.Equal(Plates(fitted), Plates(loaded)); Assert.Equal(Plates(loaded), Plates(repeated));
        Assert.Equal(4, loaded.Scene.Regions.Count(region => region.Role == "table-cell"));
        Assert.Equal(18d, node.ContentPadding); Assert.Equal(320d, node.MaximumTextWidth);
        Assert.Equal(fontSize, node.TextStyle?.FontSize);
        Assert.Equal("Number", restored.Nodes.Single(item => item.Id == "facts").TableContent!.Rows[0].Cells[1].Metadata["raw.type"]);
        // Selecting an automatic layout retains captured table units, while the new viewport alone adds its output scale.
        restored.LayoutMode = TopologyLayoutMode.Layered;
        restored.Viewport.Width = 1200; restored.Viewport.Height = 1000;
        foreach (var item in restored.Nodes) { item.X = 0; item.Y = 0; }
        var fresh = restored.Prepare(CapturedOptions()).Visual;
        Assert.Equal(4, fresh.Scene.Regions.Count(region => region.Role == "table-cell"));
        Assert.Equal(TableText(fitted), TableText(fresh));
        foreach (var pose in new[] { (raw, "raw"), (fitted, "fitted"), (loaded, "loaded"), (repeated, "second"), (fresh, "fresh-layout") })
            Capture(pose.Item1, "table-spacing-" + (fontSize?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "default") + "-" + pose.Item2);
        Assert.NotEmpty(fresh.ToPng());
    }

    private static TopologyChart Source(double? fontSize) {
        var chart = TopologyChart.Create().WithViewport(1000, 700, 24).WithLegend(null)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family))
            .WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight)
            .AddAutoNode("facts", "Facts", width: 300, height: 240)
            .AddAutoNode("next", "Continue", width: 260, height: 100).AddEdge("next-step", "facts", "next");
        foreach (var item in chart.Nodes) {
            item.Kind = TopologyNodeKind.Generic; item.Shape = TopologyNodeShape.Rectangle;
            item.PreserveDisplayModeSize = true; item.ShowStatusBadge = false;
        }
        chart.Nodes[0].TextStyle = fontSize.HasValue ? new TextStyleOverride { FontSize = fontSize, FontFamily = BrowserProofFonts.Family } : null;
        chart.Nodes[0].FitLabelToContent = true;
        var table = TableArtifact.Create("readings").WithTitle("Readings").WithSubtitle("Source values")
            .AddColumn("field", "Property").AddColumn("value", "Value").AddRow("/count", "count", "0").AddRow("/active", "active", "false");
        table.Rows[0].Cells[1].Metadata["raw.type"] = "Number";
        chart.Nodes[0].TableContent = table;
        return chart;
    }
    private static TopologyRenderOptions Options() => new() { FitContentToViewport = true, CardSubtitleMode = TopologyCardSubtitleMode.Chip, IncludeLegend = false };
    private static TopologyRenderOptions CapturedOptions() => new() { CardSubtitleMode = TopologyCardSubtitleMode.Chip, IncludeLegend = false };
    private static VisualRenderContext Context(int width, int height) => new(new VisualLayoutOptions(new VisualSize(width, height), 16),
        VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family));
    private static (string Role, string Text, double Size)[] TableText(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneText>()
        .Where(text => text.Role is "visual-text" or "table-title" or "table-subtitle")
        .Select(text => (text.Role!, string.Join("\n", text.Text.Lines.Select(line => line.Text)), Math.Round(text.Text.Size, 7))).ToArray();
    private static (string Role, double Width, double Height, double Stroke)[] Plates(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneRectangle>()
        .Where(rect => rect.Role is "table-header" or "topology-node-badge-surface" or "topology-subtitle-chip")
        .Select(rect => (rect.Role!, Math.Round(rect.Bounds.Width, 7), Math.Round(rect.Bounds.Height, 7), Math.Round(rect.StrokeWidth, 7))).ToArray();
    private static void Capture(PreparedVisual prepared, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(output, name + ".native.png"), prepared.ToPng());
        File.WriteAllText(Path.Combine(output, name + ".json"), prepared.ToArtifact().ToInterchangeEnvelope().ToJson());
    }
}
