using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects cleared timeline rows without changing retained series identities or inventing schedule facts.</summary>
public sealed class ScheduleRangeLifecycleTests {
    private static readonly ChartColor Paint = ChartColor.FromHex("#B02D64");

    [Theory]
    [InlineData(0, VisualThemeMode.Light)]
    [InlineData(1, VisualThemeMode.Light)]
    [InlineData(2, VisualThemeMode.Light)]
    [InlineData(0, VisualThemeMode.Dark)]
    [InlineData(1, VisualThemeMode.Dark)]
    [InlineData(2, VisualThemeMode.Dark)]
    public void EmptyTimelinePeersKeepSourceIdentitiesAndPackOnlyObservedRows(int emptyIndex, VisualThemeMode mode) {
        var chart = Chart.Create().WithSize(520, 320).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithBarStyle(ChartBarStyle.Flat);
        for (var index = 0; index < 3; index++) {
            chart.AddTimelineRange("Task " + index, index + 1, index + 4, Paint);
            chart.Series[index].WithObservationId(0, new ChartObservationId("task-" + index));
        }
        var series = chart.Series.ToArray();
        var before = chart.Prepare(Context(mode)); var svg = before.ToSvg(); var png = before.ToPng(Raster());
        series[emptyIndex].Points.Clear();
        var sparse = chart.Prepare(Context(mode)); var items = Items(sparse);
        Assert.Equal(2, items.Length);
        Assert.DoesNotContain(items, item => item.Attribute("data-cfx-series")?.Value == emptyIndex.ToString());
        foreach (var index in Enumerable.Range(0, 3).Where(index => index != emptyIndex)) {
            var item = Assert.Single(items, item => item.Attribute("data-cfx-series")?.Value == index.ToString());
            Assert.Equal("task-" + index, item.Attribute("data-cfx-observation-id")?.Value);
            Assert.Equal((index + 1).ToString(), item.Attribute("data-cfx-start")?.Value);
            Assert.Equal((index + 4).ToString(), item.Attribute("data-cfx-end")?.Value);
        }
        var marks = sparse.Regions.Where(region => region.Role == "timeline-item").OrderBy(region => region.Bounds.Top).ToArray();
        Assert.Equal(2, marks.Length); Assert.Equal(marks[0].Bounds.Height, marks[1].Bounds.Height, 8);
        Assert.True(marks[0].Bounds.Bottom < marks[1].Bounds.Top);
        foreach (var mark in marks) AssertNativePaint(sparse, mark.Bounds);
        series[emptyIndex].Points.Add(new ChartPoint(2, 7));
        var restored = chart.Prepare(Context(mode)); Assert.Equal(3, Items(restored).Length);
        Assert.Equal("2", Items(restored).Single(item => item.Attribute("data-cfx-series")?.Value == emptyIndex.ToString()).Attribute("data-cfx-start")?.Value);
        for (var index = 0; index < 3; index++) Assert.Same(series[index], chart.Series[index]);
        Assert.Equal(svg, before.ToSvg()); Assert.Equal(png, before.ToPng(Raster()));
        foreach (var item in series) item.Points.Clear();
        var empty = chart.Prepare(Context(mode));
        Assert.Empty(Items(empty)); Assert.Contains(empty.Diagnostics, item => item.Code == "schedule.no-data");
        Assert.NotEmpty(empty.ToSvg()); Assert.NotEmpty(empty.ToPng(Raster()));
        series[emptyIndex].Points.Add(new ChartPoint(4, 4));
        var instant = chart.Prepare(Context(mode)); var singleton = Assert.Single(Items(instant));
        Assert.Equal("4", singleton.Attribute("data-cfx-start")?.Value); Assert.Equal("4", singleton.Attribute("data-cfx-end")?.Value);
        Assert.True(Assert.Single(instant.Regions, region => region.Role == "timeline-item").Bounds.Width >= 1);
        Capture(sparse, $"timeline-cleared-{emptyIndex}-{mode}");
        Capture(restored, $"timeline-refilled-{emptyIndex}-{mode}");
        Capture(empty, $"timeline-empty-{emptyIndex}-{mode}");
        Capture(instant, $"timeline-singleton-{emptyIndex}-{mode}");
    }

    private static XElement[] Items(PreparedVisual prepared) => XDocument.Parse(prepared.ToSvg()).Descendants()
        .Where(node => node.Attribute("data-cfx-role")?.Value == "timeline-item").ToArray();
    private static VisualRenderContext Context(VisualThemeMode mode) => new(new VisualLayoutOptions(new VisualSize(520, 320)),
        VisualTheme.Graphite(), mode, frame: new VisualFrame(showLegend: false));
    private static VisualRenderOptions Raster() => new(supersampling: 1);
    private static void AssertNativePaint(PreparedVisual prepared, ChartRect bounds) {
        var image = prepared.ToRgba(Raster());
        var offset = ((int)(bounds.Top + bounds.Height / 2) * image.Width + (int)(bounds.Left + bounds.Width / 2)) * 4;
        Assert.Equal(Paint.R, image.Pixels[offset]); Assert.Equal(Paint.G, image.Pixels[offset + 1]); Assert.Equal(Paint.B, image.Pixels[offset + 2]);
    }
    private static void Capture(PreparedVisual prepared, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(directory, name + "-native.png"), prepared.ToPng(Raster()));
    }
}
