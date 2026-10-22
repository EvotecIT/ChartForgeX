using System;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    internal ChartOptions PanelView() { var view = (ChartOptions)MemberwiseClone(); view.IsPanel = true; return view; }
    internal ChartOptions PresentationView(Rendering.VisualRenderContext context) {
        var view = (ChartOptions)MemberwiseClone();
        // Prepared domains and viewport edge policies belong to this private preparation only.
        view.XAxis = XAxis.Clone(); view.YAxis = YAxis.Clone();
        view.SecondaryXAxis = SecondaryXAxis.Clone(); view.SecondaryYAxis = SecondaryYAxis.Clone();
        var font = context.Font;
        view.Theme = context.Theme.WithFontFamily(font.Family);
        view.ThemeMode = context.ThemeMode;
        view.PngFontPath = font.FilePath; view.PngFontFaceName = font.FaceName; view.PngFontCollectionIndex = font.CollectionIndex;
        return view;
    }
    private int _maximumPieSlices = 6;
    private ChartLineMarkerMode? _lineMarkerMode;
    /// <summary>Gets or sets whether the embedding host draws the frame. Removes the chart surface and outer padding.</summary>
    public bool HostOwnsFrame { get; set; }
    /// <summary>Gets or sets whether a panel uses the compact 15 px title.</summary>
    public bool IsPanel { get; set; }
    /// <summary>Gets or sets optional line-marker visibility. Null uses the shared default (last point).</summary>
    public ChartLineMarkerMode? LineMarkerMode { get => _lineMarkerMode; set { if (value.HasValue && !Enum.IsDefined(typeof(ChartLineMarkerMode), value.Value)) throw new ArgumentOutOfRangeException(nameof(value)); _lineMarkerMode = value; } }
    /// <summary>Gets or sets the maximum displayed pie or donut slices, including an aggregate Other slice.</summary>
    public int MaximumPieSlices { get => _maximumPieSlices; set { if (value < 2) throw new ArgumentOutOfRangeException(nameof(value)); _maximumPieSlices = value; } }
}

/// <summary>Controls optional markers without removing the full series data or interaction targets.</summary>
public enum ChartLineMarkerMode {
    /// <summary>Draws a marker at each retained point.</summary>
    All,
    /// <summary>Draws only the final marker.</summary>
    Last,
    /// <summary>Draws no optional markers.</summary>
    None
}
