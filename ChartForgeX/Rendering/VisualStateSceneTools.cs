using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Native state marks and measured text shared by matrix and lane producers.</summary>
internal static class VisualStateSceneTools {
    internal static TextStyle TickStyle(Chart chart, VisualRenderContext context) => chart.Options.TickLabelStyle.Resolve(new TextStyle {
        Font = context.Font, FontSize = context.Theme.Typography.AxisSize, Color = context.Theme.Resolve(context.ThemeMode).MutedForeground
    });

    internal static TextStyle DataStyle(Chart chart, VisualRenderContext context, ChartSeries series, int index, ChartColor color) {
        var style = series.DataLabelStyle.Resolve(chart.Options.DataLabelStyle.Resolve(new TextStyle {
            Font = context.Font, FontSize = context.Theme.Typography.DataLabelSize, Color = color
        }));
        return index >= 0 && index < series.PointDataLabelStyles.Count && series.PointDataLabelStyles[index] != null
            ? series.PointDataLabelStyles[index]!.Resolve(style) : style;
    }

    internal static ChartColor SeriesColor(ChartSeries series, int index, VisualThemeColors colors) => ChartSeriesColours.Resolve(series, index, colors);

    internal static string Value(Chart chart, ChartSeries series, int index, double value) =>
        index >= 0 && index < series.PointLabels.Count && series.PointLabels[index] != null
            ? series.PointLabels[index]! : ChartNumericFormatter.FormatValue(chart.Options, value);

    internal static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    internal static string SourceId(int series, int point) => "series-" + series.ToString(CultureInfo.InvariantCulture) + "-point-" + point.ToString(CultureInfo.InvariantCulture);

    internal static IDisposable Mark(VisualSceneBuilder builder, string id, string role, ChartRect bounds, string label,
        Dictionary<string, string>? metadata = null, string? href = null) {
        builder.AddRegion(new VisualSemanticRegion(id, role, bounds, label));
        metadata ??= new Dictionary<string, string>();
        metadata["aria-label"] = label;
        metadata["role"] = "img";
        return new MarkScope(builder.PushGroup(id, role, metadata), href == null
            ? builder.PushTooltip(label) : builder.PushLink(href, role: role + "-link", tooltip: label));
    }

    internal static void StateRect(VisualSceneBuilder builder, ChartRect bounds, ChartStateCategory state,
        VisualThemeColors colors, double radius, string role) {
        StateRect(builder, bounds, ChartStateMark.For(state, colors.Background), radius, role);
    }

    internal static void StateRect(VisualSceneBuilder builder, ChartRect bounds, ChartStateMark mark, double radius, string role) {
        builder.Rect(bounds, ChartColorMath.WithOpacity(mark.Color, mark.FillOpacity), radius: radius, role: role);
        // The same clipped numeric contour drives patterns and outline dashes in both backends.
        var path = RoundedRect(bounds, radius);
        if (mark.Outlined) builder.Path(path, stroke: ChartColorMath.WithOpacity(mark.Color, mark.OutlineOpacity), strokeWidth: ChartStateMark.OutlineWidth,
            role: role + "-outline", close: true, dash: new[] { ChartStateMark.OutlineDash, ChartStateMark.OutlineGap });
        builder.Pattern(path, mark.Lines, ChartColorMath.WithOpacity(mark.LineColor, ChartStateCategoryLegend.HatchOpacity),
            ChartStateCategoryLegend.HatchSpacing, ChartStateMark.PatternLineWidth, role + "-hatch");
    }

    internal static Dictionary<string, string> StateMetadata(Chart chart, ChartStateCategory state) {
        return StateMetadata(chart.Options.PinStateColorsInForcedColors, state);
    }

