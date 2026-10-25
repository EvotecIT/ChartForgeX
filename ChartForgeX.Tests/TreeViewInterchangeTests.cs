using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TreeViewInterchangeTests {
    [Theory]
    [InlineData(false, 390)]
    [InlineData(true, 390)]
    [InlineData(false, 1100)]
    [InlineData(true, 1100)]
    public void ParsedSourceSerializesImportsEditsAndRendersNativeRowsWithoutLosingSourceFacts(bool dark, int width) {
        var parsed = new MermaidParser().ParseTreeView(MermaidTreeViewNativeRowsTests.Source); Assert.Empty(parsed.Diagnostics);
        var artifact = parsed.Document!.ToVisualArtifact(new MermaidTreeViewRenderOptions {
            Width = width, Height = 320,
            Presentation = new MermaidPresentationOptions { Theme = VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light }
        });
        artifact.Metadata["host-note"] = "<tree> is inert data"; artifact.Accessibility.Name = "Portable build tree";
        artifact.Accessibility.Description = "Source and documentation"; artifact.Accessibility.Language = "en";
        var original = Assert.IsType<TreeViewBlock>(artifact.Model);
        original.Options.Accessibility.Name = artifact.Accessibility.Name;
        original.Options.Accessibility.Description = artifact.Accessibility.Description; original.Options.Accessibility.Language = artifact.Accessibility.Language;
        var json = artifact.ToInterchangeJson(); var envelope = VisualArtifactInterchangeEnvelope.FromJson(json);
        Assert.Equal(1, envelope.Version); Assert.Equal(VisualArtifactInterchangeFamily.TreeView, envelope.Family);
        Assert.Equal(VisualArtifactSourceLanguage.Mermaid, envelope.SourceLanguage);
        Assert.Equal("<tree> is inert data", envelope.Extensions["host-note"]);
        Assert.Equal(artifact.Metadata["mermaid.nodes"], envelope.Extensions["mermaid.nodes"]);
        Assert.Equal(json, envelope.ToJson()); Assert.Equal(json, VisualArtifactInterchangeEnvelope.FromUtf8Json(envelope.ToUtf8Json()).ToJson());
        var imported = envelope.ToTreeViewBlock();
        Equal(original.Items, imported.Items);
        Assert.Equal(original.Options.Theme.ToJson(), imported.Options.Theme.ToJson()); Assert.Equal(original.Options.ThemeMode, imported.Options.ThemeMode);
        Assert.Equal("Portable build tree", imported.Options.Accessibility.Name);
        Assert.Equal("Source and documentation", imported.Options.Accessibility.Description); Assert.Equal("en", imported.Options.Accessibility.Language);
        var before = original.RenderSvg("portable-tree"); var originalPng = original.ToPng();
        Assert.Equal(before, imported.RenderSvg("portable-tree")); Assert.Equal(originalPng, imported.ToPng());
        imported.AddItem("added", "Integration.cs", imported.Items[0].Id, "An editable imported row").WithNaturalSize(width, 320);
        var edited = imported.RenderSvg("portable-tree"); var editedPng = imported.ToPng();
        Save("treeview-portable", dark, width, edited, editedPng);
        Assert.NotEqual(before, edited); Assert.Contains("Integration.cs", edited);
        Assert.Equal(5, original.Items.Count); Assert.Equal(5, envelope.TreeView!.Items.Count); Assert.Equal(6, imported.Items.Count);
        Assert.Equal(before, original.RenderSvg("portable-tree")); Assert.Equal(originalPng, original.ToPng());
        var image = RasterImageDecoder.Decode(editedPng); Assert.Equal(width, image.Width); Assert.Equal(imported.Options.Size.Height, image.Height);
    }

    [Fact]
    public void EveryRowKindIconAndInertMetadataValueRetainsItsAuthoredMeaningAndPresentation() {
        var classes = new List<string> { "external", "highlight" };
        var block = TreeViewBlock.Create().WithTitle("Native rows").WithSubtitle("Portable semantics").WithPadding(27, 19, 31, 17)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), VisualThemeMode.Dark)
            .WithTransparentBackground().WithCard(false).WithPngOutputScale(2);
        block.ShowIcons = true; block.RowIndent = 25; block.RowPadding = 7;
        block.Options.Accessibility.Name = "Native tree"; block.Options.Accessibility.Description = "Complete values";
        block.Options.Accessibility.Language = "pl"; block.Options.Accessibility.IsDecorative = true;
        block.AddItem(new TreeViewItem("root", "Root/", kind: TreeViewItemKind.Directory, icon: TreeViewIcon.Folder));
        block.AddItem(new TreeViewItem("auto", "Automatic.txt", "root", "", iconReference: null));
        block.AddItem(new TreeViewItem("file", "Data<&\"😀.txt", "root", "A <complete> description", TreeViewItemKind.File, TreeViewIcon.File));
        block.AddItem(new TreeViewItem("none", "Hidden icon", "root", icon: TreeViewIcon.None, iconReference: ""));
        block.AddItem(new TreeViewItem("unknown", "External icon", "root", "Inert URL", TreeViewItemKind.File,
            TreeViewIcon.Unknown, true, "https://example.invalid/icon.svg", classes));
        classes.Clear(); block.WithNaturalSize(900, 400);
        var artifact = VisualArtifact.Create("typed-tree", VisualArtifactKind.VisualBlock, block);
        var envelope = artifact.ToInterchangeEnvelope(); block.AddItem("later", "Later.txt", "root");
        Assert.Equal(5, envelope.TreeView!.Items.Count);
        var imported = VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).ToTreeViewBlock();
        Equal(envelope.TreeView.Items, imported.Items);
        Assert.Equal(25, imported.RowIndent); Assert.Equal(7, imported.RowPadding); Assert.True(imported.ShowIcons);
        Assert.Equal((27d, 19d, 31d, 17d), Padding(imported));
        Assert.True(imported.Options.TransparentBackground); Assert.False(imported.Options.ShowCard); Assert.Equal(2, imported.Options.PngOutputScale);
        Assert.True(imported.Options.Accessibility.IsDecorative); Assert.Equal("pl", imported.Options.Accessibility.Language);
        var svg = imported.RenderSvg("native-tree"); var png = imported.ToPng(); var xml = XDocument.Parse(svg);
        Assert.Equal(5, xml.Descendants().Count(item => (string?)item.Attribute("data-cfx-role") == "treeview-row"));
        Assert.DoesNotContain(xml.Descendants(), item => item.Name.LocalName is "image" or "script" or "foreignObject");
        Assert.Equal("https://example.invalid/icon.svg", imported.Items[4].IconReference);
        var image = RasterImageDecoder.Decode(png); Assert.Equal(imported.Options.Size.Width * 2, image.Width); Assert.Equal(imported.Options.Size.Height * 2, image.Height);
    }

    [Fact]
    public void ImportedCollectionsAndFailuresDoNotMutateEarlierBlocksOrTheSourceEnvelope() {
        var envelope = Native(); var imported = envelope.ToTreeViewBlock(); var before = imported.RenderSvg("unchanged");
        var prior = envelope.TreeView!.Items.ToArray(); var size = (envelope.Width, envelope.Height);
        envelope.TreeView.Items[1] = new TreeViewItem("child", "Invalid parent", "missing");
        var invalidRow = envelope.TreeView.Items[1];
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToTreeViewBlock());
        Assert.Same(invalidRow, envelope.TreeView.Items[1]); Assert.Equal(size, (envelope.Width, envelope.Height));
        Assert.Equal(before, imported.RenderSvg("unchanged")); Assert.Equal("Child.txt", imported.Items[1].Label);
        envelope.TreeView.Items[1] = new TreeViewItem("child", "Renamed.txt", "root", "Edited immutable value");
        var edited = envelope.ToTreeViewBlock(); Assert.Equal("Renamed.txt", edited.Items[1].Label);
        envelope.TreeView.Items.Clear();
        Assert.Equal(2, imported.Items.Count); Assert.Equal(2, edited.Items.Count); Assert.Equal("Child.txt", prior[1].Label);
        Assert.Contains("Renamed.txt", edited.RenderSvg("edited")); Assert.NotEmpty(edited.ToPng());
    }

    [Fact]
    public void FlatRowsRetainNativeDepthWithoutChangingTheCommonJsonDepthLimit() {
        var block = TreeViewBlock.Create().WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family));
        for (var index = 0; index < 128; index++) block.AddItem("n" + index, "Node", index == 0 ? null : "n" + (index - 1));
        block.WithNaturalSize(3000);
        var envelope = VisualArtifact.Create("deep-tree", VisualArtifactKind.VisualBlock, block).ToInterchangeEnvelope();
        var json = envelope.ToJson(); var imported = VisualArtifactInterchangeEnvelope.FromJson(json).ToTreeViewBlock();
        Assert.Equal(128, imported.Items.Count); Equal(block.Items, imported.Items);
        envelope.TreeView!.Items.Add(new TreeViewItem("overflow", "Too deep", "n127"));
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToJson());
    }

    internal static VisualArtifactInterchangeEnvelope Native() {
        var block = TreeViewBlock.Create().WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family))
            .AddItem("root", "Root/").AddItem("child", "Child.txt", "root").WithNaturalSize(520, 300);
        return VisualArtifact.Create("tree", VisualArtifactKind.VisualBlock, block).ToInterchangeEnvelope();
    }
    internal static void Equal(IReadOnlyList<TreeViewItem> expected, IReadOnlyList<TreeViewItem> actual) {
        Assert.Equal(expected.Count, actual.Count);
        for (var index = 0; index < expected.Count; index++) {
            var a = expected[index]; var b = actual[index];
            Assert.Equal((a.Id, a.Label, a.ParentId, a.Description, a.Kind, a.Icon, a.Highlighted, a.IconReference),
                (b.Id, b.Label, b.ParentId, b.Description, b.Kind, b.Icon, b.Highlighted, b.IconReference));
            Assert.Equal(a.Classes, b.Classes);
        }
    }
    private static (double, double, double, double) Padding(TreeViewBlock block) =>
        (block.Options.Padding.Left, block.Options.Padding.Top, block.Options.Padding.Right, block.Options.Padding.Bottom);
    private static void Save(string name, bool dark, int width, string svg, byte[] png) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); var path = Path.Combine(directory, name + "-" + width + "-" + (dark ? "dark" : "light"));
        File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".png", png);
    }
}
