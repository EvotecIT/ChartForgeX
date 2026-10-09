using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Terminal;
using ChartForgeX.Typography;

namespace ChartForgeX.Stories;

internal static partial class NativeVisualStoryRenderer {
    private static void DrawReplay(VisualSceneBuilder parent, VisualStory story, VisualStoryReplaySurface surface, ChartRect bounds, double? elapsed) {
        var state = surface.Replay.At(elapsed, surface.Options.HistoryLines);
        DrawTerminal(parent, story, state.Active.Tab, state.Tabs.Select(tab => tab.Tab).ToArray(), state.Active.Lines,
            surface.Theme, surface.Options, bounds, state.Marker, state.Active.Discarded);
    }
    private static void DrawTerminalViewport(VisualSceneBuilder parent, VisualStory story, VisualStoryTerminalSurface surface, ChartRect bounds, double? elapsed) {
        var layout = TerminalStoryLayout.BuildLogical(surface.Terminal);
        var tab = layout.Tabs.OrderByDescending(item => layout.TabOpacity(item.Tab.Id, elapsed)).First();
        var lines = new List<TerminalViewportLine>();
        foreach (var line in tab.Lines) {
            var state = TerminalLinePlayback.At(line, elapsed);
            if (!state.Visible) continue;
            var text = line.IsCommand && elapsed.HasValue ? TerminalLinePlayback.CommandText(line, elapsed.Value) : line.Text;
            lines.Add(new TerminalViewportLine(text, line.IsCommand ? TerminalTextTone.Accent : line.Tone, state.Opacity));
        }
        DrawTerminal(parent, story, tab.Tab, layout.Tabs.Where(item => layout.TabVisible(item.Tab.Id, elapsed)).Select(item => item.Tab).ToArray(),
            lines, tab.Tab.Theme, surface.Options!, bounds, string.Empty, 0);
    }

    private static void DrawTerminal(VisualSceneBuilder parent, VisualStory story, TerminalTab active, IReadOnlyList<TerminalTab> tabs,
        IReadOnlyList<TerminalViewportLine> lines, TerminalTheme theme, VisualStoryTerminalOptions options, ChartRect bounds, string marker, int discarded) {
        var builder = new VisualSceneBuilder(new VisualSize(story.Width, story.Height), FontSpec.FromFamily(theme.FontFamily));
        var size = options.FontSize; var lineHeight = size * 1.5;
        var content = new ChartRect(bounds.X + 12, bounds.Y + 48, bounds.Width - 24, bounds.Height - 78);
        if (content.Width < builder.MeasureText("M", size).Width || content.Height < lineHeight)
            throw new InvalidOperationException("The terminal viewport cannot fit a readable line. Enlarge the panel or rebalance its weight.");
        var capacity = Math.Max(1, (int)(content.Height / lineHeight));
        builder.Rect(bounds, theme.Background, theme.Border, radius: 10, role: "terminal-viewport");
        builder.Rect(new ChartRect(bounds.X, bounds.Y, bounds.Width, 35), theme.HeaderBackground, radius: 10);
        var title = active.Title + (tabs.Count > 1 ? " · " + (Array.FindIndex(tabs.ToArray(), tab => tab.Id == active.Id) + 1).ToString(CultureInfo.InvariantCulture) + "/" + tabs.Count.ToString(CultureInfo.InvariantCulture) : "");
        FitText(builder, title, bounds.X + 14, bounds.Y + 23, bounds.Width - 28, 13, theme.Text, 700);
        builder.Line(bounds.X, bounds.Y + 35, bounds.X + bounds.Width, bounds.Y + 35, theme.Border);
        var columns = Math.Max(1, (int)(content.Width / builder.MeasureText("M", size).Width));
        var rows = new Queue<TerminalViewportLine>(); var removed = discarded;
        foreach (var line in lines) {
            var wrapped = options.Wrap ? TerminalTextWidth.Wrap(line.Text, columns) : new[] { line.Text };
            foreach (var row in wrapped) {
                if (rows.Count == capacity) { rows.Dequeue(); removed++; }
                rows.Enqueue(new TerminalViewportLine(row, line.Tone, line.Opacity));
            }
        }
        var y = content.Y + builder.TextAscent(size);
        using (builder.PushClip(content)) foreach (var row in rows) {
            builder.Text(TerminalTextWidth.FitContent(row.Text, content.Width, text => builder.MeasureText(text, size).Width), content.X, y,
                size, TerminalLinePlayback.ToneColor(theme, row.Tone).WithOpacity(row.Opacity), role: "terminal-viewport-text");
            y += lineHeight;
        }
        var footer = marker.Length > 0 ? marker : active.WorkingDirectory;
        if (removed > 0) footer = "↑ " + removed.ToString(CultureInfo.InvariantCulture) + " earlier lines · " + footer;
        FitText(builder, footer, bounds.X + 12, bounds.Y + bounds.Height - 10, bounds.Width - 24, 11, theme.Muted);
        parent.Append(builder.Build());
    }
}
