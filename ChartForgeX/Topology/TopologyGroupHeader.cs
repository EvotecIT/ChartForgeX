using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

/// <summary>
/// Shares caption allocations and measurement-policy reservations with layout, retaining actual painted parts after capture.
/// </summary>
internal static class TopologyGroupHeader {
    internal const double TitleHeight = 24;
    internal const double SubtitleHeight = 18;
    internal const double SymbolScale = .8;
    internal const double StatusRadius = 4;

    /// <summary>Returns true when group headers are drawn with the given options.</summary>
    public static bool IsDrawn(TopologyRenderOptions options) => options.IncludeGroups && options.IncludeGroupLabels;

    /// <summary>Returns the union used to describe the native painted header.</summary>
    public static ChartRect Bounds(TopologyChart chart, TopologyGroup group, TopologyRenderOptions options) =>
        Bounds(group, PaintedParts(chart, group, options));

    /// <summary>Describes already captured header parts without resolving font resources again.</summary>
    internal static ChartRect Bounds(TopologyGroup group, IReadOnlyList<ChartRect> parts) {
        if (parts.Count == 0) return new ChartRect(group.X, group.Y, 0, 0);
        var left = parts.Min(part => part.Left); var top = parts.Min(part => part.Top);
        return new ChartRect(left, top, parts.Max(part => part.Right) - left, parts.Max(part => part.Bottom) - top);
    }

    /// <summary>Returns the header block with the room layout keeps free below it, which nodes are moved out of.</summary>
    public static ChartRect ReservedBounds(TopologyChart chart, TopologyGroup group, TopologyRenderOptions options) {
        if (group.Presentation == TopologyGroupPresentation.Compartment) return new ChartRect(group.X, group.Y, 0, 0);
        var painted = Bounds(group, LayoutParts(chart, group, options));
        if (!group.ShowLabel) {
            var bottomWithoutCaptions = painted.Height > 0 ? painted.Bottom + 12 : group.Y;
            return new ChartRect(painted.Left, painted.Top, painted.Width, Math.Max(0, bottomWithoutCaptions - painted.Top));
        }
        var bottom = group.Y + (string.IsNullOrWhiteSpace(group.Subtitle) ? 54 : 74);
        // Only authored multiline captions expand the incumbent one-line reservation.
        if (group.TextStyle?.HasOverrides == true || group.LabelContent.IsStructured || CaptionRows(group.Label, options) > 1 || CaptionRows(group.Subtitle, options) > 1)
            bottom = Math.Max(bottom, painted.Bottom + 12);
        return new ChartRect(painted.Left, painted.Top, painted.Width, Math.Max(0, Math.Max(painted.Bottom, bottom) - painted.Top));
    }

    /// <summary>Returns captured painted parts, or policy-based reservations before native painting.</summary>
    internal static IReadOnlyList<ChartRect> PaintedParts(TopologyChart chart, TopologyGroup group, TopologyRenderOptions options) {
        if (!IsDrawn(options) || group.Presentation == TopologyGroupPresentation.Compartment) return Array.Empty<ChartRect>();
        if (chart.PreparedGroupHeaderParts != null)
            return chart.PreparedGroupHeaderParts.TryGetValue(group.Id, out var captured) ? captured : Array.Empty<ChartRect>();
        return LayoutParts(chart, group, options);
    }

    /// <summary>Returns the stable measurement-policy reservations used when the prepared routes were planned.</summary>
    internal static IReadOnlyList<ChartRect> LayoutParts(TopologyChart chart, TopologyGroup group, TopologyRenderOptions options) {
        if (!IsDrawn(options) || group.Presentation == TopologyGroupPresentation.Compartment) return Array.Empty<ChartRect>();
        if (chart.PreparedGroupHeaderLayoutParts != null)
            return chart.PreparedGroupHeaderLayoutParts.TryGetValue(group.Id, out var captured) ? captured : Array.Empty<ChartRect>();
        return (chart.GroupHeaderLayout ??= new TopologyGroupHeaderLayout()).Parts(chart, group, options);
    }

    internal static double SymbolReserve(TopologyGroup group, TopologyRenderOptions options) =>
        string.IsNullOrWhiteSpace(group.Symbol ?? ResolveGroupIcon(group, options)?.Symbol) && string.IsNullOrWhiteSpace(group.IconId) ? 0 : 28;

    internal static ChartRect TitleBounds(TopologyGroup group, TopologyRenderOptions options, double lineHeight = TitleHeight) =>
        TextBounds(group, options, 9, CaptionHeight(group.Label, options, group.TextStyle?.HasOverrides == true || group.LabelContent.IsStructured ? Math.Max(TitleHeight, lineHeight) : TitleHeight, lineHeight));

    internal static ChartRect SubtitleBounds(TopologyGroup group, TopologyRenderOptions options, bool fitted,
        double titleLineHeight = TitleHeight, double subtitleLineHeight = SubtitleHeight) =>
        TextBounds(group, options, (fitted ? 33 : 31) + CaptionHeight(group.Label, options, group.TextStyle?.HasOverrides == true || group.LabelContent.IsStructured ? Math.Max(TitleHeight, titleLineHeight) : TitleHeight, titleLineHeight) - TitleHeight,
            CaptionHeight(group.Subtitle, options, group.TextStyle?.HasOverrides == true ? Math.Max(SubtitleHeight, subtitleLineHeight) : SubtitleHeight, subtitleLineHeight));

    /// <summary>Counts explicit authored rows; wrapping never invents an unbounded header height.</summary>
    internal static int CaptionRows(string? value, TopologyRenderOptions options) =>
        TopologyTextLayout.AuthoredRowCount(value, options.AllowMultilineNodeLabels);

    private static double CaptionHeight(string? value, TopologyRenderOptions options, double ordinary, double measured) {
        var rows = CaptionRows(value, options);
        return rows == 1 ? ordinary : rows * Math.Max(ordinary, measured);
    }

    internal static ChartPoint SymbolCenter(TopologyGroup group) => new(group.X + 22, group.Y + 21);
    internal static ChartPoint StatusCenter(TopologyGroup group) => new(group.X + group.Width - 12, group.Y + 16);

    internal static ChartRect SymbolBounds(TopologyGroup group) {
        var center = SymbolCenter(group); var half = 13 * SymbolScale;
        return new ChartRect(center.X - half, center.Y - half, half * 2, half * 2);
    }

    internal static ChartRect StatusBounds(TopologyGroup group) {
        var center = StatusCenter(group);
        return new ChartRect(center.X - StatusRadius, center.Y - StatusRadius, StatusRadius * 2, StatusRadius * 2);
    }

    private static ChartRect TextBounds(TopologyGroup group, TopologyRenderOptions options, double top, double height) {
        var reserve = SymbolReserve(group, options);
        return new ChartRect(group.X + 12 + reserve, group.Y + top, Math.Max(0, group.Width - 24 - reserve), height);
    }

}
