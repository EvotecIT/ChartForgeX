using System;
using System.Globalization;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderTimelineDiagram(SvgMarkupWriter writer, TimelineDiagramBlock block) {
        var theme = block.Options.ResolvedTheme; var content = VisualBlockRendering.ContentRect(block.Options); var y = content.Top;
        RenderBlockHeading(writer, block, ref y, content.X, content.Width);
        var body = new ChartRect(content.X, y, content.Width, Math.Max(1, content.Bottom - y));
        var horizontal = block.Direction == TimelineDiagramDirection.LeftToRight;
        var periodWidth = Math.Min(142, body.Width * .27);
        var sectionWidth = horizontal ? 0 : VisualBlockRendering.TimelineSectionWidth(block, body.Width, periodWidth);
        var extent = (horizontal ? body.Width : body.Height) / block.Periods.Count;
        for (var index = 0; index < block.Periods.Count; index++) {
            var period = block.Periods[index]; var sectionIndex = -1;
            for (var s = 0; s < block.Sections.Count; s++) if (block.Sections[s].Id == period.SectionId) { sectionIndex = s; break; }
            var accent = VisualBlockRendering.PaletteAt(theme, Math.Max(0, sectionIndex));
            var cell = horizontal ? new ChartRect(body.Left + index * extent, body.Top, extent, body.Height) : new ChartRect(body.Left, body.Top + index * extent, body.Width, extent);
            writer.StartElement("g").Attribute("data-cfx-role", "timeline-period").Attribute("data-period-id", period.Id).Attribute("data-period-index", index).Attribute("data-section-id", period.SectionId).EndStartElement();
            if (index == 0 || block.Periods[index - 1].SectionId != period.SectionId) {
                var end = index + 1; while (end < block.Periods.Count && block.Periods[end].SectionId == period.SectionId) end++;
                var section = horizontal ? new ChartRect(cell.Left + 4, cell.Top, extent * (end - index) - 8, 26) : new ChartRect(cell.Left, cell.Top + 4, sectionWidth, extent * (end - index) - 8);
                if (sectionIndex >= 0) {
                    DiagramRect(writer, section, accent.WithAlpha(36), accent);
                    WriteDiagramLabel(writer, block.Sections[sectionIndex].Label,
                        new ChartRect(section.Left + 6, section.Top, Math.Max(1, section.Width - 12), section.Height),
                        theme, theme.Text, 12, 700, "timeline-section", preserveWords: true);
                }
            }
            var heading = horizontal ? new ChartRect(cell.Left + 6, cell.Top + 38, cell.Width - 12, 48) : new ChartRect(cell.Left + sectionWidth + 12, cell.Top + 8, periodWidth, cell.Height - 16);
            DiagramRect(writer, heading, accent.WithAlpha(56), accent);
            WriteDiagramLabel(writer, period.Label, new ChartRect(heading.Left + 8, heading.Top + 4, heading.Width - 16, heading.Height - 8), theme, theme.Text, 13, 700, "timeline-period-label", preserveWords: true);
            var eventSpace = Math.Max(1, horizontal ? cell.Height - 104 : cell.Height - 16) / period.Events.Count;
            for (var eventIndex = 0; eventIndex < period.Events.Count; eventIndex++) {
                var item = horizontal ? new ChartRect(cell.Left + 8, cell.Top + 100 + eventIndex * eventSpace, cell.Width - 16, eventSpace - 8) : new ChartRect(heading.Right + 16, cell.Top + 8 + eventIndex * eventSpace, Math.Max(1, cell.Right - heading.Right - 24), eventSpace - 8);
                writer.StartElement("g").Attribute("data-cfx-role", "timeline-event").Attribute("data-event-index", eventIndex).EndStartElement();
                DiagramRect(writer, item, theme.CardBackground, accent.WithAlpha(160));
                WriteDiagramLabel(writer, period.Events[eventIndex], new ChartRect(item.Left + 8, item.Top + 6, Math.Max(1, item.Width - 16), Math.Max(1, item.Height - 12)), theme, theme.Text, 12, 500, "timeline-event-label", preserveWords: true);
                writer.EndElement().Line();
            }
            writer.EndElement().Line();
        }
    }
}
