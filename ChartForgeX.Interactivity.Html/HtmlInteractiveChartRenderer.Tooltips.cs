using System;
using System.Globalization;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveChartRenderer {
    // HTML-only metadata: native SVG/PNG captions and custom numeric policies stay untouched.
    private static void AppendTooltipMetadata(StringBuilder json, Chart chart) {
        json.Append(",\"tooltip\":{\"axes\":{");
        Axis("x", chart.XAxisTitle, chart.Options.XAxis, false);
        Axis("y", chart.YAxisTitle, chart.Options.YAxis, true);
        Axis("secondaryX", chart.SecondaryXAxisTitle, chart.Options.SecondaryXAxis, true);
        Axis("secondaryY", chart.SecondaryYAxisTitle, chart.Options.SecondaryYAxis, true);
        json.Append("},\"sizeLabel\":").Append(HtmlJsonString.Encode(chart.Options.Bubble.SizeDimension.Name ?? chart.Options.Labels.Size))
            .Append(",\"series\":[");
        var horizontal = ChartCartesianOrientation.IsHorizontal(chart);
        for (var index = 0; index < chart.Series.Count; index++) {
            if (index > 0) json.Append(',');
            var series = chart.Series[index];
            var axis = series.ValueAxis == ChartAxisSide.Secondary
                ? horizontal ? chart.Options.SecondaryXAxis : chart.Options.SecondaryYAxis
                : horizontal ? chart.Options.XAxis : chart.Options.YAxis;
            var preserve = !ReferenceEquals(chart.Options.ValueFormat, ChartValueFormat.ExistingValue) || axis.ValueFormat != null;
            json.Append("{\"preserveFormatted\":").Append(preserve ? "true" : "false");
            if (!preserve) {
                var decimals = 0; var hasValues = false;
                void Observe(double value) {
                    hasValues = true;
                    var caption = ChartNumericFormatter.FormatValue(chart.Options, value);
                    var dot = caption.IndexOf('.');
                    if (dot >= 0 && caption.IndexOf('E') < 0 && caption.IndexOf('e') < 0)
                        decimals = Math.Max(decimals, caption.Length - dot - 1);
                }
                foreach (var point in series.Points) Observe(point.Y);
                foreach (var bubble in series.Bubbles) Observe(bubble.Y);
                foreach (var candle in series.FinancialValues) {
                    Observe(candle.Open); Observe(candle.High); Observe(candle.Low); Observe(candle.Close);
                }
                if (hasValues) json.Append(",\"decimals\":").Append(decimals.ToString(CultureInfo.InvariantCulture));
            }
            if (!string.IsNullOrWhiteSpace(chart.Options.ValueUnit)) json.Append(",\"unit\":").Append(HtmlJsonString.Encode(chart.Options.ValueUnit!));
            json.Append('}');
        }
        json.Append("]}");

        void Axis(string name, string title, ChartAxis axis, bool comma) {
            if (comma) json.Append(',');
            json.Append(HtmlJsonString.Encode(name)).Append(":{\"title\":").Append(HtmlJsonString.Encode(title))
                .Append(",\"preserveFormatted\":").Append(axis.ValueFormat != null ? "true" : "false").Append('}');
        }
    }
}
