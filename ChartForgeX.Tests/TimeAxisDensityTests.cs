using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TimeAxisDensityTests {
    private static readonly DateTime Start = new(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(640, 7, false, 3, VisualThemeMode.Light)]
    [InlineData(640, 7, false, 3, VisualThemeMode.Dark)]
    [InlineData(800, 19, true, 5, VisualThemeMode.Light)]
    [InlineData(800, 19, true, 5, VisualThemeMode.Dark)]
    public void MeasuredTimeScaleKeepsDailyOrIntermediateDayTicksWhenTheyFit(int width, int days, bool authoredCount, int minimumLabels, VisualThemeMode mode) {
        var first = authoredCount ? new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc) : Start;
        var chart = Create(width).WithThemeMode(mode).AddLine("Readings", Enumerable.Range(0, days).Select(day => new ChartPoint(first.AddDays(day), 40 + day % 5)));
        if (authoredCount) chart.Options.XAxis.TickCount = 6;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        ScatterBubbleColorRenderingTests.Capture(prepared, $"time-density-{width}-{days}-{mode}");
        var captions = Captions(prepared);
        Assert.InRange(captions.Length, minimumLabels, authoredCount ? 6 : days);
        AssertDateLabels(prepared);
        AssertAnchoredAndPainted(prepared);
    }

    [Theory]
    [InlineData(false, VisualThemeMode.Light)]
    [InlineData(false, VisualThemeMode.Dark)]
    [InlineData(true, VisualThemeMode.Light)]
    [InlineData(true, VisualThemeMode.Dark)]
    public void NarrowDateScaleKeepsCalendarBoundariesInsteadOfPaddedEndpointTimes(bool bars, VisualThemeMode mode) {
        var chart = Create(360).WithThemeMode(mode).WithPadding(64, 14, 20, 46);
        var points = Enumerable.Range(0, bars ? 15 : 30).Select(day => new ChartPoint(Start.AddDays(day * (bars ? 2 : 1)), 40 + day % 5));
        if (bars) chart.AddBar("Readings", points); else chart.AddLine("Readings", points);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        ScatterBubbleColorRenderingTests.Capture(prepared, $"time-narrow-{(bars ? "bar" : "line")}-{mode}");
        AssertDateLabels(prepared);
        Assert.InRange(Captions(prepared).Length, 2, 8);
        AssertAnchoredAndPainted(prepared);
        if (bars) {
            var marks = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "bar").ToArray();
            Assert.NotEmpty(marks);
            foreach (var caption in Captions(prepared)) {
                var center = caption.X + caption.Text.Metrics.Width / 2;
                Assert.Contains(marks, mark => Math.Abs(mark.Bounds.Left + mark.Bounds.Width / 2 - center) < .5);
            }
        }
    }

    [Fact]
    public void AuthoredTimeTickCountRemainsAnUpperBoundAndSurvivesDetachedConfiguration() {
        var chart = Create(640)
            .AddLine("Readings", Enumerable.Range(0, 7).Select(day => new ChartPoint(Start.AddDays(day), 40 + day % 5)));
        var automaticLabels = Captions(chart.Prepare(VisualExportRequest.ForChart(chart).Context)).Select(node => node.Text.Lines.Single().Text).ToArray();
        chart.ConfigureXAxis(axis => axis.TickCount = 2);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(2, Captions(prepared).Length);
        AssertDateLabels(prepared);
        var svg = prepared.ToSvg();
        chart.ConfigureXAxis(axis => axis.WithAutomaticTickCount());
        Assert.Equal(automaticLabels, Captions(chart.Prepare(VisualExportRequest.ForChart(chart).Context)).Select(node => node.Text.Lines.Single().Text));
        Assert.Equal(svg, prepared.ToSvg());
    }

    [Fact]
    public void TwoWeekIntervalKeepsWholeCalendarDatesAcrossMonthBoundaries() {
        var axis = new ChartAxis().WithTimeScale(); axis.TickCount = 3;
        var ticks = ChartTicks.GenerateInside(axis, Start.AddHours(-8).ToOADate(), Start.AddDays(29).AddHours(8).ToOADate());
        Assert.Equal(new[] { Start, Start.AddDays(14), Start.AddDays(28) }, ticks.Select(DateTime.FromOADate));
    }

    private static Chart Create(int width) => Chart.Create().WithSize(width, 320).WithHeader(false).WithLegend(false).WithXAxisTimeScale()
        .WithPngFont(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"));

    private static VisualSceneText[] Captions(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneText>()
        .Where(node => node.Role == "axis-x-label").ToArray();

    private static void AssertDateLabels(PreparedVisual prepared) {
        Assert.NotEmpty(Captions(prepared));
        Assert.All(Captions(prepared), caption => {
            var text = caption.Text.Lines.Single().Text;
            Assert.DoesNotContain(':', text);
            Assert.DoesNotContain('…', text);
            Assert.True(DateTime.TryParseExact(text, new[] { "yyyy-MM-dd", "MM-dd" }, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out _), "A multi-day axis must label dates: " + text);
        });
    }

    private static void AssertAnchoredAndPainted(PreparedVisual prepared) {
        var facts = prepared.Regions.Where(region => region.Role == "axis-x-label").ToArray();
        var captions = Captions(prepared);
        Assert.Equal(captions.Length, facts.Length);
        var svg = XDocument.Parse(prepared.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "axis-x-label").ToArray();
        Assert.Equal(captions.Select(caption => caption.Text.Lines.Single().Text), svg.Select(node => node.Value));
        for (var index = 0; index < captions.Length; index++) {
            var caption = captions[index];
            Assert.InRange(Math.Abs(caption.X + caption.Text.Metrics.Width / 2 - facts[index].Bounds.Left), 0, .5);
            if (index > 0) Assert.True(caption.X >= captions[index - 1].X + captions[index - 1].Text.Metrics.Width);
        }
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.axis-label-overflow");
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        var scene = prepared.Scene;
        var without = VisualSceneRasterRenderer.Render(new VisualScene(scene.Size, scene.Nodes.Where(node => node.Role != "axis-x-label").ToArray(),
            scene.Diagnostics, scene.Regions), supersampling: 1);
        foreach (var caption in captions) {
            var left = Math.Max(0, (int)Math.Floor(caption.X)); var right = Math.Min(image.Width, (int)Math.Ceiling(caption.X + caption.Text.Metrics.Width));
            var top = Math.Max(0, (int)Math.Floor(caption.Baseline - caption.Text.Ascent));
            var bottom = Math.Min(image.Height, (int)Math.Ceiling(caption.Baseline - caption.Text.Ascent + caption.Text.Metrics.Height));
            var ink = false;
            for (var y = top; y < bottom && !ink; y++) for (var x = left; x < right && !ink; x++)
                for (var channel = 0; channel < 4; channel++)
                    ink |= image.Pixels[(y * image.Width + x) * 4 + channel] != without.Pixels[(y * image.Width + x) * 4 + channel];
            Assert.True(ink, "Every SVG date caption must paint in the same prepared location in native PNG.");
        }
    }
}
