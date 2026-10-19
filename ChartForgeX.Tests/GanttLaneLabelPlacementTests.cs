using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GanttLaneLabelPlacementTests {
    private static readonly DateTime Start = new(2025, 11, 3, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(390, 12, VisualThemeMode.Light)]
    [InlineData(390, 12, VisualThemeMode.Dark)]
    [InlineData(760, 12, VisualThemeMode.Light)]
    [InlineData(760, 12, VisualThemeMode.Dark)]
    [InlineData(390, 8, VisualThemeMode.Light)]
    [InlineData(390, 8, VisualThemeMode.Dark)]
    [InlineData(760, 8, VisualThemeMode.Light)]
    [InlineData(760, 8, VisualThemeMode.Dark)]
    public void PackedCaptionsClearOtherItemsCaptionsAndGroupBandsWithoutLosingSourceFacts(int width, double fontSize, VisualThemeMode mode) {
        var chart = DenseChart(width, fontSize, mode);
        var original = chart.Series.Select(series => series.GanttLaneItems.ToArray()).ToArray();
        var prepared = Prepare(chart); var svg = XDocument.Parse(prepared.ToSvg());
        var source = svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "gantt-lane-item").ToArray();
        Assert.Equal(16, source.Length);
        Assert.Equal(16, source.Select(element => (string?)element.Attribute("data-cfx-observation-id")).Distinct().Count());
        for (var lane = 0; lane < 2; lane++) {
            Assert.Equal(original[lane], chart.Series[lane].GanttLaneItems);
            for (var point = 0; point < 8; point++) {
                var item = original[lane][point]; var id = $"series-{lane}-point-{point}";
                var mark = Assert.Single(source, element => (string?)element.Attribute("data-cfx-series") == lane.ToString(CultureInfo.InvariantCulture)
                    && (string?)element.Attribute("data-cfx-point") == point.ToString(CultureInfo.InvariantCulture));
                Assert.Equal($"gantt-{lane}", (string?)mark.Attribute("data-cfx-series-key"));
                Assert.Equal($"lane-{lane}-batch-{point}", (string?)mark.Attribute("data-cfx-observation-id"));
                Assert.Equal(item.Start, Number(mark, "data-cfx-start")); Assert.Equal(item.End!.Value, Number(mark, "data-cfx-end"));
                Assert.Equal(item.Category, (string?)mark.Attribute("data-cfx-status")); Assert.Equal("false", (string?)mark.Attribute("data-cfx-open"));
                Assert.Contains(item.Label!, mark.Attribute("aria-label")!.Value); Assert.Contains(item.Detail!, mark.Attribute("aria-label")!.Value);
                Assert.Equal(item.Label, Assert.Single(prepared.Regions, region => region.Id == id + "-label").Label);
            }
        }

        var plot = Assert.Single(prepared.Regions, region => region.Role == "schedule-plot").Bounds;
        var marks = prepared.Regions.Where(region => region.Role == "gantt-lane-item").ToArray();
        var groups = prepared.Regions.Where(region => region.Role == "gantt-lane-group").ToArray();
        var footprints = NumericRadialAxisTextTests.Footprints(prepared.Scene);
        var captions = footprints.Where(item => item.Text.Role == "data-label").ToArray();
        Assert.NotEmpty(captions);
        foreach (var caption in captions) {
            Assert.Equal(fontSize, caption.Text.Text.Style.FontSize);
            Assert.True(LabelPlacementService.Contains(plot, caption.Bounds), "A painted item caption must fit the schedule plot.");
            var captionId = caption.Text.Id!; var ownerId = captionId.Substring(0, captionId.Length - "-label".Length);
            var own = Assert.Single(marks, mark => mark.Id == ownerId);
            if (Overlaps(caption.Bounds, own.Bounds)) Assert.True(LabelPlacementService.Contains(own.Bounds, caption.Bounds));
            foreach (var other in marks.Where(mark => mark.Id != ownerId)) Assert.False(Overlaps(caption.Bounds, other.Bounds), "An item caption must clear every other packed bar.");
            foreach (var other in captions.Where(other => !ReferenceEquals(other.Text, caption.Text))) Assert.False(Overlaps(caption.Bounds, other.Bounds), "Packed item captions must not overprint each other.");
            foreach (var group in groups) Assert.False(Overlaps(caption.Bounds, group.Bounds), "An item caption must clear the complete visible group band.");
            foreach (var group in footprints.Where(item => item.Text.Role == "gantt-lane-group")) Assert.False(Overlaps(caption.Bounds, group.Bounds));
            var serialized = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "data-label"
                && (string?)element.Attribute("data-cfx-source-id") == caption.Text.Id);
            Assert.Single(serialized.DescendantsAndSelf(), element => element.Name.LocalName == "text");
            Assert.Contains(serialized.Ancestors(), element => (string?)element.Attribute("data-cfx-role") == "gantt-lane-item"
                && (string?)element.Attribute("data-cfx-source-id") == ownerId);
        }
        var image = PngReader.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.Equal(width, image.Width); Assert.Equal(width == 390 ? 360 : 440, image.Height);
        if (mode == VisualThemeMode.Light && (width == 390 && fontSize == 12 || width == 760 && fontSize == 8)) {
            var caption = captions[0];
            Assert.True(HasNativeInk(prepared, image, caption.Bounds, caption.Text), "A retained packed caption must paint native raster ink.");
        }
    }

    [Fact]
    public void InfeasibleLargeCaptionsDoNotEnlargePackedRowsOrLoseTheirItems() {
        var chart = DenseChart(390, 96, VisualThemeMode.Light);
        var original = chart.Series.Select(series => series.GanttLaneItems.ToArray()).ToArray();
        var prepared = Prepare(chart);
        Assert.DoesNotContain(prepared.Scene.Nodes.OfType<VisualSceneText>(), text => text.Role == "data-label");
        Assert.Equal(96, chart.Options.DataLabelStyle.FontSize);
        var marks = prepared.Regions.Where(region => region.Role == "gantt-lane-item").ToArray();
        Assert.Equal(16, marks.Length);
        var plot = Assert.Single(prepared.Regions, region => region.Role == "schedule-plot").Bounds;
        foreach (var mark in marks) {
            Assert.True(LabelPlacementService.Contains(plot, mark.Bounds));
            foreach (var other in marks.Where(other => other.Id != mark.Id)) Assert.False(Overlaps(mark.Bounds, other.Bounds));
        }
        for (var lane = 0; lane < 2; lane++) Assert.Equal(original[lane], chart.Series[lane].GanttLaneItems);
        var unlabelled = DenseChart(390, 96, VisualThemeMode.Light);
        foreach (var series in unlabelled.Series) for (var point = 0; point < series.GanttLaneItems.Count; point++) {
            var item = series.GanttLaneItems[point];
            series.UpdateGanttLaneItem(point, new ChartGanttLaneItem(item.Start, item.End, item.Category, detail: item.Detail));
        }
        var unlabelledMarks = Prepare(unlabelled).Regions.Where(region => region.Role == "gantt-lane-item").ToArray();
        Assert.Equal(marks.Select(mark => mark.Bounds), unlabelledMarks.Select(mark => mark.Bounds));
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(null, null, null, 16)]
    [InlineData(false, null, null, 0)]
    [InlineData(true, false, false, 0)]
    [InlineData(false, true, null, 8)]
    [InlineData(true, false, null, 8)]
    public void PackedCaptionVisibilityHonorsChartDefaultsAndSeriesOverridesWithoutHiddenBandGrowth(
        bool? chartVisible, bool? firstSeriesVisible, bool? secondSeriesVisible, int expectedCaptions) {
        var chart = DenseChart(390, 12, VisualThemeMode.Light);
        chart.Options.ShowDataLabels = chartVisible;
        chart.Series[0].ShowDataLabels = firstSeriesVisible; chart.Series[1].ShowDataLabels = secondSeriesVisible;
        var original = chart.Series.Select(series => series.GanttLaneItems.ToArray()).ToArray();
        var prepared = Prepare(chart); var svg = XDocument.Parse(prepared.ToSvg());
        var captions = NumericRadialAxisTextTests.Footprints(prepared.Scene).Where(item => item.Text.Role == "data-label").ToArray();
        Assert.Equal(expectedCaptions, captions.Length);
        for (var lane = 0; lane < 2; lane++) {
            var visible = chart.Series[lane].ShowDataLabels ?? (chartVisible ?? true);
            Assert.Equal(original[lane], chart.Series[lane].GanttLaneItems);
            for (var point = 0; point < 8; point++) {
                var id = $"series-{lane}-point-{point}";
                var mark = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "gantt-lane-item"
                    && (string?)element.Attribute("data-cfx-source-id") == id);
                Assert.Contains(original[lane][point].Label!, mark.Attribute("aria-label")!.Value);
                Assert.Equal(original[lane][point].Start, Number(mark, "data-cfx-start"));
                Assert.Equal(original[lane][point].End!.Value, Number(mark, "data-cfx-end"));
                if (!visible) Assert.DoesNotContain(captions, caption => caption.Text.Id == id + "-label");
            }
        }
        Assert.Equal(16, prepared.Regions.Count(region => region.Role == "gantt-lane-item"));
        if (expectedCaptions == 0) {
            Assert.DoesNotContain(prepared.Regions, region => region.Role == "data-label");
            var unlabelled = DenseChart(390, 12, VisualThemeMode.Light);
            foreach (var series in unlabelled.Series) for (var point = 0; point < series.GanttLaneItems.Count; point++) {
                var item = series.GanttLaneItems[point];
                series.UpdateGanttLaneItem(point, new ChartGanttLaneItem(item.Start, item.End, item.Category, detail: item.Detail));
            }
            Assert.Equal(Prepare(unlabelled).Regions.Where(region => region.Role == "gantt-lane-item").Select(region => region.Bounds),
                prepared.Regions.Where(region => region.Role == "gantt-lane-item").Select(region => region.Bounds));
        }
        var image = PngReader.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.Equal(390, image.Width); Assert.Equal(360, image.Height);
    }

    [Theory]
    [InlineData(ChartDataLabelPlacement.Right)]
    [InlineData(ChartDataLabelPlacement.Above)]
    [InlineData(ChartDataLabelPlacement.Inside)]
    public void ExplicitPlacementKeepsItsLocalSideAndAuthoredSmallFontAndColor(ChartDataLabelPlacement placement) {
        var chart = Chart.Create().WithTheme(VisualTheme.Graphite()).WithSize(760, 400).WithHeader(false).WithLegend(false)
            .WithDataLabels().WithStateCategories(States()).AddGanttLane("Delivery", new[] {
                new ChartGanttLaneItem(Start.AddHours(1), Start.AddHours(3), "up", "Batch 01", "Complete original detail")
            });
        chart.Options.XAxis.WithBounds(Start.AddHours(-1).ToOADate(), Start.AddHours(10).ToOADate());
        chart.Series[0].WithDataLabelPlacement(placement).ConfigurePointDataLabelStyle(0, style => style.WithFontSize(8).WithColor("#6a1b9a"));
        RoundedRadialSeriesTests.UseFixtureFont(chart);
        var prepared = Prepare(chart);
        var caption = Assert.Single(NumericRadialAxisTextTests.Footprints(prepared.Scene), item => item.Text.Role == "data-label");
        var mark = Assert.Single(prepared.Regions, region => region.Role == "gantt-lane-item").Bounds;
        Assert.Equal("Batch 01", string.Join("\n", caption.Text.Text.Lines.Select(line => line.Text)));
        Assert.Equal(8, caption.Text.Text.Style.FontSize); Assert.Equal(ChartColor.FromHex("#6a1b9a"), caption.Text.Text.Style.Color);
        if (placement == ChartDataLabelPlacement.Right) Assert.True(caption.Bounds.Left > mark.Right);
        else if (placement == ChartDataLabelPlacement.Above) Assert.True(caption.Bounds.Bottom < mark.Top);
        else Assert.True(LabelPlacementService.Contains(mark, caption.Bounds));
        var image = PngReader.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.True(HasNativeInk(prepared, image, caption.Bounds, caption.Text));
    }

    private static Chart DenseChart(int width, double fontSize, VisualThemeMode mode) {
        var chart = Chart.Create().WithTheme(VisualTheme.Graphite(), mode).WithSize(width, width == 390 ? 360 : 440)
            .WithTitle("Overlapping operating batches").WithDataLabels().WithStateCategories(States());
        chart.Options.DataLabelStyle.WithFontSize(fontSize);
        for (var lane = 0; lane < 2; lane++) {
            var items = Enumerable.Range(0, 8).Select(index => new ChartGanttLaneItem(Start.AddMinutes(index * 30 + lane * 15),
                Start.AddMinutes(index * 30 + lane * 15 + 90), new[] { "up", "degraded", "down" }[index % 3],
                "Regional processing batch " + (index + 1).ToString("00", CultureInfo.InvariantCulture), "Complete authored batch detail " + index)).ToArray();
            chart.AddGanttLane(lane == 0 ? "Primary delivery and recovery operations" : "Secondary delivery and validation operations",
                items, group: "Production workflow window", summary: "8 authored batches", id: new ChartSeriesId("gantt-" + lane));
            chart.Series[lane].WithObservationIds(Enumerable.Range(0, 8).Select(index => new ChartObservationId($"lane-{lane}-batch-{index}")));
        }
        RoundedRadialSeriesTests.UseFixtureFont(chart);
        return chart;
    }

    private static ChartStateCategory[] States() => new[] {
        new ChartStateCategory("up", "Operational with extended monitoring context", ChartColor.FromHex("#168257")),
        new ChartStateCategory("degraded", "Degraded while checks remain in progress", ChartColor.FromHex("#d6a423")),
        new ChartStateCategory("down", "Unavailable pending operator recovery", ChartColor.FromHex("#c33d3d"))
    };
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static double Number(XElement element, string name) => double.Parse(element.Attribute(name)!.Value, CultureInfo.InvariantCulture);
    private static bool Overlaps(ChartRect first, ChartRect second) => first.Left < second.Right - .001 && second.Left < first.Right - .001
        && first.Top < second.Bottom - .001 && second.Top < first.Bottom - .001;

    private static bool HasNativeInk(PreparedVisual prepared, RgbaImage image, ChartRect bounds, VisualSceneText caption) {
        var scene = prepared.Scene;
        var without = VisualSceneRasterRenderer.Render(new VisualScene(scene.Size,
            scene.Nodes.Where(node => !ReferenceEquals(node, caption)).ToArray(), scene.Diagnostics, scene.Regions), supersampling: 1);
        for (var y = Math.Max(0, (int)Math.Floor(bounds.Top)); y < Math.Min(image.Height, (int)Math.Ceiling(bounds.Bottom)); y++)
            for (var x = Math.Max(0, (int)Math.Floor(bounds.Left)); x < Math.Min(image.Width, (int)Math.Ceiling(bounds.Right)); x++) {
                var offset = (y * image.Width + x) * 4;
                for (var channel = 0; channel < 4; channel++) if (image.Pixels[offset + channel] != without.Pixels[offset + channel]) return true;
            }
        return false;
    }
}
