using System;
using System.Linq;
using ChartForgeX.Accessibility;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

namespace ChartForgeX.Core;

public sealed partial class ChartGrid : IVisualRenderable {
    /// <summary>Gets the text alternative for the complete chart comparison.</summary>
    public VisualAccessibility Accessibility { get; } = new();

    /// <summary>Prepares all panels once in the supplied fixed viewport with a shared theme and frame.</summary>
    /// <remarks>Panel spans, column preservation and shared axis bounds configured on the source grid are retained.
    /// Child scenes are translated and clipped directly; no child SVG is parsed or rasterized.</remarks>
    public PreparedVisual Prepare(VisualRenderContext context) {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (Charts.Count == 0) throw new InvalidOperationException("Chart grids must contain at least one chart.");
        var frame = context.Frame;
        var colors = context.Theme.Resolve(context.ThemeMode);
        TextStyle Heading(TextStyle? explicitStyle, TextStyleOverride modelStyle, double size, ChartColor color) =>
            modelStyle.Resolve(explicitStyle ?? new TextStyle { Font = context.Font, FontSize = size, Color = color });
        frame = new VisualFrame(frame.Title ?? Title, frame.Subtitle ?? Subtitle, frame.ShowLegend, frame.LegendPosition,
            frame.ShowSurface, frame.TransparentBackground,
            Heading(frame.TitleStyle, TitleStyle, context.Theme.Typography.TitleSize, colors.Foreground),
            Heading(frame.SubtitleStyle, SubtitleStyle, context.Theme.Typography.SubtitleSize, colors.MutedForeground), frame.LegendStyle,
            frame.LegendMaximumRows, frame.LegendMaximumHeightFraction);
        var resolved = new VisualRenderContext(context.Layout, context.Theme, context.ThemeMode, frame, context.Font);
        var builder = new VisualSceneBuilder(context.Layout.Size, context.Font);
        var content = VisualFrameLayout.Build(builder, resolved, Array.Empty<VisualLegendEntry>());
        var columns = PreserveEmptyColumns ? Columns : Math.Min(Columns, Charts.Count);
        var placements = ChartGridLayout.PlacePanels(this, columns);
        var rows = placements.Max(placement => placement.Row + placement.RowSpan);
        var unitWidth = (content.Width - (columns - 1) * Gap) / columns;
        var unitHeight = (content.Height - (rows - 1) * Gap) / rows;
        if (unitWidth <= 0 || unitHeight <= 0) {
            builder.AddDiagnostic(new VisualDiagnostic("grid.insufficient-space", "The fixed viewport cannot accommodate the configured panel gaps."));
        } else using (builder.PushClip(content)) {
            for (var i = 0; i < Charts.Count; i++) {
                var placement = placements[i]; var chart = Charts[i];
                var cell = new ChartRect(content.Left + placement.Column * (unitWidth + Gap), content.Top + placement.Row * (unitHeight + Gap),
                    unitWidth * placement.ColumnSpan + Gap * (placement.ColumnSpan - 1), unitHeight * placement.RowSpan + Gap * (placement.RowSpan - 1));
                var sourceSize = PanelSize ?? chart.Options.Size;
                var width = cell.Width; var height = cell.Height;
                if (PanelFit == VisualPanelFit.Contain) {
                    var scale = Math.Min(width / sourceSize.Width, height / sourceSize.Height);
                    width = sourceSize.Width * scale; height = sourceSize.Height * scale;
                }
                var x = cell.Left + (cell.Width - width) / 2; var y = cell.Top + (cell.Height - height) / 2;
                var panelId = "panel-" + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var panelFrame = new VisualFrame(chart.Title, chart.Subtitle, frame.ShowLegend, frame.LegendPosition,
                    frame.ShowSurface, transparentBackground: true, legendStyle: frame.LegendStyle,
                    legendMaximumRows: frame.LegendMaximumRows, legendMaximumHeightFraction: frame.LegendMaximumHeightFraction);
                var padding = context.Layout.PaddingEdges;
                var panelPadding = new ChartPadding(Math.Min(padding.Left / 2, width / 4), Math.Min(padding.Top / 2, height / 4),
                    Math.Min(padding.Right / 2, width / 4), Math.Min(padding.Bottom / 2, height / 4));
                var panelContext = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, height), panelPadding),
                    context.Theme, context.ThemeMode, panelFrame, context.Font);
                var child = chart.Prepare(panelContext);
                builder.AddRegion(new VisualSemanticRegion(panelId, "panel", new ChartRect(x, y, width, height), chart.Title));
                using (builder.PushClip(cell)) builder.Append(child.Scene, x, y, panelId);
            }
        }
        var accessibility = Accessibility.Clone();
        accessibility.Name ??= string.IsNullOrWhiteSpace(frame.Title) ? Title : frame.Title;
        accessibility.Description ??= string.IsNullOrWhiteSpace(frame.Subtitle) ? Subtitle : frame.Subtitle;
        return new PreparedVisual(builder.Build(), accessibility);
    }
}
