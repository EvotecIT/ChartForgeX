using System;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    // Views adjust their groups; each view owns copies so the authored options stay unchanged.
    private ChartOptions CopyWithGroups() {
        var view = (ChartOptions)MemberwiseClone();
        view.Legend = Legend.Clone();
        view.DataLabels = DataLabels.Clone();
        view.Bars = Bars.Clone();
        view.Lines = Lines.Clone();
        view.Heatmap = Heatmap.Clone();
        view.Map = Map.Clone();
        view.Pictorial = Pictorial.Clone();
        view.Progress = Progress.Clone();
        view.Circle = Circle.Clone();
        view.Pie = Pie.Clone();
        view.WordCloud = WordCloud.Clone();
        view.Raster = Raster.Clone();
        view.Gantt = Gantt.Clone();
        view.Polar = Polar.Clone();
        view.StateTimeline = StateTimeline.Clone();
        return view;
    }
    internal ChartOptions PanelView() { var view = CopyWithGroups(); view.IsPanel = true; return view; }
    internal ChartOptions PresentationView(Rendering.VisualRenderContext context) {
        var view = CopyWithGroups();
        // Prepared domains and viewport edge policies belong to this private preparation only.
        view.XAxis = XAxis.Clone(); view.YAxis = YAxis.Clone();
        view.SecondaryXAxis = SecondaryXAxis.Clone(); view.SecondaryYAxis = SecondaryYAxis.Clone();
        view.RangeBar = RangeBar.Clone(); view.BoxPlot = BoxPlot.Clone();
        var font = context.Font;
        view.Theme = context.Theme.WithFontFamily(font.Family);
        view.ThemeMode = context.ThemeMode;
        view.Raster.FontPath = font.FilePath; view.Raster.FontFaceName = font.FaceName; view.Raster.FontCollectionIndex = font.CollectionIndex;
        return view;
    }
    /// <summary>Gets or sets whether the embedding host draws the frame. Removes the chart surface and outer padding.</summary>
    public bool HostOwnsFrame { get; set; }
    /// <summary>Gets or sets whether a panel uses the compact 15 px title.</summary>
    public bool IsPanel { get; set; }
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
