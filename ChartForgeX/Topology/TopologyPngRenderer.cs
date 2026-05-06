using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

/// <summary>
/// Renders topology charts to dependency-free PNG images.
/// </summary>
public sealed class TopologyPngRenderer {
    /// <summary>
    /// Renders a topology chart to PNG bytes.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>A PNG image.</returns>
    public byte[] Render(TopologyChart chart, TopologyRenderOptions? options = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        options ??= new TopologyRenderOptions();
        if (options.Preset != TopologyViewPreset.Default) options.ApplyPreset(options.Preset);
        var prepared = TopologyLayoutEngine.Prepare(chart, options.View, options);
        var validation = new TopologyChartValidator().Validate(prepared);
        if (!validation.IsValid) throw new TopologyValidationException(validation);

        var width = (int)Math.Ceiling(prepared.Viewport.Width);
        var height = (int)Math.Ceiling(prepared.Viewport.Height);
        var theme = prepared.Theme ?? TopologyTheme.Light();
        var highlight = TopologyHighlightState.From(prepared, options);
        var canvas = new RgbaCanvas(width, height, Math.Max(1, options.PngSupersamplingScale), null, Math.Max(1, options.PngOutputScale));
        canvas.Clear(Color(theme.Background));
        if (options.IncludeTitle) DrawHeader(canvas, prepared, theme);
        if (options.IncludeGroups) DrawGroups(canvas, prepared, theme, options, highlight);
        DrawEdges(canvas, prepared, theme, options, highlight);
        if (options.IncludeEdgeLabels) DrawEdgeLabels(canvas, prepared, theme, options, highlight);
        DrawNodes(canvas, prepared, theme, options, highlight);
        if (options.IncludeStatusBadges) DrawStatusBadges(canvas, prepared, theme, options, highlight);
        if (options.IncludeLegend && prepared.Legend != null) DrawLegend(canvas, prepared, theme);
        return PngWriter.WriteRgba(canvas.OutputWidth, canvas.OutputHeight, canvas.ToOutputPixels());
    }

    private static void DrawHeader(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme) {
        if (string.IsNullOrWhiteSpace(chart.Title) && string.IsNullOrWhiteSpace(chart.Subtitle)) return;
        var x = chart.Viewport.Padding;
        var y = chart.Viewport.Padding + 8;
        if (!string.IsNullOrWhiteSpace(chart.Title)) canvas.DrawTextEmphasized(x, y, chart.Title!, Color(theme.Foreground), 22);
        if (!string.IsNullOrWhiteSpace(chart.Subtitle)) canvas.DrawText(x, y + 27, chart.Subtitle!, Color(theme.MutedForeground), 13);
    }

    private static void DrawGroups(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        foreach (var group in chart.Groups) {
            var isHighlighted = highlight.IsGroupHighlighted(group);
            var isSelected = IsSelected(options.SelectedGroupIds, group.Id);
            var accentValue = GroupAccentColor(group, theme);
            var accent = Color(accentValue);
            canvas.FillRoundedRect(group.X, group.Y, group.Width, group.Height, 12, Color(StatusFill(accentValue, theme.Background)));
            canvas.StrokeRoundedRect(group.X, group.Y, group.Width, group.Height, 12, WithAlpha(accent, isSelected ? (byte)230 : (byte)170), isSelected ? 2.4 : 1.2);
            if (!isHighlighted && highlight.IsActive) canvas.FillRoundedRect(group.X, group.Y, group.Width, group.Height, 12, WithAlpha(Color(theme.Background), 180));
            if (options.IncludeGroupLabels) {
                var cx = group.X + group.Width / 2;
                canvas.DrawCircle(cx - 52, group.Y + 26, 10, Color(StatusFill(accentValue, theme.Background)));
                canvas.DrawCircleOutline(cx - 52, group.Y + 26, 10, accent, 1);
                DrawGroupSymbol(canvas, group, cx - 52, group.Y + 26, accent);
                DrawCentered(canvas, cx, group.Y + 16, group.Label, accent, 16, true);
                if (!string.IsNullOrWhiteSpace(group.Subtitle)) DrawCentered(canvas, cx, group.Y + 38, group.Subtitle!, Color(theme.MutedForeground), 12, false);
            }
        }
    }

