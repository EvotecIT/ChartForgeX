using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyTableContentTests {
    [Theory]
    [InlineData(390, false)] [InlineData(390, true)] [InlineData(1000, false)] [InlineData(1000, true)]
    public void ActualTableCellsHeadingsFooterNoteAndGraphIdentityShareOneNativeNodeFootprint(int width, bool dark) {
        var chart = Source(width, dark); var original = chart.Nodes.Single(node => node.Id == "Details").TableContent!;
        original.Title = "Captured release"; original.Subtitle = "Complete source rows";
        var node = chart.Nodes.Single(node => node.Id == "Details"); node.Subtitle = "Snapshot"; node.Badge = "Retained";
        var prepared = chart.Prepare(); var envelope = prepared.ToInterchangeEnvelope();
        var portable = envelope.Nodes.Single(item => item.Id == "Details"); var table = portable.Topology!.TableContent!;
        Assert.Equal(9, table.Rows.Count); Assert.Equal(original.Rows.Select(row => row.Key), table.Rows.Select(row => row.Key));
        Assert.Equal(2, table.Columns.Count); Assert.Equal(2, envelope.Edges.Count); Assert.Single(envelope.Annotations);
        Assert.Equal("Details", Assert.Single(envelope.Annotations[0].TargetIds)); Assert.Equal(2, envelope.Version);
        var scene = prepared.Visual.Scene; var texts = scene.Nodes.OfType<VisualSceneText>().SelectMany(text => text.Text.Lines.Select(line => line.Text)).ToArray();
        foreach (var text in new[] { "Captured release", "Complete source rows", "Property", "Value", "Aster", "false", "null", "1.00", "signed", "kept", "[]", "Snapshot", "Retained" }) Assert.Contains(text, texts);
        var cells = scene.Regions.Where(region => region.Role == "table-cell").ToArray(); Assert.Equal(18, cells.Length);
        Assert.Equal(cells.Length, cells.Select(region => region.Id).Distinct().Count());
        var bounds = new ChartRect(portable.X!.Value, portable.Y!.Value, portable.Width!.Value, portable.Height!.Value);
        Assert.All(cells, cell => { Assert.True(cell.Bounds.Left >= bounds.Left); Assert.True(cell.Bounds.Right <= bounds.Right); Assert.True(cell.Bounds.Top >= bounds.Top); Assert.True(cell.Bounds.Bottom <= bounds.Bottom); });
        // Keep this pre-assert capture so exact source body/route facts are observable if a compact endpoint assertion fails.
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } capture) {
            Directory.CreateDirectory(capture); var stem = Path.Combine(capture, "topology-table-endpoint-before-" + width + (dark ? "-dark" : "-light"));
            File.WriteAllText(stem + ".json", envelope.ToJson()); File.WriteAllText(stem + ".svg", prepared.ToSvg()); File.WriteAllBytes(stem + ".native.png", prepared.ToPng());
        }
        Assert.All(envelope.Edges, edge => Assert.DoesNotContain(edge.ResolvedRoute, point => point.X > bounds.Left && point.X < bounds.Right && point.Y > bounds.Top && point.Y < bounds.Bottom));
        var svg = prepared.ToSvg(); var png = prepared.ToPng(); var xml = XDocument.Parse(svg);
        Assert.DoesNotContain(xml.Descendants(), element => element.Name.LocalName is "script" or "image" or "foreignObject");
        TopologyInterchangeRestorationTests.Save("topology-table-" + width + (dark ? "-dark" : "-light"), svg, png);
        original.Rows[0].Cells[1].DisplayText = "changed"; original.Metadata["local"] = "changed"; node.TableContent = null;
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng()); Assert.Equal("1.00", table.Rows[0].Cells[1].DisplayText);
    }

    [Fact]
    public void PreparedScalePreservesTheMinimumHeaderLawAcrossSaveLoadSecondPrepareAndFreshViewport() {
        var chart = Source(390, false); chart.Viewport.Height = 360; var prepared = chart.Prepare(); var envelope = prepared.ToInterchangeEnvelope();
        var portable = envelope.Nodes.Single(node => node.Id == "Details").Topology!.TableContent!;
        Assert.InRange(portable.PresentationScale, double.Epsilon, 1d); Assert.NotEqual(1d, portable.PresentationScale);
        // Header max(1, bodyScale) is evaluated before viewport scale; scaling only base font size would enlarge the restored header.
        var first = prepared.Visual.Scene.Nodes.OfType<VisualSceneRectangle>().Single(rect => rect.Role == "table-header").Bounds.Height;
        var loaded = VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).ToTopologyChart();
        var restored = loaded.Prepare(); var second = restored.Visual.Scene.Nodes.OfType<VisualSceneRectangle>().Single(rect => rect.Role == "table-header").Bounds.Height;
        Assert.Equal(first, second, 8); Assert.Equal(portable.PresentationScale, restored.ToInterchangeEnvelope().Nodes.Single(node => node.Id == "Details").Topology!.TableContent!.PresentationScale, 8);
        Assert.Equal(18, restored.Visual.Scene.Regions.Count(region => region.Role == "table-cell"));
        loaded.LayoutMode = TopologyLayoutMode.Layered; loaded.Viewport.Width = 1000; loaded.Viewport.Height = 1000;
        var resized = loaded.Prepare(); Assert.Equal(18, resized.Visual.Scene.Regions.Count(region => region.Role == "table-cell"));
        Assert.Equal("1.00", loaded.Nodes.Single(node => node.Id == "Details").TableContent!.Rows[0].Cells[1].DisplayText);
    }

    [Fact]
    public void PortableTableIsDetachedEditableAndRetainsColumnPoliciesRawFactsAndEmptyRefillIdentity() {
        var chart = Source(1000, false); var table = chart.Nodes[2].TableContent!;
        table.Capabilities = TableArtifactCapabilities.Copy | TableArtifactCapabilities.Sort; table.Columns[0].Copyable = false;
        table.Rows[0].Cells[1].CopyText = "version = 1.00"; table.Rows[0].Metadata["raw"] = "opaque";
        var envelope = chart.ToVisualArtifact().ToInterchangeEnvelope(); var json = envelope.ToJson(); var imported = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();
        var loaded = imported.Nodes.Single(node => node.Id == "Details").TableContent!;
        Assert.NotSame(table, loaded); Assert.NotSame(table.Rows[0].Cells[0], loaded.Rows[0].Cells[0]);
        Assert.Equal(table.Capabilities, loaded.Capabilities); Assert.False(loaded.Columns[0].Copyable); Assert.Equal("version = 1.00", loaded.Rows[0].Cells[1].CopyText);
        loaded.Rows[0].Cells[1].DisplayText = "2.00"; Assert.Contains("2.00", imported.ToSvg()); Assert.Equal(json, envelope.ToJson());
        var node = imported.Nodes.Single(node => node.Id == "Details"); node.FitLabelToContent = true;
        node.TableContent = TableArtifact.Create(loaded.Id).AddColumn("property", "Property").AddColumn("value", "Value");
        Assert.Empty(node.TableContent.Rows); Assert.DoesNotContain(imported.Prepare().Visual.Scene.Regions, region => region.Role == "table-cell");
        node.TableContent.AddRow("/version", "version", "3.00"); var refilled = imported.Prepare();
        Assert.Equal(2, refilled.Visual.Scene.Regions.Count(region => region.Role == "table-cell")); Assert.Contains("3.00", refilled.ToSvg());
        Assert.Equal("/version", refilled.ToInterchangeEnvelope().Nodes.Single(item => item.Id == "Details").Topology!.TableContent!.Rows[0].Key);
    }

    [Theory]
    [InlineData("actor")] [InlineData("details")] [InlineData("virtual")] [InlineData("width")]
    public void UnsupportedCombinationsStopAtTheNativeAndPortableBoundaries(string kind) {
        var chart = Source(1000, false); var node = chart.Nodes[2];
        if (kind == "actor") node.Shape = TopologyNodeShape.Actor;
        if (kind == "details") node.Details.Add(new TopologyNodeDetail { Text = "contradictory" });
        if (kind == "virtual") node.TableContent!.TotalRowCount = 100;
        if (kind == "width") { node.FitLabelToContent = false; node.Width = 60; node.TableContent!.Columns[0].Width = 500; }
        if (kind == "width") Assert.Throws<InvalidOperationException>(() => chart.Prepare());
        else if (kind == "virtual") {
            var unsupported = Assert.Throws<NotSupportedException>(() => chart.Prepare());
            Assert.Contains("every declared row", unsupported.Message); Assert.Equal(100, node.TableContent!.TotalRowCount);
        } else Assert.Throws<TopologyValidationException>(() => chart.Prepare());
        var good = Source(1000, false).ToVisualArtifact().ToInterchangeEnvelope(); var facts = good.Nodes.Single(item => item.Id == "Details").Topology!;
        facts.TableContent!.PresentationScale = double.NaN; Assert.ThrowsAny<ArgumentException>(() => good.Validate());
    }

    [Fact]
    public void NullTableKeepsDefaultVersionOneAndDowngradedTablePayloadCannotBeSilentlyDiscarded() {
        var chart = Source(1000, false); chart.Theme=VisualTheme.Graphite(); chart.Nodes[2].TableContent = null;
        chart.LayoutMode = TopologyLayoutMode.Manual;
        chart.Title = null; chart.Subtitle = null;
        chart.Viewport.Padding = new TopologyViewport().Padding;
        chart.WithRenderOptions(new TopologyRenderOptions { IncludeLegend = false, FitContentToViewport = false });
        var user = chart.Nodes.Single(node => node.Id == "User"); user.X = 80; user.Y = 130;
        var publish = chart.Nodes.Single(node => node.Id == "Publish"); publish.X = 360; publish.Y = 140;
        var details = chart.Nodes.Single(node => node.Id == "Details"); details.X = 650; details.Y = 110;
        var before = chart.Prepare();
        if (Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY") is { Length: > 0 } capture) {
            Directory.CreateDirectory(capture); var stem = Path.Combine(capture, "table-null-genuine-unit-default-v1");
            File.WriteAllText(stem + ".svg", before.ToSvg()); File.WriteAllBytes(stem + ".native.png", before.ToPng()); File.WriteAllText(stem + ".json", before.ToInterchangeEnvelope().ToJson());
        }
        var ordinary = before.ToInterchangeEnvelope();
        Assert.All(ordinary.Nodes, node => { Assert.Null(node.Topology!.TableContent); Assert.Null(node.Topology.PreparedNodeBasis); Assert.Null(node.Topology.SurfaceElementAppearance); });
        Assert.Equal(1, ordinary.Version);
        chart.Nodes[2].TableContent = null; var after = chart.Prepare(); Assert.Equal(before.ToSvg(), after.ToSvg()); Assert.Equal(before.ToPng(), after.ToPng());
        chart.Nodes[2].TableContent = TopologyTableContentRecipe.CreateTable();
        var tableEnvelope = chart.Prepare().ToInterchangeEnvelope(); var json = tableEnvelope.ToJson();
        Assert.NotNull(tableEnvelope.Nodes.Single(node => node.Id == "Details").Topology!.TableContent); Assert.Equal(2, tableEnvelope.Version);
        var withoutTable = VisualArtifactInterchangeEnvelope.FromJson(json);
        withoutTable.Nodes.Single(node => node.Id == "Details").Topology!.TableContent = null;
        Assert.Equal(1, withoutTable.Version); withoutTable.Validate();
        Assert.Throws<NotSupportedException>(() => VisualArtifactInterchangeEnvelope.FromJson(json.Replace("\"version\":2", "\"version\":1", StringComparison.Ordinal)));
    }
    private static TopologyChart Source(int width, bool dark) => TopologyTableContentRecipe.Create(width == 390 ? "compact" : "wide", dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
        .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
}
