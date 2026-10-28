using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

public static partial class MermaidFlowchartRendering {
    /// <summary>Applies source-only marker dimensions and caption visibility without changing retained labels.</summary>
    private static void ApplyCatalogShapePresentation(MermaidFlowchartDocument document, MermaidFlowchartNode source, TopologyNode target) {
        switch (source.Shape) {
            case MermaidFlowchartNodeShape.SynchronizationBar:
                var direction = document.Direction;
                var group = source.SubgraphId == null ? null : document.Subgraphs.Find(item => item.Id == source.SubgraphId);
                if (group != null) direction = EffectiveSubgraphDirection(document, group);
                var horizontalFlow = direction is MermaidFlowchartDirection.LeftToRight or MermaidFlowchartDirection.RightToLeft;
                target.Width = horizontalFlow ? 10 : 70; target.Height = horizontalFlow ? 70 : 10;
                HideCaption(); break;
            case MermaidFlowchartNodeShape.FilledCircle:
            case MermaidFlowchartNodeShape.FramedCircle:
                target.Width = target.Height = 14; HideCaption(); break;
            case MermaidFlowchartNodeShape.Hourglass:
                target.Width = target.Height = 30; HideCaption(); break;
            case MermaidFlowchartNodeShape.CrossedCircle:
                target.Width = target.Height = 60; HideCaption(); break;
            case MermaidFlowchartNodeShape.LightningBolt:
                target.Width = 35; target.Height = 70; HideCaption(); break;
            case MermaidFlowchartNodeShape.Person:
                target.Height = System.Math.Max(target.Height, 108); break;
        }
        void HideCaption() { target.ShowLabel = false; target.FitLabelToContent = false; }
    }
}
