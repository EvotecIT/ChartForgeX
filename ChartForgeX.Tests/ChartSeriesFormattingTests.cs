using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartSeriesFormattingTests {
    [Theory]
    [InlineData("line", true)]
    [InlineData("horizontal", true)]
    [InlineData("bubble", true)]
    [InlineData("range", true)]
    [InlineData("pie", true)]
    [InlineData("radial", true)]
    [InlineData("polar", true)]
    [InlineData("radar", true)]
    [InlineData("gauge", true)]
    [InlineData("circle", true)]
    [InlineData("bullet", true)]
    [InlineData("ring", true)]
    [InlineData("layer", true)]
    [InlineData("progress", true)]
    [InlineData("matrix", true)]
    [InlineData("calendar", false)]
    [InlineData("tree", true)]
    [InlineData("sunburst", true)]
    [InlineData("treemap", true)]
    [InlineData("sankey", true)]
    [InlineData("chord", true)]
    [InlineData("funnel", true)]
    [InlineData("pyramid", true)]
    [InlineData("word", true)]
    [InlineData("map", true)]
    [InlineData("schedule", true)]
    [InlineData("packed", true)]
    public void FamilyPreparationCapturesLabelsAndPlainTextTooltips(string family, bool visibleLabel) {
        var chart = Family(family).WithSize(1000, 700).WithLegend(false).WithDataLabels();
        var series = chart.Series[0];
        var labels = new List<ChartValueContext>();
        var tips = new List<int>();
        series.WithDataLabelFormatter(context => { labels.Add(context); return "label-" + context.Index; })
            .WithTooltipFormatter(context => { tips.Add(context.Index); return "tip <&> " + context.Index; });

        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = XDocument.Parse(prepared.ToSvg());
        var relationshipCount = series.Kind == ChartSeriesKind.Tree ? series.TreeLinks.Count : series.FlowLinks.Count;
        var expectedIndices = Enumerable.Range(0, series.ObservationCount).Concat(Enumerable.Range(0, relationshipCount)).ToArray();
        Assert.Equal(expectedIndices, labels.Select(context => context.Index));
        Assert.Equal(expectedIndices, tips);
        Assert.All(labels, context => {
            Assert.Equal(series.Id, context.Series.Id);
            Assert.Equal(series.Kind, context.Series.Kind);
            Assert.Equal(series.Name, context.Series.Name);
            Assert.NotNull(context.Datum);
        });
        if (series.Kind == ChartSeriesKind.Tree)
            Assert.All(labels.Skip(series.ObservationCount), context => Assert.IsType<ChartTreeLink>(context.Datum));
        if (series.Kind is ChartSeriesKind.Sankey or ChartSeriesKind.Chord)
            Assert.All(labels.Skip(series.ObservationCount), context => Assert.IsType<ChartFlowLink>(context.Datum));
        Assert.Contains(svg.Descendants(), element => (string?)element.Attribute("data-cfx-full-label") == "label-0");
        Assert.Contains(svg.Descendants(), element => (string?)element.Attribute("data-cfx-tooltip-text") == "tip <&> 0");
        Assert.Contains(svg.Descendants(), element => element.Name.LocalName == "title" && element.Value == "tip <&> 0");
        if (visibleLabel) Assert.Contains(svg.Descendants(), element => element.Name.LocalName == "text" && element.Value.Contains("label-", StringComparison.Ordinal));
        var calls = labels.Count;
        var authored = Family(family).WithSize(1000, 700).WithLegend(false).WithDataLabels();
        for (var index = 0; index < series.ObservationCount; index++) authored.Series[0].WithPointLabel(index, "label-" + index);
        var explicitCaptions = authored.Prepare(VisualExportRequest.ForChart(authored).Context);
        Assert.Equal(explicitCaptions.ToPng(), prepared.ToPng());
        Assert.Equal(XDocument.Parse(explicitCaptions.ToSvg()).Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value),
            svg.Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value));
        _ = prepared.ToSvg();
        Assert.Equal(calls, labels.Count);
        Assert.Equal(calls, tips.Count);
        Assert.Empty(series.PointLabels);
    }

    [Fact]
    public void FlowFormatterReceivesAuthoredLinkWeightAndNamesWithoutReplacingNodeText() {
        var chart = Family("sankey"); var calls = 0;
        chart.Series[0].WithDataLabelFormatter(context => context.Datum is ChartFlowLink ? "flow-caption" : null)
            .WithTooltipFormatter(context => {
                if (context.Datum is not ChartFlowLink) return null;
                var link = context.GetDatum<ChartFlowLink>(); calls++;
                Assert.Equal("ab", link.Id);
                Assert.Equal("a", link.SourceId); Assert.Equal("b", link.TargetId);
                Assert.Equal(0, context.Index); Assert.Equal(20, context.Value);
                Assert.Equal("A → B", context.Category); Assert.Null(context.Percent);
                return "Transferred <20>";
            });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = XDocument.Parse(prepared.ToSvg());
        var flow = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-target-kind") == "link");
        Assert.Equal("Transferred <20>", (string?)flow.Attribute("data-cfx-tooltip-text"));
        Assert.Equal("flow-caption", (string?)flow.Attribute("data-cfx-full-label"));
        Assert.Equal("20", (string?)flow.Attribute("data-cfx-value"));
        Assert.DoesNotContain(svg.Descendants(), element => (string?)element.Attribute("data-cfx-target-kind") == "node"
            && (string?)element.Attribute("data-cfx-full-label") == "flow-caption");
        _ = prepared.ToSvg();
        Assert.Equal(1, calls);
    }

    [Fact]
    public void NullFallsBackEmptySuppressesAndDetachedExportsNeverReinvokeCallbacks() {
        var chart = Chart.Create().WithSize(900, 500).WithLegend(false).WithDataLabels()
            .AddBar("Orders", new[] { new ChartPoint(1, 11), new ChartPoint(2, 22), new ChartPoint(3, 33) });
        var series = chart.Series[0]; series.WithPointLabel(0, "authored");
        var prefix = "first"; var calls = 0;
        series.WithDataLabelFormatter(context => { calls++; return context.Index == 0 ? null : context.Index == 1 ? "" : prefix; })
            .WithTooltipFormatter(context => context.Index == 0 ? null : context.Index == 1 ? "" : prefix + " <script>");
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        prefix = "later";
        series.WithDataLabelFormatter(_ => throw new InvalidOperationException("Must not execute during export"));
        series.WithTooltipFormatter(_ => throw new InvalidOperationException("Must not execute during export"));

        var svg = XDocument.Parse(prepared.ToSvg());
        var text = string.Join("|", svg.Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value));
        Assert.Contains("authored", text);
        Assert.Contains("first", text);
        Assert.DoesNotContain(svg.Descendants(), element => element.Name.LocalName == "text" && element.Value == "22");
        Assert.DoesNotContain("later", text);
        Assert.Contains(svg.Descendants(), element => (string?)element.Attribute("data-cfx-tooltip-text") == "");
        Assert.DoesNotContain(svg.Descendants(), element => element.Name.LocalName == "script");
        Assert.Contains(svg.Descendants(), element => element.Name.LocalName == "title" && element.Value == "first <script>");
        _ = prepared.ToPng();
        Assert.Equal(3, calls);
        Assert.Equal(new string?[] { "authored" }, series.PointLabels);
    }

    [Fact]
    public void ContextUsesScalarFractionsAndCompleteTypedCompoundObservations() {
        var pie = Chart.Create().WithXLabels("Small", "Large").AddPie("Share", new[] { new ChartPoint(1, 1), new ChartPoint(2, 3) });
        var seen = new List<ChartValueContext>();
        pie.Series[0].WithDataLabelFormatter(context => { seen.Add(context); return null; });
        _ = pie.Prepare(VisualExportRequest.ForChart(pie).Context);
        Assert.Equal(new[] { "Small", "Large" }, seen.Select(context => context.Category));
        Assert.Equal(new double?[] { 1, 3 }, seen.Select(context => context.Value));
        Assert.Equal(new double?[] { .25, .75 }, seen.Select(context => context.Percent));
        Assert.Equal(3, seen[1].GetDatum<ChartPoint>().Y);
        Assert.Throws<InvalidOperationException>(() => seen[1].GetDatum<ChartInterval>());
        pie.Series[0].WithName("Renamed");
        Assert.Equal("Share", seen[0].Series.Name);

        var range = Family("range"); ChartValueContext? compound = null;
        range.Series[0].WithTooltipFormatter(context => { compound = context; return null; });
        _ = range.Prepare(VisualExportRequest.ForChart(range).Context);
        Assert.NotNull(compound);
        Assert.Null(compound.Value);
        Assert.Null(compound.Percent);
        Assert.Equal(8, compound.GetDatum<ChartInterval>().Start);
        Assert.Equal(18, compound.GetDatum<ChartInterval>().End);

        var hierarchy = Chart.Create().AddSunburst("Totals", new[] {
            new ChartHierarchyItem("root", "Root"), new ChartHierarchyItem("leaf", "Leaf", "root", 9)
        });
        var sizes = new List<double?>();
        hierarchy.Series[0].WithDataLabelFormatter(context => { sizes.Add(context.Value); return null; });
        _ = hierarchy.Prepare(VisualExportRequest.ForChart(hierarchy).Context);
        Assert.Equal(new double?[] { 9, 9 }, sizes);

        var box = Chart.Create().WithXLabels(new[] { new ChartAxisLabel(42, "Budget") })
            .AddBoxPlot("Boxes", new[] { new ChartBoxPlot(42, 1, 2, 3, 4, 5) });
        var boxed = CaptureContext(box);
        Assert.Equal("Budget", boxed.Category);
        Assert.Null(boxed.Value);
        Assert.Equal(42, boxed.GetDatum<ChartBoxPlot>().X);

        var progress = Chart.Create().WithProgressMaximum(200).AddSeries(new ChartSeries(new ChartSeriesId("p"), "M",
            ChartSeriesKind.ProgressBar, new[] { new ChartPoint(1, 30) }));
        Assert.Equal(.15, CaptureContext(progress).Percent);
        var ring = Chart.Create().WithProgressMaximum(200).AddProgressRing("M", new[] { new ChartPoint(1, 30) });
        Assert.Equal(.3, CaptureContext(ring).Percent);
        var independent = Chart.Create().AddProgressBars("M", new[] { new ChartProgressItem("A", 30, 20, 60) }, 200);
        Assert.Equal(.25, CaptureContext(independent).Percent!.Value, 12);

        var waterfall = Chart.Create().AddWaterfall("Balances", new[] {
            ChartWaterfallItem.OpeningBalance(1, 40), ChartWaterfallItem.Delta(2, -5), ChartWaterfallItem.Total(3)
        });
        var balances = new List<double?>();
        waterfall.Series[0].WithDataLabelFormatter(context => { balances.Add(context.Value); return null; });
        _ = waterfall.Prepare(VisualExportRequest.ForChart(waterfall).Context);
        Assert.Equal(new double?[] { 40, -5, 35 }, balances);
    }

    [Fact]
    public void MutableFamilyDatumCannotChangeAnotherCallbackOrTheAuthoredLayer() {
        var chart = Family("layer"); var series = chart.Series[0]; ChartValueContext? retained = null;
        series.WithDataLabelFormatter(context => {
            retained = context; context.GetDatum<ChartRadialLayer>().Value = 99; return null;
        }).WithTooltipFormatter(context => {
            Assert.Equal(30, context.GetDatum<ChartRadialLayer>().Value);
            Assert.Equal(.3, context.Percent); return null;
        });
        _ = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.NotNull(retained);
        series.RadialLayers[0].Value = 70;
        Assert.Equal(30, retained.GetDatum<ChartRadialLayer>().Value);
        Assert.Equal(30, retained.Value);
    }

    [Fact]
    public void NativeReflowSnapshotsTextOnceRemapsOrdinalsAndIncludesHiddenTextInSourceIdentity() {
        var id = new ChartSeriesId("orders");
        var series = new ChartSeries(id, "Orders", ChartSeriesKind.Bar,
            new[] { new ChartPoint(1, 10), new ChartPoint(2, 20) })
            .WithObservationIds(new[] { new ChartObservationId("first"), new ChartObservationId("second") });
        var chart = Chart.Create().WithLegend(false).WithDataLabels().AddSeries(series);
        var labels = 0; var tooltips = 0; var hiddenText = "hidden-a";
        series.WithDataLabelFormatter(context => { labels++; return context.Index == 0 ? hiddenText : "retained"; })
            .WithTooltipFormatter(context => { tooltips++; return "tip-" + context.Index; });
        var context = VisualExportRequest.ForChart(chart).Context;
        var visibility = new ChartVisibilityView(new[] { ChartVisibilityTarget.Observation(id, new ChartObservationId("first")) }, ChartVisibilityLayout.Reflow);
        var first = chart.PrepareVisibility(context, visibility);
        Assert.Equal(2, labels); Assert.Equal(2, tooltips);
        var retained = Assert.Single(XDocument.Parse(first.ToSvg()).Descendants(), element =>
            (string?)element.Attribute("data-cfx-observation-id") == "second" && (string?)element.Attribute("data-cfx-role") == "point");
        Assert.Equal("retained", (string?)retained.Attribute("data-cfx-full-label"));
        Assert.Equal("tip-1", (string?)retained.Attribute("data-cfx-tooltip-text"));
        hiddenText = "hidden-b";
        var second = chart.PrepareVisibility(context, visibility);
        var firstCapture = first.Scene.VisibilityCapture!; var secondCapture = second.Scene.VisibilityCapture!;
        Assert.NotEqual(firstCapture.SourceKey, secondCapture.SourceKey);
        Assert.Contains("hidden-b", secondCapture.ReflowSource!.CanonicalFacts(context));
        series.WithDataLabelFormatter(_ => throw new InvalidOperationException("Must not be retained"))
            .WithTooltipFormatter(_ => throw new InvalidOperationException("Must not be retained"));
        _ = firstCapture.ReflowSource!.CreateNativeView(visibility).Prepare(context).ToSvg();
        Assert.Equal(4, labels); Assert.Equal(4, tooltips);
    }

    [Fact]
    public void HiddenCaptionsStillEvaluateAndFailureLeavesAuthoredPresentationUnchanged() {
        var chart = Family("line").WithDataLabels(false);
        var series = chart.Series[0]; series.WithPointLabel(0, "keep"); var calls = 0;
        series.WithDataLabelFormatter(_ => { calls++; return "captured"; });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(series.ObservationCount, calls);
        Assert.DoesNotContain(XDocument.Parse(prepared.ToSvg()).Descendants(), element => element.Name.LocalName == "text" && element.Value == "captured");
        series.WithTooltipFormatter(_ => throw new InvalidOperationException("formatter failure"));
        var failure = Assert.Throws<InvalidOperationException>(() => chart.Prepare(VisualExportRequest.ForChart(chart).Context));
        Assert.Equal("formatter failure", failure.Message);
        Assert.Equal(new string?[] { "keep" }, series.PointLabels);
    }

    [Theory]
    [InlineData(ChartChordLabelContent.LabelAndValue)]
    [InlineData(ChartChordLabelContent.LabelAndTotals)]
    public void ChordFormatterReplacesTheCompleteCaptionAndEmptySuppressesGeometry(ChartChordLabelContent content) {
        var chart = Family("chord").WithSize(1000, 700).WithLegend(false)
            .ConfigureChord(options => options.LabelContent = content);
        chart.Series[0].WithDataLabelFormatter(context => context.Index == 0 ? "Sent twenty" : "");
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = XDocument.Parse(prepared.ToSvg());
        var caption = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "chord-node-label");
        Assert.Equal("Sent twenty", caption.Value);
        var facts = svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "chord-node").ToArray();
        Assert.Equal(2, facts.Length);
        Assert.All(facts, element => Assert.Equal("20", (string?)element.Attribute("data-cfx-value")));
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData("difference")]
    [InlineData("schedule")]
    public void PresentationCopiesKeepSeriesOrdinalsForLegendPaintAndLabelIdentity(string family) {
        var chart = family == "difference"
            ? Chart.Create().AddDifferenceArea("Compared", new[] { new ChartDifferencePoint(1, 30, 20), new ChartDifferencePoint(2, 10, 20) })
            : Family("schedule").WithDataLabels();
        chart.WithLegend(true);
        var context = VisualExportRequest.ForChart(chart).Context;
        var baseline = XDocument.Parse(chart.Prepare(context).ToSvg());
        chart.Series[0].WithTooltipFormatter(_ => null);
        var prepared = chart.Prepare(context);
        var svg = XDocument.Parse(prepared.ToSvg());
        if (family == "difference") {
            foreach (var role in new[] { "legend-difference-positive", "legend-difference-negative" }) {
                var expected = Assert.Single(baseline.Descendants(), element => (string?)element.Attribute("data-cfx-role") == role);
                var actual = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == role);
                Assert.Equal((string?)expected.Attribute("fill"), (string?)actual.Attribute("fill"));
            }
        } else {
            var label = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "data-label");
            Assert.Equal("series-0-point-0-label", (string?)label.Attribute("data-cfx-source-id"));
        }
        Assert.DoesNotContain(svg.Descendants().Attributes(), attribute => attribute.Value.StartsWith("series--1", StringComparison.Ordinal));
        Assert.NotEmpty(prepared.ToPng());
    }

    private static Chart Family(string family) {
        var chart = Chart.Create().WithXLabels("A", "B");
        var points = new[] { new ChartPoint(1, 30), new ChartPoint(2, 60) };
        var nodes = new[] { new ChartNode("a", "A"), new ChartNode("b", "B") };
        var flow = new[] { new ChartFlowLink("ab", "a", "b", 20) };
        var hierarchy = new[] { new ChartHierarchyItem("a", "A", value: 30), new ChartHierarchyItem("b", "B", value: 60) };
        return family switch {
            "line" => chart.AddLine("M", points),
            "horizontal" => chart.AddHorizontalBar("M", points),
            "bubble" => chart.AddBubble("M", new[] { new ChartBubble(1, 30, 4), new ChartBubble(2, 60, 9) }),
            "range" => chart.AddRangeBar("M", new[] { new ChartInterval(1, 8, 18) }),
            "pie" => chart.AddPie("M", points),
            "radial" => chart.AddRadialColumn("M", points),
            "polar" => chart.AddPolar("M", points),
            "radar" => chart.AddRadar("M", new[] { new ChartPoint(1, 30), new ChartPoint(2, 60), new ChartPoint(3, 40) }),
            "gauge" => chart.AddGauge("M", 30),
            "circle" => chart.AddCircle("M", 30),
            "bullet" => chart.AddBullet("M", 30, 60),
            "ring" => chart.AddProgressRing("M", new[] { new ChartPoint(1, 30) }),
            "layer" => chart.AddLayeredRadial("M", new[] { new ChartRadialLayer("A", 30, 0, 100) }),
            "progress" => chart.AddProgressBars("M", new[] { new ChartProgressItem("A", 30), new ChartProgressItem("B", 60) }),
            "matrix" => chart.AddHeatmapRow("M", points),
            "calendar" => chart.AddCalendarHeatmap("M", new[] { new ChartCalendarHeatmapItem(new DateTime(2026, 1, 5), 30) }),
            "tree" => chart.AddTree("M", nodes, new[] { new ChartTreeLink("a", "b") }),
            "sunburst" => chart.AddSunburst("M", new[] {
                new ChartHierarchyItem("root", "Root"),
                new ChartHierarchyItem("a", "A", "root", 30), new ChartHierarchyItem("b", "B", "root", 60)
            }),
            "treemap" => chart.AddTreemap("M", hierarchy),
            "sankey" => chart.AddSankey("M", nodes, flow),
            "chord" => chart.AddChord("M", nodes, flow),
            "funnel" => chart.AddFunnel("M", points.AsEnumerable().Reverse()),
            "pyramid" => chart.AddPyramid("M", points),
            "word" => chart.AddWordCloud("M", new[] { new ChartWordCloudItem("A", 30), new ChartWordCloudItem("B", 60) }),
            "map" => chart.AddDottedMap("M", new[] { new ChartMapPoint("A", 20, 50) }),
            "schedule" => chart.AddGanttTask("M", 1, 30, .3),
            "packed" => chart.AddGanttLane("M", new[] { new ChartGanttLaneItem(1, 30, "work", "A") }),
            _ => throw new ArgumentOutOfRangeException(nameof(family))
        };
    }

    private static ChartValueContext CaptureContext(Chart chart) {
        ChartValueContext? captured = null;
        chart.Series[0].WithDataLabelFormatter(context => { captured = context; return null; });
        _ = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        return Assert.IsType<ChartValueContext>(captured);
    }
}
