using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private NativeTableData TableData(TopologyNode node, ChartRect bounds, double size, double scale = 1) {
        var typography = _context.Theme.Typography; var ratio = size / typography.DataLabelSize;
        var family = node.TextStyle?.FontFamily ?? _context.Font.Family;
        var theme = _context.Theme.WithTypography(new VisualTypography(family, typography.TitleSize * ratio,
            typography.SubtitleSize * ratio, typography.AxisSize * ratio, typography.LegendSize * ratio,
            size, typography.ScalarValueSize * ratio, typography.CenterValueSize * ratio, typography.AxisTitleSize * ratio, typography.MonospaceFamily));
        var style = TopologyTextPolicy.Resolve(node.TextStyle, _context.Font, size, 400);
        var data = NativeTableData.Capture(node.TableContent!, bounds, new ResolvedVisualTheme(theme, _context.ThemeMode), style.Font,
            node.Id + "/table/" + node.TableContent!.Id, scale);
        if (node.TextStyle?.FontSize != null || node.TableContent!.CapturedPresentationScale != 1)
            data.AbsoluteDataLabelSize = size * scale;
        if (node.TextStyle?.FontWeight != null) data.WeightOverride = style.Font.Weight;
        return data;
    }
    // Captured table interiors use the retained geometry scale. Explicit captured role fonts are already absolute.
    private double ShapeContentSpacingScale(TopologyNode node) => node.TableContent?.CapturedPresentationScale ?? 1;
    private double ShapeContentFontSize(TopologyNode node) => TextPolicySize(_context.Theme.Typography.DataLabelSize)
        * (node.TableContent != null && node.TextStyle?.FontSize == null ? node.TableContent.CapturedPresentationScale : 1);

    private double TableHeight(TopologyNode node, double size) {
        if (node.TableContent == null) return 0;
        var spacing = ShapeContentSpacingScale(node);
        var width = Math.Max(1, node.Width - 20 * spacing - 2 * (node.ContentPadding ?? 0) * spacing);
        var available = Math.Max(node.Height, (node.TableContent.Rows.Count + 8) * size * 4);
        return NativeTablePaintPlan.Capture(TableData(node, new ChartRect(0, 0, width, available), size)).RequiredHeight;
    }
    private void GrowTableWidth(TopologyNode node, double size) {
        if (node.TableContent == null) return;
        var data = TableData(node, new ChartRect(0, 0, node.Width, Math.Max(1, node.Height)), size);
        var wanted = NativeTablePaintPlan.PreferredWidth(data);
        var spacing = ShapeContentSpacingScale(node);
        if (node.MaximumTextWidth.HasValue) wanted = Math.Min(wanted, node.MaximumTextWidth.Value * spacing);
        node.Width = Math.Max(node.Width, wanted + 20 * spacing + 2 * (node.ContentPadding ?? 0) * spacing);
        // Resolve every table row before sizing, layout, notes or obstacle routing use the node body.
        node.Height = Math.Max(node.Height, ShapeContent(node, size).RequiredHeight + 12 * spacing + 2 * (node.ContentPadding ?? 0) * spacing);
    }
    private void BuildTableContent(TopologyNode node, ChartRect bounds, double size, bool active) {
        var projected = Bounds(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        var data = TableData(node, projected, size, _scale);
        data.Opacity = HighlightFactor(active);
        if (!string.IsNullOrWhiteSpace(node.LabelColor)) data.Foreground = Color(node.LabelColor, _colors.Foreground);
        var plan = NativeTablePaintPlan.Capture(data);
        if (plan.RequiredWidth > projected.Width + 1e-8)
            throw new InvalidOperationException("Embedded table fixed column widths exceed its prepared node body. Enlarge the node or remove conflicting width hints.");
        if (plan.VisibleRows != node.TableContent!.Rows.Count)
            throw new InvalidOperationException("Embedded table '" + node.TableContent.Id + "' does not fit its prepared node body. Enable FitLabelToContent or enlarge the node.");
        using (_builder.PushGroup(node.Id + "/table/" + node.TableContent.Id, "topology-node-table", new Dictionary<string, string> {
            ["data-node-id"] = node.Id, ["data-table-id"] = node.TableContent.Id })) plan.AppendTo(_builder);
        if (plan.HasShortenedText) _builder.AddDiagnostic(new VisualDiagnostic("topology.table-cell-truncated",
            "An authored table width shortens one or more native cell captions; complete source values remain in typed table content."));
    }
}