    internal static Dictionary<string, string> StateMetadata(bool pinStateColors, ChartStateCategory state) {
        var result = new Dictionary<string, string> { ["data-cfx-status"] = state.Key, ["data-cfx-state-label"] = state.Label };
        if (state.Pattern != ChartStatePattern.Solid) result["data-cfx-pattern"] = state.Pattern == ChartStatePattern.CrossHatched ? "cross-hatched" : state.Pattern.ToString().ToLowerInvariant();
        if (state.Emphasis == ChartStateEmphasis.Quiet) result["data-cfx-emphasis"] = "quiet";
        if (pinStateColors) result["data-cfx-pin-state-colors"] = "true";
        return result;
    }

    internal static void Text(VisualSceneBuilder builder, string text, ChartRect bounds, TextStyle style, string role,
        string id, TextAlignment alignment = TextAlignment.Left, bool shrink = false, SvgPaint? paint = null) {
        builder.AddRegion(new VisualSemanticRegion(id, role, bounds, text));
        if (bounds.Width <= 0 || bounds.Height <= 0 || text.Length == 0) return;
        style = style.Clone();
        if (shrink) {
            var measured = builder.MeasureText(text, style);
            var ratio = Math.Min(1, Math.Min(bounds.Width / Math.Max(1, measured.Width), bounds.Height / Math.Max(1, measured.Height)));
            style.FontSize = Math.Max(1, style.EffectiveFontSize * ratio); style.Baseline = TextBaseline.Normal;
        }
        var fit = ChartTextFitting.TrimEnd(text, style.EffectiveFontSize, bounds.Width, (value, _) => builder.MeasureText(value, style).Width);
        if (fit != text) builder.AddDiagnostic(new VisualDiagnostic("text.overflow", "Text was shortened to fit the fixed viewport; complete text remains in descriptive regions."));
        if (builder.MeasureText(fit, style).Height > bounds.Height) return;
        style.Alignment = alignment;
        var x = alignment == TextAlignment.Center ? bounds.Left + bounds.Width / 2 : alignment == TextAlignment.Right ? bounds.Right : bounds.Left;
        var y = bounds.Top + (bounds.Height - builder.MeasureText(fit, style).Height) / 2 + builder.TextAscent(style);
        using (builder.PushClip(bounds)) builder.Text(fit, x, y, style, role, id, paint);
    }

    internal static ChartPath RoundedRect(ChartRect bounds, double radius) {
        // A rectangle's existing cubic contour owner also handles the zero-radius case.
        return ChartPathBuilder.RoundedRectangle(bounds, radius);
    }

    internal static void Connector(VisualSceneBuilder builder, ChartOptions options, ChartPoint start, ChartPoint end, ChartColor fallback) {
        var middle = start.X + (end.X - start.X) / 2;
        var commands = new List<ChartPathCommand> { ChartPathCommand.MoveTo(start.X, start.Y) };
        if (options.DataLabelConnectorStyle == ChartDataLabelConnectorStyle.Curve)
            commands.Add(ChartPathCommand.CubicTo(middle, start.Y, middle, end.Y, end.X, end.Y));
        else {
            if (options.DataLabelConnectorStyle == ChartDataLabelConnectorStyle.Elbow) {
                commands.Add(ChartPathCommand.LineTo(middle, start.Y)); commands.Add(ChartPathCommand.LineTo(middle, end.Y));
            }
            commands.Add(ChartPathCommand.LineTo(end.X, end.Y));
        }
        builder.Path(new ChartPath(commands), stroke: ChartColorMath.WithOpacity(options.DataLabelConnectorColor ?? fallback, options.DataLabelConnectorOpacity),
            strokeWidth: options.DataLabelConnectorStrokeWidth, role: "data-label-connector");
    }

    private sealed class MarkScope : IDisposable {
        private readonly IDisposable _group; private readonly IDisposable _annotation;
        internal MarkScope(IDisposable group, IDisposable annotation) { _group = group; _annotation = annotation; }
        public void Dispose() { _annotation.Dispose(); _group.Dispose(); }
    }
}