    private static string GroupAccentColor(TopologyGroup group, TopologyTheme theme) => string.IsNullOrWhiteSpace(group.Color) ? theme.StatusColor(group.Status) : group.Color!.Trim();

    private static void DrawGroupSymbol(RgbaCanvas canvas, TopologyGroup group, double cx, double cy, ChartColor color) {
        var symbol = string.IsNullOrWhiteSpace(group.Symbol) ? string.Empty : group.Symbol!.Trim();
        if (symbol.Equals("region", StringComparison.OrdinalIgnoreCase) || symbol.Equals("globe", StringComparison.OrdinalIgnoreCase)) {
            canvas.DrawCircleOutline(cx, cy, 5.8, color, 1.2);
            canvas.DrawLine(cx - 5.2, cy, cx + 5.2, cy, color, 1);
            canvas.DrawArc(cx, cy, 3.2, Math.PI / 2, Math.PI * 1.5, color, 1);
            canvas.DrawArc(cx, cy, 3.2, -Math.PI / 2, Math.PI / 2, color, 1);
            return;
        }

        if (string.IsNullOrWhiteSpace(symbol)) canvas.DrawCircleOutline(cx, cy, 4, color, 1);
        else DrawCentered(canvas, cx, cy - 5, TrimTo(symbol, 3), color, 7, true);
    }

    private static void DrawEdges(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        foreach (var edge in chart.Edges) {
            var points = EdgePoints(chart, edge, nodes);
            var baseColor = edge.IsMuted ? Color(theme.Border) : Color(theme.StatusColor(edge.Status));
            var color = highlight.IsEdgeHighlighted(edge) ? baseColor : WithAlpha(baseColor, (byte)Math.Round(255 * highlight.DimmedOpacity));
            var isSelected = IsSelected(options.SelectedEdgeIds, edge.Id);
            var dash = EdgePngDash(edge);
            for (var i = 0; i < points.Count - 1; i++) {
                if (!edge.IsMuted && dash.Dashed) {
                    canvas.DrawDashedLine(points[i].X, points[i].Y, points[i + 1].X, points[i + 1].Y, color, isSelected ? 3.4 : 2.2, dash.Dash, dash.Gap);
                } else {
                    canvas.DrawLine(points[i].X, points[i].Y, points[i + 1].X, points[i + 1].Y, color, isSelected ? 3.4 : edge.IsMuted ? 1.45 : 2.2);
                }
            }

            if (options.IncludeDirectionMarkers && edge.Direction is TopologyDirection.Forward or TopologyDirection.Bidirectional) DrawArrow(canvas, points[points.Count - 2], points[points.Count - 1], color);
            if (options.IncludeDirectionMarkers && edge.Direction is TopologyDirection.Backward or TopologyDirection.Bidirectional) DrawArrow(canvas, points[1], points[0], color);
        }
    }

    private static void DrawEdgeLabels(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        foreach (var layout in EdgeLabelLayouts(chart, options)) {
            var edge = layout.Edge;
            var cx = layout.CenterX;
            var cy = layout.CenterY;
            if (options.IncludeEdgeLabelBackplates) {
                canvas.FillRoundedRect(cx - layout.Width / 2, cy - layout.Height / 2, layout.Width, layout.Height, 9, Color(theme.Background));
                canvas.StrokeRoundedRect(cx - layout.Width / 2, cy - layout.Height / 2, layout.Width, layout.Height, 9, Color(theme.Border), 1);
            }
            var baseColor = edge.IsMuted ? Color(theme.MutedForeground) : Color(theme.StatusColor(edge.Status));
            var color = highlight.IsEdgeHighlighted(edge) ? baseColor : WithAlpha(baseColor, (byte)Math.Round(255 * highlight.DimmedOpacity));
            DrawEdgeLabelLines(canvas, layout, cx, cy, color, Color(theme.MutedForeground));
        }
    }

    private static void DrawEdgeLabelLines(RgbaCanvas canvas, TopologyEdgeLabelLayout layout, double cx, double cy, ChartColor primaryColor, ChartColor secondaryColor) {
        var lines = new List<(string Text, bool Primary)>();
        if (!string.IsNullOrWhiteSpace(layout.Label)) lines.Add((layout.Label, true));
        if (!string.IsNullOrWhiteSpace(layout.SecondaryLabel)) lines.Add((layout.SecondaryLabel, false));
        if (!string.IsNullOrWhiteSpace(layout.TertiaryLabel)) lines.Add((layout.TertiaryLabel, false));
        var start = cy - (lines.Count - 1) * 8;
        for (var i = 0; i < lines.Count; i++) {
            var line = lines[i];
            DrawCentered(canvas, cx, start + i * 16 - (line.Primary ? 7 : 6), line.Text, line.Primary ? primaryColor : secondaryColor, line.Primary ? 12 : 10, line.Primary);
        }
    }

