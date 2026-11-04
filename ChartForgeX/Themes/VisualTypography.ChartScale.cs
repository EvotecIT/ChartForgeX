using System;

namespace ChartForgeX.Themes;

public sealed partial class VisualTypography {
    internal VisualTypography? CompactScale { get; set; }
    internal VisualTypography? WideScale { get; set; }
    internal double CompactBreakpoint { get; set; } = 480;
    internal double MinimumTextSize { get; set; } = 10;
    internal int TitleWeight { get; set; } = 600;
    internal int PanelTitleWeight { get; set; } = 600;
    internal int SubtitleWeight { get; set; } = 400;
    internal int AxisTitleWeight { get; set; } = 500;
    internal int TickWeight { get; set; } = 400;
    internal int LegendWeight { get; set; } = 400;
    internal int LegendValueWeight { get; set; } = 500;
    internal int DataLabelWeight { get; set; } = 500;
    internal int CenterValueWeight { get; set; } = 700;
    internal double CenterMinimumSize { get; set; } = 20;
    internal double CenterMaximumSize { get; set; } = 40;
    internal double CenterRadiusFactor { get; set; } = .16;
    internal double CenterCaptionSize { get; set; } = 12;
    internal double CenterCaptionGap { get; set; } = 4;

    /// <summary>Resolves logical chart type sizes before measurement; a custom scale without compact roles stays unchanged.</summary>
    internal VisualTypography ForWidth(double width) {
        var wide = WideScale ?? this;
        return width < wide.CompactBreakpoint ? wide.CompactScale ?? wide : wide;
    }
    internal double CenterSize(double radius) => Math.Max(CenterMinimumSize, Math.Min(CenterMaximumSize, radius * CenterRadiusFactor));
}
