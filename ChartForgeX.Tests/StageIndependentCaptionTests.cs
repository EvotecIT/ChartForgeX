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
    [InlineData(VisualThemeMode.Dark, 12)]
    [InlineData(VisualThemeMode.Light, 13.5)]
    public void FiveStageConePaintsEverySeparateCaptionWhenTheMeasuredRailHasRoom(VisualThemeMode mode, double fontSize) {
        var names = new[] { "Visitors", "Sign-ups", "Trials", "Paid", "Renewed" };
        var chart = ConeFunnelRecipe.Create().WithSize(800, 480).ConfigureDataLabelStyle(style => style.WithFontSize(fontSize));
        RoundedRadialSeriesTests.UseFixtureFont(chart);
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(800, 480)),
            VisualTheme.Graphite(), mode, new VisualFrame("From visitor to renewed subscription",
                "Each stage narrows the cone by the customers it keeps"),
            ChartForgeX.Typography.FontSpec.FromFile(chart.Options.PngFontPath!)));
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>()
            .Where(node => node.Role?.StartsWith("funnel-", StringComparison.Ordinal) == true && node.Role.EndsWith("-label", StringComparison.Ordinal)).ToArray();
        Assert.Equal(19, labels.Length);
        Assert.Equal(names, labels.Where(node => node.Role == "funnel-category-label").Select(node => Assert.Single(node.Text.Lines).Text));
        Assert.Equal(new[] { "12,400", "5,270", "2,130", "860", "610" },
            labels.Where(node => node.Role == "funnel-value-label").Select(node => Assert.Single(node.Text.Lines).Text));
        Assert.Equal(4, labels.Count(node => node.Role == "funnel-dropoff-label"));
        Assert.All(labels, node => Assert.Equal(fontSize, node.Text.Size));
        var bounds = labels.Select(node => prepared.Regions.Single(region => region.Id == node.Id).Bounds).ToArray();
        for (var first = 0; first < bounds.Length; first++) {
            Assert.InRange(bounds[first].Top, 0, 480);
            Assert.InRange(bounds[first].Bottom, 0, 480);
            for (var second = first + 1; second < bounds.Length; second++) Assert.False(Overlap(bounds[first], bounds[second]));
        }
        var leaders = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "data-label-connector").ToArray();
        Assert.Equal(5, leaders.Length);
        foreach (var leader in leaders) {
            Assert.Equal(2, leader.Commands.Count);
            var stroke = new LabelMarkShape(new[] { leader.Commands.Select(command => new ChartPoint(command.X, command.Y)).ToList() }, false, leader.StrokeWidth);
            foreach (var box in bounds) Assert.False(stroke.Intersects(box), "A stage leader crosses a painted caption.");
        }
        Assert.Equal(new[] { "12400", "5270", "2130", "860", "610" }, XDocument.Parse(prepared.ToSvg()).Descendants()
            .Where(node => (string?)node.Attribute("data-cfx-role") == "funnel-stage").Select(node => (string?)node.Attribute("data-cfx-value")));
        Assert.NotEmpty(prepared.ToPng());
    }

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
        Assert.Single(prepared.Regions, region => region.Role == "funnel-dropoff-label" && region.Label == "No previous baseline");
        Assert.DoesNotContain(prepared.Regions, region => region.Id == "series-0-point-0-dropoff-label");
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
