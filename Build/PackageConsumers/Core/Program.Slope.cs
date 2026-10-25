using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

internal static partial class Program {
    private static void SlopeStageContracts() {
        var source = new[] { new ChartPoint(1, 20), new ChartPoint(2, 45), new ChartPoint(3, 35, true), new ChartPoint(4, 70) };
        var chart = Chart.Create().WithSize(640, 360).WithLegend(false).WithXLabels("First", "Second", "Third", "Fourth")
            .AddSlope("Recorded", source);
        var series = chart.Series[0].UseSecondaryValueAxis().WithObservationIds(new[] {
            new ChartObservationId("first"), new ChartObservationId("second"), new ChartObservationId("third"), new ChartObservationId("fourth")
        });
        chart.Options.SlopeOrientation = ChartOrientation.Horizontal;
        chart.WithXAxisBounds(0, 100).WithYAxisBounds(0, 5).WithSecondaryXAxisBounds(0, 100);
        source[1] = new ChartPoint(2, 999);
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 360)));
        var prepared = chart.Prepare(context, new ChartCartesianViewport(y: new ChartAxisWindow(1.5, 3.5)));
        var svg = XDocument.Parse(prepared.ToSvg("packed-stages"));
        var facts = svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
        PackageAssertions.Require(prepared.CartesianOrientation == ChartOrientation.Horizontal && prepared.SecondaryXProjection != null
            && prepared.SecondaryYProjection == null && facts.Length == 4 && series.Points[1].Y == 45,
            "Packed slopes lost ordered captured stages or physical secondary routing.");
        PackageAssertions.Require(facts.Select(node => (string?)node.Attribute("data-cfx-observation-id"))
            .SequenceEqual(new[] { "first", "second", "third", "fourth" })
            && facts.Select(node => (string?)node.Attribute("data-cfx-source-point")).SequenceEqual(new[] { "0", "1", "2", "3" }),
            "Packed slopes lost stage identity or source ordinals.");
        PackageAssertions.Require(facts.Count(node => (string?)node.Attribute("data-cfx-visible") != "false") == 2,
            "Packed horizontal slope windows used the wrong physical category axis.");
        var path = svg.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "slope-line");
        PackageAssertions.Require(((string)path.Attribute("d")!).Count(character => character == 'M') == 2,
            "Packed slopes bridged an explicit missing interval.");
        var captured = prepared.ToSvg("packed-stages");
        series.Points.Clear(); PackageAssertions.Png(chart.ToPng());
        series.Points.Add(new ChartPoint(1, 10)); series.Points.Add(new ChartPoint(4, 90));
        PackageAssertions.Require(prepared.ToSvg("packed-stages") == captured, "Packed slope refill changed a prepared export.");
        PackageAssertions.Png(prepared.ToPng()); PackageAssertions.Png(chart.ToPng());
    }
}
