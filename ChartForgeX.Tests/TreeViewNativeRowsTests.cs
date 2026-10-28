using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TreeViewNativeRowsTests {
    [Theory]
    [InlineData(false, 390)]
    [InlineData(true, 390)]
    [InlineData(false, 1100)]
    [InlineData(true, 1100)]
    public void NativeRowsPreservePreorderAndParentConnectorsAndPaintFileMeaning(bool dark, int width) {
        var block = Sample().WithTheme(VisualTheme.Graphite().WithTypography(new VisualTypography(BrowserProofFonts.Family)), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithTitle("Build inputs").WithNaturalSize(width, 320);
        var svg = block.ToSvg(); var png = block.ToPng(); var xml = XDocument.Parse(svg);
        Save("treeview-core", dark, width, svg, png);
        var rows = Roles(xml, "treeview-row");
        Assert.Equal(new[] { "root", "src", "file", "empty", "readme" }, rows.Select(item => (string?)item.Attribute("data-item-id")));
        Assert.Equal(new[] { "directory", "directory", "file", "directory", "file" }, rows.Select(item => (string?)item.Attribute("data-kind")));
        Assert.Equal(new[] { "0", "1", "2", "1", "1" }, rows.Select(item => (string?)item.Attribute("data-depth")));
        Assert.Single(Roles(xml, "treeview-highlight"));
        Assert.Equal(5, Roles(xml, "treeview-icon").Length);
        Assert.Equal(6, Roles(xml, "treeview-connector").Length); // two parent trunks and four child branches
        Assert.All(Roles(xml, "treeview-description"), item => Assert.Equal("italic", (string?)item.Attribute("font-style")));
        Assert.Equal("DeliveryNotes.md: Release history and migration instructions", rows[4].Elements().First(item => item.Name.LocalName == "title").Value);
        var baselines = Roles(xml, "treeview-label").Select(item => Number(item, "y")).ToArray();
        Assert.All(baselines.Zip(baselines.Skip(1)), pair => Assert.True(pair.Second > pair.First));
        var image = RasterImageDecoder.Decode(png); Assert.Equal(width, image.Width); Assert.Equal(block.Options.Size.Height, image.Height);
        Assert.Equal(svg, block.ToSvg()); Assert.Equal(png, block.ToPng());
    }

    [Fact]
    public void NaturalHeightKeepsReadableRowsAndRejectsAnUndersizedExplicitFrame() {
        var block = TreeViewBlock.Create().WithTitle("Source inventory");
        for (var i = 0; i < 40; i++) block.AddItem("row-" + i, "Document " + i);
        Assert.Throws<InvalidOperationException>(() => block.WithSize(390, 100).ToSvg());
        var measured = block.MeasureNaturalSize(390);
        block.WithNaturalSize(390); Assert.Equal(measured.Height, block.Options.Size.Height);
        var xml = XDocument.Parse(block.ToSvg()); Assert.Equal(40, Roles(xml, "treeview-row").Length);
        Assert.All(Roles(xml, "treeview-label"), label => Assert.True(Number(label, "font-size") >= 12));
        Assert.Throws<ArgumentOutOfRangeException>(() => block.WithNaturalSize(8193));
        Assert.Throws<ArgumentOutOfRangeException>(() => block.WithNaturalSize(390, 8193));
        block.RowPadding = 64;
        for (var i = 40; i < 80; i++) block.AddItem("row-" + i, "Document " + i);
        Assert.Throws<InvalidOperationException>(() => block.WithNaturalSize(390));
    }

    [Fact]
    public void ParentOrderingAndFileMembershipRejectBeforeRenderingRows() {
        Assert.Throws<InvalidOperationException>(() => TreeViewBlock.Create().AddItem("same", "One").AddItem("same", "Two").MeasureNaturalSize());
        Assert.Throws<InvalidOperationException>(() => TreeViewBlock.Create().AddItem("child", "Child", "missing").MeasureNaturalSize());
        var reopened = TreeViewBlock.Create().AddItem("a", "A").AddItem("b", "B").AddItem("child", "Child", "a");
        Assert.Throws<InvalidOperationException>(() => reopened.MeasureNaturalSize());
        var file = TreeViewBlock.Create().AddItem(new TreeViewItem("file", "File", kind: TreeViewItemKind.File)).AddItem("child", "Child", "file");
        Assert.Throws<InvalidOperationException>(() => file.MeasureNaturalSize());
        var deep = TreeViewBlock.Create();
        for (var i = 0; i < 129; i++) deep.AddItem("d" + i, "D", i == 0 ? null : "d" + (i - 1));
        Assert.Throws<InvalidOperationException>(() => deep.MeasureNaturalSize(8192));
        var full = TreeViewBlock.Create(); for (var i = 0; i < 10000; i++) full.AddItem("r" + i, "Row");
        Assert.Throws<InvalidOperationException>(() => full.AddItem("extra", "Row")); Assert.Equal(10000, full.Items.Count);
    }

    [Fact]
    public void ItemMetadataIsCopiedAndLongCaptionsKeepTheirFullSemanticText() {
        var names = new List<string> { "custom" }; var label = "Document " + new string('W', 160);
        var item = new TreeViewItem("long", label, description: "Source description", classes: names, icon: TreeViewIcon.Unknown, iconReference: "pack:unknown");
        names.Clear(); Assert.Equal(new[] { "custom" }, item.Classes);
        var block = TreeViewBlock.Create().AddItem(item).WithNaturalSize(220);
        var xml = XDocument.Parse(block.ToSvg()); var row = Assert.Single(Roles(xml, "treeview-row"));
        Assert.Equal(label + ": Source description", row.Elements().First(value => value.Name.LocalName == "title").Value);
        Assert.EndsWith("...", Assert.Single(Roles(xml, "treeview-label")).Value);
        Assert.Equal("custom", (string?)row.Attribute("data-source-classes"));
        Assert.Equal("pack:unknown", (string?)row.Attribute("data-icon-reference"));
        Assert.Single(Roles(xml, "treeview-unknown-icon"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TreeViewItem("id", "Label", icon: (TreeViewIcon)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => block.RowIndent = double.NaN);
        Assert.Throws<ArgumentException>(() => new TreeViewItem(new string('x', 4097), "Label"));
    }

    private static TreeViewBlock Sample() {
        var block = TreeViewBlock.Create(); block.ShowIcons = true;
        return block.AddItem("root", "package/").AddItem("src", "Source/", "root")
            .AddItem(new TreeViewItem("file", "ChartRenderer.cs", "src", "Native render entry", highlighted: true))
            .AddItem(new TreeViewItem("empty", "fixtures/", "root", kind: TreeViewItemKind.Directory))
            .AddItem("readme", "DeliveryNotes.md", "root", "Release history and migration instructions");
    }
    private static XElement[] Roles(XDocument xml, string role) => xml.Descendants().Where(item => (string?)item.Attribute("data-cfx-role") == role).ToArray();
    private static double Number(XElement item, string name) => double.Parse(item.Attribute(name)!.Value, CultureInfo.InvariantCulture);
    private static void Save(string name, bool dark, int width, string svg, byte[] png) {
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output); var path = Path.Combine(output, name + "-" + width + "-" + (dark ? "dark" : "light"));
        File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".native.png", png);
    }
}
