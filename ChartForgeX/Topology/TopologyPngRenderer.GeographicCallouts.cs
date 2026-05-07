using System.Collections.Generic;
using System.Globalization;
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
            var connector = CalloutConnectorPoint(callout);
            var lineAlpha = isHighlighted ? (byte)184 : (byte)System.Math.Round(184 * highlight.DimmedOpacity);
            canvas.DrawDashedLine(callout.AnchorX, callout.AnchorY, connector.X, connector.Y, WithAlpha(accent, lineAlpha), 1.4, 4, 5);
            canvas.DrawCircle(callout.AnchorX, callout.AnchorY, 6.2, Color(theme.Background));
            canvas.DrawCircle(callout.AnchorX, callout.AnchorY, 4.2, accent);
            canvas.FillRoundedRect(callout.X + 2, callout.Y + 5, callout.Width, callout.Height, 10, ChartColor.FromRgba(15, 23, 42, 18));
            canvas.FillRoundedRect(callout.X, callout.Y, callout.Width, callout.Height, 10, Color(theme.Card));
            canvas.StrokeRoundedRect(callout.X, callout.Y, callout.Width, callout.Height, 10, WithAlpha(accent, isSelected ? (byte)230 : (byte)120), isSelected ? 2.4 : 1.2);
            canvas.FillRoundedRect(callout.X, callout.Y, 5, callout.Height, 2.5, accent);
            canvas.DrawTextEmphasized(callout.X + 18, callout.Y + 12, TrimTo(callout.Label, 18), Color(theme.Foreground), 13);
            canvas.DrawText(callout.X + 18, callout.Y + 31, TrimTo(callout.Subtitle, 24), Color(theme.MutedForeground), 10.5);
            DrawCalloutStatusChips(canvas, callout, callout.X + 18, callout.Y + 58, theme);
            if (!isHighlighted && highlight.IsActive) canvas.FillRoundedRect(callout.X, callout.Y, callout.Width, callout.Height, 10, WithAlpha(Color(theme.Background), 178));
        }
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
}
