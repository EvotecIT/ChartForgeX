using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class StageIndependentCaptionTests {
    [Theory]
    [InlineData(false, ChartFunnelForm.StageBars, ChartOrientation.Vertical, 390, VisualThemeMode.Light)]
    [InlineData(false, ChartFunnelForm.StageBars, ChartOrientation.Horizontal, 800, VisualThemeMode.Dark)]
    [InlineData(false, ChartFunnelForm.Cone, ChartOrientation.Vertical, 800, VisualThemeMode.Light)]
    [InlineData(false, ChartFunnelForm.Cone, ChartOrientation.Horizontal, 390, VisualThemeMode.Dark)]
    [InlineData(true, ChartFunnelForm.StageBars, ChartOrientation.Vertical, 390, VisualThemeMode.Dark)]
    [InlineData(true, ChartFunnelForm.StageBars, ChartOrientation.Horizontal, 800, VisualThemeMode.Light)]
    public void SeparateCaptionsRetainIndependentStylePlacementSourceIdentityAndCompleteDescriptions(bool pyramid, ChartFunnelForm form,
        ChartOrientation orientation, int width, VisualThemeMode mode) {
        var chart = StagePresentationOptionsTests.Create(pyramid, form, orientation, width, 640).WithTheme(VisualTheme.Graphite(), mode);
        var ink = ChartColor.FromHex("#A73761");
        chart.ConfigureFunnel(options => { options.LabelContent = ChartStageLabelContent.Separate; options.RetentionLabel.Visible = false; options.DropOffLabel.Visible = false;
            options.CategoryLabel.Style.FontSize = 12; options.CategoryLabel.Style.Color = ink; options.ValueLabel.Style.FontSize = 13; });
        chart.ConfigurePyramid(options => { options.LabelContent = ChartStageLabelContent.Separate; options.CategoryLabel.Style.FontSize = 12;
            options.CategoryLabel.Style.Color = ink; options.ValueLabel.Style.FontSize = 13; options.ValueEncoding = ChartPyramidValueEncoding.Area; options.Reversed = true; });
        var prepared = StagePresentationOptionsTests.Prepare(chart); var family = pyramid ? "pyramid" : "funnel";
        var categories = prepared.Regions.Where(region => region.Role == family + "-category-label").ToArray();
        var values = prepared.Regions.Where(region => region.Role == family + "-value-label").ToArray();
        Assert.Equal(new[] { "Intake", "Review", "Complete" }, categories.Select(region => region.Label));
        Assert.Equal(new[] { "100", "75", "25" }, values.Select(region => region.Label));
        var painted = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == family + "-category-label" || text.Role == family + "-value-label").ToArray();
        Assert.Contains(painted, text => text.Role == family + "-category-label");
        Assert.Contains(painted, text => text.Role == family + "-value-label");
        Assert.All(painted.Where(text => text.Role == family + "-category-label"), text => { Assert.Equal(ink, text.Color); Assert.Equal(12, text.Text.Size); });
        var boxes = painted.Select(text => prepared.Regions.Single(region => region.Id == text.Id).Bounds).ToArray();
        for (var first = 0; first < boxes.Length; first++) for (var second = first + 1; second < boxes.Length; second++)
            Assert.False(Overlap(boxes[first], boxes[second]));
        var document = XDocument.Parse(prepared.ToSvg());
        var groups = document.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == family + "-stage").ToArray();
        Assert.Equal(new[] { "intake", "review", "complete" }, groups.Select(node => (string?)node.Attribute("data-cfx-observation-id")));
        Assert.Equal(new[] { "100", "75", "25" }, groups.Select(node => (string?)node.Attribute("data-cfx-value")));
        Assert.DoesNotContain(document.Descendants(), node => node.Name.LocalName == "script");
        ScatterBubbleColorRenderingTests.Capture(prepared, $"independent-{family}-{form}-{orientation}-{width}-{mode}");
    }

    [Fact]
    public void SeparateMetricsDistinguishUndefinedBaselinesAndNegativeDropOffWithoutInventingValues() {
        var chart = StagePresentationOptionsTests.Create(false, ChartFunnelForm.StageBars, ChartOrientation.Vertical, 900, 700);
        chart.Series[0].Points[0] = new ChartPoint(1, 0); chart.Series[0].Points[1] = new ChartPoint(2, 20); chart.Series[0].Points[2] = new ChartPoint(3, 40);
        chart.ConfigureFunnel(options => {
            options.LabelContent = ChartStageLabelContent.Separate; options.CategoryLabel.Visible = false; options.ValueLabel.Visible = false;
            options.RetentionLabel.Placement = ChartDataLabelPlacement.Left; options.DropOffLabel.Placement = ChartDataLabelPlacement.Right;
        });
        var prepared = StagePresentationOptionsTests.Prepare(chart);
        Assert.Equal(3, prepared.Regions.Count(region => region.Role == "funnel-retention-label" && region.Label == "No initial baseline"));
        Assert.Equal(2, prepared.Regions.Count(region => region.Role == "funnel-dropoff-label" && region.Label == "No previous baseline"));
        Assert.Contains(prepared.Regions, region => region.Role == "funnel-dropoff-label" && region.Label == "-100% drop-off");
        Assert.Contains("data-cfx-retention-defined=\"false\"", prepared.ToSvg());
        Assert.Contains("data-cfx-dropoff=\"-1\"", prepared.ToSvg());
        Assert.DoesNotContain(prepared.Regions, region => region.Role is "funnel-category-label" or "funnel-value-label");
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TinyFramesOmitOnlyPaintedCaptionsAndPreserveEveryCompleteSourceFact(bool pyramid) {
        var chart = StagePresentationOptionsTests.Create(pyramid, ChartFunnelForm.StageBars, ChartOrientation.Vertical);
        chart.ConfigureFunnel(options => options.LabelContent = ChartStageLabelContent.Separate)
            .ConfigurePyramid(options => options.LabelContent = ChartStageLabelContent.Separate);
        var context = new VisualRenderContext(); var builder = new VisualSceneBuilder(new VisualSize(90, 30), context.Font);
        VisualSpecialtyCompiler.Build(chart, context, builder, new ChartRect(0, 0, 90, 30));
        var scene = builder.Build(); var family = pyramid ? "pyramid" : "funnel";
        Assert.Equal(3, scene.Regions.Count(region => region.Role == family + "-stage"));
        Assert.Equal(3, scene.Regions.Count(region => region.Role == family + "-category-label"));
        Assert.Contains(scene.Diagnostics, diagnostic => diagnostic.Code == family + ".label-hidden");
        Assert.All(scene.Nodes.OfType<VisualSceneText>(), text => Assert.True(text.Text.Size >= 10));
        Assert.NotEmpty(new PreparedVisual(scene).ToPng());
    }
    [Fact]
    public void ZeroPyramidPartitionsNeverCreatePhantomMarkObstaclesOrDanglingCaptionLeaders() {
        var chart = StagePresentationOptionsTests.Create(true, ChartFunnelForm.StageBars, ChartOrientation.Vertical);
        for (var index = 0; index < chart.Series[0].Points.Count; index++) chart.Series[0].Points[index] = new ChartPoint(index + 1, 0);
        chart.ConfigurePyramid(options => options.LabelContent = ChartStageLabelContent.Separate);
        var prepared = StagePresentationOptionsTests.Prepare(chart);
        Assert.Equal(3, prepared.Regions.Count(region => region.Role == "pyramid-stage"));
        Assert.Equal(3, prepared.Regions.Count(region => region.Role == "pyramid-category-label"));
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role is "pyramid-segment" or "data-label-connector");
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "pyramid.all-zero");
        Assert.NotEmpty(prepared.ToPng());
    }

    private static bool Overlap(ChartRect first, ChartRect second) => first.Left < second.Right && first.Right > second.Left
        && first.Top < second.Bottom && first.Bottom > second.Top;
}
