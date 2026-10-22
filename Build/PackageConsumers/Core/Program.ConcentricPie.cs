using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void ConcentricPieBands() {
        var inner = new ChartSeries(new ChartSeriesId("prior"), "Prior", ChartSeriesKind.Donut,
            new[] { new ChartPoint(1, 3), new ChartPoint(2, 1) }).WithRadialBand(new ChartRadialBand(.2, .5));
        inner.DataLabelPlacement = ChartDataLabelPlacement.Inside;
        var outer = new ChartSeries(new ChartSeriesId("current"), "Current", ChartSeriesKind.Pie,
            new[] { new ChartPoint(1, 1), new ChartPoint(2, 1) }).WithRadialBand(new ChartRadialBand(.65, 1));
        var chart = Chart.Create().WithSize(520, 340).WithLegend(false).AddSeries(inner).AddSeries(outer);
        var prepared = chart.Prepare(DepthContext(chart, showLegend: false));
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        var points = XDocument.Parse(svg).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "radial-point").ToArray();
        PackageAssertions.Require(points.Length == 4 && points.Select(node => (string?)node.Attribute("data-cfx-series-key")).Distinct().Count() == 2,
            "Packed concentric Pie/Donut preparation lost an independent ring or its stable series identity.");
        PackageAssertions.Require(points.Where(node => (string?)node.Attribute("data-cfx-series-key") == "prior")
            .Any(node => (string?)node.Attribute("data-cfx-percent") == "0.75"), "Packed concentric percentages borrowed another ring's total.");
        var envelope = chart.ToVisualArtifact("rings").ToInterchangeEnvelope();
        PackageAssertions.Require(envelope.Extensions["chart.series.0.radial.innerRatio"] == "0.2"
            && envelope.Extensions["chart.series.1.radial.outerRatio"] == "1", "Packed common interchange lost explicit band ratios.");
        inner.RadialBand = new ChartRadialBand(.1, .4); outer.Points[0] = new ChartPoint(1, 5);
        PackageAssertions.Require(prepared.ToSvg() == svg && prepared.ToPng().SequenceEqual(png), "A source band edit changed an existing prepared export.");
        inner.ShowDataLabels = true; inner.DataLabelPlacement = ChartDataLabelPlacement.Outside;
        var rejected = false;
        try { chart.Prepare(DepthContext(chart, showLegend: false)); }
        catch (InvalidOperationException error) { rejected = error.Message.IndexOf("outermost", StringComparison.Ordinal) >= 0; }
        PackageAssertions.Require(rejected, "Packed inner external labels were silently painted across another ring.");
        inner.ShowDataLabels = false;
        var ordinary = Chart.Create().WithSize(520, 340).WithHeader(false).WithLegend(false)
            .AddDonut("Ordinary", new[] { new ChartPoint(1, 3), new ChartPoint(2, 1) });
        var defaultSvg = ordinary.ToSvg(); var defaultPng = ordinary.ToPng();
        ordinary.Series[0].WithRadialBand(new ChartRadialBand(.1, .7));
        PackageAssertions.Require(ordinary.ToSvg() != defaultSvg, "Packed explicit single-band geometry was ignored.");
        ordinary.Series[0].WithRadialBand(null);
        PackageAssertions.Require(ordinary.ToSvg() == defaultSvg && ordinary.ToPng().SequenceEqual(defaultPng),
            "Clearing a packed explicit band did not recover the ordinary single-series output.");
        PackageAssertions.Png(png);
    }
}
