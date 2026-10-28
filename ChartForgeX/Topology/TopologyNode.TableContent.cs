using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Topology;

public sealed partial class TopologyNode {
    private TableArtifact? _tableContent;
    /// <summary>Gets or sets finite in-memory table content enclosed by this node's native body.</summary>
    /// <remarks>Preparation captures columns, keyed rows, cell values and metadata independently.
    /// Supported Generic Card/CompactCard rectangle/rounded nodes retain their label/subtitle and footer.
    /// Use FitLabelToContent to grow minimum bounds before routing. Providers and virtual rows are not captured.</remarks>
    public TableArtifact? TableContent { get => _tableContent; set { InvalidateSurfaceElementAppearance(); _tableContent = value; } }
}
