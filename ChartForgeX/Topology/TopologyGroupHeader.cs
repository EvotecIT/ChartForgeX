using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

/// <summary>
/// Shares native caption allocation and measured painted header obstacles with normalization, routing, and diagnostics.
/// </summary>
internal static class TopologyGroupHeader {
    internal const double TitleHeight = 24;
    internal const double SubtitleHeight = 18;
    internal const double SymbolScale = .8;
    internal const double StatusRadius = 4;

    /// <summary>Returns true when group headers are drawn with the given options.</summary>
    public static bool IsDrawn(TopologyRenderOptions options) => options.IncludeGroups && options.IncludeGroupLabels;

    /// <summary>Returns the union used to describe the native painted header.</summary>
    public static ChartRect Bounds(TopologyChart chart, TopologyGroup group, TopologyRenderOptions options) {
        var parts = PaintedParts(chart, group, options);
        if (parts.Count == 0) return new ChartRect(group.X, group.Y, 0, 0);
        var left = parts.Min(part => part.Left); var top = parts.Min(part => part.Top);
        return new ChartRect(left, top, parts.Max(part => part.Right) - left, parts.Max(part => part.Bottom) - top);
    }

    /// <summary>Returns the header block with the room layout keeps free below it, which nodes are moved out of.</summary>
    public static ChartRect ReservedBounds(TopologyChart chart, TopologyGroup group, TopologyRenderOptions options) {
        var painted = Bounds(chart, group, options);
        var bottom = group.Y + (string.IsNullOrWhiteSpace(group.Subtitle) ? 54 : 74);
        return new ChartRect(painted.Left, painted.Top, painted.Width, Math.Max(0, Math.Max(painted.Bottom, bottom) - painted.Top));
    }

    /// <summary>Returns individual painted obstacles without filling the space between captions and status dots.</summary>
    internal static IReadOnlyList<ChartRect> PaintedParts(TopologyChart chart, TopologyGroup group, TopologyRenderOptions options) {
        if (!IsDrawn(options)) return Array.Empty<ChartRect>();
        var font = chart.TextMeasurement?.Font ?? chart.Theme.Typography.ToFontSpec();
        var builder = new VisualSceneBuilder(new VisualSize(1, 1), font);
        var size = chart.Theme.Typography.DataLabelSize;
        var fitTitle = size > VisualTextFitting.MaximumOrdinaryFontSize || builder.MeasureText("Ag", size, 600).LineHeight > TitleHeight;
        var fitSubtitle = size > VisualTextFitting.MaximumOrdinaryFontSize || builder.MeasureText("Ag", size * .85, 400).LineHeight > SubtitleHeight;
        var parts = new List<ChartRect>();
        AddCaption(group.Label, TitleBounds(group, options), size, 600, fitTitle);
        if (!string.IsNullOrWhiteSpace(group.Subtitle))
            AddCaption(group.Subtitle!, SubtitleBounds(group, options, fitTitle || fitSubtitle), size * .85, 400, fitSubtitle);
        if (SymbolReserve(group, options) > 0) {
            var center = SymbolCenter(group); var half = 13 * SymbolScale;
            parts.Add(new ChartRect(center.X - half, center.Y - half, half * 2, half * 2));
        }
        if (options.IncludeGroupStatusDots) {
            var center = StatusCenter(group);
            parts.Add(new ChartRect(center.X - StatusRadius, center.Y - StatusRadius, StatusRadius * 2, StatusRadius * 2));
        }
        return parts;

        void AddCaption(string value, ChartRect allocation, double preferred, int weight, bool fit) {
            var layout = TopologyTextLayout.Prepare(value, allocation.Width, allocation.Height, preferred, weight, 1,
                fit, DotNodeSymbolFontSize, options, builder.MeasureText);
            if (layout != null && layout.Width > 0 && layout.Height > 0)
                parts.Add(new ChartRect(allocation.X, allocation.Y, layout.Width, layout.Height));
        }
    }

    internal static double SymbolReserve(TopologyGroup group, TopologyRenderOptions options) =>
        string.IsNullOrWhiteSpace(group.Symbol ?? ResolveGroupIcon(group, options)?.Symbol) && string.IsNullOrWhiteSpace(group.IconId) ? 0 : 28;

    internal static ChartRect TitleBounds(TopologyGroup group, TopologyRenderOptions options) => TextBounds(group, options, 9, TitleHeight);

    internal static ChartRect SubtitleBounds(TopologyGroup group, TopologyRenderOptions options, bool fitted) =>
        TextBounds(group, options, fitted ? 33 : 31, SubtitleHeight);

    internal static ChartPoint SymbolCenter(TopologyGroup group) => new(group.X + 22, group.Y + 21);
    internal static ChartPoint StatusCenter(TopologyGroup group) => new(group.X + group.Width - 12, group.Y + 16);

    private static ChartRect TextBounds(TopologyGroup group, TopologyRenderOptions options, double top, double height) {
        var reserve = SymbolReserve(group, options);
        return new ChartRect(group.X + 12 + reserve, group.Y + top, Math.Max(0, group.Width - 24 - reserve), height);
    }

}
