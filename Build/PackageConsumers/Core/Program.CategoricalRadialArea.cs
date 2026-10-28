using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

internal static partial class Program {
    private static void CategoricalRadialAreaContracts() {
        var first = new ChartSeries(new ChartSeriesId("completed"), "Completed", ChartSeriesKind.RadialColumn,
            new[] { new ChartPoint(1, 20), new ChartPoint(2, 40) }).WithObservationId(0, new ChartObservationId("north"));
        var second = new ChartSeries(new ChartSeriesId("follow-up"), "Follow-up", ChartSeriesKind.RadialColumn,
            new[] { new ChartPoint(1, 30), new ChartPoint(2, 10) }).WithObservationId(0, new ChartObservationId("north"));
        var reference = new ChartSeries(new ChartSeriesId("reference"), "Reference", ChartSeriesKind.RadialColumn,
            new[] { new ChartPoint(1, 75) }).UseSecondaryValueAxis();
        var chart = Chart.Create().WithSize(520, 400).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithXLabels("North", "South").WithYAxisBounds(0, 100)
            .WithRadialGeometry(new(valueEncoding: ChartRadialValueEncoding.Area))
            .AddSeries(first).AddSeries(second).AddSeries(reference);
        chart.Options.SecondaryYAxis.WithBounds(50, 150).WithReversal();
        var grouped = XDocument.Parse(chart.ToSvg());
        PackageAssertions.Require(grouped.Descendants().Count(IsPoint) == 5,
            "Packed categorical area grouping lost or synthesized source observations.");
        first.WithStackGroup("requests").WithNormalization(100); second.WithStackGroup("requests").WithNormalization(100);
        chart.Options.YAxis.WithReversal(); chart.Options.XAxis.WithReversal();
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(520, 400))));
        var svg = prepared.ToSvg(); var png = prepared.ToPng(); var document = XDocument.Parse(svg);
        var followup = document.Descendants().Single(node => IsPoint(node)
            && (string?)node.Attribute("data-cfx-series-key") == "follow-up" && (string?)node.Attribute("data-cfx-observation-id") == "north");
        PackageAssertions.Require((string?)followup.Attribute("data-cfx-y") == "30"
            && (string?)followup.Attribute("data-cfx-rendered-y") == "60"
            && (string?)followup.Attribute("data-cfx-base") == "40"
            && (string?)followup.Attribute("data-cfx-stack-end") == "100"
            && (string?)followup.Attribute("data-cfx-value-encoding") == "area",
            "Packed categorical area stacks lost raw values, normalization or their selected encoding.");
        var secondary = document.Descendants().Single(node => IsPoint(node) && (string?)node.Attribute("data-cfx-series-key") == "reference");
        PackageAssertions.Require((string?)secondary.Attribute("data-cfx-axis") == "secondary"
            && (string?)secondary.Attribute("data-cfx-y") == "75"
            && document.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "radial-value-axis")
                .All(node => (string?)node.Attribute("data-cfx-reversed") == "true"),
            "Packed categorical area reversal changed raw facts or lost an independent value axis.");
        first.Points[0] = new ChartPoint(1, 80); chart.WithRadialGeometry(new());
        PackageAssertions.Require(prepared.ToSvg() == svg && prepared.ToPng().SequenceEqual(png),
            "Later source or encoding edits changed a prepared categorical area artifact.");
        PackageAssertions.Png(png); PackageAssertions.Png(chart.ToPng());
        var defaults = Chart.Create().WithSize(320, 280).WithHeader(false).WithLegend(false)
            .AddRadialColumn("Radius default", new[] { new ChartPoint(1, 25), new ChartPoint(2, 100) });
        var radiusSvg = defaults.ToSvg(); var radiusPng = defaults.ToPng();
        defaults.WithRadialGeometry(new(valueEncoding: ChartRadialValueEncoding.Radius));
        PackageAssertions.Require(defaults.ToSvg() == radiusSvg && defaults.ToPng().SequenceEqual(radiusPng),
            "Explicit packed radius encoding changed the unchanged default output.");
    }

    private static bool IsPoint(XElement element) => (string?)element.Attribute("data-cfx-role") == "point";
}
