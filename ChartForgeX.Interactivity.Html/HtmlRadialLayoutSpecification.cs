using System;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

namespace ChartForgeX.Interactivity.Html;

// Native frame measurements and radius constraints, without sampled drawings or browser-owned data preparation.
internal static class HtmlRadialLayoutSpecification {
    internal static bool Supports(Chart chart) => chart.Series.Count > 0 &&
        chart.Series.All(series => series.Kind is ChartSeriesKind.Pie or ChartSeriesKind.Donut);

    // The portable scene must contain every legend target. Grow only its source frame when native
    // legend budgets would otherwise omit entries; the browser still starts from the authored height.
    internal static VisualRenderContext SourceContext(Chart chart, VisualRenderContext source) {
        var frame = source.Frame;
        var colors = source.Theme.Resolve(source.ThemeMode);
        var complete = new VisualFrame(frame.Title, frame.Subtitle, frame.ShowLegend, frame.LegendPosition,
            frame.ShowSurface, frame.TransparentBackground,
            Chart.ResolveFrameTextStyle(source, frame.TitleStyle, chart.Options.TitleStyle, source.Theme.Typography.TitleSize, colors.Foreground, 700),
            Chart.ResolveFrameTextStyle(source, frame.SubtitleStyle, chart.Options.SubtitleStyle, source.Theme.Typography.SubtitleSize, colors.MutedForeground, 400),
            Chart.ResolveFrameTextStyle(source, frame.LegendStyle, chart.Options.LegendStyle, source.Theme.Typography.LegendSize, colors.Foreground, 400),
            legendMaximumHeightFraction: 1, showCard: frame.ShowCard, legendTitle: frame.LegendTitle);
        var entries = complete.ShowLegend ? VisualRadialCompiler.LegendEntries(chart, source.Theme.Resolve(source.ThemeMode)) : Array.Empty<VisualLegendEntry>();
        var height = source.Layout.Size.Height;
        while (true) {
            var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(source.Layout.Size.Width, height), source.Layout.PaddingEdges),
                source.Theme, source.ThemeMode, complete, source.Font);
            var plot = VisualFrameLayout.Build(new VisualSceneBuilder(context.Layout.Size, context.Font), context, entries);
            if (plot.Height >= Math.Min(plot.Width, 2 * (source.Theme.Typography.DataLabelSize * 3 + source.Theme.Spacing))) return context;
            height *= 2;
        }
    }

    internal static void Append(StringBuilder json, Chart chart, VisualRenderContext context, PreparedVisual prepared, int authoredHeight, int minimum) {
        var builder = new VisualSceneBuilder(context.Layout.Size, context.Font);
        var entries = context.Frame.ShowLegend ? VisualRadialCompiler.LegendEntries(chart, context.Theme.Resolve(context.ThemeMode)) : Array.Empty<VisualLegendEntry>();
        var plot = VisualFrameLayout.Build(builder, context, entries);
        var padding = context.Layout.PaddingEdges;
        var typography = context.Theme.Typography;
        TextStyle Style(double size, int weight = 400) {
            var font = context.Font.Clone(); font.Weight = weight;
            return new TextStyle { Font = font, FontSize = size, LineHeight = 1 };
        }
        // Preparation can fit oversized frame text. Measure the styles actually painted in its scene.
        var nativeText = prepared.Scene.Nodes.OfType<VisualSceneText>().ToArray();
        var legend = nativeText.FirstOrDefault(text => text.Role == "legend-label")?.Text.Style ?? context.Frame.LegendStyle!;
        var headingStyles = nativeText.Where(text => text.Role == "frame-heading").Select(text => text.Text.Style).ToArray();
        var compact = typography.ForWidth(0);
        var headings = new[] { (context.Frame.Title, context.Frame.TitleStyle!, typography.TitleSize, compact.TitleSize), (context.Frame.Subtitle, context.Frame.SubtitleStyle!, typography.SubtitleSize, compact.SubtitleSize) }
            .Where(item => !string.IsNullOrEmpty(item.Item1))
            .Select((item, index) => (Text: item.Item1!, Style: index < headingStyles.Length ? headingStyles[index] : item.Item2, Wide: item.Item3, Compact: item.Item4)).ToArray();
        json.Append(",\"radial\":{\"height\":").Append(authoredHeight).Append(",\"padding\":[").Append(N(padding.Left)).Append(',').Append(N(padding.Top)).Append(',').Append(N(padding.Right)).Append(',').Append(N(padding.Bottom))
            .Append("],\"plot\":[").Append(N(plot.X)).Append(',').Append(N(plot.Y)).Append(',').Append(N(plot.Width)).Append(',').Append(N(plot.Height)).Append(']')
            .Append(",\"centerSize\":").Append(N(typography.CenterValueSize)).Append(",\"captionSize\":").Append(N(typography.CenterCaptionSize))
            .Append(",\"connector\":").Append(HtmlJsonString.Encode(chart.Options.DataLabelConnectorStyle.ToString()));
        Metrics(legend, headings.Select(item => item.Style).ToArray(), typography.DataLabelSize, true);
        // Below the breakpoint the native drawing uses the compact type roles. Carry their measurements so a narrow
        // container matches the static drawing at the same width; authored or fitted sizes keep their painted size.
        if (!ReferenceEquals(compact, typography) && minimum < typography.CompactBreakpoint) {
            json.Append(",\"compact\":{\"breakpoint\":").Append(N(typography.CompactBreakpoint));
            Metrics(Compact(legend, typography.LegendSize, compact.LegendSize),
                headings.Select(item => Compact(item.Style, item.Wide, item.Compact)).ToArray(), compact.DataLabelSize, false);
            json.Append('}');
        }
        static TextStyle Compact(TextStyle painted, double wide, double size) {
            if (painted.FontSize != wide) return painted;
            var style = painted.Clone(); style.FontSize = size; return style;
        }
        void Metrics(TextStyle legendStyle, TextStyle[] headingStyle, double labelSize, bool text) {
            json.Append(",\"line\":").Append(N(Math.Max(legendStyle.EffectiveFontSize * 1.5, builder.MeasureText("Mg", legendStyle).Height)))
                .Append(",\"ascent\":").Append(N(builder.TextAscent(legendStyle))).Append(",\"legendSize\":").Append(N(legendStyle.FontSize))
                .Append(",\"labelSize\":").Append(N(labelSize))
                .Append(",\"labelLine\":").Append(N(builder.MeasureText("Mg", Style(labelSize)).Height))
                .Append(",\"labelAscent\":").Append(N(builder.TextAscent(Style(labelSize)))).Append(",\"headings\":[");
            for (var index = 0; index < headings.Length; index++) {
                var style = headingStyle[index];
                json.Append(index == 0 ? "[" : ",[");
                if (text) json.Append(HtmlJsonString.Encode(TextCaseTransformer.Apply(headings[index].Text, style.TextCase))).Append(',');
                json.Append(N(Math.Max(style.EffectiveFontSize * 1.3, builder.MeasureText("Mg", style).Height))).Append(',').Append(N(builder.TextAscent(style)))
                    .Append(',').Append(N(style.FontSize)).Append(']');
            }
            json.Append("],\"entries\":[");
            for (var index = 0; index < entries.Count; index++) {
                var entry = entries[index];
                json.Append(index == 0 ? "[" : ",[");
                if (text) json.Append(HtmlJsonString.Encode("legend-" + entry.Id)).Append(',').Append(HtmlJsonString.Encode(TextCaseTransformer.Apply(entry.Label, legendStyle.TextCase))).Append(',');
                json.Append(N(builder.MeasureText(entry.Label.Replace('\r', ' ').Replace('\n', ' '), legendStyle).Width)).Append(',');
                if (text) json.Append(HtmlJsonString.Encode(TextCaseTransformer.Apply(entry.Value ?? "", legendStyle.TextCase))).Append(',');
                json.Append(N(builder.MeasureText(entry.Value ?? "", legendStyle).Width)).Append(',');
                if (text) json.Append(HtmlJsonString.Encode(TextCaseTransformer.Apply(entry.Percentage ?? "", legendStyle.TextCase))).Append(',');
                json.Append(N(builder.MeasureText(entry.Percentage ?? "", legendStyle).Width)).Append(']');
            }
            json.Append(']');
        }
        json.Append(",\"frames\":["); var first = true;
        var bands = ChartRadialBandResolver.Resolve(chart);
        var frames = VisualRadialCompiler.ResponsiveFrames(chart, context, plot);
        for (var index = 0; index < frames.Length; index++) {
            if (!first) json.Append(','); first = false;
            var frame = frames[index];
            var inner = bands[index]?.InnerRatio ?? (chart.Series[index].Kind == ChartSeriesKind.Donut ? chart.Options.DonutInnerRadiusRatio : 0);
            json.Append('[').Append(N(frame.Horizontal)).Append(',').Append(N(frame.Vertical)).Append(',').Append(N(frame.Extent)).Append(',')
                .Append(N(frame.Radius(plot, context.Theme.Spacing))).Append(',').Append(N(inner)).Append(',').Append(N(bands[index]?.OuterRatio ?? 1))
                .Append(',').Append(HtmlJsonString.Encode((chart.Series[index].DataLabelPlacement ?? chart.Options.DataLabelPlacement).ToString()))
                .Append(',').Append(chart.Options.PieInsideLabelRadiusRatio.HasValue ? N(chart.Options.PieInsideLabelRadiusRatio.Value) : "null")
                .Append(',').Append(bands[index] != null || chart.Series.Count > 1 ? "true" : "false");
            var overflow = VisualRadialCompiler.ResponsiveOverflowCenter(chart, context, index);
            json.Append(',').Append(overflow == null ? "null" : "[" + string.Join(",", overflow.Select(HtmlJsonString.Encode)) + "]").Append(']');
        }
        json.Append("],\"slices\":["); first = true;
        foreach (var slice in prepared.Scene.Nodes.OfType<VisualSceneSlice>().Where(slice => slice.Role is "pie-slice" or "donut-slice")) {
            if (!first) json.Append(','); first = false;
            json.Append('[').Append(HtmlJsonString.Encode(slice.Id!)).Append(',').Append(N(slice.Cx)).Append(',').Append(N(slice.Cy)).Append(',')
                .Append(N(slice.Outer)).Append(',').Append(N(slice.Inner)).Append(',').Append(N(slice.Start)).Append(',').Append(N(slice.Sweep)).Append(']');
        }
        json.Append("]}");
    }
    private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
