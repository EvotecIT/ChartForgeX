using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Prepared SVG roots can coexist in a document without sharing DOM identities.</summary>
public sealed class V2SvgEmbeddingTests {
    [Fact]
    public void DifferentFrames_UseDisjointIdsAndRootLocalReferences() {
        var first = Chart().Prepare(Context("First chart"));
        var second = Chart().Prepare(Context("Second chart"));
        var roots = new[] { XDocument.Parse(first.ToSvg()).Root!, XDocument.Parse(second.ToSvg()).Root! };
        var inline = new XElement("div", roots);
        Assert.Equal(2, inline.Elements().Count());
        AssertDisjoint(roots[0], roots[1]);
        AssertLocalReferences(roots[0]);
        AssertLocalReferences(roots[1]);
        Assert.Equal(first.ToSvg(), first.ToSvg());
        Assert.Equal(first.ToSvg(), Chart().Prepare(Context("First chart")).ToSvg());
    }

    [Fact]
    public void ExplicitScopes_AllowIdenticalCopiesWithoutChangingSemanticIds() {
        var prepared = Chart().Prepare(Context("Copies"));
        var original = prepared.ToSvg();
        var sourceIds = prepared.Regions.Select(region => region.Id).ToArray();
        var left = XDocument.Parse(prepared.ToSvg("report-left")).Root!;
        var right = XDocument.Parse(prepared.ToSvg("report-right")).Root!;
        AssertDisjoint(left, right);
        AssertLocalReferences(left);
        AssertLocalReferences(right);
        Assert.Equal(left.Descendants().Attributes("data-cfx-source-id").Select(attribute => attribute.Value),
            right.Descendants().Attributes("data-cfx-source-id").Select(attribute => attribute.Value));
        Assert.Equal(sourceIds, prepared.Regions.Select(region => region.Id));
        Assert.Equal(original, prepared.ToSvg());
        Assert.All(Ids(left), id => Assert.StartsWith("report-left-", id));
        Assert.All(Ids(right), id => Assert.StartsWith("report-right-", id));
    }

    [Fact]
    public void AccessibleAlternativeAndLanguage_AreEscapedAndAssociatedWithThisRoot() {
        var chart = Chart();
        chart.Accessibility.WithTextAlternative("CPU < 50% & stable", "A \"quoted\" description", "pl-PL");
        var root = XDocument.Parse(chart.Prepare(Context("Load")).ToSvg()).Root!;
        Assert.Equal("img", (string?)root.Attribute("role"));
        Assert.Equal("CPU < 50% & stable", (string?)root.Attribute("aria-label"));
        Assert.Equal("CPU < 50% & stable", root.Element(root.Name.Namespace + "title")!.Value);
        Assert.Equal("A \"quoted\" description", root.Element(root.Name.Namespace + "desc")!.Value);
        Assert.Equal("pl-PL", (string?)root.Attribute("lang"));
        Assert.Equal("pl-PL", (string?)root.Attribute(XNamespace.Xml + "lang"));
        AssertLocalReferences(root);
    }

    [Fact]
    public void DecorativeVisual_IsHiddenAndDoesNotAcquireAnImageRole() {
        var chart = Chart();
        chart.Accessibility.WithTextAlternative("Decorative chart", "Decoration", "en-GB").AsDecorative();
        var prepared = chart.Prepare(Context("Decoration"));
        var root = XDocument.Parse(prepared.ToSvg()).Root!;
        Assert.Equal("true", (string?)root.Attribute("aria-hidden"));
        Assert.Equal("false", (string?)root.Attribute("focusable"));
        Assert.Null(root.Attribute("role"));
        Assert.Null(root.Attribute("aria-label"));
        Assert.Equal("en-GB", (string?)root.Attribute(XNamespace.Xml + "lang"));
        Assert.True(prepared.Accessibility.IsDecorative);
        Assert.Equal("Decorative chart", prepared.Accessibility.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1-report")]
    [InlineData("report with spaces")]
    [InlineData("report#clip")]
    [InlineData("report\" onload=\"run")]
    public void ExplicitScope_RejectsInvalidFragmentIdentifiers(string prefix) {
        var prepared = Chart().Prepare(Context("Invalid namespace"));
        Assert.Throws<ArgumentException>(() => prepared.ToSvg(prefix));
    }

    [Fact]
    public void DefaultScope_ChangesWithPaintAndSourceMetadataEvenWhenGeometryIsEqual() {
        var first = Chart();
        var second = Chart();
        var third = Chart("Another source");
        second.Series[0].Color = ChartColor.FromHex("#112233");
        var roots = new[] { first, second, third }.Select(chart => XDocument.Parse(chart.Prepare(Context("Same frame")).ToSvg()).Root!).ToArray();
        AssertDisjoint(roots[0], roots[1]);
        AssertDisjoint(roots[0], roots[2]);
        AssertDisjoint(roots[1], roots[2]);
    }

    private static void AssertDisjoint(XElement first, XElement second) {
        var left = Ids(first);
        var right = Ids(second);
        Assert.NotEmpty(left);
        Assert.NotEmpty(right);
        Assert.Equal(left.Length, left.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(right.Length, right.Distinct(StringComparer.Ordinal).Count());
        Assert.Empty(left.Intersect(right, StringComparer.Ordinal));
    }

    private static void AssertLocalReferences(XElement root) {
        var ids = Ids(root).ToHashSet(StringComparer.Ordinal);
        var references = root.DescendantsAndSelf().Attributes().Where(attribute => attribute.Name.LocalName == "clip-path").ToArray();
        Assert.NotEmpty(references);
        foreach (var reference in references) {
            Assert.StartsWith("url(#", reference.Value);
            Assert.EndsWith(")", reference.Value);
            Assert.Contains(reference.Value.Substring(5, reference.Value.Length - 6), ids);
        }
        var description = (string?)root.Attribute("aria-describedby");
        if (description != null) Assert.Contains(description, ids);
    }

    private static string[] Ids(XElement root) => root.DescendantsAndSelf().Attributes("id").Select(attribute => attribute.Value).ToArray();
    private static Chart Chart(string seriesName = "Observed") => ChartForgeX.Core.Chart.Create().AddLine(seriesName, new[] { new ChartPoint(1, 3), new ChartPoint(2, 5) });
    private static VisualRenderContext Context(string title) => new(new VisualLayoutOptions(new VisualSize(360, 240)),
        VisualTheme.Graphite(), frame: new VisualFrame(title: title, showLegend: false));
}
