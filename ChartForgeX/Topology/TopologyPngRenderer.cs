using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

/// <summary>
/// Renders topology charts to dependency-free PNG images.
/// </summary>
public sealed partial class TopologyPngRenderer {
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
        var requestedWidth = (int)Math.Ceiling(chart.Viewport.Width);
        var requestedHeight = (int)Math.Ceiling(chart.Viewport.Height);
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
        if (prepared.LayoutMode == TopologyLayoutMode.Geographic) DrawGeographicFrame(canvas, prepared, theme, options);
        if (prepared.LayoutMode == TopologyLayoutMode.Geographic && options.IncludeGeographicRegionHulls) DrawGeographicRegionHulls(canvas, prepared, theme, options);
        if (options.IncludeGroups) DrawGroups(canvas, prepared, theme, options, highlight);
        DrawEdges(canvas, prepared, theme, options, highlight);
        if (options.IncludeEdgeLabels) DrawEdgeLabels(canvas, prepared, theme, options, highlight);
        DrawNodes(canvas, prepared, theme, options, highlight);
        if (options.IncludeStatusBadges) DrawStatusBadges(canvas, prepared, theme, options, highlight);
        if (prepared.LayoutMode == TopologyLayoutMode.Geographic) DrawGeographicCallouts(canvas, prepared, theme, options, highlight);
        if (options.IncludeLegend && prepared.Legend != null) DrawLegend(canvas, prepared, theme);
        var pixels = canvas.ToOutputPixels();
        if (!options.FitContentToViewport) return PngWriter.WriteRgba(canvas.OutputWidth, canvas.OutputHeight, pixels);
        var targetWidth = Math.Max(1, requestedWidth * Math.Max(1, options.PngOutputScale));
        var targetHeight = Math.Max(1, requestedHeight * Math.Max(1, options.PngOutputScale));
        return PngWriter.WriteRgba(targetWidth, targetHeight, FitPixels(pixels, canvas.OutputWidth, canvas.OutputHeight, targetWidth, targetHeight, Color(theme.Background)));
    }

    private static void DrawGeographicFrame(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme, TopologyRenderOptions options) {
        var map = TopologyMapProjection.MapRect(chart);
        var softMap = UseSoftMapBackground(options);
        canvas.FillRoundedRect(map.Left, map.Top, map.Width, map.Height, softMap ? 12 : 16, Color(StatusFill(theme.Accent, theme.Background, softMap ? 0.035 : 0.10)));
        canvas.StrokeRoundedRect(map.Left, map.Top, map.Width, map.Height, softMap ? 12 : 16, Color(theme.Border), 1);
        DrawGeographicLandLayer(canvas, chart, map, theme, options);
        for (var i = 1; i < 4; i++) {
            var longitude = chart.MapViewport.MinimumLongitude + (chart.MapViewport.MaximumLongitude - chart.MapViewport.MinimumLongitude) * i / 4.0;
            var x = TopologyMapProjection.Project(map, chart.MapViewport, longitude, chart.MapViewport.MinimumLatitude).X;
            canvas.DrawLine(x, map.Top, x, map.Bottom, WithAlpha(Color(theme.Border), softMap ? (byte)58 : (byte)110), softMap ? 0.55 : 0.8);
        }

        for (var i = 1; i < 3; i++) {
            var latitude = chart.MapViewport.MinimumLatitude + (chart.MapViewport.MaximumLatitude - chart.MapViewport.MinimumLatitude) * i / 3.0;
            var y = TopologyMapProjection.Project(map, chart.MapViewport, chart.MapViewport.MinimumLongitude, latitude).Y;
            canvas.DrawLine(map.Left, y, map.Right, y, WithAlpha(Color(theme.Border), softMap ? (byte)46 : (byte)88), softMap ? 0.55 : 0.8);
        }
    }

    private static void DrawGeographicLandLayer(RgbaCanvas canvas, TopologyChart chart, ChartRect map, TopologyTheme theme, TopologyRenderOptions options) {
        var softMap = UseSoftMapBackground(options);
        var land = WithAlpha(Color(theme.MutedForeground), softMap ? (byte)36 : (byte)28);
        var boundaryColor = WithAlpha(Color(theme.MutedForeground), softMap ? (byte)42 : (byte)56);
        var boundaries = TopologyMapProjection.BoundaryLines(chart.MapViewport);
        foreach (var boundary in boundaries) {
            var points = ProjectBoundary(boundary, map, chart.MapViewport);
            if (TopologyMapProjection.CanFillBoundary(boundary)) canvas.FillPolygon(points, land);
            for (var i = 1; i < points.Count; i++) canvas.DrawLine(points[i - 1].X, points[i - 1].Y, points[i].X, points[i].Y, boundaryColor, softMap ? 0.62 : 0.75);
        }

        if (softMap && boundaries.Length > 0) return;

        var dotColor = WithAlpha(Color(theme.MutedForeground), softMap ? (byte)18 : (byte)27);
        var radius = TopologyMapProjection.LandDotRadius(map, chart.MapViewport);
        foreach (var point in TopologyMapProjection.LandDots(chart.MapViewport)) {
            foreach (var offset in TopologyMapProjection.LandOffsets(chart.MapViewport)) {
                var longitude = point.X + offset.X;
                var latitude = point.Y + offset.Y;
                if (!TopologyMapProjection.IsVisible(chart.MapViewport, longitude, latitude)) continue;
                var projected = TopologyMapProjection.Project(map, chart.MapViewport, longitude, latitude);
                canvas.DrawCircle(projected.X, projected.Y, radius, dotColor);
            }
        }
    }

    private static List<ChartPoint> ProjectBoundary(ChartPoint[] boundary, ChartRect map, ChartMapViewport viewport) {
        var points = new List<ChartPoint>(boundary.Length);
        foreach (var item in boundary) {
            var projected = TopologyMapProjection.Project(map, viewport, item.X, item.Y);
            points.Add(new ChartPoint(projected.X, projected.Y));
        }

        return points;
    }

    private static void DrawGeographicRegionHulls(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme, TopologyRenderOptions options) {
        var map = TopologyMapProjection.MapRect(chart);
        foreach (var group in chart.Groups) {
            if (!group.Longitude.HasValue || !group.Latitude.HasValue) continue;
            if (!TopologyMapProjection.IsVisible(chart.MapViewport, group.Longitude.Value, group.Latitude.Value)) continue;
            var anchor = TopologyMapProjection.Project(map, chart.MapViewport, group.Longitude.Value, group.Latitude.Value);
            var accentValue = GroupAccentColor(group, theme, options);
            var accent = Color(accentValue);
            var radius = GeographicRegionHullRadius(chart, group, new ChartPoint(anchor.X, anchor.Y), map, options);
            canvas.DrawCircle(anchor.X, anchor.Y, radius, Color(StatusFill(accentValue, theme.Background, IsMonitoringDashboardStyle(options) ? 0.22 : 0.16)));
            canvas.DrawCircleOutline(anchor.X, anchor.Y, radius, WithAlpha(accent, IsMonitoringDashboardStyle(options) ? (byte)74 : (byte)98), IsMonitoringDashboardStyle(options) ? 1.1 : 1.4);
        }
    }

    private static double GeographicRegionHullRadius(TopologyChart chart, TopologyGroup group, ChartPoint anchor, ChartRect map, TopologyRenderOptions options) {
        var radius = options.GeographicRegionHullMinRadius;
        foreach (var node in chart.Nodes) {
            if (!string.Equals(node.GroupId, group.Id, StringComparison.Ordinal) || !node.Longitude.HasValue || !node.Latitude.HasValue) continue;
            if (EffectiveNodeDisplayMode(node, options) == TopologyNodeDisplayMode.Hidden) continue;
            if (!TopologyMapProjection.IsVisible(chart.MapViewport, node.Longitude.Value, node.Latitude.Value)) continue;
            var projected = TopologyMapProjection.Project(map, chart.MapViewport, node.Longitude.Value, node.Latitude.Value);
            var dx = projected.X - anchor.X;
            var dy = projected.Y - anchor.Y;
            radius = Math.Max(radius, Math.Sqrt(dx * dx + dy * dy) + options.GeographicRegionHullPadding);
        }

        return Math.Min(options.GeographicRegionHullMaxRadius, Math.Max(options.GeographicRegionHullMinRadius, radius));
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
            var accentValue = GroupAccentColor(group, theme, options);
            var accent = Color(accentValue);
            var monitoring = IsMonitoringDashboardStyle(options);
            var radius = monitoring ? 10 : 12;
            canvas.FillRoundedRect(group.X, group.Y, group.Width, group.Height, radius, Color(GroupFill(accentValue, theme, options)));
            canvas.StrokeRoundedRect(group.X, group.Y, group.Width, group.Height, radius, WithAlpha(accent, isSelected ? (byte)(monitoring ? 210 : 230) : UseNeutralGroupSurface(options) ? (byte)98 : (byte)(monitoring ? 118 : 170)), isSelected ? (monitoring ? 2.2 : 2.4) : 1.2);
            if (!isHighlighted && highlight.IsActive) canvas.FillRoundedRect(group.X, group.Y, group.Width, group.Height, radius, WithAlpha(Color(theme.Background), 180));
            if (options.IncludeGroupLabels) {
                var cx = group.X + group.Width / 2;
                var groupIcon = ResolveGroupIcon(group, options);
                var renderSymbol = !monitoring || !string.IsNullOrWhiteSpace(group.Symbol) || groupIcon != null;
                var neutralSurface = monitoring && UseNeutralGroupSurface(options);
                var groupLabelWidth = GroupHeaderLabelWidth(group, options, renderSymbol);
                var groupLabelSize = FitFontSize(group.Label, groupLabelWidth, 16, 12, true);
                var groupLabel = TrimToEstimatedWidth(group.Label, groupLabelWidth, groupLabelSize, true);
                var textWidth = RgbaCanvas.MeasureTextEmphasizedWidth(groupLabel, groupLabelSize, null);
                if (renderSymbol && !neutralSurface) {
                    var symbolCx = cx - (textWidth + 30) / 2 + 10;
                    canvas.DrawCircle(symbolCx, group.Y + 26, 10, Color(StatusFill(accentValue, theme.Background)));
                    canvas.DrawCircleOutline(symbolCx, group.Y + 26, 10, accent, 1);
                    DrawGroupSymbol(canvas, group, symbolCx, group.Y + 26, accent, options);
                }

                if (neutralSurface) {
                    var labelX = group.X + 22;
                    var labelWidth = GroupHeaderLabelWidth(group, options, false);
                    if (renderSymbol) {
                        var symbolCx = group.X + 22;
                        canvas.DrawCircle(symbolCx, group.Y + 26, 9.5, Color(StatusFill(accentValue, theme.Background)));
                        canvas.DrawCircleOutline(symbolCx, group.Y + 26, 9.5, accent, 1);
                        DrawGroupSymbol(canvas, group, symbolCx, group.Y + 26, accent, options);
                        labelX = group.X + 42;
                        labelWidth = GroupHeaderLabelWidth(group, options, true);
                    }

                    var neutralLabelSize = FitFontSize(group.Label, labelWidth, 15, 12, true);
                    canvas.DrawTextEmphasized(labelX, group.Y + 17, TrimToEstimatedWidth(group.Label, labelWidth, neutralLabelSize, true), accent, neutralLabelSize);
                    DrawGroupStatusDot(canvas, group, group.X + group.Width - 22, group.Y + 26, theme, options);
                    if (!string.IsNullOrWhiteSpace(group.Subtitle)) canvas.DrawText(labelX, group.Y + 36, TrimToEstimatedWidth(group.Subtitle!, labelWidth, 11, false), Color(theme.MutedForeground), 11);
                    continue;
                }

                if (renderSymbol) canvas.DrawTextEmphasized(cx - (textWidth + 30) / 2 + 30, group.Y + 16, groupLabel, accent, groupLabelSize);
                else DrawCentered(canvas, cx, group.Y + 16, groupLabel, accent, groupLabelSize, true);
                DrawGroupStatusDot(canvas, group, group.X + group.Width - 22, group.Y + 26, theme, options);
                if (!string.IsNullOrWhiteSpace(group.Subtitle)) DrawCentered(canvas, cx, group.Y + 38, TrimToEstimatedWidth(group.Subtitle!, group.Width - 44, 12, false), Color(theme.MutedForeground), 12, false);
            }
        }
    }

    private static void DrawGroupStatusDot(RgbaCanvas canvas, TopologyGroup group, double cx, double cy, TopologyTheme theme, TopologyRenderOptions options) {
        if (!options.IncludeGroupStatusDots || !IsMonitoringDashboardStyle(options) || group.Status == TopologyHealthStatus.Unknown) return;
        var statusColor = Color(theme.StatusColor(group.Status));
        canvas.DrawCircle(cx, cy, 7.3, Color(theme.Background));
        canvas.DrawCircle(cx, cy, 5.3, statusColor);
    }

    private static double GroupHeaderLabelWidth(TopologyGroup group, TopologyRenderOptions options, bool includesLeadingSymbol) {
        var statusReserve = options.IncludeGroupStatusDots && IsMonitoringDashboardStyle(options) && group.Status != TopologyHealthStatus.Unknown ? 38 : 0;
        var symbolReserve = includesLeadingSymbol ? 42 : 0;
        return Math.Max(36, group.Width - 44 - statusReserve - symbolReserve);
    }

    private static string GroupAccentColor(TopologyGroup group, TopologyTheme theme, TopologyRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(group.Color)) return group.Color!.Trim();
        var icon = ResolveGroupIcon(group, options);
        return !string.IsNullOrWhiteSpace(icon?.Color) ? icon!.Color!.Trim() : theme.StatusColor(group.Status);
    }

    private static void DrawGroupSymbol(RgbaCanvas canvas, TopologyGroup group, double cx, double cy, ChartColor color, TopologyRenderOptions options) {
        var symbol = string.IsNullOrWhiteSpace(group.Symbol) ? string.Empty : group.Symbol!.Trim();
        if (symbol.Equals("region", StringComparison.OrdinalIgnoreCase) || symbol.Equals("globe", StringComparison.OrdinalIgnoreCase)) {
            canvas.DrawCircleOutline(cx, cy, 5.8, color, 1.2);
            canvas.DrawLine(cx - 5.2, cy, cx + 5.2, cy, color, 1);
            canvas.DrawArc(cx, cy, 3.2, Math.PI / 2, Math.PI * 1.5, color, 1);
            canvas.DrawArc(cx, cy, 3.2, -Math.PI / 2, Math.PI / 2, color, 1);
            return;
        }

        var icon = ResolveGroupIcon(group, options);
        if (icon != null && DrawGroupIconSymbol(canvas, icon, cx, cy, color, options)) return;

        if (string.IsNullOrWhiteSpace(symbol)) canvas.DrawCircleOutline(cx, cy, 4, color, 1);
        else DrawCentered(canvas, cx, cy - 5, TrimTo(symbol, 3), color, 7, true);
    }

    private static bool DrawGroupIconSymbol(RgbaCanvas canvas, TopologyIconDefinition icon, double cx, double cy, ChartColor color, TopologyRenderOptions options) {
        if (icon.Shape == TopologyIconShape.Cloud) {
            canvas.DrawCircleOutline(cx - 3, cy + 1, 4.2, color, 1.2);
            canvas.DrawCircleOutline(cx + 3, cy - 1, 5, color, 1.2);
            return true;
        }

        var node = new TopologyNode { Id = "__group-icon", Label = icon.Label, IconId = icon.QualifiedId, Kind = icon.NodeKind, Symbol = icon.Symbol };
        return DrawInfrastructureGlyph(canvas, node, cx, cy, color, options);
    }

    private static void DrawEdges(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        foreach (var (edge, _) in OrderedEdgesForRendering(chart, options)) {
            var points = EdgePoints(chart, edge, nodes);
            var isSelected = IsSelected(options.SelectedEdgeIds, edge.Id);
            var baseColor = Color(EdgeColor(edge, theme, options));
            var routeOpacity = isSelected ? 1 : EdgeOpacity(edge, options);
            if (!highlight.IsEdgeHighlighted(edge)) routeOpacity *= highlight.DimmedOpacity;
            var color = WithAlpha(baseColor, (byte)Math.Round(255 * Clamp(routeOpacity, 0, 1)));
            var dash = EdgePngDash(edge);
            var routePoints = IsGeographicCurve(chart, edge, nodes)
                ? GeographicCurveSamplePoints(chart, edge, nodes, points)
                : points;
            var width = EdgeStrokeWidth(edge, isSelected, options);
            if (ShouldRenderMonitoringRouteHalo(chart, edge, nodes, options)) DrawEdgeRoute(canvas, routePoints, WithAlpha(Color(theme.Background), HighlightAlpha(224, highlight.IsEdgeHighlighted(edge), highlight)), width + (IsGeographicCurve(chart, edge, nodes) ? 4.2 : 3.4), false, 0, 0);
            DrawEdgeRoute(canvas, routePoints, color, width, !edge.IsMuted && dash.Dashed, dash.Dash, dash.Gap);

            if (options.IncludeDirectionMarkers && edge.Direction is TopologyDirection.Forward or TopologyDirection.Bidirectional) DrawArrow(canvas, routePoints[routePoints.Count - 2], routePoints[routePoints.Count - 1], color);
            if (options.IncludeDirectionMarkers && edge.Direction is TopologyDirection.Backward or TopologyDirection.Bidirectional) DrawArrow(canvas, routePoints[1], routePoints[0], color);
        }
    }

    private static void DrawEdgeRoute(RgbaCanvas canvas, IReadOnlyList<ChartPoint> points, ChartColor color, double width, bool dashed, double dash, double gap) {
        for (var i = 0; i < points.Count - 1; i++) {
            if (dashed) canvas.DrawDashedLine(points[i].X, points[i].Y, points[i + 1].X, points[i + 1].Y, color, width, dash, gap);
            else canvas.DrawLine(points[i].X, points[i].Y, points[i + 1].X, points[i + 1].Y, color, width);
        }
    }

    private static void DrawEdgeLabels(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        foreach (var (layout, _) in OrderedEdgeLabelsForRendering(chart, options)) {
            var edge = layout.Edge;
            var cx = layout.CenterX;
            var cy = layout.CenterY;
            if (options.IncludeEdgeLabelBackplates) {
                var radius = IsMonitoringDashboardStyle(options) ? 7 : 9;
                canvas.FillRoundedRect(cx - layout.Width / 2, cy - layout.Height / 2, layout.Width, layout.Height, radius, Color(IsMonitoringDashboardStyle(options) ? theme.Card : theme.Background));
                canvas.StrokeRoundedRect(cx - layout.Width / 2, cy - layout.Height / 2, layout.Width, layout.Height, radius, WithAlpha(Color(theme.Border), IsMonitoringDashboardStyle(options) ? (byte)184 : byte.MaxValue), 1);
            }
            var baseColor = edge.IsMuted ? Color(theme.MutedForeground) : Color(theme.StatusColor(edge.Status));
            var isHighlighted = highlight.IsEdgeHighlighted(edge);
            var color = isHighlighted ? baseColor : WithAlpha(baseColor, HighlightAlpha(255, false, highlight));
            var secondaryColor = isHighlighted ? Color(theme.MutedForeground) : WithAlpha(Color(theme.MutedForeground), HighlightAlpha(255, false, highlight));
            var haloColor = WithAlpha(Color(theme.Background), HighlightAlpha(255, isHighlighted, highlight));
            DrawEdgeLabelClearance(canvas, chart, layout, cx, cy, theme, options, highlight, isHighlighted);
            DrawEdgeLabelLines(canvas, layout, cx, cy, color, secondaryColor, haloColor, IsMonitoringDashboardStyle(options) && !options.IncludeEdgeLabelBackplates);
        }
    }

    private static void DrawEdgeLabelLines(RgbaCanvas canvas, TopologyEdgeLabelLayout layout, double cx, double cy, ChartColor primaryColor, ChartColor secondaryColor, ChartColor haloColor, bool useHalo) {
        var lines = new List<(string Text, bool Primary)>();
        if (!string.IsNullOrWhiteSpace(layout.Label)) lines.Add((layout.Label, true));
        if (!string.IsNullOrWhiteSpace(layout.SecondaryLabel)) lines.Add((layout.SecondaryLabel, false));
        if (!string.IsNullOrWhiteSpace(layout.TertiaryLabel)) lines.Add((layout.TertiaryLabel, false));
        var start = cy - (lines.Count - 1) * 8;
        for (var i = 0; i < lines.Count; i++) {
            var line = lines[i];
            var y = start + i * 16 - (line.Primary ? 7 : 6);
            var color = line.Primary ? primaryColor : secondaryColor;
            if (useHalo) DrawCenteredWithHalo(canvas, cx, y, line.Text, color, line.Primary ? 12 : 10, line.Primary, haloColor);
            else DrawCentered(canvas, cx, y, line.Text, color, line.Primary ? 12 : 10, line.Primary);
        }
    }

    private static void DrawNodes(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        foreach (var node in chart.Nodes) {
            var isHighlighted = highlight.IsNodeHighlighted(node);
            var isSelected = IsSelected(options.SelectedNodeIds, node.Id);
            var accent = Color(NodeAccentColor(node, theme, options));
            var displayMode = EffectiveNodeDisplayMode(node, options);
            if (displayMode == TopologyNodeDisplayMode.Hidden) continue;
            if (displayMode == TopologyNodeDisplayMode.Dot) {
                var radius = Math.Max(5, Math.Min(node.Width, node.Height) / 2);
                if (isSelected) canvas.DrawCircleOutline(CenterX(node), CenterY(node), radius + 5, WithAlpha(accent, 140), 2.4);
                canvas.DrawCircle(CenterX(node), CenterY(node), radius + 2, Color(theme.Background));
                canvas.DrawCircle(CenterX(node), CenterY(node), radius, accent);
                DrawDotNodeSymbol(canvas, node, options);
                DrawNodeBadge(canvas, node, theme, accent, displayMode);
                continue;
            }

            if (IsMonitoringDashboardStyle(options) && displayMode == TopologyNodeDisplayMode.Icon && node.Kind == TopologyNodeKind.Cloud) {
                var radius = Math.Min(node.Width, node.Height) / 2;
                canvas.DrawCircle(CenterX(node) + 2, CenterY(node) + 5, radius, ChartColor.FromRgba(15, 23, 42, 18));
                canvas.DrawCircle(CenterX(node), CenterY(node), radius + (isSelected ? 4 : 3), Color(theme.Background));
                canvas.DrawCircle(CenterX(node), CenterY(node), radius, accent);
                if (!isHighlighted && highlight.IsActive) canvas.DrawCircle(CenterX(node), CenterY(node), radius, WithAlpha(Color(theme.Background), 185));
                DrawNodeIcon(canvas, node, theme, accent, displayMode, options);
                DrawNodeBadge(canvas, node, theme, accent, displayMode);
                continue;
            }

            var radiusRect = displayMode == TopologyNodeDisplayMode.Pill ? node.Height / 2 : displayMode is TopologyNodeDisplayMode.Icon or TopologyNodeDisplayMode.Tile ? 12 : 10;
            canvas.FillRoundedRect(node.X + 2, node.Y + 5, node.Width, node.Height, 10, ChartColor.FromRgba(15, 23, 42, 18));
            canvas.FillRoundedRect(node.X, node.Y, node.Width, node.Height, radiusRect, Color(theme.Card));
            canvas.StrokeRoundedRect(node.X, node.Y, node.Width, node.Height, radiusRect, accent, isSelected ? 2.8 : 1.5);
            if (!isHighlighted && highlight.IsActive) canvas.FillRoundedRect(node.X, node.Y, node.Width, node.Height, radiusRect, WithAlpha(Color(theme.Background), 185));
            DrawNodeIcon(canvas, node, theme, accent, displayMode, options);
            if (options.IncludeNodeLabels && displayMode == TopologyNodeDisplayMode.Icon && options.IncludeIconLabels) {
                var label = IconLabelText(node);
                var plateWidth = IconLabelPlateWidth(node);
                var plateX = CenterX(node) - plateWidth / 2;
                var plateY = IconLabelPlateY(node);
                canvas.FillRoundedRect(plateX, plateY, plateWidth, 15, 7.5, Color(theme.Background));
                canvas.StrokeRoundedRect(plateX, plateY, plateWidth, 15, 7.5, Color(theme.Border), 0.7);
                DrawCentered(canvas, CenterX(node), plateY + 3, label, Color(theme.Foreground), 10.5, true);
            } else if (options.IncludeNodeLabels && displayMode != TopologyNodeDisplayMode.Icon) {
                if (displayMode == TopologyNodeDisplayMode.Tile) {
                    DrawCentered(canvas, CenterX(node), node.Y + node.Height + 4, TrimTo(node.Label, NodeTitleMaxLength(displayMode)), Color(theme.Foreground), 10.5, true);
                    if (options.IncludeTileSubtitles && !string.IsNullOrWhiteSpace(node.Subtitle)) DrawTileSubtitle(canvas, node, theme, accent, options);
                    DrawNodeBadge(canvas, node, theme, accent, displayMode);
                    continue;
                }

                var textX = displayMode == TopologyNodeDisplayMode.Pill ? node.X + 34 : node.X + 42;
                var titleY = displayMode == TopologyNodeDisplayMode.Pill ? node.Y + node.Height / 2 - 7 : node.Y + (displayMode == TopologyNodeDisplayMode.CompactCard ? 13 : 17);
                var titleSize = displayMode == TopologyNodeDisplayMode.Pill ? 11.5 : displayMode == TopologyNodeDisplayMode.CompactCard ? 11.5 : 12.5;
                var textRightPadding = 10;
                var textWidth = Math.Max(24, node.Width - (textX - node.X) - textRightPadding);
                var titleValue = TrimTo(node.Label, NodeTitleMaxLength(displayMode));
                titleSize = FitFontSize(titleValue, textWidth, titleSize, 10, true);
                canvas.DrawTextEmphasized(textX, titleY, TrimToEstimatedWidth(titleValue, textWidth, titleSize, true), Color(theme.Foreground), titleSize);
                if (displayMode != TopologyNodeDisplayMode.Pill && !string.IsNullOrWhiteSpace(node.Subtitle)) {
                    if (options.CardSubtitleMode == TopologyCardSubtitleMode.Chip) DrawCardSubtitleChip(canvas, node, theme, accent, displayMode, options);
                    else canvas.DrawText(textX, node.Y + (displayMode == TopologyNodeDisplayMode.CompactCard ? 31 : 37), TrimToEstimatedWidth(TrimTo(node.Subtitle!, NodeLabelMaxLength), textWidth, 10.5, false), Color(theme.MutedForeground), 10.5);
                }
            }

            DrawNodeBadge(canvas, node, theme, accent, displayMode);
        }
    }

    private static void DrawCardSubtitleChip(RgbaCanvas canvas, TopologyNode node, TopologyTheme theme, ChartColor accent, TopologyNodeDisplayMode displayMode, TopologyRenderOptions options) {
        var subtitle = TrimTo(node.Subtitle!, displayMode == TopologyNodeDisplayMode.CompactCard ? 12 : 16);
        var width = Math.Min(Math.Max(48, RgbaCanvas.MeasureTextEmphasizedWidth(subtitle, 8.5, null) + 18), Math.Max(48, node.Width - 50));
        var height = 17.0;
        var x = node.X + 42;
        var y = node.Y + (displayMode == TopologyNodeDisplayMode.CompactCard ? 31 : node.Height - 22);
        canvas.FillRoundedRect(x, y, width, height, 8.5, Color(StatusFill(NodeAccentColor(node, theme, options), theme.Background)));
        canvas.StrokeRoundedRect(x, y, width, height, 8.5, WithAlpha(accent, 115), 1);
        DrawCentered(canvas, x + width / 2, y + 3.8, subtitle, accent, 8.5, true);
    }

    private static string NodeAccentColor(TopologyNode node, TopologyTheme theme, TopologyRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(node.Color)) return node.Color!.Trim();
        var icon = ResolveNodeIcon(node, options);
        return !string.IsNullOrWhiteSpace(icon?.Color) ? icon!.Color!.Trim() : theme.StatusColor(node.Status);
    }

    private static bool IsSelected(List<string> ids, string id) {
        foreach (var selectedId in ids) {
            if (string.Equals(selectedId, id, StringComparison.Ordinal)) return true;
        }

        return false;
    }

    private static void DrawTileSubtitle(RgbaCanvas canvas, TopologyNode node, TopologyTheme theme, ChartColor accent, TopologyRenderOptions options) {
        var subtitle = TrimTo(node.Subtitle!, 16);
        var width = Math.Min(Math.Max(46, RgbaCanvas.MeasureTextEmphasizedWidth(subtitle, 8.5, null) + 18), Math.Max(46, node.Width + 28));
        var x = CenterX(node) - width / 2;
        var y = node.Y + node.Height + 14;
        canvas.FillRoundedRect(x, y, width, 17, 8.5, Color(StatusFill(NodeAccentColor(node, theme, options), theme.Background)));
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

    private static void DrawDotNodeSymbol(RgbaCanvas canvas, TopologyNode node, TopologyRenderOptions options) {
        if (!IsMonitoringDashboardStyle(options)) return;
        if (string.IsNullOrWhiteSpace(node.Symbol) && node.Kind != TopologyNodeKind.Server) return;
        var cx = CenterX(node);
        var cy = CenterY(node);
        var symbol = string.IsNullOrWhiteSpace(node.Symbol) ? NodeGlyph(node, options) : node.Symbol!.Trim();
        if (node.Kind == TopologyNodeKind.Server || symbol.Equals("DC", StringComparison.OrdinalIgnoreCase)) {
            canvas.DrawLine(cx - 4.2, cy - 3.6, cx + 4.2, cy - 3.6, ChartColor.White, 1.05);
            canvas.DrawLine(cx - 4.2, cy - 0.8, cx + 4.2, cy - 0.8, ChartColor.White, 1.05);
            canvas.DrawLine(cx - 4.2, cy + 1.5, cx + 4.2, cy + 1.5, ChartColor.White, 1.05);
            canvas.DrawLine(cx - 4.2, cy + 4.2, cx + 4.2, cy + 4.2, ChartColor.White, 1.05);
            canvas.DrawLine(cx - 4.2, cy - 3.6, cx - 4.2, cy - 0.8, ChartColor.White, 1.05);
            canvas.DrawLine(cx + 4.2, cy - 3.6, cx + 4.2, cy - 0.8, ChartColor.White, 1.05);
            canvas.DrawLine(cx - 4.2, cy + 1.5, cx - 4.2, cy + 4.2, ChartColor.White, 1.05);
            canvas.DrawLine(cx + 4.2, cy + 1.5, cx + 4.2, cy + 4.2, ChartColor.White, 1.05);
            return;
        }

        DrawCentered(canvas, cx, cy - 4, TrimTo(symbol, 2), ChartColor.White, 5.8, true);
    }

    private static void DrawNodeIcon(RgbaCanvas canvas, TopologyNode node, TopologyTheme theme, ChartColor status, TopologyNodeDisplayMode displayMode, TopologyRenderOptions options) {
        var cx = displayMode is TopologyNodeDisplayMode.Icon or TopologyNodeDisplayMode.Tile ? CenterX(node) : node.X + 22;
        var cy = displayMode == TopologyNodeDisplayMode.Tile ? node.Y + node.Height / 2 - 1 : node.Y + node.Height / 2;
        var size = displayMode == TopologyNodeDisplayMode.Pill ? 18 : displayMode == TopologyNodeDisplayMode.Icon ? 26 : displayMode == TopologyNodeDisplayMode.Tile ? 24 : 22;
        if (IsMonitoringDashboardStyle(options) && displayMode == TopologyNodeDisplayMode.Icon && node.Kind == TopologyNodeKind.Cloud) {
            canvas.DrawCircleOutline(cx - 5, cy, 7, ChartColor.White, 2.4);
            canvas.DrawCircleOutline(cx + 4, cy - 2, 8, ChartColor.White, 2.4);
            return;
        }

        canvas.FillRoundedRect(cx - size / 2, cy - size / 2, size, size, 6, Color(StatusFill(theme.StatusColor(node.Status), theme.Background)));
        canvas.StrokeRoundedRect(cx - size / 2, cy - size / 2, size, size, 6, status, 1);
        if (!DrawInfrastructureGlyph(canvas, node, cx, cy, status, options)) DrawCentered(canvas, cx, cy - 6, NodeGlyph(node, options), status, displayMode == TopologyNodeDisplayMode.Pill ? 7.5 : 8.5, true);
    }

    private static bool DrawInfrastructureGlyph(RgbaCanvas canvas, TopologyNode node, double cx, double cy, ChartColor color, TopologyRenderOptions options) {
        switch (EffectiveIconShape(node, options)) {
            case TopologyIconShape.Site:
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
            case TopologyIconShape.Server:
            case TopologyIconShape.DomainController:
            case TopologyIconShape.ReadOnlyDomainController:
                canvas.StrokeRect(cx - 7, cy - 6, 14, 5, color, 1);
                canvas.StrokeRect(cx - 7, cy + 2, 14, 5, color, 1);
                canvas.DrawLine(cx + 4.5, cy - 3.5, cx + 5.5, cy - 3.5, color, 1.4);
                canvas.DrawLine(cx + 4.5, cy + 4.5, cx + 5.5, cy + 4.5, color, 1.4);
                if (EffectiveIconShape(node, options) == TopologyIconShape.ReadOnlyDomainController) canvas.DrawLine(cx - 8, cy + 8, cx + 8, cy - 8, color, 1.2);
                return true;
            case TopologyIconShape.Network:
                canvas.DrawLine(cx - 7, cy + 5, cx, cy - 6, color, 1.3);
                canvas.DrawLine(cx, cy - 6, cx + 7, cy + 5, color, 1.3);
                canvas.DrawLine(cx - 7, cy + 5, cx + 7, cy + 5, color, 1.3);
                canvas.DrawCircle(cx, cy - 6, 2.2, color);
                canvas.DrawCircle(cx - 7, cy + 5, 2.2, color);
                canvas.DrawCircle(cx + 7, cy + 5, 2.2, color);
                return true;
            case TopologyIconShape.NetworkSwitch:
                canvas.StrokeRect(cx - 8, cy - 4, 16, 8, color, 1);
                canvas.DrawLine(cx - 5, cy, cx - 2, cy, color, 1.2);
                canvas.DrawLine(cx + 2, cy, cx + 5, cy, color, 1.2);
                canvas.DrawLine(cx - 4, cy - 7, cx - 1, cy - 4, color, 1.2);
                canvas.DrawLine(cx + 4, cy + 7, cx + 1, cy + 4, color, 1.2);
                return true;
            case TopologyIconShape.Router:
                canvas.DrawLine(cx, cy - 8, cx + 8, cy, color, 1.2);
                canvas.DrawLine(cx + 8, cy, cx, cy + 8, color, 1.2);
                canvas.DrawLine(cx, cy + 8, cx - 8, cy, color, 1.2);
                canvas.DrawLine(cx - 8, cy, cx, cy - 8, color, 1.2);
                canvas.DrawLine(cx - 4, cy, cx + 4, cy, color, 1.3);
                canvas.DrawLine(cx, cy - 4, cx, cy + 4, color, 1.3);
                return true;
            case TopologyIconShape.NetworkSegment:
                canvas.DrawLine(cx - 9, cy - 4, cx + 9, cy - 4, color, 1.2);
                canvas.DrawLine(cx - 9, cy + 4, cx + 9, cy + 4, color, 1.2);
                canvas.DrawLine(cx - 5, cy - 7, cx - 5, cy + 7, color, 1.2);
                canvas.DrawLine(cx + 5, cy - 7, cx + 5, cy + 7, color, 1.2);
                return true;
            case TopologyIconShape.LoadBalancer:
                canvas.DrawLine(cx, cy - 8, cx, cy + 8, color, 1.3);
                canvas.DrawLine(cx - 8, cy - 3, cx, cy - 3, color, 1.3);
                canvas.DrawLine(cx, cy - 3, cx + 6, cy - 7, color, 1.3);
                canvas.DrawLine(cx - 8, cy + 3, cx, cy + 3, color, 1.3);
                canvas.DrawLine(cx, cy + 3, cx + 6, cy + 7, color, 1.3);
                return true;
            case TopologyIconShape.Firewall:
                canvas.StrokeRect(cx - 8, cy - 6, 16, 12, color, 1);
                canvas.DrawLine(cx - 3, cy - 6, cx - 3, cy - 1, color, 1.2);
                canvas.DrawLine(cx + 3, cy - 1, cx + 3, cy + 6, color, 1.2);
                canvas.DrawLine(cx - 8, cy, cx - 2, cy, color, 1.2);
                canvas.DrawLine(cx + 2, cy, cx + 8, cy, color, 1.2);
                return true;
            case TopologyIconShape.Service:
                canvas.DrawCircleOutline(cx, cy, 5.5, color, 1.4);
                canvas.DrawLine(cx, cy - 9, cx, cy - 7, color, 1.4);
                canvas.DrawLine(cx, cy + 7, cx, cy + 9, color, 1.4);
                canvas.DrawLine(cx - 9, cy, cx - 7, cy, color, 1.4);
                canvas.DrawLine(cx + 7, cy, cx + 9, cy, color, 1.4);
                return true;
            case TopologyIconShape.Person:
                canvas.DrawCircleOutline(cx, cy - 5, 4, color, 1.3);
                canvas.DrawArc(cx, cy + 8, 8, Math.PI, Math.PI * 2, color, 1.3);
                return true;
            case TopologyIconShape.Team:
                canvas.DrawCircleOutline(cx - 5, cy - 5, 3, color, 1.2);
                canvas.DrawCircleOutline(cx + 5, cy - 5, 3, color, 1.2);
                canvas.DrawArc(cx - 5, cy + 7, 6, Math.PI, Math.PI * 2, color, 1.2);
                canvas.DrawArc(cx + 5, cy + 7, 6, Math.PI, Math.PI * 2, color, 1.2);
                return true;
            case TopologyIconShape.Storage:
                canvas.StrokeRect(cx - 8, cy - 6, 16, 5, color, 1);
                canvas.StrokeRect(cx - 8, cy + 2, 16, 5, color, 1);
                canvas.DrawLine(cx - 4.5, cy - 3.5, cx + 1.5, cy - 3.5, color, 1.2);
                canvas.DrawLine(cx - 4.5, cy + 4.5, cx + 1.5, cy + 4.5, color, 1.2);
                canvas.DrawCircle(cx + 5, cy - 3.5, 1.2, color);
                canvas.DrawCircle(cx + 5, cy + 4.5, 1.2, color);
                return true;
            case TopologyIconShape.Application:
                canvas.StrokeRect(cx - 8, cy - 7, 16, 14, color, 1);
                canvas.DrawLine(cx - 8, cy - 3, cx + 8, cy - 3, color, 1.1);
                canvas.DrawLine(cx - 5, cy - 5, cx - 4, cy - 5, color, 1.2);
                canvas.DrawLine(cx - 1.5, cy - 5, cx - 0.5, cy - 5, color, 1.2);
                canvas.DrawLine(cx - 3, cy + 1, cx + 3, cy + 1, color, 1.2);
                canvas.DrawLine(cx - 3, cy + 4, cx + 3, cy + 4, color, 1.2);
                return true;
            case TopologyIconShape.Certificate:
                canvas.DrawLine(cx - 6, cy - 8, cx + 4, cy - 8, color, 1.1);
                canvas.DrawLine(cx + 4, cy - 8, cx + 8, cy - 4, color, 1.1);
                canvas.DrawLine(cx + 8, cy - 4, cx + 8, cy + 7, color, 1.1);
                canvas.DrawLine(cx + 8, cy + 7, cx - 6, cy + 7, color, 1.1);
                canvas.DrawLine(cx - 6, cy + 7, cx - 6, cy - 8, color, 1.1);
                canvas.DrawLine(cx + 4, cy - 8, cx + 4, cy - 4, color, 1.1);
                canvas.DrawLine(cx + 4, cy - 4, cx + 8, cy - 4, color, 1.1);
                canvas.DrawLine(cx - 3, cy - 1, cx + 4, cy - 1, color, 1.1);
                canvas.DrawLine(cx - 3, cy + 2, cx + 2, cy + 2, color, 1.1);
                canvas.DrawCircleOutline(cx - 4, cy + 7, 2.5, color, 1.1);
                return true;
            case TopologyIconShape.Desktop:
                canvas.StrokeRect(cx - 8, cy - 7, 16, 11, color, 1);
                canvas.DrawLine(cx, cy + 4, cx, cy + 8, color, 1.2);
                canvas.DrawLine(cx - 5, cy + 8, cx + 5, cy + 8, color, 1.2);
                return true;
            case TopologyIconShape.Laptop:
                canvas.StrokeRect(cx - 7, cy - 7, 14, 10, color, 1);
                canvas.DrawLine(cx - 10, cy + 7, cx + 10, cy + 7, color, 1.2);
                canvas.DrawLine(cx - 10, cy + 7, cx - 7, cy + 3, color, 1.2);
                canvas.DrawLine(cx + 10, cy + 7, cx + 7, cy + 3, color, 1.2);
                return true;
            case TopologyIconShape.Forest:
                canvas.DrawLine(cx, cy - 9, cx + 6, cy, color, 1.2);
                canvas.DrawLine(cx + 6, cy, cx + 2.5, cy, color, 1.2);
                canvas.DrawLine(cx + 2.5, cy, cx + 8, cy + 8, color, 1.2);
                canvas.DrawLine(cx + 8, cy + 8, cx - 8, cy + 8, color, 1.2);
                canvas.DrawLine(cx - 8, cy + 8, cx - 2.5, cy, color, 1.2);
                canvas.DrawLine(cx - 2.5, cy, cx - 6, cy, color, 1.2);
                canvas.DrawLine(cx - 6, cy, cx, cy - 9, color, 1.2);
                canvas.DrawLine(cx, cy, cx, cy + 8, color, 1.2);
                return true;
            case TopologyIconShape.Domain:
                canvas.DrawLine(cx, cy - 9, cx + 8, cy - 3, color, 1.2);
                canvas.DrawLine(cx + 8, cy - 3, cx + 8, cy + 5, color, 1.2);
                canvas.DrawLine(cx + 8, cy + 5, cx, cy + 9, color, 1.2);
                canvas.DrawLine(cx, cy + 9, cx - 8, cy + 5, color, 1.2);
                canvas.DrawLine(cx - 8, cy + 5, cx - 8, cy - 3, color, 1.2);
                canvas.DrawLine(cx - 8, cy - 3, cx, cy - 9, color, 1.2);
                canvas.DrawLine(cx - 8, cy - 3, cx, cy + 2, color, 1.1);
                canvas.DrawLine(cx, cy + 2, cx + 8, cy - 3, color, 1.1);
                canvas.DrawLine(cx, cy + 2, cx, cy + 9, color, 1.1);
                return true;
            default:
                if (node.Kind != TopologyNodeKind.Queue) return false;
                canvas.DrawLine(cx - 7, cy - 6, cx + 7, cy - 6, color, 1.7);
                canvas.DrawLine(cx - 7, cy, cx + 7, cy, color, 1.7);
                canvas.DrawLine(cx - 7, cy + 6, cx + 7, cy + 6, color, 1.7);
                return true;
        }
    }

    private static void DrawStatusBadges(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        foreach (var node in chart.Nodes) {
            if (!ShouldRenderNodeStatusBadge(node, options)) continue;
            if (highlight.IsActive && !highlight.IsNodeHighlighted(node)) continue;
            var color = Color(theme.StatusColor(node.Status));
            var cx = node.X + node.Width - 11;
            var cy = node.Y + 11;
            canvas.DrawCircle(cx, cy, 9, Color(theme.Background));
            canvas.DrawCircle(cx, cy, 7, color);
            if (IsMonitoringDashboardStyle(options) && node.Status == TopologyHealthStatus.Healthy) {
                canvas.DrawLine(cx - 3.8, cy, cx - 1, cy + 3, ChartColor.White, 1.8);
                canvas.DrawLine(cx - 1, cy + 3, cx + 4.4, cy - 3.6, ChartColor.White, 1.8);
            } else {
                DrawCentered(canvas, cx, cy - 5, StatusGlyph(node.Status), ChartColor.White, 8, true);
            }
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

    private static void DrawCenteredWithHalo(RgbaCanvas canvas, double centerX, double y, string text, ChartColor color, double fontSize, bool emphasized, ChartColor haloColor) {
        var width = emphasized ? RgbaCanvas.MeasureTextEmphasizedWidth(text, fontSize, null) : RgbaCanvas.MeasureTextWidth(text, fontSize, null);
        var x = centerX - width / 2;
        var offsets = fontSize >= 12
            ? new[] { (-1.5, 0.0), (1.5, 0.0), (0.0, -1.5), (0.0, 1.5), (-1.05, -1.05), (1.05, -1.05), (-1.05, 1.05), (1.05, 1.05) }
            : new[] { (-1.1, 0.0), (1.1, 0.0), (0.0, -1.1), (0.0, 1.1), (-0.8, -0.8), (0.8, -0.8), (-0.8, 0.8), (0.8, 0.8) };
        foreach (var (dx, dy) in offsets) {
            if (emphasized) canvas.DrawTextEmphasized(x + dx, y + dy, text, haloColor, fontSize);
            else canvas.DrawText(x + dx, y + dy, text, haloColor, fontSize);
        }

        if (emphasized) canvas.DrawTextEmphasized(x, y, text, color, fontSize);
        else canvas.DrawText(x, y, text, color, fontSize);
    }

    private static ChartColor Color(string value) => ChartColor.FromHex(value);

    private static ChartColor WithAlpha(ChartColor color, byte alpha) => ChartColor.FromRgba(color.R, color.G, color.B, alpha);

    private static byte[] FitPixels(byte[] source, int sourceWidth, int sourceHeight, int targetWidth, int targetHeight, ChartColor background) {
        var target = new byte[targetWidth * targetHeight * 4];
        for (var i = 0; i < target.Length; i += 4) {
            target[i] = background.R;
            target[i + 1] = background.G;
            target[i + 2] = background.B;
            target[i + 3] = background.A;
        }

        var scale = Math.Min(targetWidth / (double)sourceWidth, targetHeight / (double)sourceHeight);
        scale = Math.Min(1, scale);
        var fittedWidth = Math.Max(1, (int)Math.Round(sourceWidth * scale));
        var fittedHeight = Math.Max(1, (int)Math.Round(sourceHeight * scale));
        for (var y = 0; y < fittedHeight; y++) {
            var sourceY = scale >= 1 ? y : ((y + 0.5) / scale) - 0.5;
            for (var x = 0; x < fittedWidth; x++) {
                var sourceX = scale >= 1 ? x : ((x + 0.5) / scale) - 0.5;
                var dst = (y * targetWidth + x) * 4;
                SampleBilinear(source, sourceWidth, sourceHeight, sourceX, sourceY, target, dst);
            }
        }

        return target;
    }

    private static void SampleBilinear(byte[] source, int sourceWidth, int sourceHeight, double sourceX, double sourceY, byte[] target, int dst) {
        sourceX = Clamp(sourceX, 0, sourceWidth - 1);
        sourceY = Clamp(sourceY, 0, sourceHeight - 1);
        var x0 = (int)Math.Floor(sourceX);
        var y0 = (int)Math.Floor(sourceY);
        var x1 = Math.Min(sourceWidth - 1, x0 + 1);
        var y1 = Math.Min(sourceHeight - 1, y0 + 1);
        var wx = sourceX - x0;
        var wy = sourceY - y0;
        var topLeft = (y0 * sourceWidth + x0) * 4;
        var topRight = (y0 * sourceWidth + x1) * 4;
        var bottomLeft = (y1 * sourceWidth + x0) * 4;
        var bottomRight = (y1 * sourceWidth + x1) * 4;
        for (var channel = 0; channel < 4; channel++) {
            var top = source[topLeft + channel] * (1 - wx) + source[topRight + channel] * wx;
            var bottom = source[bottomLeft + channel] * (1 - wx) + source[bottomRight + channel] * wx;
            target[dst + channel] = (byte)Clamp((int)Math.Round(top * (1 - wy) + bottom * wy), 0, 255);
        }
    }

    private static double Clamp(double value, double min, double max) {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private static int Clamp(int value, int min, int max) {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
