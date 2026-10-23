using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

internal static partial class Program {
    private static void NativeAxisAndUncertaintyContracts() {
        foreach (var negative in new[] { false, true }) {
            var values = negative ? new[] { -16d, -4, -1 } : new[] { 1d, 4, 16 };
            var chart = Chart.Create().WithSize(600, 360).WithLegend(false).WithLineMarkers(ChartLineMarkerMode.All)
                .AddLine("Magnitude", values.Select((value, index) => new ChartPoint(index, value)));
            chart.Options.YAxis.WithLogarithmicScale(4, negative ? ChartLogarithmicDomain.Negative : ChartLogarithmicDomain.Positive)
                .WithBounds(values[0], values[2]);
            var prepared = chart.Prepare(DepthContext(chart, showLegend: false)); var axis = prepared.YProjection!;
            PackageAssertions.Require(axis.LogarithmBase == 4 && axis.LogarithmicDomain == (negative ? ChartLogarithmicDomain.Negative : ChartLogarithmicDomain.Positive)
                && Math.Abs(axis.Project(values[1]) - (axis.Project(values[0]) + axis.Project(values[2])) / 2) < 1e-7
                && Math.Abs(axis.Unproject(axis.Project(values[1])) - values[1]) < 1e-7,
                "Packed authored signed logarithm lost its base, equal geometric steps or source inverse.");
            var svg = prepared.ToSvg(); var png = prepared.ToPng(); chart.Options.YAxis.LogarithmBase = 2;
            PackageAssertions.Require(svg == prepared.ToSvg() && png.SequenceEqual(prepared.ToPng()), "Packed signed-log export retained a live source policy.");
            PackageAssertions.Png(png);
        }
        var january = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        foreach (var ordinal in new[] { false, true }) {
            var dates = ordinal ? new[] { january, january.AddDays(1), january.AddDays(11) }
                : new[] { january.AddDays(9), january.AddMonths(2).AddDays(29) };
            var chart = Chart.Create().WithSize(600, 360).WithLegend(false)
                .AddLine("Instants", dates.Select((date, index) => new ChartPoint(date, index + 1)));
            chart.Options.XAxis.WithTimeSpacing(ordinal ? ChartTimeSpacing.Ordinal
                : ChartTimeSpacing.Unit(new ChartTimeTickInterval(ChartTimeTickUnit.Month)));
            if (!ordinal) chart.Options.XAxis.WithLabelFormatter(value => DateTime.FromOADate(value).ToString("yyyy-MM", CultureInfo.InvariantCulture));
            var prepared = chart.Prepare(DepthContext(chart, showLegend: false)); var axis = prepared.XProjection!;
            if (ordinal) {
                var positions = dates.Select(date => axis.Project(date.ToOADate())).ToArray();
                PackageAssertions.Require(Math.Abs((positions[1] - positions[0]) - (positions[2] - positions[1])) < 1e-7
                    && Math.Abs(axis.Unproject(positions[1]) - dates[1].ToOADate()) < 1e-7,
                    "Packed Ordinal spacing used elapsed gaps or lost its source inverse.");
            } else {
                var first = axis.Project(dates[0].ToOADate());
                var middle = axis.Project(january.AddMonths(1).AddDays(14).ToOADate());
                var last = axis.Project(dates[1].ToOADate());
                PackageAssertions.Require(Math.Abs((middle - first) - (last - middle)) < 1e-7
                    && Math.Abs(axis.Unproject(first) - january.ToOADate()) < 1e-7
                    && XDocument.Parse(prepared.ToSvg()).Descendants().Any(node => (string?)node.Attribute("data-cfx-role") == "axis-x-label"
                        && node.Descendants().Any(text => text.Name.LocalName == "text" && text.Value == "2024-02")),
                    "Packed Unit spacing lost an empty calendar month or its canonical band-start inverse.");
            }
            var before = chart.Series[0].Points.Select(point => point.X).ToArray();
            PackageAssertions.Require(before.SequenceEqual(dates.Select(date => date.ToOADate())), "Packed time spacing rewrote authored instants.");
            PackageAssertions.Png(prepared.ToPng());
        }
        var mixed = Chart.Create().WithSize(600, 360).WithLegend(false)
            .AddHorizontalBar("Counts", new[] { new ChartPoint(1, 10), new ChartPoint(2, 20), new ChartPoint(3, 30) })
            .AddLine("Duration", new[] { new ChartPoint(1, 100), new ChartPoint(2, 300), new ChartPoint(3, 500) });
        mixed.Options.LineOrientation = ChartOrientation.Horizontal; mixed.Series[1].UseSecondaryValueAxis();
        mixed.WithXAxisBounds(0, 40).WithYAxisBounds(0, 4).WithSecondaryXAxisBounds(100, 500);
        var context = DepthContext(mixed, showLegend: false);
        var full = mixed.Prepare(context);
        var window = mixed.Prepare(context, new ChartCartesianViewport(y: new ChartAxisWindow(1.25, 2.75), secondaryX: new ChartAxisWindow(200, 400)));
        PackageAssertions.Require(window.SecondaryXProjection!.Dimension == ChartAxisDimension.X && window.SecondaryXProjection.Side == ChartAxisSide.Secondary
            && window.SecondaryYProjection == null && window.SecondaryXProjection.Minimum == 200 && window.SecondaryXProjection.Maximum == 400
            && window.XProjection!.Minimum == 0 && window.XProjection.Maximum == 40 && window.YProjection!.Minimum == 1.25
            && Math.Abs(window.SecondaryXProjection.Unproject(window.SecondaryXProjection.Project(300)) - 300) < 1e-7
            && mixed.Prepare(context, null).ToSvg() == full.ToSvg(), "Packed horizontal mixture confused physical secondary X with vertical Y or reset domains.");
        PackageAssertions.Png(window.ToPng());
        var fitted = Chart.Create().WithSize(600, 360).WithLegend(false)
            .AddLine("Counts", new[] { new ChartPoint(0, 0), new ChartPoint(10, 100) })
            .AddLine("Duration", new[] { new ChartPoint(0, 100), new ChartPoint(10, 200) });
        fitted.Series[1].UseSecondaryValueAxis();
        var fittedContext = DepthContext(fitted, showLegend: false); var fullFit = fitted.Prepare(fittedContext);
        var visibleFit = fitted.Prepare(fittedContext, new ChartCartesianViewport(x: new ChartAxisWindow(4, 6), valueDomain: ChartValueDomain.VisibleCategories));
        PackageAssertions.Require(visibleFit.YProjection!.Minimum > fullFit.YProjection!.Minimum
            && visibleFit.YProjection.Maximum < fullFit.YProjection.Maximum
            && visibleFit.SecondaryYProjection!.Minimum > fullFit.SecondaryYProjection!.Minimum
            && visibleFit.SecondaryYProjection.Maximum < fullFit.SecondaryYProjection.Maximum
            && visibleFit.YProjection.FullMinimum == fullFit.YProjection.FullMinimum
            && fitted.Series.All(series => series.SourcePointCount == 2),
            "Packed visible-category preparation lost crossing-segment extents, independent secondary auto-domains or full source facts.");
        PackageAssertions.Png(visibleFit.ToPng());
        var uncertainty = Chart.Create().WithSize(600, 360).WithLegend(false).WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddSeries(ChartSeries.CreateErrorBar(new ChartSeriesId("estimate"), "Estimate", new[] {
                new ChartErrorBar(5, 5, new ChartErrorBounds(1, 9), new ChartErrorBounds(2, 8))
            }).WithObservationId(0, new ChartObservationId("sample")));
        var errors = uncertainty.Prepare(DepthContext(uncertainty, showLegend: false));
        var errorSvg = XDocument.Parse(errors.ToSvg());
        var xRange = errorSvg.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "error-range-x");
        var yRange = errorSvg.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "error-range");
        // Native SVG retains three fractional places; compare the independently projected source endpoints in that exact format.
        Func<double, string> coordinate = value => value.ToString("0.###", CultureInfo.InvariantCulture);
        PackageAssertions.Require((string?)xRange.Attribute("x1") == coordinate(errors.XProjection!.Project(1))
            && (string?)xRange.Attribute("x2") == coordinate(errors.XProjection.Project(9))
            && (string?)yRange.Attribute("y1") == coordinate(errors.YProjection!.Project(2))
            && (string?)yRange.Attribute("y2") == coordinate(errors.YProjection.Project(8)),
            "Packed independent X/Y intervals painted the wrong source endpoints or physical dimension.");
        var fact = errorSvg.Descendants().Single(node => (string?)node.Attribute("data-cfx-observation-id") == "sample");
        PackageAssertions.Require((string?)fact.Attribute("data-cfx-x-lower") == "1" && (string?)fact.Attribute("data-cfx-y-upper") == "8",
            "Packed uncertainty export lost complete independently authored intervals.");
        PackageAssertions.Png(errors.ToPng());
        PackedCalendarWindowBoundary();
        Console.WriteLine("Packed signed logarithms, Unit/Ordinal time, physical secondary windows and independent X/Y uncertainty passed.");
    }
    private static void PackedCalendarWindowBoundary() {
        var start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var instants = new[] { start, start.AddMonths(1), start.AddMonths(2) };
        var chart = Chart.Create().WithSize(600, 360).WithLegend(false)
            .AddLine("Calendar", instants.Select((date, index) => new ChartPoint(date, index + 1)));
        chart.Options.XAxis.WithTimeSpacing(ChartTimeSpacing.Unit(new ChartTimeTickInterval(ChartTimeTickUnit.Month)));
        var context = DepthContext(chart, showLegend: false);
        var full = chart.Prepare(context); var axis = full.XProjection!;
        var first = axis.Project(instants[0].ToOADate());
        var last = axis.Project(instants[2].ToOADate());
        var gesture = axis.ConstrainViewport(first + (last - first) * .25, first + (last - first) * .75)
            ?? throw new InvalidOperationException("Packed Unit gesture did not produce a bounded native window.");
        PackageAssertions.Require(!gesture.MaximumInclusive && gesture.Minimum < gesture.Maximum,
            "Packed Unit gesture did not retain its canonical exclusive upper band edge.");
        var viewed = chart.Prepare(context, new ChartCartesianViewport(x: gesture));
        var repeated = viewed.XProjection!.ConstrainViewport(viewed.XProjection.PlotBounds.Left, viewed.XProjection.PlotBounds.Right)
            ?? throw new InvalidOperationException("Packed Unit re-preparation discarded the committed bounded window.");
        PackageAssertions.Require(repeated.Minimum == gesture.Minimum && repeated.Maximum == gesture.Maximum
            && repeated.MaximumInclusive == gesture.MaximumInclusive
            && chart.Prepare(context, new ChartCartesianViewport(x: repeated)).ToSvg() == viewed.ToSvg(),
            "Packed native gesture/window/re-preparation expanded a calendar band or lost upper-bound semantics.");
        var upper = instants[2].ToOADate();
        var inclusive = new ChartAxisWindow(start.ToOADate(), upper);
        PackageAssertions.Require(inclusive.MaximumInclusive, "Packed authored windows changed their inclusive default.");
        var included = chart.Prepare(context, new ChartCartesianViewport(x: inclusive));
        var excluded = chart.Prepare(context, new ChartCartesianViewport(x:
            new ChartAxisWindow(inclusive.Minimum, inclusive.Maximum, maximumInclusive: false)));
        Func<PreparedVisual, bool> upperVisible = visual => XDocument.Parse(visual.ToSvg()).Descendants()
            .Any(node => (string?)node.Attribute("data-cfx-role") == "point"
                && (string?)node.Attribute("data-cfx-x") == upper.ToString("G17", CultureInfo.InvariantCulture)
                && (string?)node.Attribute("data-cfx-visible") != "false");
        PackageAssertions.Require(upperVisible(included) && !upperVisible(excluded),
            "Packed authored inclusive boundary and explicit exclusive boundary did not select different source bands.");
        PackageAssertions.Png(viewed.ToPng()); PackageAssertions.Png(included.ToPng());
    }

}
