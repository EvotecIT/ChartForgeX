using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class LegendControlTests {
    private static readonly ChartColor Target = ChartColor.FromHex("#D97706");

    [Fact]
    public void ReferenceLineItemFollowsTheSeriesAsADashedKeyWithoutASeriesIdentity() {
        var plain = Revenue().AddHorizontalLine(1_200_000, color: Target);
        var chart = Revenue().AddHorizontalLine(1_200_000, color: Target)
            .AddLegendItem(ChartLegendItem.Line("Target", Target, ChartLegendItem.ReferenceDash));
        var entries = Entries(chart);
        // One series alone hides its legend; the reference entry makes the legend worth showing.
        Assert.Empty(Entries(plain));
        Assert.Equal(new[] { "Revenue", "Target" }, entries.Select(Label));
        var item = entries[1];
        Assert.Equal("true", (string?)item.Attribute("data-cfx-legend-custom"));
        Assert.Equal("", (string?)item.Attribute("data-cfx-series-key"));
        Assert.DoesNotContain("series", (string?)item.Attribute("data-cfx-source-id"));
        var line = Assert.Single(item.Descendants(), e => (string?)e.Attribute("data-cfx-role") == "legend-line");
        Assert.Equal(Target.ToCss(), (string?)line.Attribute("stroke"));
        Assert.Equal("6 5", (string?)line.Attribute("stroke-dasharray"));
        Assert.False(plain.WithLegend(true).ToPng().SequenceEqual(chart.WithLegend(true).ToPng()));
    }

    [Fact]
    public void FormatterRewritesEntryTextFromSeriesValuesAndNullKeepsTheDefault() {
        var chart = Revenue().AddLine("Cost", new[] { new ChartPoint(1, 600_000), new ChartPoint(2, 700_000) })
            .AddLegendItem(ChartLegendItem.Band("Tolerance", Target))
            .WithValueFormat(ChartValueFormat.Compact())
            .WithLegendFormatter(item => item.SeriesIndex == 0 ? item.Label + " — " + item.FormatValue(item.Total) + " total" : null);
        var entries = Entries(chart);
        Assert.Equal(new[] { "Revenue — 2.1M total", "Cost", "Tolerance" }, entries.Select(Label));
        Assert.Equal("Revenue — 2.1M total", (string?)entries[0].Attribute("data-cfx-description"));
    }

    [Fact]
    public void KeyShapeAndSizeReplaceTheFamilyKeyAndAnItemKeepsItsOwnSize() {
        var chart = Revenue().AddBar("Orders", new[] { new ChartPoint(1, 400_000), new ChartPoint(2, 500_000) })
            .WithLegendKey(ChartLegendKeyShape.Circle, 16)
            .AddLegendItem(ChartLegendItem.Marker("Review", Target, ChartMarkerShape.Diamond).WithKeySize(8));
        var entries = Entries(chart);
        foreach (var entry in entries.Take(2)) {
            var dot = Assert.Single(entry.Descendants(), e => (string?)e.Attribute("data-cfx-role") == "legend-swatch");
            Assert.Equal("ellipse", dot.Name.LocalName);
            Assert.Equal(6.4, (double)dot.Attribute("rx")!, 3);
        }
        var marker = Assert.Single(entries[2].Descendants(), e => (string?)e.Attribute("data-cfx-role") == "legend-swatch");
        Assert.Equal("path", marker.Name.LocalName);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.WithLegendKey(ChartLegendKeyShape.Square, 40));
    }

    [Fact]
    public void NativeReflowKeepsCustomItemsAndRejectsTheFormatterCallback() {
        var chart = Chart.Create().WithSize(480, 300)
            .AddSeries(new ChartSeries(new ChartSeriesId("revenue"), "Revenue", ChartSeriesKind.Line, new[] { new ChartPoint(1, 4), new ChartPoint(2, 6) }))
            .AddSeries(new ChartSeries(new ChartSeriesId("cost"), "Cost", ChartSeriesKind.Line, new[] { new ChartPoint(1, 2), new ChartPoint(2, 3) }))
            .AddLegendItem(ChartLegendItem.Line("Target", Target, ChartLegendItem.ReferenceDash));
        var renderer = new HtmlInteractiveChartRenderer();
        var view = new ChartVisibilityView(new[] { ChartVisibilityTarget.Series(new ChartSeriesId("cost")) }, ChartVisibilityLayout.Reflow);
        var svg = XDocument.Parse(renderer.PrepareVisibilitySection(chart, view).PreparedVisual.ToSvg());
        Assert.Contains("Target", svg.Descendants().Where(IsEntry).Select(Label));
        chart.WithLegendFormatter(item => item.Label + "!");
        Assert.Throws<NotSupportedException>(() => renderer.PrepareVisibilitySection(chart, view));
    }

    private static Chart Revenue() => Chart.Create().WithSize(560, 320)
        .AddLine("Revenue", new[] { new ChartPoint(1, 1_000_000), new ChartPoint(2, 1_100_000) });

    private static XElement[] Entries(Chart chart) => XDocument.Parse(chart.ToSvg()).Descendants().Where(IsEntry).ToArray();

    private static bool IsEntry(XElement e) => (string?)e.Attribute("data-cfx-role") == "legend-entry";

    private static string Label(XElement entry) =>
        entry.Descendants().Single(e => e.Name.LocalName == "text" && (string?)e.Parent?.Attribute("data-cfx-role") == "legend-label").Value;
}
