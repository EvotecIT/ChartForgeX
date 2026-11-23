using ChartForgeX.Primitives;
namespace ChartForgeX.Topology;

public sealed partial class TopologyGroup {
    /// <summary>Gets or sets an explicit container surface paint. Null keeps the render option's native surface.</summary>
    public ChartColor? BackgroundColor { get; set; }

    /// <summary>Gets or sets an explicit container outline paint. Null keeps the group's accent-derived outline.</summary>
    public ChartColor? BorderColor { get; set; }

    /// <summary>Gets or sets an explicit group title and subtitle paint. Null keeps the theme's text paints.</summary>
    public ChartColor? LabelColor { get; set; }
}
