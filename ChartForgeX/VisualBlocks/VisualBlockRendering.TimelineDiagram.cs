using System;
using System.Collections.Generic;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualBlocks;

internal static partial class VisualBlockRendering {
    internal static double TimelineSectionWidth(TimelineDiagramBlock block, double bodyWidth, double periodWidth) {
        var legacyWidth = Math.Min(102, bodyWidth * .17);
        var theme = block.Options.ResolvedTheme;
        var sectionStyle = new TextStyle { Font = new FontSpec { Family = theme.FontFamily, Weight = 700 }, FontSize = BodyFontSize(theme, 12) };
        var eventStyle = new TextStyle { Font = new FontSpec { Family = theme.FontFamily, Weight = 500 }, FontSize = BodyFontSize(theme, 12) };
        var requested = legacyWidth; var eventMinimum = 1.0;
        foreach (var period in block.Periods) {
            foreach (var section in block.Sections) if (section.Id == period.SectionId)
                requested = Math.Max(requested, DiagramWordWidth(section.Label, sectionStyle) + 12);
            foreach (var text in period.Events) eventMinimum = Math.Max(eventMinimum, DiagramWordWidth(text, eventStyle) + 16);
        }
        // The authored frame stays fixed. Reserve the period column, actual event words and
        // existing inter-column gaps before giving a section caption additional space.
        var available = Math.Max(legacyWidth, bodyWidth - periodWidth - 36 - eventMinimum);
        return Math.Min(requested, available);
    }

    public static void ValidateTimelineDiagram(TimelineDiagramBlock block) {
        if (block.Periods.Count == 0 || block.Periods.Count > 512) throw new InvalidOperationException("Categorical timelines require one through 512 periods.");
        if (block.Sections.Count > 128) throw new InvalidOperationException("Categorical timelines allow no more than 128 sections.");
        var sections = new HashSet<string>(StringComparer.Ordinal);
        foreach (var section in block.Sections) DiagramIdentity(sections, section.Id, section.Label, "timeline section");
        var periods = new HashSet<string>(StringComparer.Ordinal); var events = 0;
        foreach (var period in block.Periods) {
            DiagramIdentity(periods, period.Id, period.Label, "timeline period");
            if (period.SectionId != null && !sections.Contains(period.SectionId)) throw new InvalidOperationException("Unknown timeline section: " + period.SectionId + ".");
            if (period.Events.Count == 0) throw new InvalidOperationException("A timeline period requires at least one event.");
            foreach (var item in period.Events) if (string.IsNullOrWhiteSpace(item)) throw new InvalidOperationException("Timeline events must not be empty.");
            events += period.Events.Count;
        }
        if (events > 2048) throw new InvalidOperationException("Categorical timelines allow no more than 2048 events.");
    }
}
