using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Automatic bar captions read as one set: same side, same unit, close to their bars.</summary>
public sealed class BarValueCaptionTests {
    private static (VisualSceneText Label, ChartRect Bar)[] Captions(params double[] values) {
        var chart = Chart.Create().WithSize(360, 220).WithTheme(VisualTheme.Graphite()).WithTitle("Availability by site").WithDataLabels()
            .WithXLabels("Warsaw", "Krakow", "Frankfurt", "Gdansk")
            .AddBar("Availability", values.Select((value, index) => new ChartPoint(index + 1, value)));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        return prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "data-label")
            .Select(label => (label, prepared.Regions.Single(region => region.Id + "-label" == label.Id).Bounds)).ToArray();
    }

    private static double Bottom(VisualSceneText text) => text.Baseline - text.Text.Ascent + text.Text.Metrics.Height;

    [Fact]
    public void Prepare_TallestBarAtTheAxisMaximum_KeepsItsCaptionAboveLikeTheOthers() {
        var captions = Captions(100, 64, 82, 45);

        Assert.Equal(4, captions.Length);
        Assert.All(captions, caption => Assert.True(Bottom(caption.Label) <= caption.Bar.Top,
            $"Caption '{caption.Label.Text.Lines.Single().Text}' must sit above its bar, not inside it."));
    }

    [Fact]
    public void Prepare_BarCaptions_StayCloseToTheirBarEnds() {
        var captions = Captions(98.6, 61, 77, 40);

        Assert.All(captions, caption => Assert.InRange(caption.Bar.Top - Bottom(caption.Label), 0, 8));
    }

    [Fact]
    public void Prepare_SeriesWithLargeValues_UsesOneCompactUnitForEveryCaption() {
        var captions = Captions(12480, 9800, 11990, 6400);

        Assert.Equal(new[] { "12.5k", "9.8k", "12.0k", "6.4k" }, captions.Select(caption => caption.Label.Text.Lines.Single().Text).ToArray());
    }

    [Fact]
    public void Prepare_SeriesBelowTenThousand_KeepsFullNumbers() {
        var captions = Captions(9800, 6400, 120, 4);

        Assert.Equal(new[] { "9,800", "6,400", "120", "4" }, captions.Select(caption => caption.Label.Text.Lines.Single().Text).ToArray());
    }

    [Fact]
    public void Prepare_WaterfallOpeningBalanceNearTheAxisMaximum_KeepsItsCaptionAboveLikeTheTotals() {
        var chart = Chart.Create().WithSize(800, 440).WithTheme(VisualTheme.Graphite()).WithDataLabels()
            .WithXLabels("Purchased", "Unassigned", "Inactive", "In use")
            .AddSeries(ChartSeries.CreateWaterfall(new ChartSeriesId("licences"), "Licences", new[] {
                ChartWaterfallItem.OpeningBalance(1, 1200), ChartWaterfallItem.Delta(2, -86), ChartWaterfallItem.Delta(3, -142), ChartWaterfallItem.Total(4)
            }));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var levels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "data-label")
            .Select(label => (Label: label, Bar: prepared.Regions.Single(region => region.Id + "-label" == label.Id)))
            .Where(caption => caption.Label.Text.Lines.Single().Text is "1,200" or "972").ToArray();

        Assert.Equal(2, levels.Length);
        Assert.All(levels, caption => Assert.True(Bottom(caption.Label) <= caption.Bar.Bounds.Top,
            $"Level caption '{caption.Label.Text.Lines.Single().Text}' must sit above its bar."));
    }

    [Fact]
    public void SeriesCaptionFormat_ValueFarBelowTheUnit_KeepsItsOwnCaption() {
        var format = ChartNumericFormatter.SeriesCaptionFormat(Chart.Create().Options, new[] { 12480d, 40 })!;

        Assert.Equal("12.5k", format(12480));
        Assert.Equal("40", format(40));
    }

    [Fact]
    public void SeriesCaptionFormat_AuthoredFormat_IsLeftAlone() {
        var options = Chart.Create().Options; options.ValueFormat = ChartValueFormat.Number("#,0");

        Assert.Null(ChartNumericFormatter.SeriesCaptionFormat(options, new[] { 12480d, 9800 }));
    }
}
