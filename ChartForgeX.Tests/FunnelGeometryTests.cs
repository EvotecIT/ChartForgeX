using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects proportional funnel encoding, ordered source stages and actual shared static exports.</summary>
public sealed class FunnelGeometryTests {
    [Fact]
    public void OptionsDefaultToVerticalStageBarsAndRejectUnknownSelectors() {
        var chart = Chart.Create();
        Assert.Equal(ChartFunnelForm.StageBars, chart.Options.Funnel.Form);
        Assert.Equal(ChartOrientation.Vertical, chart.Options.Funnel.Orientation);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.Funnel.Form = (ChartFunnelForm)99);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.Funnel.Orientation = (ChartOrientation)99);
        Assert.Throws<ArgumentNullException>(() => chart.WithFunnel(null!));
    }

    [Theory]
    [InlineData(ChartOrientation.Vertical)]
    [InlineData(ChartOrientation.Horizontal)]
    public void StageBarExtentsAndPngPixelsFollowValuesIncludingZeroAndReopenedStages(ChartOrientation orientation) {
        var values = new[] { 100d, 75, 25, 0, 50 };
        var chart = Funnel(values, ChartFunnelForm.StageBars, orientation);
        var scene = Compile(chart);
        var marks = scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == "funnel-segment").ToArray();
        var regions = scene.Regions.Where(region => region.Role == "funnel-stage").ToArray();
        var maximumExtent = CrossExtent(marks[0], orientation);
        Assert.Equal(4, marks.Length);
        Assert.Equal(maximumExtent * .75, CrossExtent(marks[1], orientation), 8);
        Assert.Equal(maximumExtent * .25, CrossExtent(marks[2], orientation), 8);
        Assert.Equal(maximumExtent * .5, CrossExtent(marks[3], orientation), 8);
        Assert.Equal(0, orientation == ChartOrientation.Vertical ? regions[3].Bounds.Width : regions[3].Bounds.Height);
        Assert.Single(scene.Nodes, node => node.Role == "funnel-zero");
        Assert.Equal(values.Length - 1, scene.Nodes.Count(node => node.Role == "funnel-connection"));
        Assert.Equal(values.Select((value, index) => "Stage " + index + ": " + value.ToString("0", CultureInfo.InvariantCulture)),
            regions.Select(region => region.Label!.Split(", retained", StringSplitOptions.None)[0]));

        var png = RasterImageDecoder.Decode(new PreparedVisual(scene).ToPng());
        var small = regions[2].Bounds;
        var x = small.Left + small.Width / 2; var y = small.Top + small.Height / 2;
        var pixel = Pixel(png, x, y);
        var color = chart.Series[0].Color!.Value;
        Assert.Equal(new[] { color.R, color.G, color.B, color.A }, pixel);
        var beyond = orientation == ChartOrientation.Vertical ? Pixel(png, x + maximumExtent * .2, y) : Pixel(png, x, y + maximumExtent * .2);
        Assert.Equal(0, beyond[3]);
        var svg = XDocument.Parse(new PreparedVisual(scene).ToSvg(new VisualSvgOptions()));
        Assert.Equal(marks.Length, Roles(svg, "funnel-segment").Length);
        Assert.Contains("stage-bars", (string?)Assert.Single(Roles(svg, "funnel-chart")).Attribute("data-cfx-form"));
    }

    [Theory]
    [InlineData(ChartOrientation.Vertical)]
    [InlineData(ChartOrientation.Horizontal)]
    public void ConeHasOneExactStageLinePerSourceAndOnlyAuthoredAdjacentConnections(ChartOrientation orientation) {
        var chart = Funnel(new[] { 100d, 25, 0, 75 }, ChartFunnelForm.Cone, orientation);
        var scene = Compile(chart);
        var lines = scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == "funnel-stage-line").ToArray();
        var connections = scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == "funnel-connection-area").ToArray();
        Assert.Equal(4, lines.Length); Assert.Equal(3, connections.Length);
        var maximumExtent = CrossExtent(lines[0], orientation);
        Assert.Equal(maximumExtent * .25, CrossExtent(lines[1], orientation), 8);
        Assert.Equal(0, CrossExtent(lines[2], orientation));
        Assert.Equal(maximumExtent * .75, CrossExtent(lines[3], orientation), 8);
        Assert.All(lines, line => Assert.Equal(VisualStrokeCap.Butt, line.Cap));
        for (var index = 0; index < connections.Length; index++) {
            var connection = connections[index];
            Assert.Equal(lines[index].Commands[0].X, connection.Commands[0].X);
            Assert.Equal(lines[index].Commands[0].Y, connection.Commands[0].Y);
            Assert.Equal(lines[index].Commands[1].X, connection.Commands[1].X);
            Assert.Equal(lines[index].Commands[1].Y, connection.Commands[1].Y);
            Assert.Equal(lines[index + 1].Commands[1].X, connection.Commands[2].X);
            Assert.Equal(lines[index + 1].Commands[1].Y, connection.Commands[2].Y);
        }
        var sources = scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "funnel-stage").ToArray();
        Assert.Equal("false", sources[3].Metadata["data-cfx-dropoff-defined"]);
        Assert.Equal("0.75", sources[3].Metadata["data-cfx-retention"]);
        Assert.DoesNotContain("data-cfx-dropoff", sources[3].Metadata.Keys);
        var transition = scene.Nodes.OfType<VisualSceneGroup>().Last(group => group.Role == "funnel-connection");
        Assert.Equal("2", transition.Metadata["data-cfx-from-point"]);
        Assert.Equal("3", transition.Metadata["data-cfx-to-point"]);
        Assert.Equal("0", transition.Metadata["data-cfx-from-value"]);
        Assert.Equal("75", transition.Metadata["data-cfx-to-value"]);
        var svg = XDocument.Parse(new PreparedVisual(scene).ToSvg(new VisualSvgOptions()));
        Assert.Equal(4, Roles(svg, "funnel-stage-line").Length);
        Assert.Equal(3, Roles(svg, "funnel-connection-area").Length);
        Assert.Equal("cone", (string?)Assert.Single(Roles(svg, "funnel-chart")).Attribute("data-cfx-form"));
        Assert.NotEmpty(new PreparedVisual(scene).ToPng());
    }

    [Theory]
    [InlineData(ChartFunnelForm.StageBars, ChartOrientation.Vertical)]
    [InlineData(ChartFunnelForm.StageBars, ChartOrientation.Horizontal)]
    [InlineData(ChartFunnelForm.Cone, ChartOrientation.Vertical)]
    [InlineData(ChartFunnelForm.Cone, ChartOrientation.Horizontal)]
    public void SingletonAndAllZeroInputsDoNotInventStagesOrPositiveValueWidths(ChartFunnelForm form, ChartOrientation orientation) {
        var singleton = Compile(Funnel(new[] { 8d }, form, orientation));
        Assert.Single(singleton.Nodes, node => node.Role == "funnel-stage");
        Assert.DoesNotContain(singleton.Nodes, node => node.Role == "funnel-connection");
        var singletonMark = Assert.Single(singleton.Nodes.OfType<VisualScenePath>(), path => path.Role is "funnel-stage-line" or "funnel-segment");
        Assert.Equal(orientation == ChartOrientation.Vertical ? 320 : 180, CrossExtent(singletonMark, orientation));

        var empty = Compile(Funnel(new[] { 0d, 0, 0 }, form, orientation));
        Assert.Equal(3, empty.Nodes.Count(node => node.Role == "funnel-stage"));
        Assert.Equal(3, empty.Nodes.Count(node => node.Role == "funnel-zero"));
        Assert.All(empty.Regions.Where(region => region.Role == "funnel-stage"), region =>
            Assert.Equal(0, orientation == ChartOrientation.Vertical ? region.Bounds.Width : region.Bounds.Height));
        Assert.All(empty.Nodes.OfType<VisualScenePath>().Where(path => path.Role is "funnel-stage-line" or "funnel-connection-area" or "funnel-dropoff"),
            path => Assert.Equal(0, CrossExtent(path, orientation)));
        Assert.All(empty.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "funnel-stage"), group => {
            Assert.Equal("false", group.Metadata["data-cfx-retention-defined"]);
            Assert.Equal("false", group.Metadata["data-cfx-dropoff-defined"]);
        });
        Assert.Contains(empty.Diagnostics, diagnostic => diagnostic.Code == "funnel.all-zero");
        Assert.NotEmpty(new PreparedVisual(empty).ToPng());
    }

    [Theory]
    [InlineData(ChartFunnelForm.StageBars)]
    [InlineData(ChartFunnelForm.Cone)]
    public void ExplicitPaintPatternAndLabelStylesSurvivePreparationAndInputMutation(ChartFunnelForm form) {
        var chart = Funnel(new[] { 100d, 30 }, form, ChartOrientation.Vertical).WithDataLabels();
        var authored = ChartColor.FromHex("#2468AC"); var ink = ChartColor.FromHex("#AB1234");
        chart.Series[0].WithPointColor(0, authored).WithPointFillPattern(0, ChartFillPattern.DiagonalForward)
            .WithPointDataLabelStyle(0, style => style.WithColor(ink).WithWeight("700"));
        chart.Series[0].WithPointLabel(0, "Complete");
        var prepared = new PreparedVisual(Compile(chart));
        var mark = prepared.Scene.Nodes.OfType<VisualScenePath>().First(path => path.Role is "funnel-stage-line" or "funnel-segment");
        Assert.Equal(authored, form == ChartFunnelForm.Cone ? mark.Stroke : mark.Fill);
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "fill-pattern");
        var label = prepared.Scene.Nodes.OfType<VisualSceneText>().First(text => text.Role == "funnel-label");
        Assert.Equal(ink, label.Color);
        Assert.Contains(prepared.Regions, region => region.Role == "funnel-label" && region.Label == "Stage 0: Complete");
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.Options.Funnel.Form = form == ChartFunnelForm.Cone ? ChartFunnelForm.StageBars : ChartFunnelForm.Cone;
        chart.Options.Funnel.Orientation = ChartOrientation.Horizontal;
        chart.Series[0].Points.Clear();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Theory]
    [InlineData(ChartFunnelForm.StageBars, ChartOrientation.Vertical)]
    [InlineData(ChartFunnelForm.StageBars, ChartOrientation.Horizontal)]
    [InlineData(ChartFunnelForm.Cone, ChartOrientation.Vertical)]
    [InlineData(ChartFunnelForm.Cone, ChartOrientation.Horizontal)]
    public void CompactLabelsKeepReadableTypeAndMeasuredBoundsAcrossForms(ChartFunnelForm form, ChartOrientation orientation) {
        var chart = Funnel(new[] { 100d, 75, 25, 0 }, form, orientation).WithSize(360, 360).WithDataLabels()
            .WithXLabels("Received", "Reviewed", "Qualified", "Completed")
            .WithTitle("Requests from receipt to completion").WithSubtitle("Stage lines encode values including zero");
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "funnel-label").ToArray();
        var ratios = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "funnel-ratio").ToArray();
        Assert.Equal(4, labels.Length); Assert.Equal(3, ratios.Length);
        foreach (var label in labels.Concat(ratios)) {
            Assert.True(label.Text.Size >= 10, "Automatic fitting must preserve a readable logical font size.");
            var bounds = Assert.Single(prepared.Regions, region => region.Id == label.Id).Bounds;
            Assert.True(label.LineLeft(label.Text.Lines[0]) >= bounds.Left - .000001);
            Assert.True(label.LineLeft(label.Text.Lines[0]) + label.Text.Metrics.Width <= bounds.Right + .000001);
            Assert.True(label.Baseline - label.Text.Ascent >= bounds.Top - .000001);
            Assert.True(label.Baseline - label.Text.Ascent + label.Text.Metrics.Height <= bounds.Bottom + .000001);
        }
        Assert.Contains(prepared.Regions, region => region.Role == "funnel-label" && region.Label == "Received: 100");
        Assert.Contains(prepared.Regions, region => region.Role == "funnel-ratio" && region.Label == "75% retained\n25% drop-off");
        if (form == ChartFunnelForm.Cone && orientation == ChartOrientation.Horizontal) {
            var firstLabel = Assert.Single(prepared.Regions, region => region.Id == labels[0].Id).Bounds;
            var secondRatio = Assert.Single(prepared.Regions, region => region.Id == ratios[0].Id).Bounds;
            Assert.True(firstLabel.Bottom <= secondRatio.Top);
            var marks = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == "funnel-stage-line").ToArray();
            Assert.Equal(CrossExtent(marks[0], orientation) * .25, CrossExtent(marks[2], orientation), 8);
        }
        var svg = XDocument.Parse(prepared.ToSvg(new VisualSvgOptions()));
        var svgText = Roles(svg, "funnel-label").Concat(Roles(svg, "funnel-ratio"))
            .SelectMany(group => group.Descendants().Where(element => element.Name.LocalName == "text")).ToArray();
        Assert.Equal(labels.Concat(ratios).Sum(label => label.Text.Lines.Count), svgText.Length);
        Assert.All(svgText, element =>
            Assert.True(double.Parse(element.Attribute("font-size")!.Value, CultureInfo.InvariantCulture) >= 10));
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void LabelsThatCannotFitReadableTypeKeepCompleteDescriptionsInsteadOfTinyGlyphs() {
        var chart = Funnel(new[] { 100d, 25, 0 }, ChartFunnelForm.Cone, ChartOrientation.Horizontal)
            .WithDataLabels().WithXLabels("Received", "Qualified", "Completed");
        var context = new VisualRenderContext(); var builder = new VisualSceneBuilder(new VisualSize(160, 25), context.Font);
        VisualSpecialtyCompiler.Build(chart, context, builder, new ChartRect(0, 0, 160, 25));
        var scene = builder.Build();
        Assert.DoesNotContain(scene.Nodes.OfType<VisualSceneText>(), node => node.Role is "funnel-label" or "funnel-ratio");
        Assert.Contains(scene.Diagnostics, diagnostic => diagnostic.Code == "funnel.label-hidden");
        Assert.Equal(new[] { "Received: 100", "Qualified: 25", "Completed: 0" },
            scene.Regions.Where(region => region.Role == "funnel-label").Select(region => region.Label));
        Assert.Contains(scene.Regions, region => region.Role == "funnel-ratio" && region.Label == "25% retained\n75% drop-off");
    }

    private static Chart Funnel(double[] values, ChartFunnelForm form, ChartOrientation orientation) {
        var chart = Chart.Create().WithXLabels(values.Select((_, index) => "Stage " + index).ToArray())
            .WithFunnel(options => { options.Form = form; options.Orientation = orientation; })
            .AddFunnel("Stages", values.Select((value, index) => new ChartPoint(index + 1, value)), ChartColor.FromHex("#2468AC"));
        return chart;
    }

    private static VisualScene Compile(Chart chart) {
        var context = new VisualRenderContext(); var builder = new VisualSceneBuilder(new VisualSize(320, 180), context.Font);
        VisualSpecialtyCompiler.Build(chart, context, builder, new ChartRect(0, 0, 320, 180));
        return builder.Build();
    }

    private static double CrossExtent(VisualScenePath path, ChartOrientation orientation) => orientation == ChartOrientation.Vertical
        ? path.Commands.Max(command => command.X) - path.Commands.Min(command => command.X)
        : path.Commands.Max(command => command.Y) - path.Commands.Min(command => command.Y);

    private static byte[] Pixel(RgbaImage image, double x, double y) {
        var offset = ((int)y * image.Width + (int)x) * 4;
        return new[] { image.Pixels[offset], image.Pixels[offset + 1], image.Pixels[offset + 2], image.Pixels[offset + 3] };
    }

    private static XElement[] Roles(XDocument svg, string role) => svg.Descendants()
        .Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
}
