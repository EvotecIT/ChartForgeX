namespace ChartForgeX.Interactivity;

/// <summary>
/// Provides reusable presets for large topology and relationship-graph exploration.
/// </summary>
public static class GraphSceneSuperTopologyExtensions {
    /// <summary>
    /// Applies large-topology defaults that keep the scene model host-neutral while preparing adapters for vis-network-style exploration.
    /// </summary>
    /// <param name="options">The graph scene options to configure.</param>
    /// <param name="enableManipulation">Whether opt-in editing/manipulation capabilities should be advertised.</param>
    /// <returns>The current options instance.</returns>
    public static GraphSceneOptions UseSuperTopologyDefaults(this GraphSceneOptions options, bool enableManipulation = false) {
        if (options == null) throw new System.ArgumentNullException(nameof(options));
        options.Enable(GraphSceneFeatures.RuntimePhysics | GraphSceneFeatures.Stabilization | GraphSceneFeatures.DragNodes | GraphSceneFeatures.Export | GraphSceneFeatures.PerformanceTelemetry | GraphSceneFeatures.IncrementalUpdates);
        if (enableManipulation) {
            options.Enable(GraphSceneFeatures.Manipulation);
            options.Manipulation.EnableEditing();
        }

        options.Physics.Solver = GraphPhysicsSolver.BarnesHut;
        options.Physics.StabilizationIterations = 900;
        options.LevelOfDetail.ClusterNodeThreshold = 80;
        options.LevelOfDetail.HideEdgeLabelsThreshold = 120;
        options.LevelOfDetail.CompactNodeThreshold = 220;
        options.LevelOfDetail.CanvasPreferredNodeThreshold = 500;
        options.Cluster.Mode = GraphClusterMode.Hybrid;
        options.Cluster.Adaptive = true;
        options.Cluster.TargetClusterSize = 160;
        options.Performance.FrameBudgetMilliseconds = 10;
        options.Performance.MaxInteractiveSvgNodes = 900;
        options.Performance.MaxInteractiveSvgEdges = 1800;
        options.Performance.MaxInteractiveCanvasNodes = 10000;
        options.Performance.MaxInteractiveCanvasEdges = 24000;
        return options;
    }
}
