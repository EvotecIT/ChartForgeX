using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidFlowchartRenderOptions {
    /// <summary>Gets or sets an optional finite native icon catalog for exact Flowchart and UseCase icon ids. Null uses the built-in catalog.</summary>
    /// <remarks>Conversion and host snapshots copy the catalog, packs, definitions and safe artwork. No external icon provider or alias resolution is performed.</remarks>
    public TopologyIconCatalog? IconCatalog { get; set; }

    internal override void SnapshotMutableFields() {
        IconCatalog = IconCatalog?.Clone();
        SnapshotImageBindings();
    }
}
