using ChartForgeX.Mermaid;

namespace ChartForgeX.Markup.Mermaid;

public sealed partial class MermaidVisualMarkupBlockParser {
    private static MermaidFlowchartRenderOptions Clone(MermaidFlowchartRenderOptions options) {
        var clone = new MermaidFlowchartRenderOptions {
            Id = options.Id,
            Title = options.Title,
            Subtitle = options.Subtitle,
            FitContent = options.FitContent,
            Padding = options.Padding,
            MinimumFittedWidth = options.MinimumFittedWidth,
            MinimumFittedHeight = options.MinimumFittedHeight,
            MaximumFittedWidth = options.MaximumFittedWidth,
            MaximumFittedHeight = options.MaximumFittedHeight
        };
        if (options.HasExplicitViewportSize) {
            clone.Width = options.Width;
            clone.Height = options.Height;
        }

        return clone;
    }

    private static MermaidSequenceRenderOptions Clone(MermaidSequenceRenderOptions options) =>
        new() {
            Id = options.Id,
            Title = options.Title,
            Subtitle = options.Subtitle,
            Width = options.Width,
            Height = options.Height,
            Padding = options.Padding
        };

    private static MermaidPieRenderOptions Clone(MermaidPieRenderOptions options) =>
        new() {
            Id = options.Id,
            Title = options.Title,
            Subtitle = options.Subtitle,
            SeriesName = options.SeriesName,
            Width = options.Width,
            Height = options.Height
        };

    private static MermaidJourneyRenderOptions Clone(MermaidJourneyRenderOptions options) =>
        new() {
            Id = options.Id,
            Title = options.Title,
            Subtitle = options.Subtitle,
            SeriesName = options.SeriesName,
            Width = options.Width,
            Height = options.Height
        };

    private static MermaidGitGraphRenderOptions Clone(MermaidGitGraphRenderOptions options) =>
        new() {
            Id = options.Id,
            Title = options.Title,
            Subtitle = options.Subtitle,
            Width = options.Width,
            Height = options.Height,
            Padding = options.Padding,
            ShowBranchLabels = options.ShowBranchLabels,
            ShowCommitLabels = options.ShowCommitLabels
        };

    private static MermaidTimelineRenderOptions Clone(MermaidTimelineRenderOptions options) =>
        new() {
            Id = options.Id,
            Title = options.Title,
            Subtitle = options.Subtitle,
            Width = options.Width,
            Height = options.Height,
            ShowEventDurations = options.ShowEventDurations
        };

    private static MermaidQuadrantRenderOptions Clone(MermaidQuadrantRenderOptions options) =>
        new() {
            Id = options.Id,
            Title = options.Title,
            Subtitle = options.Subtitle,
            SeriesName = options.SeriesName,
            Width = options.Width,
            Height = options.Height
        };

    private static MermaidXYChartRenderOptions Clone(MermaidXYChartRenderOptions options) =>
        new() {
            Id = options.Id,
            Title = options.Title,
            Subtitle = options.Subtitle,
            Width = options.Width,
            Height = options.Height,
            ShowDataLabels = options.ShowDataLabels
        };

    private static MermaidSankeyRenderOptions Clone(MermaidSankeyRenderOptions options) =>
        new() {
            Id = options.Id,
            Title = options.Title,
            Subtitle = options.Subtitle,
            SeriesName = options.SeriesName,
            Width = options.Width,
            Height = options.Height
        };

    private static MermaidRadarRenderOptions Clone(MermaidRadarRenderOptions options) =>
        new() {
            Id = options.Id,
            Title = options.Title,
            Subtitle = options.Subtitle,
            Width = options.Width,
            Height = options.Height
        };

    private static MermaidTreemapRenderOptions Clone(MermaidTreemapRenderOptions options) =>
        new() {
            Id = options.Id,
            Title = options.Title,
            Subtitle = options.Subtitle,
            SeriesName = options.SeriesName,
            Width = options.Width,
            Height = options.Height
        };

    private static MermaidGanttRenderOptions Clone(MermaidGanttRenderOptions options) =>
        new() {
            Id = options.Id,
            Title = options.Title,
            Subtitle = options.Subtitle,
            Width = options.Width,
            Height = options.Height,
            Today = options.Today
        };

    private static MermaidPacketRenderOptions Clone(MermaidPacketRenderOptions options) =>
        new() {
            Id = options.Id,
            Title = options.Title,
            Subtitle = options.Subtitle,
            Width = options.Width,
            Height = options.Height,
            Padding = options.Padding,
            BitsPerRow = options.BitsPerRow,
            ShowBitNumbers = options.ShowBitNumbers
        };

    private static MermaidBlockRenderOptions Clone(MermaidBlockRenderOptions options) =>
        new() {
            Id = options.Id,
            Title = options.Title,
            Subtitle = options.Subtitle,
            Width = options.Width,
            Height = options.Height,
            Padding = options.Padding,
            Columns = options.Columns,
            ShowEdges = options.ShowEdges
        };

    private static MermaidVennRenderOptions Clone(MermaidVennRenderOptions options) =>
        new() {
            Id = options.Id,
            Title = options.Title,
            Subtitle = options.Subtitle,
            Width = options.Width,
            Height = options.Height,
            Padding = options.Padding
        };

    private static MermaidIshikawaRenderOptions Clone(MermaidIshikawaRenderOptions options) =>
        new() {
            Id = options.Id,
            Title = options.Title,
            Subtitle = options.Subtitle,
            Width = options.Width,
            Height = options.Height,
            Padding = options.Padding
        };

    private static MermaidWardleyRenderOptions Clone(MermaidWardleyRenderOptions options) =>
        new() {
            Id = options.Id,
            Title = options.Title,
            Subtitle = options.Subtitle,
            Width = options.Width,
            Height = options.Height,
            Padding = options.Padding
        };

    private static MermaidTopologyRenderOptions Clone(MermaidTopologyRenderOptions options) =>
        new() {
            Id = options.Id,
            Title = options.Title,
            Subtitle = options.Subtitle,
            Width = options.Width,
            Height = options.Height,
            Padding = options.Padding
        };
}
