namespace ChartForgeX.Topology;

public sealed partial class TopologyGroup {
    /// <summary>Gets or sets an explicit container surface paint. Null keeps the render option's native surface.</summary>
    public string? BackgroundColor { get; set; }

    /// <summary>Gets or sets an explicit container outline paint. Null keeps the group's accent-derived outline.</summary>
    public string? BorderColor { get; set; }

    /// <summary>Gets or sets an explicit group title and subtitle paint. Null keeps the theme's text paints.</summary>
    public string? LabelColor { get; set; }
}
