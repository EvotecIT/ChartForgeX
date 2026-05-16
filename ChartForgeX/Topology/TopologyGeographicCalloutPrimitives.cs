using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static class TopologyGeographicCalloutPrimitives {
    public const double AnchorHaloRadius = 6.2;
    public const double AnchorRadius = 4.2;
    public const double CardRadius = 10.0;
    public const double CardStrokeWidth = 1.2;
    public const double CardSelectedStrokeWidth = 2.4;
    public const double CardStrokeOpacity = 0.42;
    public const double CardSelectedStrokeOpacity = 0.90;
    public const byte PngCardStrokeAlpha = 120;
    public const byte PngCardSelectedStrokeAlpha = 230;
    public const double PngShadowXOffset = 2.0;
    public const double PngShadowYOffset = 5.0;
    public const byte PngShadowAlpha = 18;
    public const double AccentStripWidth = 5.0;
    public const double AccentStripRadius = 2.5;
    public const double AccentStripOpacity = 0.92;
    public const double TitleXOffset = 18.0;
    public const double TitleYOffset = 24.0;
    public const double PngTitleYOffset = 12.0;
    public const double TitleFontSize = 13.0;
    public const int TitleMaxLength = 18;
    public const double SubtitleYOffset = 42.0;
    public const double PngSubtitleYOffset = 31.0;
    public const double SubtitleFontSize = 10.5;
    public const int SubtitleMaxLength = 24;
    public const double MiniTopologyRightInset = 60.0;
    public const double MiniTopologyYOffset = 19.0;
    public const double StatusChipsXOffset = 18.0;
    public const double StatusChipsYOffset = 58.0;
    public const double StatusChipBaseWidth = 30.0;
    public const double StatusChipDigitWidth = 5.5;
    public const double StatusChipHeight = 20.0;
    public const double StatusChipRadius = 10.0;
    public const double StatusChipDotX = 10.0;
    public const double StatusChipDotRadius = 3.4;
    public const double StatusChipTextX = 19.0;
    public const double StatusChipTextSvgBaseline = 13.5;
    public const double StatusChipTextPngTop = 3.5;
    public const double StatusChipTextFontSize = 9.5;
    public const double StatusChipStrokeOpacity = 0.38;
    public const byte PngStatusChipStrokeAlpha = 96;
    public const double StatusChipGap = 6.0;
    public const double MiniTopologyCenterX = 24.0;
    public const double MiniTopologyCenterY = 17.0;
    public const double MiniTopologyCenterRadius = 4.0;
    public const double MiniTopologyCenterHaloRadius = 5.6;
    public const double MiniTopologyCenterStrokeWidth = 1.6;
    public const double MiniTopologyNodeRadius = 3.8;
    public const double MiniTopologyNodeHaloRadius = 5.2;
    public const double MiniTopologyNodeStrokeWidth = 1.4;

    public static TopologyHealthStatus[] PreviewStatuses(TopologyGeographicCallout callout) {
        var statuses = new List<TopologyHealthStatus>(4);
        AddStatuses(statuses, TopologyHealthStatus.Healthy, callout.HealthyCount);
        AddStatuses(statuses, TopologyHealthStatus.Warning, callout.WarningCount);
        AddStatuses(statuses, TopologyHealthStatus.Critical, callout.CriticalCount);
        AddStatuses(statuses, TopologyHealthStatus.Unknown, callout.UnknownCount + callout.DisabledCount);
        while (statuses.Count < 4) statuses.Add(callout.Group.Status);
        return new[] { statuses[0], statuses[1], statuses[2], statuses[3] };
    }

    public static List<ChartPoint> LeaderPoints(TopologyGeographicCallout callout) {
        var connector = ConnectorPoint(callout);
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

    public static List<TopologyGeographicCalloutStatusChip> StatusChips(TopologyGeographicCallout callout) {
        var chips = new List<TopologyGeographicCalloutStatusChip>(4);
        AddChip(chips, TopologyHealthStatus.Healthy, callout.HealthyCount);
        AddChip(chips, TopologyHealthStatus.Warning, callout.WarningCount);
        AddChip(chips, TopologyHealthStatus.Critical, callout.CriticalCount);
        AddChip(chips, TopologyHealthStatus.Unknown, callout.UnknownCount);
        return chips;
    }

    public static ChartPoint MiniTopologyCenter(double x, double y) =>
        new(x + MiniTopologyCenterX, y + MiniTopologyCenterY);

    public static ChartPoint[] MiniTopologyPoints(double x, double y) => new[] {
        new ChartPoint(x + 7, y + 9),
        new ChartPoint(x + 29, y + 5),
        new ChartPoint(x + 45, y + 12),
        new ChartPoint(x + 31, y + 30)
    };

    private static ChartPoint ConnectorPoint(TopologyGeographicCallout callout) {
        var middleY = callout.Y + callout.Height / 2;
        if (callout.AnchorX < callout.X) return new ChartPoint(callout.X, middleY);
        if (callout.AnchorX > callout.X + callout.Width) return new ChartPoint(callout.X + callout.Width, middleY);
        return new ChartPoint(callout.X + callout.Width / 2, callout.AnchorY < callout.Y ? callout.Y : callout.Y + callout.Height);
    }

    private static void AddStatuses(List<TopologyHealthStatus> statuses, TopologyHealthStatus status, int count) {
        for (var i = 0; i < count && statuses.Count < 4; i++) statuses.Add(status);
    }

    private static void AddChip(List<TopologyGeographicCalloutStatusChip> chips, TopologyHealthStatus status, int count) {
        if (count > 0) chips.Add(new TopologyGeographicCalloutStatusChip(status, count));
    }
}

internal readonly struct TopologyGeographicCalloutStatusChip {
    public TopologyGeographicCalloutStatusChip(TopologyHealthStatus status, int count) {
        Status = status;
        Count = count;
    }

    public TopologyHealthStatus Status { get; }
    public int Count { get; }
}
