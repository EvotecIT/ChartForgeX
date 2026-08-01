using System;
using System.Collections.Generic;
using ChartForgeX.Raster;

namespace ChartForgeX.Terminal;

internal sealed class TerminalTabRasterFonts {
    private readonly IReadOnlyDictionary<string, TrueTypeFont?> _outlineFonts;
    private readonly IReadOnlyDictionary<string, TrueTypeFont?> _tableFonts;

    private TerminalTabRasterFonts(
        IReadOnlyDictionary<string, TrueTypeFont?> outlineFonts,
        IReadOnlyDictionary<string, TrueTypeFont?> tableFonts) {
        _outlineFonts = outlineFonts;
        _tableFonts = tableFonts;
    }

    internal TrueTypeFont? InitialOutline(TerminalStory story) => Outline(story.Tabs[0]);

    internal TrueTypeFont? Outline(TerminalTab tab) => _outlineFonts[tab.Id];

    internal TrueTypeFont? Table(TerminalTab tab) => _tableFonts[tab.Id];

    internal static TerminalTabRasterFonts Resolve(TerminalStory story) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        return Create(
            story,
            tab => TrueTypeFont.TryLoadForFamily(tab.Theme.FontFamily, out _) ?? TrueTypeFont.TryLoadDefault());
    }

    internal static TerminalTabRasterFonts WithOutline(TerminalStory story, TrueTypeFont? outlineFont) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        return Create(story, _ => outlineFont);
    }

    private static TerminalTabRasterFonts Create(TerminalStory story, Func<TerminalTab, TrueTypeFont?> resolveOutline) {
        var outlines = new Dictionary<string, TrueTypeFont?>(StringComparer.OrdinalIgnoreCase);
        var tables = new Dictionary<string, TrueTypeFont?>(StringComparer.OrdinalIgnoreCase);
        foreach (var tab in story.Tabs) {
            var outline = resolveOutline(tab);
            outlines.Add(tab.Id, outline);
            tables.Add(tab.Id, PngTerminalStoryRenderer.ResolveTableFont(tab.Theme, outline));
        }
        return new TerminalTabRasterFonts(outlines, tables);
    }
}
