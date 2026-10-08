using System;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;

using static ChartForgeX.VisualBlocks.SvgVisualBlockRenderer;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgFactBlockRenderer {
    private static void RenderSegmentedMetricFunnelColumns(SvgMarkupWriter writer, SegmentedMetricBlock card) {
        var options = card.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        RenderSegmentedMetricHeading(writer, card, ref y, content.X, content.Width);
        y += 10;

        var layout = VisualFactBlockRendering.SegmentedFunnelLayout(card, content, y);

        writer.StartElement("g").Attribute("data-cfx-role", "segmented-metric-funnel-columns").Attribute("data-cfx-stages", card.Items.Count).Attribute("data-cfx-segments", layout.TotalSegments).Attribute("data-cfx-bar-y", layout.BarY).Attribute("data-cfx-bar-height", layout.BarHeight).Attribute("data-cfx-bar-width", layout.BarWidth).Attribute("data-cfx-bar-gap", layout.BarGap).EndStartElement().Line();
        foreach (var stage in layout.Stages) {
            var item = stage.Item;
            var color = VisualFactBlockRendering.SegmentedItemColor(theme, item, stage.Index);
            var value = VisualFactBlockRendering.SegmentedItemDisplayValue(card, item);
            writer.StartElement("g").Attribute("data-cfx-role", "segmented-metric-funnel-stage").Attribute("data-cfx-label", item.Label).Attribute("data-cfx-value", item.Value).Attribute("data-cfx-segments", stage.SegmentCount).Attribute("data-cfx-x", stage.X).Attribute("data-cfx-width", stage.Width).EndStartElement().Line();
            writer.StartElement("rect").Attribute("data-cfx-role", "segmented-metric-funnel-marker").Attribute("x", stage.X).Attribute("y", y + 2).Attribute("width", 7).Attribute("height", 24).Attribute("rx", 3.5).Attribute("fill", color.ToCss()).EndEmptyElement().Line();
            WriteText(writer, item.Label, stage.X + 11, y + 16, Math.Max(48, stage.Width + layout.GroupGap * 0.5), TextAlignment.Left, theme.MutedText, theme.FontFamily, Math.Max(10, theme.SubtitleFontSize), "500");
            WriteText(writer, value, stage.X + 11, y + 35, Math.Max(48, stage.Width + layout.GroupGap * 0.5), TextAlignment.Left, theme.Text, theme.FontFamily, Math.Max(13, theme.SubtitleFontSize + 2), "850");
            for (var i = 0; i < stage.SegmentCount; i++) {
                var x = stage.X + i * (layout.BarWidth + layout.BarGap);
                writer.StartElement("rect")
                    .Attribute("data-cfx-role", "segmented-metric-funnel-bar")
                    .Attribute("data-cfx-stage", stage.Index)
                    .Attribute("data-cfx-index", i)
                    .Attribute("x", x)
                    .Attribute("y", layout.BarY)
                    .Attribute("width", layout.BarWidth)
                    .Attribute("height", layout.BarHeight)
                    .Attribute("rx", Math.Min(4, layout.BarWidth * 0.48))
                    .Attribute("fill", color.ToCss())
                    .EndEmptyElement().Line();
                if (!theme.FlatMarks) writer.StartElement("rect")
                    .Attribute("data-cfx-role", "segmented-metric-funnel-bar-highlight")
                    .Attribute("data-cfx-stage", stage.Index)
                    .Attribute("data-cfx-index", i)
                    .Attribute("x", x + Math.Max(0.6, layout.BarWidth * 0.18))
                    .Attribute("y", layout.BarY + 2)
                    .Attribute("width", Math.Max(0.8, layout.BarWidth * 0.28))
                    .Attribute("height", Math.Max(1, layout.BarHeight - 4))
                    .Attribute("rx", Math.Min(3, layout.BarWidth * 0.24))
                    .Attribute("fill", ChartColor.White.WithAlpha(48).ToCss())
                    .EndEmptyElement().Line();
            }

            writer.EndElement().Line();
        }

        writer.EndElement().Line();
    }
}