    private static void DrawNodes(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        foreach (var node in chart.Nodes) {
            var isHighlighted = highlight.IsNodeHighlighted(node);
            var isSelected = IsSelected(options.SelectedNodeIds, node.Id);
            var accent = Color(NodeAccentColor(node, theme));
            var displayMode = EffectiveNodeDisplayMode(node, options);
            if (displayMode == TopologyNodeDisplayMode.Dot) {
                var radius = Math.Max(5, Math.Min(node.Width, node.Height) / 2);
                if (isSelected) canvas.DrawCircleOutline(CenterX(node), CenterY(node), radius + 5, WithAlpha(accent, 140), 2.4);
                canvas.DrawCircle(CenterX(node), CenterY(node), radius + 2, Color(theme.Background));
                canvas.DrawCircle(CenterX(node), CenterY(node), radius, accent);
                DrawNodeBadge(canvas, node, theme, accent, displayMode);
                continue;
            }

            var radiusRect = displayMode == TopologyNodeDisplayMode.Pill ? node.Height / 2 : displayMode is TopologyNodeDisplayMode.Icon or TopologyNodeDisplayMode.Tile ? 12 : 10;
            canvas.FillRoundedRect(node.X + 2, node.Y + 5, node.Width, node.Height, 10, ChartColor.FromRgba(15, 23, 42, 18));
            canvas.FillRoundedRect(node.X, node.Y, node.Width, node.Height, radiusRect, Color(theme.Card));
            canvas.StrokeRoundedRect(node.X, node.Y, node.Width, node.Height, radiusRect, accent, isSelected ? 2.8 : 1.5);
            if (!isHighlighted && highlight.IsActive) canvas.FillRoundedRect(node.X, node.Y, node.Width, node.Height, radiusRect, WithAlpha(Color(theme.Background), 185));
            DrawNodeIcon(canvas, node, theme, accent, displayMode);
            if (options.IncludeNodeLabels && displayMode != TopologyNodeDisplayMode.Icon) {
                if (displayMode == TopologyNodeDisplayMode.Tile) {
                    DrawCentered(canvas, CenterX(node), node.Y + node.Height + 4, TrimTo(node.Label, 14), Color(theme.Foreground), 10.5, true);
                    if (options.IncludeTileSubtitles && !string.IsNullOrWhiteSpace(node.Subtitle)) DrawTileSubtitle(canvas, node, theme, accent);
                    DrawNodeBadge(canvas, node, theme, accent, displayMode);
                    continue;
                }

                var textX = displayMode == TopologyNodeDisplayMode.Pill ? node.X + 34 : node.X + 42;
                var titleY = displayMode == TopologyNodeDisplayMode.Pill ? node.Y + node.Height / 2 - 7 : node.Y + (displayMode == TopologyNodeDisplayMode.CompactCard ? 13 : 17);
                var titleSize = displayMode == TopologyNodeDisplayMode.Pill ? 11.5 : displayMode == TopologyNodeDisplayMode.CompactCard ? 11.5 : 12.5;
                canvas.DrawTextEmphasized(textX, titleY, TrimTo(node.Label, NodeTitleMaxLength(displayMode)), Color(theme.Foreground), titleSize);
                if (displayMode != TopologyNodeDisplayMode.Pill && !string.IsNullOrWhiteSpace(node.Subtitle)) {
                    if (options.CardSubtitleMode == TopologyCardSubtitleMode.Chip) DrawCardSubtitleChip(canvas, node, theme, accent, displayMode);
                    else canvas.DrawText(textX, node.Y + (displayMode == TopologyNodeDisplayMode.CompactCard ? 31 : 37), TrimTo(node.Subtitle!, NodeLabelMaxLength), Color(theme.MutedForeground), 10.5);
                }
            }

            DrawNodeBadge(canvas, node, theme, accent, displayMode);
        }
    }

