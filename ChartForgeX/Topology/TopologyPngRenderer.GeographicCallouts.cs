using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologyPngRenderer {
    private static void DrawGeographicCallouts(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        var callouts = TopologyGeographicCallouts.Build(chart, options, theme);
        foreach (var callout in callouts) {
            var isSelected = IsSelected(options.SelectedGroupIds, callout.Group.Id);
            var isHighlighted = highlight.IsGroupHighlighted(callout.Group);
            var accent = Color(callout.AccentColor);
            var leader = TopologyGeographicCalloutPrimitives.LeaderPoints(callout);
            var leaderStyle = ChartRouteVisualStyles.TopologyGeographicCalloutLeader();
            DrawCalloutLeaderHalo(canvas, leader, WithAlpha(Color(theme.Background), HighlightAlpha(Alpha(leaderStyle.HaloOpacity), isHighlighted, highlight)), leaderStyle);
            DrawCalloutLeader(canvas, leader, WithAlpha(accent, HighlightAlpha(Alpha(leaderStyle.StrokeOpacity), isHighlighted, highlight)), leaderStyle);
            canvas.DrawCircle(callout.AnchorX, callout.AnchorY, TopologyGeographicCalloutPrimitives.AnchorHaloRadius, Color(theme.Background));
            canvas.DrawCircle(callout.AnchorX, callout.AnchorY, TopologyGeographicCalloutPrimitives.AnchorRadius, accent);
            canvas.FillRoundedRect(callout.X + TopologyGeographicCalloutPrimitives.PngShadowXOffset, callout.Y + TopologyGeographicCalloutPrimitives.PngShadowYOffset, callout.Width, callout.Height, TopologyGeographicCalloutPrimitives.CardRadius, ChartColor.FromRgba(15, 23, 42, TopologyGeographicCalloutPrimitives.PngShadowAlpha));
            canvas.FillRoundedRect(callout.X, callout.Y, callout.Width, callout.Height, TopologyGeographicCalloutPrimitives.CardRadius, Color(theme.Card));
            canvas.StrokeRoundedRect(callout.X, callout.Y, callout.Width, callout.Height, TopologyGeographicCalloutPrimitives.CardRadius, WithAlpha(accent, isSelected ? TopologyGeographicCalloutPrimitives.PngCardSelectedStrokeAlpha : TopologyGeographicCalloutPrimitives.PngCardStrokeAlpha), isSelected ? TopologyGeographicCalloutPrimitives.CardSelectedStrokeWidth : TopologyGeographicCalloutPrimitives.CardStrokeWidth);
            canvas.FillRoundedRect(callout.X, callout.Y, TopologyGeographicCalloutPrimitives.AccentStripWidth, callout.Height, TopologyGeographicCalloutPrimitives.AccentStripRadius, accent);
            canvas.DrawTextEmphasized(callout.X + TopologyGeographicCalloutPrimitives.TitleXOffset, callout.Y + TopologyGeographicCalloutPrimitives.PngTitleYOffset, TrimTo(callout.Label, TopologyGeographicCalloutPrimitives.TitleMaxLength), Color(theme.Foreground), TopologyGeographicCalloutPrimitives.TitleFontSize);
            canvas.DrawText(callout.X + TopologyGeographicCalloutPrimitives.TitleXOffset, callout.Y + TopologyGeographicCalloutPrimitives.PngSubtitleYOffset, TrimTo(callout.Subtitle, TopologyGeographicCalloutPrimitives.SubtitleMaxLength), Color(theme.MutedForeground), TopologyGeographicCalloutPrimitives.SubtitleFontSize);
            DrawCalloutMiniTopology(canvas, callout, callout.X + callout.Width - TopologyGeographicCalloutPrimitives.MiniTopologyRightInset, callout.Y + TopologyGeographicCalloutPrimitives.MiniTopologyYOffset, theme);
            DrawCalloutStatusChips(canvas, callout, callout.X + TopologyGeographicCalloutPrimitives.StatusChipsXOffset, callout.Y + TopologyGeographicCalloutPrimitives.StatusChipsYOffset, theme);
            if (!isHighlighted && highlight.IsActive) canvas.FillRoundedRect(callout.X, callout.Y, callout.Width, callout.Height, TopologyGeographicCalloutPrimitives.CardRadius, WithAlpha(Color(theme.Background), 178));
        }
    }

    private static void DrawCalloutMiniTopology(RgbaCanvas canvas, TopologyGeographicCallout callout, double x, double y, TopologyTheme theme) {
        var center = TopologyGeographicCalloutPrimitives.MiniTopologyCenter(x, y);
        var points = TopologyGeographicCalloutPrimitives.MiniTopologyPoints(x, y);
        var statuses = TopologyGeographicCalloutPrimitives.PreviewStatuses(callout);
        var accent = Color(callout.AccentColor);
        var leaderStyle = ChartRouteVisualStyles.TopologyGeographicMiniTopologyLeader();
        foreach (var point in points) canvas.DrawDashedLine(center.X, center.Y, point.X, point.Y, WithAlpha(accent, Alpha(leaderStyle.StrokeOpacity)), leaderStyle.StrokeWidth, leaderStyle.Dash, leaderStyle.Gap);
        canvas.DrawCircle(center.X, center.Y, TopologyGeographicCalloutPrimitives.MiniTopologyCenterHaloRadius, Color(theme.Background));
        canvas.DrawCircle(center.X, center.Y, TopologyGeographicCalloutPrimitives.MiniTopologyCenterRadius, accent);
        for (var i = 0; i < points.Length; i++) {
            var color = Color(theme.StatusColor(statuses[i]));
            canvas.DrawCircle(points[i].X, points[i].Y, TopologyGeographicCalloutPrimitives.MiniTopologyNodeHaloRadius, Color(theme.Background));
            canvas.DrawCircle(points[i].X, points[i].Y, TopologyGeographicCalloutPrimitives.MiniTopologyNodeRadius, color);
        }
    }

    private static void DrawCalloutStatusChips(RgbaCanvas canvas, TopologyGeographicCallout callout, double x, double y, TopologyTheme theme) {
        var offset = 0.0;
        foreach (var chip in TopologyGeographicCalloutPrimitives.StatusChips(callout)) {
            var text = chip.Count.ToString(CultureInfo.InvariantCulture);
            var width = TopologyGeographicCalloutPrimitives.StatusChipBaseWidth + text.Length * TopologyGeographicCalloutPrimitives.StatusChipDigitWidth;
            var color = Color(theme.StatusColor(chip.Status));
            canvas.FillRoundedRect(x + offset, y, width, TopologyGeographicCalloutPrimitives.StatusChipHeight, TopologyGeographicCalloutPrimitives.StatusChipRadius, Color(StatusFill(theme.StatusColor(chip.Status), theme.Background)));
            canvas.StrokeRoundedRect(x + offset, y, width, TopologyGeographicCalloutPrimitives.StatusChipHeight, TopologyGeographicCalloutPrimitives.StatusChipRadius, WithAlpha(color, TopologyGeographicCalloutPrimitives.PngStatusChipStrokeAlpha), 1);
            canvas.DrawCircle(x + offset + TopologyGeographicCalloutPrimitives.StatusChipDotX, y + TopologyGeographicCalloutPrimitives.StatusChipRadius, TopologyGeographicCalloutPrimitives.StatusChipDotRadius, color);
            canvas.DrawTextEmphasized(x + offset + TopologyGeographicCalloutPrimitives.StatusChipTextX, y + TopologyGeographicCalloutPrimitives.StatusChipTextPngTop, text, color, TopologyGeographicCalloutPrimitives.StatusChipTextFontSize);
            offset += width + TopologyGeographicCalloutPrimitives.StatusChipGap;
        }
    }

    private static void DrawCalloutLeader(RgbaCanvas canvas, IReadOnlyList<ChartPoint> points, ChartColor color, ChartLeaderVisualStyle style) {
        for (var i = 0; i < points.Count - 1; i++) canvas.DrawDashedLine(points[i].X, points[i].Y, points[i + 1].X, points[i + 1].Y, color, style.StrokeWidth, style.Dash, style.Gap);
    }

    private static void DrawCalloutLeaderHalo(RgbaCanvas canvas, IReadOnlyList<ChartPoint> points, ChartColor color, ChartLeaderVisualStyle style) {
        for (var i = 0; i < points.Count - 1; i++) canvas.DrawLine(points[i].X, points[i].Y, points[i + 1].X, points[i + 1].Y, color, style.HaloStrokeWidth);
    }

    private static byte Alpha(double opacity) => (byte)System.Math.Round(255 * opacity);

}
