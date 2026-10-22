using System;
using System.Globalization;
using System.Text;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;
using ChartForgeX.Themes;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderJourneyDiagram(SvgMarkupWriter writer, JourneyDiagramBlock block) {
        var theme = block.Options.ResolvedTheme; var content = VisualBlockRendering.ContentRect(block.Options); var y = content.Top;
        RenderBlockHeading(writer, block, ref y, content.X, content.Width);
        var actors = VisualBlockRendering.JourneyActors(block);
        var bodyHeight = Math.Max(1, content.Bottom - y); var column = Math.Max(1, (content.Width - Math.Min(130, content.Width * .22)) / block.Tasks.Count);
        var left = content.Left + Math.Min(130, content.Width * .22); var header = Math.Min(92, bodyHeight * .24);
        var actorHeight = actors.Count == 0 ? 0 : Math.Min(bodyHeight * .38, 30 * actors.Count + 16);
        var scoreTop = y + header + 16; var scoreHeight = Math.Max(1, bodyHeight - header - actorHeight - 40);
        var path = new StringBuilder();
        for (var score = 1; score <= 5; score++) {
            var scoreY = scoreTop + (5 - score) * scoreHeight / 4;
            writer.StartElement("line").Attribute("x1", F(left)).Attribute("x2", F(content.Right)).Attribute("y1", F(scoreY)).Attribute("y2", F(scoreY)).Attribute("stroke", theme.CardBorder.ToCss()).Attribute("stroke-dasharray", "3 5").EndEmptyElement().Line();
            WriteText(writer, score.ToString(CultureInfo.InvariantCulture), content.Left, scoreY + 4, left - content.Left - 12, TextAlignment.Right, theme.MutedText, theme.FontFamily, 11, "400", "journey-score-tick");
        }
        for (var index = 0; index < block.Tasks.Count; index++) {
            var task = block.Tasks[index]; var x = left + column * index; var center = x + column / 2;
            var sectionIndex = -1; for (var s = 0; s < block.Sections.Count; s++) if (block.Sections[s].Id == task.SectionId) { sectionIndex = s; break; }
            var accent = VisualBlockRendering.PaletteAt(theme, Math.Max(0, sectionIndex));
            writer.StartElement("g").Attribute("data-cfx-role", "journey-task").Attribute("data-task-id", task.Id).Attribute("data-section-id", task.SectionId).Attribute("data-score", task.Score.ToString("R", CultureInfo.InvariantCulture)).EndStartElement();
            if (index == 0 || block.Tasks[index - 1].SectionId != task.SectionId) {
                var end = index + 1; while (end < block.Tasks.Count && block.Tasks[end].SectionId == task.SectionId) end++;
                var sectionBounds = new ChartRect(x + 3, y, column * (end - index) - 6, 25);
                if (sectionIndex >= 0) { DiagramRect(writer, sectionBounds, accent.WithAlpha(36), accent); WriteDiagramLabel(writer, block.Sections[sectionIndex].Label, sectionBounds, theme, theme.Text, 12, 700, "journey-section"); }
            }
            var label = new ChartRect(x + 4, y + 31, Math.Max(1, column - 8), Math.Max(1, header - 31));
            WriteDiagramLabel(writer, task.Label, label, theme, theme.Text, ResolvedVisualTheme.DefaultDataLabelFontSize, 600, "journey-task-label");
            var pointY = scoreTop + (5 - task.Score) * scoreHeight / 4;
            path.Append(index == 0 ? "M" : " L").Append(F(center)).Append(' ').Append(F(pointY));
            writer.StartElement("circle").Attribute("data-cfx-role", "journey-score").Attribute("cx", F(center)).Attribute("cy", F(pointY)).Attribute("r", 7).Attribute("fill", accent.ToCss()).Attribute("stroke", theme.CardBackground.ToCss()).Attribute("stroke-width", 2).EndEmptyElement().Line();
            var actorRow = actors.Count == 0 ? 0 : actorHeight / actors.Count;
            for (var a = 0; a < actors.Count; a++) {
                var rowY = content.Bottom - actorHeight + actorRow * a;
                if (index == 0) WriteDiagramLabel(writer, actors[a], new ChartRect(content.Left, rowY, left - content.Left - 12, actorRow), theme, theme.Text, 11, 500, "journey-actor-label");
                var participates = false; foreach (var actor in task.Actors) if (actor == actors[a]) { participates = true; break; }
                if (participates) writer.StartElement("circle").Attribute("data-cfx-role", "journey-actor-membership").Attribute("data-actor", actors[a]).Attribute("cx", F(center)).Attribute("cy", F(rowY + actorRow / 2)).Attribute("r", F(Math.Min(5, actorRow * .22))).Attribute("fill", VisualBlockRendering.PaletteAt(theme, a).ToCss()).EndEmptyElement().Line();
            }
            writer.EndElement().Line();
        }
        writer.StartElement("path").Attribute("data-cfx-role", "journey-score-path").Attribute("d", path.ToString()).Attribute("fill", "none").Attribute("stroke", theme.Text.WithAlpha(115).ToCss()).Attribute("stroke-width", 2).EndEmptyElement().Line();
    }
}