    private static void DrawCardSubtitleChip(RgbaCanvas canvas, TopologyNode node, TopologyTheme theme, ChartColor accent, TopologyNodeDisplayMode displayMode) {
        var subtitle = TrimTo(node.Subtitle!, displayMode == TopologyNodeDisplayMode.CompactCard ? 12 : 16);
        var width = Math.Min(Math.Max(48, RgbaCanvas.MeasureTextEmphasizedWidth(subtitle, 8.5, null) + 18), Math.Max(48, node.Width - 50));
        var height = 17.0;
        var x = node.X + 42;
        var y = node.Y + (displayMode == TopologyNodeDisplayMode.CompactCard ? 31 : node.Height - 22);
        canvas.FillRoundedRect(x, y, width, height, 8.5, Color(StatusFill(NodeAccentColor(node, theme), theme.Background)));
        canvas.StrokeRoundedRect(x, y, width, height, 8.5, WithAlpha(accent, 115), 1);
        DrawCentered(canvas, x + width / 2, y + 3.8, subtitle, accent, 8.5, true);
    }

    private static string NodeAccentColor(TopologyNode node, TopologyTheme theme) => string.IsNullOrWhiteSpace(node.Color) ? theme.StatusColor(node.Status) : node.Color!.Trim();

    private static bool IsSelected(List<string> ids, string id) {
        foreach (var selectedId in ids) {
            if (string.Equals(selectedId, id, StringComparison.Ordinal)) return true;
        }

        return false;
    }

    private static void DrawTileSubtitle(RgbaCanvas canvas, TopologyNode node, TopologyTheme theme, ChartColor accent) {
        var subtitle = TrimTo(node.Subtitle!, 16);
        var width = Math.Min(Math.Max(46, RgbaCanvas.MeasureTextEmphasizedWidth(subtitle, 8.5, null) + 18), Math.Max(46, node.Width + 28));
        var x = CenterX(node) - width / 2;
        var y = node.Y + node.Height + 14;
        canvas.FillRoundedRect(x, y, width, 17, 8.5, Color(StatusFill(NodeAccentColor(node, theme), theme.Background)));
        canvas.StrokeRoundedRect(x, y, width, 17, 8.5, WithAlpha(accent, 115), 1);
        DrawCentered(canvas, CenterX(node), y + 3.8, subtitle, Color(theme.MutedForeground), 8.5, true);
    }

    private static void DrawNodeBadge(RgbaCanvas canvas, TopologyNode node, TopologyTheme theme, ChartColor color, TopologyNodeDisplayMode displayMode) {
        var badge = NodeBadge(node);
        if (string.IsNullOrWhiteSpace(badge)) return;
        var width = Math.Max(18, RgbaCanvas.MeasureTextEmphasizedWidth(badge, 8.5, null) + 12);
        var height = 18.0;
        var x = displayMode == TopologyNodeDisplayMode.Dot ? CenterX(node) + 8 : displayMode == TopologyNodeDisplayMode.Icon ? CenterX(node) - width / 2 : node.X + node.Width - width - 6;
        var y = displayMode == TopologyNodeDisplayMode.Dot ? CenterY(node) - 21 : displayMode == TopologyNodeDisplayMode.Icon ? node.Y + node.Height + 4 : displayMode == TopologyNodeDisplayMode.Tile ? node.Y - 8 : node.Y + node.Height - height - 6;
        canvas.FillRoundedRect(x, y, width, height, 9, Color(StatusFill(theme.StatusColor(node.Status), theme.Background)));
        canvas.StrokeRoundedRect(x, y, width, height, 9, color, 1);
        DrawCentered(canvas, x + width / 2, y + 4, badge, color, 8.5, true);
    }

    private static void DrawNodeIcon(RgbaCanvas canvas, TopologyNode node, TopologyTheme theme, ChartColor status, TopologyNodeDisplayMode displayMode) {
        var cx = displayMode is TopologyNodeDisplayMode.Icon or TopologyNodeDisplayMode.Tile ? CenterX(node) : node.X + 22;
        var cy = displayMode == TopologyNodeDisplayMode.Tile ? node.Y + node.Height / 2 - 1 : node.Y + node.Height / 2;
        var size = displayMode == TopologyNodeDisplayMode.Pill ? 18 : displayMode == TopologyNodeDisplayMode.Icon ? 26 : displayMode == TopologyNodeDisplayMode.Tile ? 24 : 22;
        canvas.FillRoundedRect(cx - size / 2, cy - size / 2, size, size, 6, Color(StatusFill(theme.StatusColor(node.Status), theme.Background)));
        canvas.StrokeRoundedRect(cx - size / 2, cy - size / 2, size, size, 6, status, 1);
        if (!DrawInfrastructureGlyph(canvas, node, cx, cy, status)) DrawCentered(canvas, cx, cy - 6, NodeGlyph(node), status, displayMode == TopologyNodeDisplayMode.Pill ? 7.5 : 8.5, true);
    }

