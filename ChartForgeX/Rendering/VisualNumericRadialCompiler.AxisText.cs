using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualNumericRadialCompiler {
    /// <summary>Reserves title strips inside the shared frame, before fitting the radial geometry and tick lanes.</summary>
    private static ChartRect AxisTitles(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect content,
        Dictionary<ChartAxisSide, RadialValueScale> scales) {
        if (!chart.Options.ShowAxes) return content;
        var category = chart.Options.XAxis.Visible && chart.XAxisTitle.Length > 0;
        var primary = scales.ContainsKey(ChartAxisSide.Primary) && chart.Options.YAxis.Visible && chart.YAxisTitle.Length > 0;
        var secondary = scales.ContainsKey(ChartAxisSide.Secondary) && chart.Options.SecondaryYAxis.Visible && chart.SecondaryYAxisTitle.Length > 0;
        if (!category && !primary && !secondary) return content;
        var style = chart.Options.AxisTitleStyle.Resolve(new TextStyle {
            Font = context.Font, FontSize = context.Theme.Typography.AxisSize, Color = context.Theme.Resolve(context.ThemeMode).Foreground
        });
        var spacing = context.Theme.Spacing;
        var primaryHeight = primary ? builder.MeasureText(chart.YAxisTitle, style).Height : 0;
        var secondaryHeight = secondary ? builder.MeasureText(chart.SecondaryYAxisTitle, style).Height : 0;
        var top = primary || secondary ? Math.Min(content.Height * .25, Math.Max(primaryHeight, secondaryHeight) + spacing) : 0;
        var bottom = category ? Math.Min(content.Height * .3, builder.MeasureText(chart.XAxisTitle, style).Height + spacing) : 0;
        var plot = new ChartRect(content.Left, content.Top + top, content.Width, Math.Max(0, content.Height - top - bottom));
        var titleWidth = primary && secondary ? Math.Max(0, (content.Width - spacing) / 2) : content.Width;
        if (primary) Title(chart.YAxisTitle, new ChartRect(content.Left, content.Top, titleWidth, Math.Max(0, top - spacing)),
            TextAlignment.Left, "primary", "radial-value-axis-title");
        if (secondary) Title(chart.SecondaryYAxisTitle, new ChartRect(content.Right - titleWidth, content.Top, titleWidth, Math.Max(0, top - spacing)),
            TextAlignment.Right, "secondary", "radial-value-axis-title");
        if (category) {
            var y = Math.Min(content.Bottom, plot.Bottom + spacing);
            Title(chart.XAxisTitle, new ChartRect(content.Left, y, content.Width, Math.Max(0, content.Bottom - y)),
                TextAlignment.Center, "category", "radial-category-axis-title");
        }
        return plot;

        void Title(string text, ChartRect bounds, TextAlignment alignment, string axis, string role) {
            var titleStyle = style.Clone(); titleStyle.Alignment = alignment;
            var id = role + "-" + axis;
            using (builder.PushGroup(id + "-source", role + "-source", new Dictionary<string, string> {
                ["data-cfx-axis"] = axis, ["data-cfx-full-label"] = text
            }))
                VisualAxisText.Title(builder, text, bounds, titleStyle, role, id, "numeric-radial.axis-title-overflow",
                    "An axis title was shortened or omitted within its measured strip; complete text remains available in descriptive regions.");
        }
    }
}
