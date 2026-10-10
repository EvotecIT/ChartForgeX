namespace ChartForgeX.Interactivity.Html;

/// <summary>Copies adapter tooltip values while keeping each chart's mutable option groups independent.</summary>
internal static class HtmlChartTooltipOptionsCopy {
    internal static void Copy(HtmlChartTooltipOptions source, HtmlChartTooltipOptions destination) {
        destination.Mode = source.Mode;
        destination.Range = source.Range;
        destination.DelayMilliseconds = source.DelayMilliseconds;
        destination.Position.Anchor = source.Position.Anchor;
        destination.Position.Placements = source.Position.Placements;
        destination.Position.Gap = source.Position.Gap;
        destination.Position.OffsetX = source.Position.OffsetX;
        destination.Position.OffsetY = source.Position.OffsetY;
    }
}
