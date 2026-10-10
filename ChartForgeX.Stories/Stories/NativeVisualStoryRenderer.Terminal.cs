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
        var theme = surface.UsesStoryTheme && story.Theme.WindowStyle != TerminalWindowStyle.Minimal ? story.Theme.TerminalPalette() : surface.Theme;
        DrawTerminalBackground(parent, story, theme, bounds);
        DrawTerminalContents(parent, story, state.Active.Tab, state.Tabs.Select(tab => tab.Tab).ToArray(), state.Active.Lines,
            theme, surface.Options, bounds, state.Marker, state.Active.Discarded);
    }
    private static void DrawTerminalViewport(VisualSceneBuilder parent, VisualStory story, VisualStoryTerminalSurface surface, ChartRect bounds, double? elapsed) {
        var layout = TerminalStoryLayout.BuildLogical(surface.Terminal);
        var tabs = layout.Tabs.Where(item => layout.TabVisible(item.Tab.Id, elapsed)).Select(item => item.Tab).ToArray();
        TerminalTheme? background = null;
        var weight = 0.0;
        foreach (var tab in layout.Tabs) {
            var opacity = layout.TabOpacity(tab.Tab.Id, elapsed);
            if (opacity <= 0) continue;
            if (background == null) background = tab.Tab.Theme.Copy();
            else {
                var amount = opacity / (weight + opacity);
                background.Background = ChartColorMath.BlendPremultiplied(background.Background, tab.Tab.Theme.Background, amount);
                background.HeaderBackground = ChartColorMath.BlendPremultiplied(background.HeaderBackground, tab.Tab.Theme.HeaderBackground, amount);
                background.Border = ChartColorMath.BlendPremultiplied(background.Border, tab.Tab.Theme.Border, amount);
            }
            weight += opacity;
        }
        if (background != null) DrawTerminalBackground(parent, story, background, bounds);
        foreach (var tab in layout.Tabs) {
            var opacity = layout.TabOpacity(tab.Tab.Id, elapsed);
            if (opacity <= 0) continue;
            var lines = new List<TerminalViewportLine>();
            foreach (var line in tab.Lines) {
                var state = TerminalLinePlayback.At(line, elapsed);
                if (!state.Visible) continue;
                var text = line.IsCommand && elapsed.HasValue ? TerminalLinePlayback.CommandText(line, elapsed.Value) : line.Text;
                lines.Add(new TerminalViewportLine(text, line.IsCommand ? TerminalTextTone.Accent : line.Tone, state.Opacity));
            }
            var discarded = Math.Max(0, lines.Count - surface.Options!.HistoryLines);
            if (discarded > 0) lines.RemoveRange(0, discarded);
            DrawTerminalContents(parent, story, tab.Tab, tabs, lines, tab.Tab.Theme, surface.Options!, bounds, string.Empty, discarded, opacity);
        }
    }

    private static void DrawTerminalBackground(VisualSceneBuilder parent, VisualStory story, TerminalTheme theme, ChartRect bounds) {
        var builder = new VisualSceneBuilder(new VisualSize(story.Width, story.Height), FontSpec.FromFamily(theme.FontFamily));
        builder.Rect(bounds, theme.Background, theme.Border, radius: 10, role: "terminal-viewport");
        if (story.Theme.WindowStyle == TerminalWindowStyle.Minimal) {
            builder.Rect(new ChartRect(bounds.X, bounds.Y, bounds.Width, 35), theme.HeaderBackground, radius: 10);
            builder.Line(bounds.X, bounds.Y + 35, bounds.X + bounds.Width, bounds.Y + 35, theme.Border);
        } else if (story.Theme.WindowStyle != TerminalWindowStyle.None)
            DrawStoryWindowHeaderBackground(builder, bounds, TerminalWindowChrome.HeaderHeight(story.Theme.WindowStyle), theme);
        parent.Append(builder.Build());
    }

    private static void DrawTerminalContents(VisualSceneBuilder parent, VisualStory story, TerminalTab active, IReadOnlyList<TerminalTab> tabs,
        IReadOnlyList<TerminalViewportLine> lines, TerminalTheme theme, VisualStoryTerminalOptions options, ChartRect bounds, string marker, int discarded, double opacity = 1) {
        var builder = new VisualSceneBuilder(new VisualSize(story.Width, story.Height), FontSpec.FromFamily(theme.FontFamily));
        var size = options.FontSize; var lineHeight = size * 1.5;
        var header = story.Theme.WindowStyle == TerminalWindowStyle.Minimal ? 35 : TerminalWindowChrome.HeaderHeight(story.Theme.WindowStyle);
        var content = new ChartRect(bounds.X + 12, bounds.Y + header + 13, bounds.Width - 24, bounds.Height - header - 43);
        if (content.Width < builder.MeasureText("M", size).Width || content.Height < lineHeight)
            throw new InvalidOperationException("The terminal viewport cannot fit a readable line. Enlarge the panel or rebalance its weight.");
        var capacity = Math.Max(1, (int)(content.Height / lineHeight));
        var title = active.Title + (tabs.Count > 1 ? " · " + (Array.FindIndex(tabs.ToArray(), tab => tab.Id == active.Id) + 1).ToString(CultureInfo.InvariantCulture) + "/" + tabs.Count.ToString(CultureInfo.InvariantCulture) : "");
        if (story.Theme.WindowStyle == TerminalWindowStyle.Minimal)
            FitText(builder, title, bounds.X + 14, bounds.Y + 23, bounds.Width - 28, 13, ChartColorMath.WithOpacity(theme.Text, opacity), 700);
        else if (header > 0) DrawStoryWindowHeader(builder, bounds, story.Theme.WindowStyle, theme, title, opacity, paintHeader: false);
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
                size, ChartColorMath.WithOpacity(TerminalLinePlayback.ToneColor(theme, row.Tone), row.Opacity * opacity), role: "terminal-viewport-text");
            y += lineHeight;
        }
        var footer = marker.Length > 0 ? marker : active.WorkingDirectory;
        if (removed > 0) footer = "↑ " + removed.ToString(CultureInfo.InvariantCulture) + " earlier lines · " + footer;
        FitText(builder, footer, bounds.X + 12, bounds.Y + bounds.Height - 10, bounds.Width - 24, 11, ChartColorMath.WithOpacity(theme.Muted, opacity));
        parent.Append(builder.Build());
    }
}
