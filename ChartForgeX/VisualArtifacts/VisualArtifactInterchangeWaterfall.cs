using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Themes;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Portable authoring data for one exclusive native waterfall, separate from calculated balances and connectors.</summary>
/// <remarks>Typed items include their explicit checkpoints. Numeric deltas append a calculated final total through the native owner.
/// Only one source collection may be populated. Callbacks, custom cultures, annotations and host-local resources are not portable.
/// Reader versions without the Waterfall family reject it and may use the separately delivered static fallback.</remarks>
public sealed partial class VisualArtifactInterchangeWaterfallArtifact {
    /// <summary>Gets or sets whether Items owns the source. False retains the numeric delta overload and its appended final total.</summary>
    public bool TypedItems { get; set; } = true;
    /// <summary>Gets immutable authored opening/delta/checkpoint items in accumulation order.</summary>
    public List<ChartWaterfallItem> Items { get; } = new();
    /// <summary>Gets authored numeric changes in input order, without a serialized calculated final total.</summary>
    public List<ChartPoint> DeltaPoints { get; } = new();
    /// <summary>Gets or sets the exact series identity. Automatic IDs remain local to the rebuilt chart.</summary>
    public string SeriesId { get; set; } = "waterfall";
    /// <summary>Gets or sets whether SeriesId belongs to the reserved chart-local automatic namespace.</summary>
    public bool AutomaticSeriesId { get; set; }
    /// <summary>Gets or sets the display name independently of the series identity.</summary>
    public string SeriesName { get; set; } = string.Empty;
    /// <summary>Gets or sets the physical primary or secondary value-axis slot.</summary>
    public ChartAxisSide ValueAxis { get; set; }
    /// <summary>Gets or sets the source series paint, including alpha. Null retains native waterfall status paints.</summary>
    public ChartColor? Color { get; set; }
    /// <summary>Gets or sets the native semantic series paint state, without host selection or muting state.</summary>
    public ChartSeriesState StateRole { get; set; }
    /// <summary>Gets or sets the shared native fill pattern.</summary>
    public ChartFillPattern FillPattern { get; set; }
    /// <summary>Gets or sets whether this series contributes its authored items to the legend.</summary>
    public bool ShowInLegend { get; set; } = true;
    /// <summary>Gets or sets the optional semantic mark role.</summary>
    public string? SemanticRole { get; set; }
    /// <summary>Gets or sets explicit data-caption visibility; null inherits the chart policy.</summary>
    public bool? SeriesShowDataLabels { get; set; }
    /// <summary>Gets or sets explicit series caption placement; null inherits the chart policy.</summary>
    public ChartDataLabelPlacement? SeriesDataLabelPlacement { get; set; }
    /// <summary>Gets or sets the series caption typography, cloned on export and import.</summary>
    public TextStyleOverride SeriesDataLabelStyle { get; set; } = new();
    /// <summary>Gets or sets the optional authored series stroke width. Null retains the native default.</summary>
    public double? StrokeWidth { get; set; }
    /// <summary>Gets sparse authored identities and per-item presentation. A numeric appended total permits paint/label overrides, never a source ID.</summary>
    public List<VisualArtifactInterchangeWaterfallPoint> PointPresentation { get; } = new();

    internal VisualArtifactInterchangeWaterfallArtifact Clone() {
        return CopyWaterfall(this);
    }

    private static VisualArtifactInterchangeWaterfallArtifact CopyWaterfall(VisualArtifactInterchangeWaterfallArtifact source) {
        var copy = new VisualArtifactInterchangeWaterfallArtifact();
        CopyPresentation(source, copy);
        copy.TypedItems = source.TypedItems; copy.SeriesId = source.SeriesId; copy.AutomaticSeriesId = source.AutomaticSeriesId;
        copy.SeriesName = source.SeriesName; copy.ValueAxis = source.ValueAxis; copy.Color = source.Color;
        copy.StateRole = source.StateRole; copy.FillPattern = source.FillPattern; copy.ShowInLegend = source.ShowInLegend;
        copy.SemanticRole = source.SemanticRole; copy.SeriesShowDataLabels = source.SeriesShowDataLabels;
        copy.SeriesDataLabelPlacement = source.SeriesDataLabelPlacement; copy.SeriesDataLabelStyle = source.SeriesDataLabelStyle.Clone(); copy.StrokeWidth = source.StrokeWidth;
        copy.Items.AddRange(source.Items); copy.DeltaPoints.AddRange(source.DeltaPoints);
        foreach (var point in source.PointPresentation) copy.PointPresentation.Add(point.Clone());
        return copy;
    }
}

/// <summary>Sparse identity and native presentation at one authored item index, or paint for the numeric derived final total.</summary>
public sealed class VisualArtifactInterchangeWaterfallPoint {
    /// <summary>Gets or sets the zero-based display-item index. Checkpoints count as typed authored items.</summary>
    public int Index { get; set; }
    /// <summary>Gets or sets an exact authored observation ID. Null retains the native ordinal identity.</summary>
    public string? ObservationId { get; set; }
    /// <summary>Gets or sets an explicit point fill, including alpha.</summary>
    public ChartColor? Color { get; set; }
    /// <summary>Gets or sets the point pattern; null inherits the series pattern.</summary>
    public ChartFillPattern? FillPattern { get; set; }
    /// <summary>Gets or sets the complete authored caption; null retains native formatting.</summary>
    public string? Label { get; set; }
    /// <summary>Gets or sets the point caption typography; null inherits the series policy.</summary>
    public TextStyleOverride? TextStyle { get; set; }
    internal VisualArtifactInterchangeWaterfallPoint Clone() => new() {
        Index = Index, ObservationId = ObservationId, Color = Color, FillPattern = FillPattern, Label = Label, TextStyle = TextStyle?.Clone()
    };
}