    private static bool DrawInfrastructureGlyph(RgbaCanvas canvas, TopologyNode node, double cx, double cy, ChartColor color) {
        switch (node.Kind) {
            case TopologyNodeKind.Hub:
            case TopologyNodeKind.Branch:
            case TopologyNodeKind.Location:
                canvas.DrawLine(cx - 6, cy + 7, cx - 6, cy - 7, color, 1.4);
                canvas.DrawLine(cx - 6, cy - 7, cx + 6, cy - 7, color, 1.4);
                canvas.DrawLine(cx + 6, cy - 7, cx + 6, cy + 7, color, 1.4);
                canvas.DrawLine(cx - 2, cy + 7, cx - 2, cy + 2, color, 1.4);
                canvas.DrawLine(cx - 2, cy + 2, cx + 2, cy + 2, color, 1.4);
                canvas.DrawLine(cx + 2, cy + 2, cx + 2, cy + 7, color, 1.4);
                canvas.DrawLine(cx - 3.5, cy - 3, cx - 0.5, cy - 3, color, 1.4);
                canvas.DrawLine(cx + 2.5, cy - 3, cx + 5.5, cy - 3, color, 1.4);
                canvas.DrawLine(cx - 3.5, cy + 1, cx - 0.5, cy + 1, color, 1.4);
                canvas.DrawLine(cx + 2.5, cy + 1, cx + 5.5, cy + 1, color, 1.4);
                return true;
            case TopologyNodeKind.Server:
                canvas.StrokeRect(cx - 7, cy - 6, 14, 5, color, 1);
                canvas.StrokeRect(cx - 7, cy + 2, 14, 5, color, 1);
                canvas.DrawLine(cx + 4.5, cy - 3.5, cx + 5.5, cy - 3.5, color, 1.4);
                canvas.DrawLine(cx + 4.5, cy + 4.5, cx + 5.5, cy + 4.5, color, 1.4);
                return true;
            case TopologyNodeKind.Network:
            case TopologyNodeKind.NetworkSegment:
                canvas.DrawLine(cx - 6, cy + 5, cx, cy - 5, color, 1.3);
                canvas.DrawLine(cx, cy - 5, cx + 6, cy + 5, color, 1.3);
                canvas.DrawLine(cx, cy - 5, cx, cy + 7, color, 1.3);
                canvas.DrawCircle(cx, cy - 6, 2.3, color);
                canvas.DrawCircle(cx - 7, cy + 6, 2.3, color);
                canvas.DrawCircle(cx + 7, cy + 6, 2.3, color);
                return true;
            case TopologyNodeKind.Service:
                canvas.DrawCircleOutline(cx, cy, 5.5, color, 1.4);
                canvas.DrawLine(cx, cy - 9, cx, cy - 7, color, 1.4);
                canvas.DrawLine(cx, cy + 7, cx, cy + 9, color, 1.4);
                canvas.DrawLine(cx - 9, cy, cx - 7, cy, color, 1.4);
                canvas.DrawLine(cx + 7, cy, cx + 9, cy, color, 1.4);
                return true;
            case TopologyNodeKind.Queue:
                canvas.DrawLine(cx - 7, cy - 6, cx + 7, cy - 6, color, 1.7);
                canvas.DrawLine(cx - 7, cy, cx + 7, cy, color, 1.7);
                canvas.DrawLine(cx - 7, cy + 6, cx + 7, cy + 6, color, 1.7);
                return true;
            default:
                return false;
        }
    }

    private static int NodeTitleMaxLength(TopologyNodeDisplayMode displayMode) {
        return displayMode switch {
            TopologyNodeDisplayMode.CompactCard => 11,
            TopologyNodeDisplayMode.Pill => 14,
            _ => NodeLabelMaxLength
        };
    }

