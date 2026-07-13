namespace ChartForgeX.Interactivity;

/// <summary>
/// Describes graph explorer performance budgets that adapters can enforce before interaction becomes expensive.
/// </summary>
public sealed class GraphPerformanceOptions {
    /// <summary>Gets or sets the desired frame budget in milliseconds for interactive graph work.</summary>
    public int FrameBudgetMilliseconds { get; set; } = 16;

    /// <summary>Gets or sets the maximum SVG node count that should run fully interactive browser physics.</summary>
    public int MaxInteractiveSvgNodes { get; set; } = 1200;

    /// <summary>Gets or sets the maximum SVG edge count that should run fully interactive browser physics.</summary>
    public int MaxInteractiveSvgEdges { get; set; } = 3000;

    /// <summary>Gets or sets the maximum Canvas node count that should run fully interactive browser physics.</summary>
    public int MaxInteractiveCanvasNodes { get; set; } = 5000;

    /// <summary>Gets or sets the maximum Canvas edge count that should run fully interactive browser physics.</summary>
    public int MaxInteractiveCanvasEdges { get; set; } = 12000;

    /// <summary>Gets or sets the maximum WebGL node count that should remain fully interactive.</summary>
    public int MaxInteractiveWebGlNodes { get; set; } = 20000;

    /// <summary>Gets or sets the maximum WebGL edge count that should remain fully interactive.</summary>
    public int MaxInteractiveWebGlEdges { get; set; } = 50000;

    /// <summary>Gets or sets how many simulation ticks should pass between telemetry events.</summary>
    public int TelemetrySampleInterval { get; set; } = 30;

    /// <summary>Gets or sets how many initial browser frames are reported as warmup without affecting the steady-state frame budget.</summary>
    public int WarmupFrameCount { get; set; } = 4;

    /// <summary>Gets or sets how many worker solver ticks may run between browser-visible layout updates.</summary>
    public int WorkerProgressInterval { get; set; } = 4;
}
