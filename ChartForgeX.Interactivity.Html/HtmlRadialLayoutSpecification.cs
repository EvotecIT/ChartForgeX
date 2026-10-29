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
        var complete = new VisualFrame(frame.Title, frame.Subtitle, frame.ShowLegend, frame.LegendPosition,
            frame.ShowSurface, frame.TransparentBackground, frame.TitleStyle, frame.SubtitleStyle, frame.LegendStyle,
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

    internal static void Append(StringBuilder json, Chart chart, VisualRenderContext context, PreparedVisual prepared, int authoredHeight) {
        var builder = new VisualSceneBuilder(context.Layout.Size, context.Font);
        var entries = context.Frame.ShowLegend ? VisualRadialCompiler.LegendEntries(chart, context.Theme.Resolve(context.ThemeMode)) : Array.Empty<VisualLegendEntry>();
        var plot = VisualFrameLayout.Build(builder, context, entries);
        var padding = context.Layout.PaddingEdges;
        var typography = context.Theme.Typography;
        TextStyle Style(double size, int weight = 400) {
            var font = context.Font.Clone(); font.Weight = weight;
            return new TextStyle { Font = font, FontSize = size, LineHeight = 1 };
        }
        var legend = Style(typography.LegendSize);
        var line = Math.Max(legend.EffectiveFontSize * 1.5, builder.MeasureText("Mg", legend).Height);
        json.Append(",\"radial\":{\"height\":").Append(authoredHeight).Append(",\"padding\":[").Append(N(padding.Left)).Append(',').Append(N(padding.Top)).Append(',').Append(N(padding.Right)).Append(',').Append(N(padding.Bottom))
            .Append("],\"plot\":[").Append(N(plot.X)).Append(',').Append(N(plot.Y)).Append(',').Append(N(plot.Width)).Append(',').Append(N(plot.Height))
            .Append("],\"line\":").Append(N(line)).Append(",\"ascent\":").Append(N(builder.TextAscent(legend)))
            .Append(",\"centerSize\":").Append(N(typography.CenterValueSize)).Append(",\"labelSize\":").Append(N(typography.DataLabelSize))
            .Append(",\"labelLine\":").Append(N(builder.MeasureText("Mg", Style(typography.DataLabelSize)).Height))
            .Append(",\"labelAscent\":").Append(N(builder.TextAscent(Style(typography.DataLabelSize))))
            .Append(",\"connector\":").Append(HtmlJsonString.Encode(chart.Options.DataLabelConnectorStyle.ToString()))
            .Append(",\"headings\":[");
        var first = true;
        foreach (var item in new[] { (context.Frame.Title, typography.TitleSize, 700), (context.Frame.Subtitle, typography.SubtitleSize, 400) }) {
            if (string.IsNullOrEmpty(item.Item1)) continue;
            if (!first) json.Append(','); first = false;
            var style = Style(item.Item2, item.Item3);
            json.Append('[').Append(HtmlJsonString.Encode(item.Item1!)).Append(',')
                .Append(N(Math.Max(item.Item2 * 1.3, builder.MeasureText("Mg", style).Height))).Append(',').Append(N(builder.TextAscent(style))).Append(']');
        }
        json.Append("],\"entries\":["); first = true;
        foreach (var entry in entries) {
            if (!first) json.Append(','); first = false;
            json.Append('[').Append(HtmlJsonString.Encode("legend-" + entry.Id)).Append(',').Append(HtmlJsonString.Encode(entry.Label)).Append(',')
                .Append(N(builder.MeasureText(entry.Label.Replace('\r', ' ').Replace('\n', ' '), legend).Width)).Append(',')
                .Append(HtmlJsonString.Encode(entry.Value ?? "")).Append(',').Append(N(builder.MeasureText(entry.Value ?? "", legend).Width)).Append(',')
                .Append(HtmlJsonString.Encode(entry.Percentage ?? "")).Append(',').Append(N(builder.MeasureText(entry.Percentage ?? "", legend).Width)).Append(']');
        }
        json.Append("],\"frames\":["); first = true;
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
                .Append(',').Append(bands[index] != null || chart.Series.Count > 1 ? "true" : "false").Append(']');
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
