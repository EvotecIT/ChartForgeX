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
        ChartColor fill, int pointIndex, string role, bool center = false) {
        var style = LabelStyle(chart, context, fill, pointIndex);
        var inset = Math.Min(8, Math.Min(bounds.Width, bounds.Height) / 5);
        double width = Math.Max(0, bounds.Width - inset * 2), height = Math.Max(0, bounds.Height - inset * 2);
        var metrics = builder.MeasureText("M", style);
        int count = Math.Min(2, (int)Math.Floor(height / metrics.LineHeight));
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
            : SvgPaint.Contrast(fill, VisualChartPaint.SeriesRole(chart.Series[0], pointIndex));
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

    /// <summary>Validates mutable tuple input before recursive canonical hierarchy helpers can observe it.</summary>
    private static void ValidateTree(ChartSeries series, int labelCount) {
        if (series.Points.Count % 2 != 0) throw new InvalidOperationException("Hierarchy links require endpoint/weight pairs.");
        int count = labelCount;
        foreach (var point in series.Points.Where((_, i) => i % 2 == 0)) {
            if (!Finite(point.X) || !Finite(point.Y) || point.X < 0 || point.Y < 0 || point.X != Math.Floor(point.X) || point.Y != Math.Floor(point.Y)
                || point.X > series.Points.Count || point.Y > series.Points.Count) throw new InvalidOperationException("Hierarchy endpoints must be finite dense node indices.");
            count = Math.Max(count, (int)Math.Max(point.X, point.Y) + 1);
        }
        var children = new List<int>[count]; var parent = Enumerable.Repeat(-1, count).ToArray();
        for (int i = 0; i < count; i++) children[i] = new List<int>();
        for (int i = 0; i < series.Points.Count; i += 2) {
            var point = series.Points[i]; int from = (int)point.X, to = (int)point.Y; double value = series.Points[i + 1].Y;
            if (!Finite(value) || value <= 0 || from == to || parent[to] >= 0) throw new InvalidOperationException("Hierarchy links require positive finite weights, distinct endpoints and one parent per child.");
            parent[to] = from; children[from].Add(to);
        }
        var roots = Enumerable.Range(0, count).Where(i => parent[i] < 0).ToArray();
        if (roots.Length != 1) throw new InvalidOperationException("Hierarchy links require exactly one root.");
        var pending = new Stack<(int Node, int Depth)>(); pending.Push((roots[0], 0)); var visited = new HashSet<int>();
        while (pending.Count > 0) {
            var next = pending.Pop();
            if (!visited.Add(next.Node)) throw new InvalidOperationException("Hierarchy links must not contain cycles.");
            if (next.Depth > 512) throw new NotSupportedException("Hierarchy depth exceeds the supported layout budget of 512 levels.");
            foreach (int child in children[next.Node]) pending.Push((child, next.Depth + 1));
        }
        if (visited.Count != count) throw new InvalidOperationException("Hierarchy links must form one connected acyclic tree.");
    }
}
