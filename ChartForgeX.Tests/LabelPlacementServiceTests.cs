using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class LabelPlacementServiceTests {
    private static readonly TextStyle Style = new() { Font = FontSpec.FromFamily("Arial, sans-serif"), FontSize = 12, LineHeight = 1 };

    [Fact]
    public void HigherPriorityWinsEvenWhenItArrivesLaterAndTiesKeepInputOrder() {
        var service = new LabelPlacementService();
        var first = Request("First", 0); var second = Request("Second", 10);
        var results = service.Place(new[] { first, second }, new ChartRect(0, 0, 200, 80));
        Assert.True(results[0].IsDropped); Assert.False(results[1].IsDropped);
        results = service.Place(new[] { Request("First", 5), Request("Second", 5) }, new ChartRect(0, 0, 200, 80));
        Assert.False(results[0].IsDropped); Assert.True(results[1].IsDropped);
    }

    [Fact]
    public void CandidateOrderAndLeaderAreDeterministic() {
        var service = new LabelPlacementService();
        var label = new LabelPlacementRequest("Label", new ChartPoint(30, 30), Style, new[] { new LabelCandidate(0, 0), new LabelCandidate(0, 26), new LabelCandidate(60, 0) }) { HasLeaderLine = true };
        var obstacles = new[] { new LabelObstacle("mark", new ChartRect(20, 20, 60, 28)) };
        var first = Assert.Single(service.Place(new[] { label }, new ChartRect(0, 0, 200, 100), obstacles));
        var second = Assert.Single(service.Place(new[] { label }, new ChartRect(0, 0, 200, 100), obstacles));
        Assert.Equal(1, first.CandidateIndex); Assert.True(first.HasLeaderLine);
        Assert.Equal(first.Bounds, second.Bounds); Assert.Equal(first.Text, second.Text);
        Assert.Equal(first.LeaderEnd, second.LeaderEnd);
    }

    [Fact]
    public void DisplayCasingIsAppliedOnceAndDoesNotReportEllipsis() {
        var service = new LabelPlacementService();
        var style = Style.Clone(); style.TextCase = TextCaseTransform.ToggleCase;
        var label = new LabelPlacementRequest("WWWiii", new ChartPoint(12, 12), style, new[] { new LabelCandidate(0, 0) });
        var result = Assert.Single(service.Place(new[] { label }, new ChartRect(0, 0, 200, 80)));
        Assert.Equal("wwwIII", result.Text); Assert.False(result.IsEllipsized);
        Assert.Equal(service.Measure(label.Text, style).Width, result.Bounds.Width, 6);
    }

    [Fact]
    public void EllipsisFitsTheMeasuredWidthAndRetainsOriginalText() {
        var service = new LabelPlacementService();
        var label = new LabelPlacementRequest("Long label with combining e\u0301 and \U0001f642", new ChartPoint(0, 0), Style, new[] { new LabelCandidate(0, 0) });
        var width = service.Measure("Long label…", Style).Width;
        var result = Assert.Single(service.Place(new[] { label }, new ChartRect(0, 0, width, 50), gap: 0));
        Assert.False(result.IsDropped); Assert.True(result.IsEllipsized); Assert.EndsWith("…", result.Text);
        Assert.True(result.Bounds.Width <= width + 0.0001);
        Assert.Equal(label.Text, result.Request.Text);
    }

    [Fact]
    public void MixedAxisAnglesUseTheirOwnPreparedMetricsDuringEllipsisAndPainting() {
        var style = Style.Clone(); style.TextCase = TextCaseTransform.ToggleCase;
        var builder = new VisualSceneBuilder(new VisualSize(120, 100), style.Font);
        var requests = new[] {
            new LabelPlacementRequest("WWWiii long vertical caption", new ChartPoint(0, 0), style, new[] { new LabelCandidate(0, 0) }) { RotationDegrees = 450, Bounds = new ChartRect(0, 0, 30, 50) },
            new LabelPlacementRequest("WWWiii long diagonal caption", new ChartPoint(40, 0), style, new[] { new LabelCandidate(0, 0) }) { RotationDegrees = -30, Bounds = new ChartRect(40, 0, 65, 60) }
        };
        foreach (var request in requests) request.MeasuredSize = builder.MeasureText(request.Text, request.Style);
        var placed = new LabelPlacementService().Place(requests, new ChartRect(0, 0, 120, 100), null, 0, builder.MeasureText);
        Assert.All(placed, result => { Assert.False(result.IsDropped); Assert.True(result.IsEllipsized); Assert.StartsWith("wwwIII", result.Text); });
        for (var index = 0; index < placed.Count; index++) VisualAxisText.Draw(builder, placed[index], "axis-label", "label-" + index);
        var painted = NumericRadialAxisTextTests.Footprints(builder.Build());
        for (var index = 0; index < placed.Count; index++) {
            Assert.Equal(placed[index].Bounds.Width, painted[index].Bounds.Width, 6);
            Assert.Equal(placed[index].Bounds.Height, painted[index].Bounds.Height, 6);
            Assert.Equal(placed[index].Bounds.Left, painted[index].Bounds.Left, 6);
            Assert.Equal(placed[index].Bounds.Top, painted[index].Bounds.Top, 6);
            Assert.True(LabelPlacementService.Contains(requests[index].Bounds!.Value, painted[index].Bounds));
        }
    }

    [Fact]
    public void ExhaustedCandidatesDropRatherThanOverlapping() {
        var service = new LabelPlacementService();
        var label = Request("Value: 12345", 10);
        var result = Assert.Single(service.Place(new[] { label }, new ChartRect(0, 0, 100, 60), new[] { new LabelObstacle("all", new ChartRect(0, 0, 100, 60)) }));
        Assert.True(result.IsDropped); Assert.Equal("", result.Text); Assert.Equal("Value: 12345", result.Request.Text);
    }

    [Fact]
    public void OnlyAnAssociatedMarkMayContainItsLabel() {
        var service = new LabelPlacementService();
        var label = Request("Value", 10);
        var mark = new[] { new LabelObstacle("own", new ChartRect(0, 0, 100, 60)) };
        Assert.True(Assert.Single(service.Place(new[] { label }, new ChartRect(0, 0, 100, 60), mark)).IsDropped);
        label.AssociatedMarkId = "own";
        Assert.False(Assert.Single(service.Place(new[] { label }, new ChartRect(0, 0, 100, 60), mark)).IsDropped);
        var unrelated = new[] { mark[0], new LabelObstacle("other", new ChartRect(10, 10, 80, 40)) };
        Assert.True(Assert.Single(service.Place(new[] { label }, new ChartRect(0, 0, 100, 60), unrelated)).IsDropped);
    }

    [Fact]
    public void GlyphWidthsAndNumericWeightsUseRealFontMetrics() {
        var service = new LabelPlacementService();
        var face = TypographyFontResolver.ResolveFace(Style.Font);
        if (face.Font == null) return;
        Assert.True(service.Measure("WWW", Style).Width > service.Measure("III", Style).Width * 2);
        var weighted = Style.Clone(); weighted.Font.Weight = 650;
        var expected = TextLayoutEngine.MeasureWidth("Shaped office", weighted, TypographyFontResolver.ResolveFace(weighted.Font));
        Assert.Equal(expected, service.Measure("Shaped office", weighted).Width, 6);
    }

    [Fact]
    public void FilledMarkContainmentRejectsAConcaveCutoutInsideTheLabel() {
        var shape = new LabelMarkShape(new[] { new List<ChartPoint> {
            new(0, 0), new(100, 0), new(100, 100), new(60, 100), new(60, 30), new(40, 30), new(40, 100), new(0, 100)
        } }, true, 0);
        Assert.False(shape.Contains(new ChartRect(20, 20, 60, 50)));
        Assert.True(shape.Contains(new ChartRect(5, 5, 25, 80)));
    }

    [Fact]
    public void ClippedOffPlotGeometryDoesNotBlockAnOutsideCaption() {
        const string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='200' height='100'><defs><clipPath id='plot'><rect x='50' y='50' width='100' height='40'/></clipPath></defs>"
            + "<g clip-path='url(#plot)'><rect data-cfx-role='bar' x='10' y='10' width='100' height='80'/></g>"
            + "<text data-cfx-role='chart-title' x='12' y='30' font-family='Arial' font-size='12'>Caption</text></svg>";
        var scene = ChartLabelScene.Create(svg, Style.Font);
        Assert.Contains("data-cfx-label-status=\"placed\"", scene.ToSvg());
        Assert.Equal(0, ChartLabelScene.Inspect(scene.ToSvg(), Style.Font).LabelMark);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    public void SharedPngLabelsRetainGroupOpacityAndDeviceDensity(int supersampling, int outputScale) {
        const string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='120' height='60'><g opacity='0.25'>"
            + "<text x='12' y='30' font-family='Arial' font-size='14'>Caption</text></g></svg>";
        var scene = ChartLabelScene.Create(svg, Style.Font);
        var canvas = new Raster.RgbaCanvas(120, 60, supersampling, null, outputScale);
        scene.Paint(canvas);
        var expected = SvgRaster.SvgRasterizer.ToImage(scene.ToSvg(), canvas.DeviceWidth, canvas.DeviceHeight);
        var expectedCanvas = new Raster.RgbaCanvas(120, 60, supersampling, null, outputScale);
        expectedCanvas.DrawImage(0, 0, expected.Width, expected.Height, expected.Pixels);
        Assert.Equal(expectedCanvas.ToImage().Pixels, canvas.ToImage().Pixels);
    }

    [Fact]
    public void DisplacedCaptionLeadersAreAlsoPaintedInPng() {
        const string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='200' height='140'>"
            + "<text data-cfx-role='chart-title' x='20' y='40' fill='black' font-family='Arial' font-size='14'>Fixed heading</text>"
            + "<text x='20' y='40' fill='red' font-family='Arial' font-size='14'>Moving caption</text></svg>";
        var scene = ChartLabelScene.Create(svg, Style.Font);
        var document = System.Xml.Linq.XDocument.Parse(scene.ToSvg());
        Assert.Contains(document.Descendants(), e => (string?)e.Attribute("data-cfx-role") == "label-leader");
        var withLeader = new RgbaCanvas(200, 140, 1); scene.Paint(withLeader);
        foreach (var leader in document.Descendants().Where(e => (string?)e.Attribute("data-cfx-role") == "label-leader").ToArray()) leader.Remove();
        var withoutLeader = new RgbaCanvas(200, 140, 1); ChartLabelScene.Create(document.ToString(), Style.Font).Paint(withoutLeader);
        Assert.False(withLeader.Pixels.SequenceEqual(withoutLeader.Pixels), "The leader must survive the PNG text-layer filter and compositor bounds.");
    }

    [Fact]
    public void ImportedArtworkKeepsInternalTextAndDoesNotPaintItTwice() {
        const string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='200' height='140'>"
            + "<svg data-cfx-role='topology-icon-artwork' x='70' y='40' width='80' height='60' viewBox='0 0 160 120'>"
            + "<text x='80' y='60' font-size='14'>Asset caption</text></svg></svg>";
        var scene = ChartLabelScene.Create(svg, Style.Font);
        var caption = System.Xml.Linq.XDocument.Parse(scene.ToSvg()).Descendants().Single(e => e.Name.LocalName == "text");
        Assert.Equal("Asset caption", caption.Value); Assert.Null(caption.Attribute("data-cfx-label-status"));
        var canvas = new RgbaCanvas(200, 140, 1); scene.Paint(canvas);
        Assert.All(canvas.Pixels, value => Assert.Equal(0, value));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void SharedPngLabelsHonorNestedSvgViewports(int density) {
        const string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='200' height='100'>"
            + "<svg x='40' y='20' width='120' height='60' viewBox='0 0 240 120'>"
            + "<text x='20' y='50' fill='black' font-family='Arial' font-size='24'>Nested caption</text></svg></svg>";
        var scene = ChartLabelScene.Create(svg, Style.Font);
        var canvas = new RgbaCanvas(200, 100, 1, null, density); scene.Paint(canvas);
        var expected = SvgRaster.SvgRasterizer.ToImage(scene.ToSvg(), canvas.DeviceWidth, canvas.DeviceHeight);
        Assert.Equal(expected.Pixels, canvas.Pixels);
    }

    [Fact]
    public void MovingAnAtomicCaptionKeepsItsLeaderAttachedToTheOriginalAnchor() {
        const string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='240' height='160'>"
            + "<text data-cfx-role='chart-title' x='50' y='40' font-family='Arial' font-size='14'>Fixed heading</text>"
            + "<g data-cfx-role='topology-edge-label'><path data-cfx-role='topology-edge-label-leader' d='M 20 70 L 90 44' fill='none' stroke='black'/>"
            + "<rect data-cfx-role='topology-edge-label-backplate' x='50' y='24' width='90' height='20' fill='white'/>"
            + "<text x='52' y='40' font-family='Arial' font-size='14'>Edge caption</text></g></svg>";
        var document = System.Xml.Linq.XDocument.Parse(ChartLabelScene.Create(svg, Style.Font).ToSvg());
        var group = Assert.Single(document.Descendants(), e => (string?)e.Attribute("data-cfx-role") == "topology-edge-label");
        Assert.NotNull(group.Attribute("transform"));
        var leader = Assert.Single(group.Elements(), e => (string?)e.Attribute("data-cfx-role") == "topology-edge-label-leader");
        var start = Core.ChartMapPathParser.ParseRings(leader.Attribute("d")!.Value)[0][0];
        var world = SvgRaster.SvgRasterMatrix.ParseTransform((string?)group.Attribute("transform")).Transform(start);
        Assert.Equal(20, world.X, 3); Assert.Equal(70, world.Y, 3);
    }

    private static LabelPlacementRequest Request(string text, int priority) => new(text, new ChartPoint(12, 12), Style, new[] { new LabelCandidate(0, 0) }, priority) { Fallback = LabelFallbackRule.Drop };
}
