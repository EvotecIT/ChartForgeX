using System.Globalization;
using System.Xml.Linq;
using ChartForgeX;
using ChartForgeX.Primitives;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class FishboneNaturalLayoutTests {
    [Theory]
    [InlineData(false, 390)] [InlineData(true, 390)]
    [InlineData(false, 1100)] [InlineData(true, 1100)]
    public void NaturalTreeKeepsEveryCaptionInDisjointMeasuredCellsAndConnectsActualParents(bool dark, int minimumWidth) {
        var block = IshikawaDepthRecipe.Create(minimumWidth == 390 ? "compact" : "wide", dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        var source = Facts(block); var size = block.MeasureNaturalSize(); var svg = block.ToSvg(); var xml = XDocument.Parse(svg); var png = block.ToPng();
        Capture(block, "fishbone-natural-" + minimumWidth + (dark ? "-dark" : "-light"));
        Assert.True(block.Options.Size.Width >= minimumWidth && block.Options.Size.Width >= size.Width && block.Options.Size.Height >= size.Height);
        var groups = xml.Descendants().Where(e => (string?)e.Attribute("data-cfx-role") == "fishbone-cause").ToArray();
        Assert.Equal(source.Select(fact => fact.Id), groups.Select(e => (string?)e.Attribute("data-cause-id")));
        var bounds = new List<ChartRect>();
        for (var index = 0; index < groups.Length; index++) {
            var group = groups[index]; Assert.Equal(source[index].Parent, (string?)group.Attribute("data-parent-id"));
            var caption = group.Elements().Single(e => (string?)e.Attribute("data-cfx-role") == "fishbone-cause-caption");
            Assert.Equal(source[index].Text, string.Join(" ", caption.Elements().Where(e => e.Name.LocalName == "text").Select(e => e.Value)));
            var rect = new ChartRect(Number(group, "data-caption-x"), Number(group, "data-caption-y"), Number(group, "data-caption-width"), Number(group, "data-caption-height"));
            Assert.True(rect.Left >= 0 && rect.Top >= 0 && rect.Right <= block.Options.Size.Width && rect.Bottom <= block.Options.Size.Height);
            foreach (var old in bounds) Assert.False(rect.Left < old.Right && rect.Right > old.Left && rect.Top < old.Bottom && rect.Bottom > old.Top, source[index].Id);
            bounds.Add(rect);
            foreach (var text in caption.Elements().Where(e => e.Name.LocalName == "text")) {
                var style = new TextStyle { Font = new FontSpec { Family = (string)text.Attribute("font-family")!, Weight = int.Parse((string)text.Attribute("font-weight")!, CultureInfo.InvariantCulture) }, FontSize = Number(text, "font-size"), LineHeight = 1 };
                Assert.Equal(VisualBlockRendering.BodyFontSize(block.Options.ResolvedTheme, index == 0 || source[index].Parent == null ? 12.5 : 11.5), style.FontSize, 3);
                var metrics = TextLayoutEngine.Measure(text.Value, style); var baseline = Number(text, "y");
                Assert.True(Number(text, "x") >= rect.Left && Number(text, "x") + metrics.Width <= rect.Right + .001);
                Assert.True(baseline - VisualBlockRendering.TextAscent(style) >= rect.Top - .001 && baseline - VisualBlockRendering.TextAscent(style) + metrics.Height <= rect.Bottom + .001);
            }
            var link = group.Elements().Single(e => (string?)e.Attribute("data-cfx-role") == "fishbone-subcause-line");
            Assert.Equal(source[index].Parent, (string?)link.Attribute("data-parent-id"));
            if (source[index].Parent != null) {
                var parent = groups.Single(e => (string?)e.Attribute("data-cause-id") == source[index].Parent);
                var rail = parent.Elements().Single(e => (string?)e.Attribute("data-cfx-role") == "fishbone-parent-rail");
                var path = Assert.Single(ChartMapPathParser.ParseSubpaths((string)link.Attribute("d")!)); var endpoint = path.Points[path.Points.Count - 1];
                Assert.Equal(Number(rail, "x1"), endpoint.X, 3);
                Assert.InRange(endpoint.Y, Math.Min(Number(rail, "y1"), Number(rail, "y2")) - .001, Math.Max(Number(rail, "y1"), Number(rail, "y2")) + .001);
            }
        }
        Assert.Equal(source, Facts(block)); Assert.DoesNotContain(xml.Descendants(), e => e.Name.LocalName is "script" or "foreignObject" or "image");
        Assert.DoesNotContain(xml.Descendants(), e => (string?)e.Attribute("data-cfx-label-status") is "dropped" or "ellipsis" || (string?)e.Attribute("display") == "none");
        var image = RasterImageDecoder.Decode(png); Assert.Equal(block.Options.Size.Width, image.Width); Assert.Equal(block.Options.Size.Height, image.Height);
    }

    [Fact]
    public void MeasureIsReadOnlyAndExplicitNaturalSelectionDoesNotMutateAuthoredCauseObjects() {
        var block = FishboneDiagramBlock.Create("Effect").WithSize(840, 460); var cause = block.AddCause("Process"); var child = cause.AddChild("Review");
        var svg = block.ToSvg(); var png = block.ToPng(); _ = block.MeasureNaturalSize();
        Assert.Equal(svg, block.ToSvg()); Assert.Equal(png, block.ToPng()); Assert.False(block.UsesNaturalLayout);
        block.WithNaturalSize(840, 460); Assert.Same(cause, block.Causes[0]); Assert.Same(child, block.Causes[0].Children[0]);
        Assert.Equal("Review", child.Label); Assert.Equal(840, block.Options.Size.Width);
    }

    [Fact]
    public void LaterSourceOrTypographyGrowthRequiresRemeasureInsteadOfCaptionLoss() {
        var block = FishboneDiagramBlock.Create("Effect"); var cause = block.AddCause("Original"); block.WithNaturalSize();
        cause.AddChild(new string('W', 120)); Assert.Throws<InvalidOperationException>(() => block.ToSvg());
        block.WithNaturalSize(); Assert.Contains(new string('W', 120), block.ToSvg(), StringComparison.Ordinal);
        block.Options.Theme = block.Options.Theme.Configure(builder => builder.Typography = new VisualTypography("Arial", dataLabelSize: 25));
        Assert.Throws<InvalidOperationException>(() => block.ToPng()); block.WithNaturalSize(); Assert.NotEmpty(block.ToPng());
    }

    [Fact]
    public void RepeatedCaptionsKeepDistinctOrderedOccurrencesAndBoundedDepth() {
        var block = FishboneDiagramBlock.Create("Same"); var first = block.AddCause("Same"); first.AddChild("Same"); block.AddCause("Same"); block.WithNaturalSize();
        var groups = XDocument.Parse(block.ToSvg()).Descendants().Where(e => (string?)e.Attribute("data-cfx-role") == "fishbone-cause");
        Assert.Equal(new[] { "cause-0", "cause-0.0", "cause-1" }, groups.Select(e => (string?)e.Attribute("data-cause-id")));
        var invalid = FishboneDiagramBlock.Create("Effect"); var cause = invalid.AddCause("0"); for (var i = 1; i <= 12; i++) cause = cause.AddChild(i.ToString(CultureInfo.InvariantCulture));
        Assert.Throws<InvalidOperationException>(() => invalid.MeasureNaturalSize());
        Assert.Throws<ArgumentOutOfRangeException>(() => block.WithNaturalSize(-1));
    }

    internal static List<(string Id, string? Parent, string Text)> Facts(FishboneDiagramBlock block) {
        var result = new List<(string, string?, string)>();
        for (var i = 0; i < block.Causes.Count; i++) Visit(block.Causes[i], "cause-" + i, null);
        return result;
        void Visit(FishboneCause cause, string id, string? parent) { result.Add((id, parent, cause.Label)); for (var i = 0; i < cause.Children.Count; i++) Visit(cause.Children[i], id + "." + i, id); }
    }
    internal static double Number(XElement element, string name) => double.Parse((string)element.Attribute(name)!, CultureInfo.InvariantCulture);
    internal static void Capture(FishboneDiagramBlock block, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, name + ".svg"), block.ToSvg()); File.WriteAllBytes(Path.Combine(output, name + ".png"), block.ToPng());
    }
}
