using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;

public static partial class V2GalleryModels {
    private static Chart? GeometryOptions(ChartSeriesKind kind, string variant) {
        if (kind == ChartSeriesKind.Line) {
            var chart = Chart.Create().WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri").WithYAxis("Recorded level");
            var positions = new[] { ChartStepPosition.Start, ChartStepPosition.Middle, ChartStepPosition.End };
            for (var index = 0; index < positions.Length; index++) {
                var points = new[] { 8d, 17, 11, 23, 16 }.Select((value, point) => new ChartPoint(point + 1, value + index * 30));
                chart.AddLine(positions[index].ToString(), points);
                chart.Series[index].WithInterpolation(ChartInterpolation.Step, positions[index]);
                var shape = new[] { ChartMarkerShape.Square, ChartMarkerShape.Diamond, ChartMarkerShape.Triangle }[index];
                chart.Series[index].WithMarkers(markers => { markers.Shape = shape; markers.Enabled = true; markers.Radius = 5; });
            }
            return chart;
        }
        if (kind is ChartSeriesKind.Bar or ChartSeriesKind.HorizontalBar) {
            var chart = Chart.Create().WithXLabels("North", "South", "East", "West");
            var names = new[] { "First half · Completed", "First half · Remaining", "Second half · Completed", "Second half · Remaining" };
            var values = new[] { new[] { 60d, 75, 40, 90 }, new[] { 40d, 25, 60, 30 }, new[] { 90d, 60, 100, 70 }, new[] { 30d, 40, 25, 30 } };
            for (var index = 0; index < names.Length; index++) {
                var points = values[index].Select((value, point) => new ChartPoint(point + 1, value));
                if (kind == ChartSeriesKind.Bar) chart.AddBar(names[index], points);
                else chart.AddHorizontalBar(names[index], points);
                chart.Series[index].WithStackGroup(index < 2 ? "first-half" : "second-half").WithNormalization(100);
            }
            if (kind == ChartSeriesKind.Bar) chart.WithYAxis("Share (%)");
            else chart.WithXAxis("Share (%)");
            (kind == ChartSeriesKind.Bar ? chart.Options.YAxis : chart.Options.XAxis)
                .WithBounds(0, 100).WithLabelFormatter(Percent).WithReversal(variant == "compact-options");
            chart.WithBarStyle(ChartBarStyle.SegmentedCapsule).WithStackTotals().WithDataLabels(false);
            return chart;
        }
        if (kind == ChartSeriesKind.Waterfall) {
            var chart = Basic(kind, variant)!;
            chart.Options.XAxis.WithReversal();
            return chart;
        }
        if (kind == ChartSeriesKind.StackedArea) {
            var chart = Chart.Create().WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri").WithYAxis("Share (%)")
                .AddStackedArea("Completed", new[] { 45d, 70, 80, 55, 90 }.Select((value, point) => new ChartPoint(point + 1, value)))
                .AddStackedArea("Remaining", new[] { 55d, 45, 20, 45, 30 }.Select((value, point) => new ChartPoint(point + 1, value)));
            foreach (var series in chart.Series) series.WithNormalization(100).WithInterpolation(ChartInterpolation.Step, ChartStepPosition.Middle);
            chart.Options.YAxis.WithBounds(0, 100).WithLabelFormatter(Percent);
            return chart;
        }
        if (kind == ChartSeriesKind.RangeArea) {
            var chart = Chart.Create().WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri").WithYAxis("Expected level")
                .AddRangeArea("Expected range", new[] { new ChartRangeBand(1, 15, 30), new ChartRangeBand(2, 25, 50),
                    new ChartRangeBand(3, 20, 40), new ChartRangeBand(4, 40, 65), new ChartRangeBand(5, 30, 55) });
            chart.Series[0].WithInterpolation(ChartInterpolation.Step, ChartStepPosition.Middle);
            return chart;
        }
        if (kind is ChartSeriesKind.Scatter or ChartSeriesKind.Bubble or ChartSeriesKind.Radar)
            return MarkerOptions(kind);
        if (kind == ChartSeriesKind.Funnel)
            return Chart.Create().WithXLabels("Received", "Reviewed", "Qualified", "Completed").WithDataLabels()
                .AddFunnel("Requests", new[] { new ChartPoint(1, 100), new ChartPoint(2, 75), new ChartPoint(3, 25), new ChartPoint(4, 0) })
                .WithFunnel(options => {
                    options.Form = variant == "stage-bars-horizontal" ? ChartFunnelForm.StageBars : ChartFunnelForm.Cone;
                    options.Orientation = variant == "cone-vertical" ? ChartOrientation.Vertical : ChartOrientation.Horizontal;
                });
        if (kind == ChartSeriesKind.Pyramid) {
            var chart = Chart.Create().WithXLabels("Services", "Platform", "Support", "Unassigned").WithDataLabels()
                .AddPyramid("Allocation", new[] { new ChartPoint(1, 50), new ChartPoint(2, 30), new ChartPoint(3, 20), new ChartPoint(4, 0) })
                .WithPyramid(options => {
                    options.ValueEncoding = ChartPyramidValueEncoding.Area;
                    options.Orientation = ChartOrientation.Horizontal;
                    options.Reversed = true;
                    options.AspectRatio = .75;
                });
            chart.Series[0].WithPointColor(0, "#2F78C4").WithPointColor(1, "#E2A644").WithPointColor(2, "#34957A")
                .WithPointFillPattern(1, ChartFillPattern.DiagonalForward);
            return chart;
        }
        return null;
    }

    private static string Percent(double value) => value.ToString("0", CultureInfo.InvariantCulture) + "%";
}
