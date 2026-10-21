using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects authored source identity, mutation atomicity and unchanged native paint.</summary>
public sealed class ChartAnnotationIdentityTests {
    [Fact]
    public void AuthoredIdsUseTheExistingTrimmedOrdinalValueContract() {
        var id = new ChartAnnotationId("  threshold.warning  ");
        Assert.Equal("threshold.warning", id.Value);
        Assert.Equal(new ChartAnnotationId("threshold.warning"), id);
        Assert.NotEqual(new ChartAnnotationId("Threshold.Warning"), id);
        Assert.Throws<ArgumentException>(() => new ChartAnnotationId(" "));
        Assert.Throws<ArgumentException>(() => new ChartAnnotationId(null!));
        Assert.Throws<InvalidOperationException>(() => default(ChartAnnotationId).Value);
        Assert.Throws<InvalidOperationException>(() => new ChartAnnotation(default(ChartAnnotationId), ChartAnnotationKind.HorizontalLine,
            5, null, "Limit", ChartColor.FromHex("#FF0000"), 1));
    }

    [Fact]
    public void TypedFluentAdditionRejectsDuplicatesAndInvalidInputsBeforeMutation() {
        var chart = Create().AddHorizontalLine(new ChartAnnotationId("warning"), 5, "Limit");
        var retained = Assert.Single(chart.Annotations);
        Assert.Throws<InvalidOperationException>(() => chart.AddVerticalBand(new ChartAnnotationId(" warning "), 2, 4, "Other"));
        Assert.Throws<InvalidOperationException>(() => chart.AddVerticalLine(default(ChartAnnotationId), 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.AddHorizontalBand(new ChartAnnotationId("other"), 4, 4));
        Assert.Same(retained, Assert.Single(chart.Annotations));
        Assert.Throws<InvalidOperationException>(() => chart.AddAnnotation(new ChartAnnotation(new ChartAnnotationId("warning"), ChartAnnotationKind.VerticalLine,
            3, null, "Limit", ChartColor.FromHex("#FF0000"), 1)));
        Assert.Same(retained, Assert.Single(chart.Annotations));
    }

    [Fact]
    public void PreparedCatalogKeepsSourceIdentityAcrossReorderingAndIsDetachedFromLaterEdits() {
        var chart = Create().AddHorizontalLine(new ChartAnnotationId("threshold.warning"), 4, "Limit")
            .AddVerticalLine(new ChartAnnotationId("threshold.critical"), 7, "Limit").AddVerticalLine(2, "annotation-0");
        var prepared = chart.Prepare(new VisualRenderContext());
        var catalog = prepared.Scene.Annotations.ToDictionary(item => item.AnnotationId, item => item.SourceId);
        var svg = prepared.ToSvg();
        var png = prepared.ToPng();
        var retained = chart.Annotations[0];
        retained.StrokeStyle = new ChartStrokeStyle(width: 6);
        chart.ClearAnnotations();
        chart.AddVerticalLine(new ChartAnnotationId("threshold.critical"), 8, "Localized critical")
            .AddHorizontalLine(new ChartAnnotationId("threshold.warning"), 6, "Localized warning");
        var next = chart.Prepare(new VisualRenderContext());
        Assert.Equal(catalog.OrderBy(item => item.Key), next.Scene.Annotations.ToDictionary(item => item.AnnotationId, item => item.SourceId).OrderBy(item => item.Key));
        Assert.Equal(svg, prepared.ToSvg());
        Assert.Equal(png, prepared.ToPng());
        Assert.Equal(2, prepared.Scene.Annotations.Count);
        Assert.Contains(prepared.Regions, item => item.Role == "annotation" && item.Id == "annotation-2");
        Assert.All(prepared.Scene.Annotations, item => Assert.Contains(prepared.Regions, region => region.Id == item.SourceId && region.Role == "annotation"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AxisGuidesRetainAuthoredIdsWithoutChangingPaint(bool horizontalBars) {
        var anonymous = AllAxisGuideKinds(false, horizontalBars);
        var authored = AllAxisGuideKinds(true, horizontalBars);
        var before = anonymous.Prepare(new VisualRenderContext());
        var actual = authored.Prepare(new VisualRenderContext());
        Assert.Equal(before.ToPng(), actual.ToPng());
        Assert.Equal(4, actual.Scene.Annotations.Count);
        Assert.Equal(new[] { nameof(ChartAnnotationKind.HorizontalBand), nameof(ChartAnnotationKind.HorizontalLine),
            nameof(ChartAnnotationKind.VerticalBand), nameof(ChartAnnotationKind.VerticalLine) }, actual.Scene.Annotations.Select(item => item.Kind).Order());
        var svg = XDocument.Parse(actual.ToSvg());
        var groups = svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "annotation").ToArray();
        Assert.Equal(authored.Annotations.Select(item => item.Id!.Value.Value).Order(), groups.Select(node => (string)node.Attribute("data-cfx-annotation-id")!).Order());
        Assert.Equal(4, groups.Select(node => (string)node.Attribute("data-cfx-source-id")!).Distinct().Count());
        Assert.All(groups, node => Assert.StartsWith("annotation-id-", (string)node.Attribute("data-cfx-source-id")!));
    }

    [Fact]
    public void ScheduleVerticalAnnotationsKeepTheirOwnIdentityBesideMarkerSeries() {
        var chart = Chart.Create().AddGanttTask("Build", 1, 4, .5).AddGanttMarker("Release", 3)
            .AddVerticalLine(new ChartAnnotationId("deadline"), 2, "Limit");
        var prepared = chart.Prepare(new VisualRenderContext());
        Assert.Equal("deadline", Assert.Single(prepared.Scene.Annotations).AnnotationId);
        Assert.Contains(prepared.Regions, item => item.Role == "gantt-marker" || item.Role == "gantt-vertical-marker");
    }

    private static Chart Create() => Chart.Create().WithSize(480, 300).WithLegend(false)
        .AddLine("Readings", new[] { new ChartPoint(0, 2), new ChartPoint(10, 8) });

    private static Chart AllAxisGuideKinds(bool authored, bool horizontalBars) {
        var chart = horizontalBars ? Chart.Create().WithSize(480, 300).WithLegend(false)
            .AddHorizontalBar("Readings", new[] { new ChartPoint(0, 2), new ChartPoint(10, 8) }) : Create();
        if (authored) return chart.AddHorizontalLine(new ChartAnnotationId("annotation-0"), 3, "Limit")
            .AddVerticalLine(new ChartAnnotationId("vertical: β \"quoted\""), 4, "Limit")
            .AddHorizontalBand(new ChartAnnotationId("horizontal-band"), 5, 6, "Band")
            .AddVerticalBand(new ChartAnnotationId("vertical-band"), 7, 8, "Band");
        return chart.AddHorizontalLine(3, "Limit").AddVerticalLine(4, "Limit")
            .AddHorizontalBand(5, 6, "Band").AddVerticalBand(7, 8, "Band");
    }
}
