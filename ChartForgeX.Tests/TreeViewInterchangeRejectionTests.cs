using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TreeViewInterchangeRejectionTests {
    [Theory]
    [InlineData("duplicate")]
    [InlineData("self")]
    [InlineData("cycle")]
    [InlineData("forward")]
    [InlineData("missing")]
    [InlineData("reopened")]
    [InlineData("file-parent")]
    [InlineData("null")]
    public void MalformedParentGraphsRejectBeforeAnEditableNativeBlockCanEscape(string scenario) {
        var rows = scenario switch {
            "duplicate" => Row("root") + "," + Row("root"),
            "self" => Row("root", "root"),
            "cycle" => Row("a", "b") + "," + Row("b", "a"),
            "forward" => Row("child", "root") + "," + Row("root"),
            "missing" => Row("child", "missing"),
            "reopened" => Row("a") + "," + Row("b") + "," + Row("child", "a"),
            "file-parent" => Row("root", kind: "File") + "," + Row("child", "root"),
            _ => "null"
        };
        Assert.ThrowsAny<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(Bare(rows)));
    }

    [Theory]
    [InlineData("\"width\":520", "\"width\":520.5")]
    [InlineData("\"width\":520", "\"width\":8193")]
    [InlineData("\"height\":300", "\"height\":8193")]
    [InlineData("\"height\":300", "\"height\":1")]
    [InlineData("\"height\":300", "\"height\":-1")]
    [InlineData("\"height\":300,", "")]
    [InlineData("\"rowIndent\":18", "\"rowIndent\":3")]
    [InlineData("\"rowIndent\":18", "\"rowIndent\":257")]
    [InlineData("\"rowPadding\":5", "\"rowPadding\":-1")]
    [InlineData("\"rowPadding\":5", "\"rowPadding\":65")]
    [InlineData("\"pngOutputScale\":1", "\"pngOutputScale\":5")]
    [InlineData("\"themeMode\":\"Light\"", "\"themeMode\":\"99\"")]
    [InlineData("\"kind\":\"Auto\"", "\"kind\":\"ExecuteCallback\"")]
    [InlineData("\"icon\":\"Auto\"", "\"icon\":\"FetchRemote\"")]
    [InlineData("\"rowIndent\":18", "\"rowIndent\":\"NaN\"")]
    public void InvalidNativeFramesSpacingAndKindsRejectAtTheWireBoundary(string before, string after) {
        var json = TreeViewInterchangeTests.Native().ToJson(); Assert.Contains(before, json);
        Assert.ThrowsAny<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(json.Replace(before, after, StringComparison.Ordinal)));
    }

    [Fact]
    public void TypedRootExclusivityAndGenericGraphPresentationCannotHideContradictoryTrees() {
        var generic = new VisualArtifactInterchangeEnvelope { Id = "generic", Kind = VisualArtifactKind.VisualBlock };
        var json = generic.ToJson(); Assert.DoesNotContain("\"treeView\"", json);
        Assert.Equal(json, VisualArtifactInterchangeEnvelope.FromJson(json).ToJson());
        Assert.Throws<NotSupportedException>(() => generic.ToTreeViewBlock());
        var envelope = TreeViewInterchangeTests.Native(); envelope.Family = VisualArtifactInterchangeFamily.None;
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToTreeViewBlock());
        envelope.Family = VisualArtifactInterchangeFamily.TreeView; envelope.Railroad = new VisualArtifactInterchangeRailroadArtifact();
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToJson()); envelope.Railroad = null;
        envelope.Nodes.Add(new VisualArtifactInterchangeNode { Id = "phantom", Label = "Contradictory graph" });
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToTreeViewBlock()); envelope.Nodes.Clear();
        envelope.Presentation = new VisualArtifactInterchangePresentation();
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToTreeViewBlock()); envelope.Presentation = null;
        envelope.TreeView!.Items.Clear(); Assert.ThrowsAny<ArgumentException>(() => envelope.ToTreeViewBlock());
        envelope = TreeViewInterchangeTests.Native(); envelope.TreeView!.Items.Add(null!);
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToJson());
    }

    [Fact]
    public void ExistingWireIdTextArrayAndAggregateBudgetsStillApplyToNativeTreePayloads() {
        var envelope = TreeViewInterchangeTests.Native(); var source = envelope.TreeView!.Items[1];
        envelope.TreeView.Items[1] = new TreeViewItem(new string('i', 513), "A longer native authoring ID", "root");
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToJson());
        envelope.TreeView.Items[1] = new TreeViewItem("child", new string('W', 65537), "root");
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToJson());
        envelope.TreeView.Items[1] = source;
        var json = envelope.ToJson();
        var classes = "\"treeViewClasses\":[" + string.Join(",", Enumerable.Repeat("\"class\"", 65)) + "]";
        Assert.ThrowsAny<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(json.Replace("\"treeViewClasses\":[]", classes, StringComparison.Ordinal)));
        Assert.ThrowsAny<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(Bare(string.Join(",", Enumerable.Repeat(Row("same"), 10001)))));
        var slab = string.Join(",", Enumerable.Repeat("0", 100000));
        var excessiveValues = json.Substring(0, json.Length - 1) + ",\"unknown\":{"
            + string.Join(",", Enumerable.Range(0, 6).Select(index => "\"part" + index + "\":[" + slab + "]")) + "}}";
        Assert.ThrowsAny<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(excessiveValues));
        var duplicateProperty = json.Replace("\"rowIndent\":18", "\"rowIndent\":18,\"rowIndent\":18", StringComparison.Ordinal);
        Assert.ThrowsAny<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(duplicateProperty));
        Assert.Equal(json, envelope.ToJson());
    }

    private static string Row(string id, string? parent = null, string kind = "Auto") =>
        "{\"id\":\"" + id + "\",\"label\":\"Row\",\"description\":\"\",\"kind\":\"" + kind + "\",\"icon\":\"Auto\""
        + (parent == null ? "" : ",\"parentId\":\"" + parent + "\"") + "}";
    private static string Bare(string rows) => "{\"schema\":\"chartforgex.visual-artifact\",\"version\":1,\"id\":\"tree\",\"kind\":\"VisualBlock\",\"sourceLanguage\":\"Native\",\"family\":\"TreeView\",\"treeView\":{\"treeViewItems\":[" + rows + "]}}";
}
