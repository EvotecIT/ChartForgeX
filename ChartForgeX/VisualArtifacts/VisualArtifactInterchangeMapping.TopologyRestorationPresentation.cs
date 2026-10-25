using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    private static void RestoreTopologyPresentation(TopologyChart chart, VisualArtifactInterchangePresentation? source) {
        if (source == null) return;
        chart.PreparedFrame = source.Frame?.RequestValues(retainOwnedFonts: true);
        chart.PreparedFont = source.ContextFont;
        if (source.Theme != null) {
            chart.Theme = RestoreCompleteTopologyTheme(source.Theme);
            chart.ThemeMode = source.Theme.Mode ?? VisualThemeMode.Light;
        }
        if (source.TopologyCaptions != null) source.TopologyCaptions.Apply(chart.DefaultRenderOptions ??= new TopologyRenderOptions());
        if (source.MapViewport != null) {
            var viewport = source.MapViewport;
            if (viewport.Projection != "Equirectangular" || string.IsNullOrWhiteSpace(viewport.Name) || viewport.Name != viewport.Name!.Trim())
                throw new NotSupportedException("Native topology restoration requires an Equirectangular map viewport with its native nonempty, trimmed name.");
            chart.MapViewport = new ChartMapViewport(viewport.Name!, viewport.MinimumLongitude,
                viewport.MaximumLongitude, viewport.MinimumLatitude, viewport.MaximumLatitude);
        }
        if (source.Legend == null) return;
        var legend = TopologyLegend.Create(source.Legend.Title);
        foreach (var item in source.Legend.Items) legend.Items.Add(new TopologyLegendItem {
            Label = item.Label, Kind = item.Kind, Status = item.Status, NodeKind = item.NodeKind, EdgeKind = item.EdgeKind,
            Symbol = item.Symbol, IconId = item.IconId, Color = item.Color, BackgroundColor = item.BackgroundColor, LineStyle = item.LineStyle
        });
        chart.Legend = legend;
    }

    // The portable topology theme retains resolved colors, family and every typography role reached by its body and common frame.
    // Both modes use that snapshot; other uncaptured typography/geometry roles retain native defaults.
    private static VisualTheme RestoreTopologyTheme(VisualArtifactInterchangeTheme source) {
        var unknown = TopologyThemeColor(source.Unknown);
        var tokens = new VisualDesignTokens {
            Background = TopologyThemeColor(source.Background), Foreground = TopologyThemeColor(source.Foreground),
            MutedForeground = TopologyThemeColor(source.MutedForeground), Surface = TopologyThemeColor(source.Card),
            ElevatedSurface = TopologyThemeColor(source.Surface), Border = TopologyThemeColor(source.Border),
            Accent = TopologyThemeColor(source.Accent), Positive = TopologyThemeColor(source.Healthy),
            Warning = TopologyThemeColor(source.Warning), Negative = TopologyThemeColor(source.Critical),
            Neutral = unknown, Disabled = TopologyThemeColor(source.Disabled)
        };
        var status = tokens.Status;
        status.Pass = new VisualTokenColor(tokens.Positive, status.Pass.Ink);
        status.Medium = new VisualTokenColor(tokens.Warning, status.Medium.Ink);
        status.Critical = new VisualTokenColor(tokens.Negative, status.Critical.Ink);
        status.Neutral = new VisualTokenColor(unknown, status.Neutral.Ink);
        status.Maintenance = new VisualTokenColor(tokens.Disabled, status.Maintenance.Ink);
        return new VisualTheme(tokens, tokens, new VisualTypography(family: source.FontFamily, titleSize: source.TitleSize ?? VisualTheme.Graphite().Typography.TitleSize,
            dataLabelSize: source.DataLabelSize ?? VisualTheme.Graphite().Typography.DataLabelSize,
            subtitleSize: source.SubtitleSize ?? VisualTheme.Graphite().Typography.SubtitleSize,
            legendSize: source.LegendSize ?? VisualTheme.Graphite().Typography.LegendSize));
    }

    private static ChartColor TopologyThemeColor(string source) {
        if (SvgRasterColor.TryParse(source, out var color)) return color;
        throw new NotSupportedException("Topology restoration requires literal resolved theme colors. Host CSS variable bindings are not portable native theme values.");
    }

    private static TopologyIconArtwork? RestoreTopologyArtwork(VisualArtifactInterchangeArtwork? source) {
        // Unsafe content was deliberately omitted by the exporter and cannot be reconstructed.
        if (source == null || source.Status == VisualArtifactInterchangeArtworkStatus.UnsafeOmitted) return null;
        var artwork = new TopologyIconArtwork {
            SvgBody = source.SvgBody, SvgPath = source.SvgPath, PreviewPath = source.PreviewPath, ImageHref = source.ImageHref
        };
        if (source.SvgViewBox != null) artwork.SvgViewBox = source.SvgViewBox;
        if (source.PreserveAspectRatio != null) artwork.PreserveAspectRatio = source.PreserveAspectRatio;
        return artwork;
    }
}
