using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects radial value geometry, source identity and measured labels before serialization.</summary>
public sealed class V2RadialTests {
    [Fact]
    public void Donut_WeightsZeroValuesAndOffsets_PreserveGeometryAndIdentity() {
        var chart = Donut(0, 75, 25).WithXLabels("Zero", "Pass", "Fail");
        var red = ChartColor.FromHex("#BB3311");
        chart.Series[0].WithPointColor(1, red).WithPointSliceOffset(1, 0.2);
        var scene = Compile(chart);
        var slices = scene.Nodes.OfType<VisualSceneSlice>().ToArray();
        Assert.Equal(2, slices.Length);
        Assert.Equal("series-0-point-1", slices[0].Id);
        Assert.Equal(Math.PI * 1.5, slices[0].Sweep, 10);
        Assert.Equal(Math.PI * 0.5, slices[1].Sweep, 10);
        Assert.Equal(red, slices[0].Fill);
        Assert.Equal(slices[0].Outer * chart.Options.DonutInnerRadiusRatio, slices[0].Inner, 10);
        Assert.NotEqual(slices[1].Cx, slices[0].Cx);
        Assert.NotEqual(slices[1].Cy, slices[0].Cy);
        var metadata = scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "radial-point").ToArray();
        Assert.Equal("1", metadata[0].Metadata["data-cfx-source-points"]);
        Assert.Equal("0.75", metadata[0].Metadata["data-cfx-percent"]);
        Assert.Equal(3, VisualRadialCompiler.LegendEntries(chart, new VisualRenderContext().Theme.Resolve(VisualThemeMode.Light)).Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SinglePositiveValue_ProducesAFullCircleOrRing(bool donut) {
        var chart = donut ? Donut(12) : Chart.Create().AddPie("Total", Points(12));
        chart.WithDonutCenterLabel(false);
        var scene = Compile(chart);
        var slice = Assert.Single(scene.Nodes.OfType<VisualSceneSlice>());
        Assert.Equal(Math.PI * 2, slice.Sweep, 10);
        Assert.Equal(-Math.PI / 2, slice.Start, 10);
        Assert.Equal(donut ? slice.Outer * chart.Options.DonutInnerRadiusRatio : 0, slice.Inner, 10);
        var svg = XDocument.Parse(VisualSceneSvgRenderer.Render(scene));
        var path = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == (donut ? "donut-slice" : "pie-slice"));
        Assert.Equal("evenodd", (string?)path.Attribute("fill-rule"));
        var image = VisualSceneRasterRenderer.Render(scene);
        var center = ((int)slice.Cy * image.Width + (int)slice.Cx) * 4;
        Assert.Equal(donut ? 0 : 255, image.Pixels[center + 3]);
        var ringX = (int)(slice.Cx + (slice.Inner + slice.Outer) / 2);
        var ring = ((int)slice.Cy * image.Width + ringX) * 4;
        Assert.Equal(slice.Fill!.Value.R, image.Pixels[ring]);
        Assert.Equal(slice.Fill.Value.G, image.Pixels[ring + 1]);
        Assert.Equal(slice.Fill.Value.B, image.Pixels[ring + 2]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EmptyOrAllZeroValues_ProduceNoDataDiagnosticWithoutInvalidAngles(bool empty) {
        var chart = empty ? Donut() : Donut(0, 0, 0);
        var scene = Compile(chart);
        Assert.Empty(scene.Nodes.OfType<VisualSceneSlice>());
        Assert.Contains(scene.Diagnostics, diagnostic => diagnostic.Code == "radial.no-data");
        Assert.Contains(scene.Nodes.OfType<VisualSceneText>(), text => text.Role == "no-data");
    }

    [Fact]
    public void Aggregation_IsIndependentOfLegacyThemeAndRetainsEveryContributingSource() {
        var chart = Donut(1, 9, 2, 8, 3).WithXLabels("A", "B", "C", "D", "E");
        chart.Options.MaximumPieSlices = 3;
        var light = Compile(chart);
        chart.WithTheme(ChartTheme.GraphiteLight());
        var graphite = Compile(chart);
        var expected = new[] { "series-0-point-1", "series-0-point-3", "series-0-point-other" };
        Assert.Equal(expected, light.Nodes.OfType<VisualSceneSlice>().Select(slice => slice.Id));
        Assert.Equal(expected, graphite.Nodes.OfType<VisualSceneSlice>().Select(slice => slice.Id));
        var other = light.Nodes.OfType<VisualSceneGroup>().Single(group => group.Metadata.TryGetValue("data-cfx-point", out var point) && point == "-1");
        Assert.Equal("0,2,4", other.Metadata["data-cfx-source-points"]);
        Assert.Equal("6", other.Metadata["data-cfx-value"]);
        Assert.Equal(Math.PI * 2, light.Nodes.OfType<VisualSceneSlice>().Sum(slice => slice.Sweep), 10);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AggregatedSliceOffset_LeavesSurvivingGeometryAndVerticalLabelsUnchanged(bool donut) {
        var chart = (donut ? Donut(9, 3, 1) : Chart.Create().AddPie("Total", Points(9, 3, 1)))
            .WithXLabels("A", "B", "C").WithDataLabels().WithPieSliceLabelContent(ChartPieSliceLabelContent.Label)
            .WithDonutCenterLabel(false);
        chart.Options.MaximumPieSlices = 2;
        chart.Options.DataLabelPlacement = ChartDataLabelPlacement.Above;
        // Retain a real exploded slice; only the smaller slices become Other.
        chart.Series[0].WithPointSliceOffset(0, .1);
        var baseline = Compile(chart);
        chart.Series[0].WithPointSliceOffset(2, .35); // The largest supported source offset is discarded into Other.
        var aggregatedOffset = Compile(chart);

        Assert.Contains(aggregatedOffset.Diagnostics, diagnostic => diagnostic.Code == "radial.aggregate-offsets");
        Assert.Equal(.35, chart.Series[0].PointSliceOffsets[2]);
        var slices = aggregatedOffset.Nodes.OfType<VisualSceneSlice>().ToArray();
        Assert.Equal(2, slices.Length);
        var other = Assert.Single(slices, slice => slice.Id == "series-0-point-other");
        Assert.Equal(210, other.Cx); Assert.Equal(160, other.Cy);
        var retained = Assert.Single(slices, slice => slice.Id == "series-0-point-0");
        Assert.Equal(retained.Outer * .1, Math.Sqrt(Math.Pow(retained.Cx - other.Cx, 2) + Math.Pow(retained.Cy - other.Cy, 2)), 10);
        Assert.Contains(aggregatedOffset.Nodes.OfType<VisualSceneGroup>(), group =>
            group.Metadata.TryGetValue("data-cfx-source-points", out var sources) && sources == "1,2");
        Assert.Equal(VisualSceneSvgRenderer.Render(baseline, idPrefix: "offset-proof"),
            VisualSceneSvgRenderer.Render(aggregatedOffset, idPrefix: "offset-proof"));
        Assert.Equal(VisualSceneRasterRenderer.Render(baseline).Pixels, VisualSceneRasterRenderer.Render(aggregatedOffset).Pixels);
    }

    [Fact]
    public void ZeroValueSliceOffset_DoesNotShrinkPositiveSlices() {
        var chart = Donut(9, 0, 3).WithDonutCenterLabel(false);
        chart.Series[0].WithPointSliceOffset(0, .1);
        var baseline = Compile(chart);
        chart.Series[0].WithPointSliceOffset(1, .35);
        var zeroOffset = Compile(chart);
        Assert.Equal(VisualSceneSvgRenderer.Render(baseline, idPrefix: "zero-offset-proof"),
            VisualSceneSvgRenderer.Render(zeroOffset, idPrefix: "zero-offset-proof"));
        Assert.Equal(VisualSceneRasterRenderer.Render(baseline).Pixels, VisualSceneRasterRenderer.Render(zeroOffset).Pixels);
        Assert.Equal(3, VisualRadialCompiler.LegendEntries(chart, new VisualRenderContext().Theme.Resolve(VisualThemeMode.Light)).Count);
    }

    [Fact]
    public void Formatter_ReceivesRealAggregateValuesAndSourceSentinel() {
        var contexts = new List<ChartPieSliceLabelContext>();
        var chart = Donut(6, 3, 1).WithDataLabels();
        chart.Options.MaximumPieSlices = 2;
        chart.Options.ValueFormatter = value => value.ToString("0.0", CultureInfo.InvariantCulture) + " units";
        chart.WithPieSliceLabelFormatter(value => { contexts.Add(value); return value.FormattedValue; });
        Compile(chart);
        Assert.Equal(2, contexts.Count);
        var other = contexts.Single(value => value.PointIndex == -1);
        Assert.Equal("Other", other.Label);
        Assert.Equal(4, other.Value);
        Assert.Equal(0.4, other.Percent, 10);
        Assert.Equal("4.0 units", other.FormattedValue);
    }

    [Fact]
    public void LongCenterText_IsMeasuredWithinTheDonutHole() {
        var chart = Donut(40, 60).WithDonutCenterText("A very long headline total with units", "A long descriptive caption");
        var scene = Compile(chart);
        var inner = scene.Nodes.OfType<VisualSceneSlice>().First().Inner;
        var lines = scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role is "donut-total-label" or "donut-title").ToArray();
        Assert.Equal(2, lines.Length);
        Assert.All(lines, text => Assert.True(text.Text.Metrics.Width <= inner * 1.6 + 0.001));
        var valueBottom = lines[0].Baseline - lines[0].Text.Ascent + lines[0].Text.Metrics.Height;
        var captionTop = lines[1].Baseline - lines[1].Text.Ascent;
        Assert.True(valueBottom <= captionTop);
    }

    [Fact]
    public void FittedRadialAndCenterText_RetainsCompleteSingleFormatterResultsInSvgRegionsAndArtifact() {
        const string prefix = "Custom formatted slice output with operator context beyond the available space: ";
        const string centerValue = "Complete total with all formatted units and additional contextual information";
        const string centerCaption = "Complete center caption explaining the total and its reporting period";
        var calls = 0;
        var chart = Donut(40, 60).WithXLabels("A", "B").WithDataLabels().WithDonutCenterText(centerValue, centerCaption)
            .WithPieSliceLabelFormatter(slice => { calls++; return prefix + slice.Label; });
        chart.Options.DataLabelPlacement = ChartDataLabelPlacement.Inside;
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(240, 200), padding: 8),
            frame: new VisualFrame(showLegend: false)));
        Assert.Equal(2, calls);
        var svg = XDocument.Parse(prepared.ToSvg());
        var displayed = svg.Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value).ToArray();
        var complete = new[] { prefix + "A", prefix + "B", centerValue, centerCaption };
        Assert.All(complete, full => Assert.DoesNotContain(full, displayed));
        Assert.NotEmpty(displayed);
        RetainsPointLabels(svg);
        Assert.Equal(centerValue, Assert.Single(svg.Descendants().Attributes("data-cfx-center-value")).Value);
        Assert.Equal(centerCaption, Assert.Single(svg.Descendants().Attributes("data-cfx-center-caption")).Value);
        Assert.All(complete, full => Assert.Contains(prepared.Regions, region => region.Label == full));
        var dataLabel = Assert.Single(prepared.Regions, region => region.Id == "series-0-point-0-label");
        Assert.Equal(Assert.Single(prepared.Regions, region => region.Id == "series-0-point-0").Bounds, dataLabel.Bounds);
        var center = Assert.Single(prepared.Regions, region => region.Id == "series-0-center-value");
        Assert.True(center.Bounds.Width > 0 && center.Bounds.Height > 0);
        Assert.True(center.Bounds.Width < dataLabel.Bounds.Width);

        var artifact = prepared.ToArtifact("retained-radial-text", VisualArtifactKind.Chart);
        Assert.All(complete, full => Assert.Contains(artifact.Regions, region => region.Label == full && region.AlternativeText == full));
        var artifactSvg = XDocument.Parse(artifact.ToSvg());
        RetainsPointLabels(artifactSvg);
        Assert.Equal(centerValue, Assert.Single(artifactSvg.Descendants().Attributes("data-cfx-center-value")).Value);
        Assert.Equal(centerCaption, Assert.Single(artifactSvg.Descendants().Attributes("data-cfx-center-caption")).Value);
        string snapshot = prepared.ToSvg();
        chart.WithDonutCenterText("Changed after preparation", "Changed caption")
            .WithPieSliceLabelFormatter(_ => throw new InvalidOperationException("Prepared export must not format again."));
        Assert.Equal(snapshot, prepared.ToSvg());
        Assert.NotEmpty(artifact.ToPng());
        Assert.Equal(2, calls);

        void RetainsPointLabels(XDocument document) {
            Assert.Equal(2, document.Descendants().Attributes("data-cfx-full-label").Count());
            for (var point = 0; point < 2; point++) {
                var source = Assert.Single(document.Descendants(), element =>
                    (string?)element.Attribute("data-cfx-point") == point.ToString(CultureInfo.InvariantCulture));
                Assert.Equal(complete[point], source.Attribute("data-cfx-full-label")!.Value);
            }
        }
    }

    [Fact]
    public void OutsideLabelOverflow_IsReportedAndKeptWithinAvailableHeight() {
        var chart = Donut(1, 1, 1, 1, 1, 1, 1, 1).WithDataLabels().WithPieSliceLabelContent(ChartPieSliceLabelContent.Label);
        chart.Options.MaximumPieSlices = 10;
        chart.Options.DataLabelPlacement = ChartDataLabelPlacement.Right;
        var scene = Compile(chart, new ChartRect(0, 0, 500, 80));
        Assert.Contains(scene.Diagnostics, diagnostic => diagnostic.Code == "radial.label-overflow");
        var labels = scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "data-label").OrderBy(text => text.Baseline).ToArray();
        Assert.NotEmpty(labels);
        for (var index = 1; index < labels.Length; index++) {
            var previousBottom = labels[index - 1].Baseline - labels[index - 1].Text.Ascent + labels[index - 1].Text.Metrics.Height;
            Assert.True(previousBottom <= labels[index].Baseline - labels[index].Text.Ascent + 0.001);
        }
    }

    [Theory]
    [InlineData(false, ChartDataLabelPlacement.Above)]
    [InlineData(false, ChartDataLabelPlacement.Below)]
    [InlineData(true, ChartDataLabelPlacement.Above)]
    [InlineData(true, ChartDataLabelPlacement.Below)]
    public void VerticalLabels_EqualSlicesHaveDistinctMeasuredSvgAndNativePositions(bool donut, ChartDataLabelPlacement placement) {
        var chart = (donut ? Donut(1, 1, 1, 1) : Chart.Create().AddPie("Total", Points(1, 1, 1, 1)))
            .WithXLabels("A", "B", "C", "D").WithDataLabels()
            .WithPieSliceLabelContent(ChartPieSliceLabelContent.Label).WithDonutCenterLabel(false);
        chart.Options.DataLabelPlacement = placement;
        var scene = Compile(chart);
        var labels = scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "data-label").ToArray();
        Assert.Equal(new[] { "A", "B", "C", "D" }, labels.Select(label => Assert.Single(label.Text.Lines).Text));
        Assert.DoesNotContain(scene.Diagnostics, diagnostic => diagnostic.Code == "radial.label-overflow");
        var bounds = labels.Select(label => new ChartRect(label.X, label.Baseline - label.Text.Ascent,
            label.Text.Metrics.Width, label.Text.Metrics.Height)).ToArray();
        var slices = scene.Nodes.OfType<VisualSceneSlice>().ToArray();
        for (var index = 0; index < bounds.Length; index++) {
            Assert.True(bounds[index].Left >= 0 && bounds[index].Right <= scene.Size.Width);
            Assert.True(bounds[index].Top >= 0 && bounds[index].Bottom <= scene.Size.Height);
            Assert.All(slices, slice => Assert.True(placement == ChartDataLabelPlacement.Above
                ? bounds[index].Bottom <= slice.Cy - slice.Outer : bounds[index].Top >= slice.Cy + slice.Outer));
            for (var previous = 0; previous < index; previous++)
                Assert.True(bounds[previous].Right <= bounds[index].Left || bounds[index].Right <= bounds[previous].Left);
        }
        var svg = XDocument.Parse(VisualSceneSvgRenderer.Render(scene));
        var exported = svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "data-label")
            .Select(element => Assert.Single(element.Elements())).ToArray();
        Assert.Equal(labels.Length, exported.Length);
        for (var index = 0; index < labels.Length; index++) {
            Assert.Equal(labels[index].Text.Lines[0].Text, exported[index].Value);
            Assert.True(Math.Abs(labels[index].X - double.Parse(exported[index].Attribute("x")!.Value, CultureInfo.InvariantCulture)) <= .001);
            Assert.True(Math.Abs(labels[index].Baseline - double.Parse(exported[index].Attribute("y")!.Value, CultureInfo.InvariantCulture)) <= .001);
        }
        var textScene = new VisualScene(scene.Size, labels, Array.Empty<VisualDiagnostic>(), Array.Empty<VisualSemanticRegion>());
        var image = VisualSceneRasterRenderer.Render(textScene);
        Assert.All(bounds, rect => {
            var painted = false;
            for (var y = (int)Math.Floor(rect.Top); y < Math.Ceiling(rect.Bottom); y++)
                for (var x = (int)Math.Floor(rect.Left); x < Math.Ceiling(rect.Right); x++)
                    painted |= image.Pixels[(y * image.Width + x) * 4 + 3] > 0;
            Assert.True(painted, "Every measured label box must contain its native text paint.");
        });
    }

    [Theory]
    [InlineData(ChartDataLabelPlacement.Above)]
    [InlineData(ChartDataLabelPlacement.Below)]
    public void VerticalLabelBand_InsufficientHeightReportsOmission(ChartDataLabelPlacement placement) {
        const string prefix = "Complete formatter result omitted from the bounded label band: ";
        var calls = 0;
        var chart = Donut(1, 1, 1, 1).WithXLabels("A", "B", "C", "D").WithDataLabels()
            .WithPieSliceLabelFormatter(slice => { calls++; return prefix + slice.Label; }).WithDonutCenterLabel(false);
        chart.Options.DataLabelPlacement = placement;
        chart.Options.DataLabelStyle.FontSize = 100;
        var scene = Compile(chart, new ChartRect(0, 0, 200, 100));
        Assert.Contains(scene.Diagnostics, diagnostic => diagnostic.Code == "radial.label-overflow");
        Assert.DoesNotContain(scene.Nodes.OfType<VisualSceneText>(), text => text.Role == "data-label");
        Assert.DoesNotContain(scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "data-label-connector");
        var complete = new[] { "A", "B", "C", "D" }.Select(label => prefix + label).ToArray();
        Assert.Equal(4, calls);
        Assert.All(complete, full => Assert.Contains(scene.Regions, region => region.Label == full));
        var svg = XDocument.Parse(VisualSceneSvgRenderer.Render(scene));
        Assert.Equal(complete, svg.Descendants().Attributes("data-cfx-full-label").Select(attribute => attribute.Value));
        var artifact = new PreparedVisual(scene).ToArtifact("omitted-radial-text", VisualArtifactKind.Chart);
        Assert.All(complete, full => Assert.Contains(artifact.Regions, region => region.Label == full));
    }

    [Theory]
    [InlineData(ChartDataLabelPlacement.Above, ChartDataLabelConnectorStyle.Straight)]
    [InlineData(ChartDataLabelPlacement.Above, ChartDataLabelConnectorStyle.Elbow)]
    [InlineData(ChartDataLabelPlacement.Above, ChartDataLabelConnectorStyle.Curve)]
    [InlineData(ChartDataLabelPlacement.Below, ChartDataLabelConnectorStyle.Straight)]
    [InlineData(ChartDataLabelPlacement.Below, ChartDataLabelConnectorStyle.Elbow)]
    [InlineData(ChartDataLabelPlacement.Below, ChartDataLabelConnectorStyle.Curve)]
    public void VerticalLeaders_BypassUnrelatedSlicesAndEndAtLabelEdges(ChartDataLabelPlacement placement, ChartDataLabelConnectorStyle style) {
        foreach (var donut in new[] { false, true }) {
            var chart = (donut ? Donut(1, 1, 1, 1) : Chart.Create().AddPie("Total", Points(1, 1, 1, 1)))
                .WithXLabels("A", "B", "C", "D").WithDataLabels().WithPieSliceLabelContent(ChartPieSliceLabelContent.Label)
                .WithDonutCenterLabel(false).WithDataLabelConnectorStyle(style).WithDataLabelConnectorOpacity(1)
                .WithDataLabelConnectorStrokeWidth(2);
            chart.Options.DataLabelPlacement = placement;
            chart.Series[0].WithPointSliceOffset(0, .25).WithPointSliceOffset(2, .15);
            var scene = Compile(chart);
            var slices = scene.Nodes.OfType<VisualSceneSlice>().ToArray();
            var leaders = scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == "data-label-connector").ToArray();
            var labels = scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "data-label").ToArray();
            Assert.Equal(4, leaders.Length);
            Assert.Equal(4, labels.Length);
            Assert.DoesNotContain(scene.Diagnostics, diagnostic => diagnostic.Code == "radial.label-overflow");
            for (var index = 0; index < slices.Length; index++) {
                var source = slices[index];
                var leader = Assert.Single(leaders, path => path.Id == source.Id + "-connector");
                var label = Assert.Single(labels, text => text.Id == source.Id + "-label");
                Assert.Equal(source.Fill, leader.Stroke);
                var points = Assert.Single(VisualSceneGeometry.Flatten(leader, 2));
                Assert.All(points, point => Assert.True(point.X >= 1 && point.X <= scene.Size.Width - 1 &&
                    point.Y >= 1 && point.Y <= scene.Size.Height - 1, "The full stroked leader must fit its viewport."));
                Assert.Equal(label.X + label.Text.Metrics.Width / 2, points[points.Count - 1].X, 8);
                var nearEdge = label.Baseline - label.Text.Ascent + (placement == ChartDataLabelPlacement.Above ? label.Text.Metrics.Height : 0);
                Assert.Equal(nearEdge, points[points.Count - 1].Y, 8);
                Assert.Equal(style == ChartDataLabelConnectorStyle.Curve, leader.Commands.Any(command => command.Kind == ChartPathCommandKind.CubicTo));
                // In this equal-sector fixture the unrelated disks do not contain the source's radial attachment.
                // Testing every flattened segment catches inward straight, elbow and curved opposite-hemisphere leaders.
                foreach (var unrelated in slices.Where(slice => slice != source))
                    for (var segment = 1; segment < points.Count; segment++)
                        Assert.True(DistanceToSegment(new ChartPoint(unrelated.Cx, unrelated.Cy), points[segment - 1], points[segment]) >= unrelated.Outer - .001,
                            "A vertical leader must not cross an unrelated filled slice, including when slices are exploded.");
            }
        }
    }

    [Fact]
    public void VerticalLeader_InsufficientClearanceOmitsLabelAndReportsOverflow() {
        var chart = Donut(1).WithXLabels("A").WithDataLabels().WithPieSliceLabelContent(ChartPieSliceLabelContent.Label)
            .WithDonutCenterLabel(false).WithDataLabelConnectorStrokeWidth(8);
        chart.Options.DataLabelPlacement = ChartDataLabelPlacement.Above;
        var theme = VisualTheme.Graphite();
        var compactTheme = new VisualTheme(theme.Resolve(VisualThemeMode.Light).ToTokens(), theme.Resolve(VisualThemeMode.Dark).ToTokens(), spacing: 6);
        var scene = Compile(chart, context: new VisualRenderContext(theme: compactTheme));
        Assert.Contains(scene.Diagnostics, diagnostic => diagnostic.Code == "radial.label-overflow");
        Assert.DoesNotContain(scene.Nodes, node => node.Role is "data-label" or "data-label-connector");
        Assert.Contains(scene.Regions, region => region.Id == "series-0-point-0-label" && region.Label == "A");
    }

    private static double DistanceToSegment(ChartPoint point, ChartPoint first, ChartPoint last) {
        var dx = last.X - first.X; var dy = last.Y - first.Y;
        var square = dx * dx + dy * dy;
        var fraction = square == 0 ? 0 : Math.Max(0, Math.Min(1, ((point.X - first.X) * dx + (point.Y - first.Y) * dy) / square));
        var x = first.X + fraction * dx - point.X; var y = first.Y + fraction * dy - point.Y;
        return Math.Sqrt(x * x + y * y);
    }

    private static VisualScene Compile(Chart chart, ChartRect? bounds = null, VisualRenderContext? context = null) {
        context ??= new VisualRenderContext();
        var plot = bounds ?? new ChartRect(0, 0, 420, 320);
        var builder = new VisualSceneBuilder(new VisualSize(Math.Max(1, plot.Right), Math.Max(1, plot.Bottom)), context.Font);
        VisualRadialCompiler.Build(chart, context, builder, plot);
        return builder.Build();
    }

    private static Chart Donut(params double[] values) => Chart.Create().AddDonut("Total", Points(values));
    private static IEnumerable<ChartPoint> Points(params double[] values) => values.Select((value, index) => new ChartPoint(index + 1, value));
}
