using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualHierarchyCompiler {
    private static void Label(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, string text, ChartRect bounds,
        ChartColor fill, int pointIndex, string role, bool center = false, SvgPaint? fillPaint = null, double insetLimit = 8, int maximumLines = 2) {
        var style = LabelStyle(chart, context, fill, pointIndex);
        var inset = Math.Min(insetLimit, Math.Min(bounds.Width, bounds.Height) / 5);
        double width = Math.Max(0, bounds.Width - inset * 2), height = Math.Max(0, bounds.Height - inset * 2);
        var metrics = builder.MeasureText("M", style);
        int count = Math.Min(maximumLines, (int)Math.Floor(height / metrics.LineHeight));
        if (count < 1 || width < 6) { builder.AddDiagnostic(new VisualDiagnostic("hierarchy.label-overflow", "A hierarchy label was omitted to fit its mark; complete text remains in source semantics.")); return; }
        var paragraphs = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var lines = paragraphs.Length == 1 && count > 1 ? ChartLabelWrapping.BalancedTwoLine(text, style.EffectiveFontSize, width,
            (value, _) => builder.MeasureText(value, style).Width) : paragraphs;
        var fitted = new List<string>();
        bool shortened = lines.Length > count;
        for (int i = 0; i < Math.Min(count, lines.Length); i++) {
            string line = lines[i];
            if (i == count - 1 && lines.Length > count) line += "...";
            string result = ChartTextFitting.TrimEnd(line, style.EffectiveFontSize, width, (value, _) => builder.MeasureText(value, style).Width);
            shortened |= result != line; fitted.Add(result);
        }
        if (shortened) builder.AddDiagnostic(new VisualDiagnostic("hierarchy.label-overflow", "A hierarchy label was shortened to fit its mark; complete text remains in source semantics."));
        string displayed = string.Join("\n", fitted);
        style.Alignment = center ? TextAlignment.Center : TextAlignment.Left;
        double y = center ? bounds.Y + (bounds.Height - builder.MeasureText(displayed, style).Height) / 2 : bounds.Y + inset;
        var paint = VisualChartPaint.ExplicitDataLabelColor(chart, pointIndex) ? VisualChartPaint.Text(style)
            : fillPaint.HasValue ? SvgPaint.Contrast(fill, fillPaint.Value) : SvgPaint.Contrast(fill, VisualChartPaint.SeriesRole(chart.Series[0], pointIndex));
        builder.Text(displayed, center ? bounds.X + bounds.Width / 2 : bounds.X + inset, y + builder.TextAscent(style), style, role, paint: paint);
    }

    /// <summary>Resolves the same authored caption typography for rectangular and radial hierarchy marks.</summary>
    private static TextStyle LabelStyle(Chart chart, VisualRenderContext context, ChartColor fill, int pointIndex) {
        var series = chart.Series[0];
        var fallback = new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.DataLabelSize, Color = ChartColorMath.AccessibleTextOnBackground(fill) };
        var style = series.DataLabelStyle.Resolve(chart.Options.DataLabelStyle.Resolve(fallback));
        if (pointIndex < series.PointDataLabelStyles.Count && series.PointDataLabelStyles[pointIndex] != null) style = series.PointDataLabelStyles[pointIndex]!.Resolve(style);
        style.FontSize = style.EffectiveFontSize; style.Baseline = TextBaseline.Normal;
        return style;
    }

}