    private static void DrawStatusBadges(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        foreach (var node in chart.Nodes) {
            if (EffectiveNodeDisplayMode(node, options) == TopologyNodeDisplayMode.Dot) continue;
            if (highlight.IsActive && !highlight.IsNodeHighlighted(node)) continue;
            var color = Color(theme.StatusColor(node.Status));
            var cx = node.X + node.Width - 11;
            var cy = node.Y + 11;
            canvas.DrawCircle(cx, cy, 9, Color(theme.Background));
            canvas.DrawCircle(cx, cy, 7, color);
            DrawCentered(canvas, cx, cy - 5, StatusGlyph(node.Status), ChartColor.White, 8, true);
        }
    }

    private static void DrawLegend(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme) {
        var legend = chart.Legend!;
        var x = chart.Viewport.Padding;
        var height = LegendHeight(legend);
        var y = chart.Viewport.Height - chart.Viewport.Padding - height;
        var width = Math.Min(LegendMaxWidth, chart.Viewport.Width - chart.Viewport.Padding * 2);
        canvas.FillRoundedRect(x, y, width, height, 12, Color(theme.Card));
        canvas.StrokeRoundedRect(x, y, width, height, 12, Color(theme.Border), 1);
        if (!string.IsNullOrWhiteSpace(legend.Title)) canvas.DrawTextEmphasized(x + 16, y + 11, legend.Title!, Color(theme.Foreground), 12);
        for (var i = 0; i < legend.Items.Count; i++) {
            var item = legend.Items[i];
            var col = i % LegendColumns;
            var row = i / LegendColumns;
            var itemX = x + 18 + col * LegendItemColumnWidth;
            var itemY = y + LegendFirstItemOffsetY + row * LegendItemRowHeight;
            var color = Color(item.Color ?? (item.Status.HasValue ? theme.StatusColor(item.Status.Value) : theme.Accent));
            if (item.Kind == TopologyLegendItemKind.Edge) canvas.DrawDashedLine(itemX, itemY - 4, itemX + 24, itemY - 4, color, 2, 6, 4);
            else if (item.Kind == TopologyLegendItemKind.Node && !string.IsNullOrWhiteSpace(item.Symbol)) {
                canvas.FillRoundedRect(itemX, itemY - 13, 16, 16, 4, Color(StatusFill(item.Color ?? theme.Accent, theme.Background)));
                canvas.StrokeRoundedRect(itemX, itemY - 13, 16, 16, 4, color, 1);
                DrawCentered(canvas, itemX + 8, itemY - 11, TrimTo(item.Symbol!.Trim(), 4), color, 6.5, true);
            } else canvas.DrawCircle(itemX + 8, itemY - 5, 6, color);
            canvas.DrawText(itemX + 32, itemY - 14, item.Label, Color(theme.MutedForeground), 11);
        }
    }

    private static void DrawArrow(RgbaCanvas canvas, ChartPoint from, ChartPoint to, ChartColor color) {
        var angle = Math.Atan2(to.Y - from.Y, to.X - from.X);
        const double length = 10;
        const double spread = 0.52;
        var p1 = new ChartPoint(to.X, to.Y);
        var p2 = new ChartPoint(to.X - Math.Cos(angle - spread) * length, to.Y - Math.Sin(angle - spread) * length);
        var p3 = new ChartPoint(to.X - Math.Cos(angle + spread) * length, to.Y - Math.Sin(angle + spread) * length);
        canvas.FillPolygon(new[] { p1, p2, p3 }, color);
    }

    private static void DrawCentered(RgbaCanvas canvas, double centerX, double y, string text, ChartColor color, double fontSize, bool emphasized) {
        var width = emphasized ? RgbaCanvas.MeasureTextEmphasizedWidth(text, fontSize, null) : RgbaCanvas.MeasureTextWidth(text, fontSize, null);
        if (emphasized) canvas.DrawTextEmphasized(centerX - width / 2, y, text, color, fontSize);
        else canvas.DrawText(centerX - width / 2, y, text, color, fontSize);
    }

    private static ChartColor Color(string value) => ChartColor.FromHex(value);

    private static ChartColor WithAlpha(ChartColor color, byte alpha) => ChartColor.FromRgba(color.R, color.G, color.B, alpha);

}
