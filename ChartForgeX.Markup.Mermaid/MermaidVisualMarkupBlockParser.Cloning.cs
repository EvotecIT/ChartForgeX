using ChartForgeX.Mermaid;

namespace ChartForgeX.Markup.Mermaid;

public sealed partial class MermaidVisualMarkupBlockParser {
    private static MermaidFlowchartRenderOptions Clone(MermaidFlowchartRenderOptions options) => (MermaidFlowchartRenderOptions)options.Clone();
    private static MermaidSequenceRenderOptions Clone(MermaidSequenceRenderOptions options) => (MermaidSequenceRenderOptions)options.Clone();
    private static MermaidPieRenderOptions Clone(MermaidPieRenderOptions options) => (MermaidPieRenderOptions)options.Clone();
    private static MermaidJourneyRenderOptions Clone(MermaidJourneyRenderOptions options) => (MermaidJourneyRenderOptions)options.Clone();
    private static MermaidGitGraphRenderOptions Clone(MermaidGitGraphRenderOptions options) => (MermaidGitGraphRenderOptions)options.Clone();
    private static MermaidTimelineRenderOptions Clone(MermaidTimelineRenderOptions options) => (MermaidTimelineRenderOptions)options.Clone();
    private static MermaidQuadrantRenderOptions Clone(MermaidQuadrantRenderOptions options) => (MermaidQuadrantRenderOptions)options.Clone();
    private static MermaidXYChartRenderOptions Clone(MermaidXYChartRenderOptions options) => (MermaidXYChartRenderOptions)options.Clone();
    private static MermaidSankeyRenderOptions Clone(MermaidSankeyRenderOptions options) => (MermaidSankeyRenderOptions)options.Clone();
    private static MermaidRadarRenderOptions Clone(MermaidRadarRenderOptions options) => (MermaidRadarRenderOptions)options.Clone();
    private static MermaidTreemapRenderOptions Clone(MermaidTreemapRenderOptions options) => (MermaidTreemapRenderOptions)options.Clone();
    private static MermaidGanttRenderOptions Clone(MermaidGanttRenderOptions options) => (MermaidGanttRenderOptions)options.Clone();
    private static MermaidPacketRenderOptions Clone(MermaidPacketRenderOptions options) => (MermaidPacketRenderOptions)options.Clone();
    private static MermaidBlockRenderOptions Clone(MermaidBlockRenderOptions options) => (MermaidBlockRenderOptions)options.Clone();
    private static MermaidVennRenderOptions Clone(MermaidVennRenderOptions options) => (MermaidVennRenderOptions)options.Clone();
    private static MermaidIshikawaRenderOptions Clone(MermaidIshikawaRenderOptions options) => (MermaidIshikawaRenderOptions)options.Clone();
    private static MermaidWardleyRenderOptions Clone(MermaidWardleyRenderOptions options) => (MermaidWardleyRenderOptions)options.Clone();
    private static MermaidTopologyRenderOptions Clone(MermaidTopologyRenderOptions options) => (MermaidTopologyRenderOptions)options.Clone();
}
