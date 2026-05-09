using System.Collections.Generic;
using System.Globalization;
using System;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologyPngRenderer {
    private static void DrawGeographicCallouts(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        var callouts = TopologyGeographicCallouts.Build(chart, options, theme);
        foreach (var callout in callouts) {
            var isSelected = IsSelected(options.SelectedGroupIds, callout.Group.Id);
            var isHighlighted = highlight.IsGroupHighlighted(callout.Group);
            var accent = Color(callout.AccentColor);
            var lineAlpha = HighlightAlpha(184, isHighlighted, highlight);
            var leader = CalloutLeaderPoints(callout);
            DrawCalloutLeaderHalo(canvas, leader, WithAlpha(Color(theme.Background), HighlightAlpha(194, isHighlighted, highlight)));
            DrawCalloutLeader(canvas, leader, WithAlpha(accent, lineAlpha));
            canvas.DrawCircle(callout.AnchorX, callout.AnchorY, 6.2, Color(theme.Background));
            canvas.DrawCircle(callout.AnchorX, callout.AnchorY, 4.2, accent);
            canvas.FillRoundedRect(callout.X + 2, callout.Y + 5, callout.Width, callout.Height, 10, ChartColor.FromRgba(15, 23, 42, 18));
            canvas.FillRoundedRect(callout.X, callout.Y, callout.Width, callout.Height, 10, Color(theme.Card));
            canvas.StrokeRoundedRect(callout.X, callout.Y, callout.Width, callout.Height, 10, WithAlpha(accent, isSelected ? (byte)230 : (byte)120), isSelected ? 2.4 : 1.2);
            canvas.FillRoundedRect(callout.X, callout.Y, 5, callout.Height, 2.5, accent);
            canvas.DrawTextEmphasized(callout.X + 18, callout.Y + 12, TrimTo(callout.Label, 18), Color(theme.Foreground), 13);
            canvas.DrawText(callout.X + 18, callout.Y + 31, TrimTo(callout.Subtitle, 24), Color(theme.MutedForeground), 10.5);
            DrawCalloutMiniTopology(canvas, callout, callout.X + callout.Width - 60, callout.Y + 19, theme);
            DrawCalloutStatusChips(canvas, callout, callout.X + 18, callout.Y + 58, theme);
            if (!isHighlighted && highlight.IsActive) canvas.FillRoundedRect(callout.X, callout.Y, callout.Width, callout.Height, 10, WithAlpha(Color(theme.Background), 178));
        }
    }

    private static void DrawCalloutMiniTopology(RgbaCanvas canvas, TopologyGeographicCallout callout, double x, double y, TopologyTheme theme) {
        var center = new ChartPoint(x + 24, y + 17);
        var points = new[] {
            new ChartPoint(x + 7, y + 9),
            new ChartPoint(x + 29, y + 5),
            new ChartPoint(x + 45, y + 12),
            new ChartPoint(x + 31, y + 30)
        };
        var statuses = CalloutPreviewStatuses(callout);
        var accent = Color(callout.AccentColor);
        foreach (var point in points) canvas.DrawDashedLine(center.X, center.Y, point.X, point.Y, WithAlpha(accent, 158), 1.1, 2.5, 3);
        canvas.DrawCircle(center.X, center.Y, 5.6, Color(theme.Background));
        canvas.DrawCircle(center.X, center.Y, 4, accent);
        for (var i = 0; i < points.Length; i++) {
            var color = Color(theme.StatusColor(statuses[i]));
            canvas.DrawCircle(points[i].X, points[i].Y, 5.2, Color(theme.Background));
            canvas.DrawCircle(points[i].X, points[i].Y, 3.8, color);
        }
    }

    private static TopologyHealthStatus[] CalloutPreviewStatuses(TopologyGeographicCallout callout) {
        var statuses = new List<TopologyHealthStatus>(4);
        AddStatuses(statuses, TopologyHealthStatus.Healthy, callout.HealthyCount);
        AddStatuses(statuses, TopologyHealthStatus.Warning, callout.WarningCount);
        AddStatuses(statuses, TopologyHealthStatus.Critical, callout.CriticalCount);
        AddStatuses(statuses, TopologyHealthStatus.Unknown, callout.UnknownCount + callout.DisabledCount);
        while (statuses.Count < 4) statuses.Add(callout.Group.Status);
        return new[] { statuses[0], statuses[1], statuses[2], statuses[3] };
    }

    private static void AddStatuses(List<TopologyHealthStatus> statuses, TopologyHealthStatus status, int count) {
        for (var i = 0; i < count && statuses.Count < 4; i++) statuses.Add(status);
    }

    private static void DrawCalloutStatusChips(RgbaCanvas canvas, TopologyGeographicCallout callout, double x, double y, TopologyTheme theme) {
        var chips = new List<(TopologyHealthStatus Status, int Count)> {
            (TopologyHealthStatus.Healthy, callout.HealthyCount),
            (TopologyHealthStatus.Warning, callout.WarningCount),
            (TopologyHealthStatus.Critical, callout.CriticalCount),
            (TopologyHealthStatus.Unknown, callout.UnknownCount)
        };
        var offset = 0.0;
        foreach (var chip in chips) {
            if (chip.Count == 0) continue;
            var text = chip.Count.ToString(CultureInfo.InvariantCulture);
            var width = 30.0 + text.Length * 5.5;
            var color = Color(theme.StatusColor(chip.Status));
            canvas.FillRoundedRect(x + offset, y, width, 20, 10, Color(StatusFill(theme.StatusColor(chip.Status), theme.Background)));
            canvas.StrokeRoundedRect(x + offset, y, width, 20, 10, WithAlpha(color, 96), 1);
            canvas.DrawCircle(x + offset + 10, y + 10, 3.4, color);
            canvas.DrawTextEmphasized(x + offset + 19, y + 3.5, text, color, 9.5);
            offset += width + 6;
        }
    }

    private static ChartPoint CalloutConnectorPoint(TopologyGeographicCallout callout) {
        var middleY = callout.Y + callout.Height / 2;
        if (callout.AnchorX < callout.X) return new ChartPoint(callout.X, middleY);
        if (callout.AnchorX > callout.X + callout.Width) return new ChartPoint(callout.X + callout.Width, middleY);
        return new ChartPoint(callout.X + callout.Width / 2, callout.AnchorY < callout.Y ? callout.Y : callout.Y + callout.Height);
    }

    private static void DrawCalloutLeader(RgbaCanvas canvas, IReadOnlyList<ChartPoint> points, ChartColor color) {
        for (var i = 0; i < points.Count - 1; i++) canvas.DrawDashedLine(points[i].X, points[i].Y, points[i + 1].X, points[i + 1].Y, color, 1.4, 4, 5);
    }

    private static void DrawCalloutLeaderHalo(RgbaCanvas canvas, IReadOnlyList<ChartPoint> points, ChartColor color) {
        for (var i = 0; i < points.Count - 1; i++) canvas.DrawLine(points[i].X, points[i].Y, points[i + 1].X, points[i + 1].Y, color, 4.8);
    }

    private static List<ChartPoint> CalloutLeaderPoints(TopologyGeographicCallout callout) {
        var connector = CalloutConnectorPoint(callout);
        if (callout.AnchorX < callout.X || callout.AnchorX > callout.X + callout.Width) {
            var side = callout.AnchorX < callout.X ? -1 : 1;
            var midX = (callout.AnchorX + connector.X) / 2;
            var guardX = connector.X + side * 24;
            if (side < 0) midX = Math.Min(midX, guardX);
            else midX = Math.Max(midX, guardX);
            return new List<ChartPoint> {
                new(callout.AnchorX, callout.AnchorY),
                new(midX, callout.AnchorY),
                new(midX, connector.Y),
                connector
            };
        }

        var sideY = callout.AnchorY < callout.Y ? -1 : 1;
        var midY = (callout.AnchorY + connector.Y) / 2;
        var guardY = connector.Y + sideY * 22;
        if (sideY < 0) midY = Math.Min(midY, guardY);
        else midY = Math.Max(midY, guardY);
        return new List<ChartPoint> {
            new(callout.AnchorX, callout.AnchorY),
            new(callout.AnchorX, midY),
            new(connector.X, midY),
            connector
        };
    }
}
