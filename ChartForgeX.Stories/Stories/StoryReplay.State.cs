using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Terminal;

namespace ChartForgeX.Stories;

public sealed partial class StoryReplay {
    internal ReplayState At(double? elapsed, int historyLines) {
        var state = new ReplayState(_initialTab, historyLines);
        foreach (var item in _events) {
            if (elapsed.HasValue && item.Timestamp.TotalSeconds > elapsed.Value) break;
            state.Apply(item);
        }
        return state;
    }
}

internal sealed class ReplayState {
    private readonly int _historyLines;
    internal ReplayState(TerminalTab initial, int historyLines) { _historyLines = historyLines; Tabs.Add(new ReplayTabState(initial)); Active = Tabs[0]; }
    internal List<ReplayTabState> Tabs { get; } = new();
    internal ReplayTabState Active { get; private set; }
    internal string Marker { get; private set; } = string.Empty;
    internal void Apply(StoryReplayEvent item) {
        if (item.Kind == StoryReplayEventKind.Marker) { Marker = item.Text; return; }
        if (item.Kind == StoryReplayEventKind.OpenTab) { Active = new ReplayTabState(item.Tab!); Tabs.Add(Active); return; }
        if (item.Kind == StoryReplayEventKind.SelectTab) { Active = Tabs.Find(tab => tab.Tab.Id == item.TabId)!; return; }
        var target = Tabs.Find(tab => tab.Tab.Id == item.TabId)!;
        switch (item.Kind) {
            case StoryReplayEventKind.Clear: target.Lines.Clear(); target.Discarded = 0; break;
            case StoryReplayEventKind.Directory: target.Tab.WorkingDirectory = item.Text; break;
            case StoryReplayEventKind.Command: Add(target, target.Tab.Prompt() + item.Text, TerminalTextTone.Accent); break;
            case StoryReplayEventKind.ReplaceLine:
                if (target.Lines.Count > 0) target.Lines.RemoveAt(target.Lines.Count - 1);
                Add(target, item.Text, item.Tone); break;
            case StoryReplayEventKind.Output:
                foreach (var line in TextLineScanner.Enumerate(item.Text)) Add(target, item.Text.Substring(line.Start, line.Length), item.Tone);
                break;
        }
    }
    private void Add(ReplayTabState target, string text, TerminalTextTone tone) {
        if (target.Lines.Count == _historyLines) { target.Lines.RemoveAt(0); target.Discarded++; }
        target.Lines.Add(new TerminalViewportLine(text, tone));
    }
}
internal sealed class ReplayTabState {
    internal ReplayTabState(TerminalTab tab) {
        Tab = new TerminalTab(tab.Id, tab.Title, tab.Dialect, tab.WorkingDirectory, tab.CustomPrompt, tab.Theme, tab.Icon);
    }
    internal TerminalTab Tab { get; }
    internal List<TerminalViewportLine> Lines { get; } = new();
    internal int Discarded { get; set; }
}
internal readonly struct TerminalViewportLine {
    internal TerminalViewportLine(string text, TerminalTextTone tone, double opacity = 1) { Text = text; Tone = tone; Opacity = opacity; }
    internal string Text { get; }
    internal TerminalTextTone Tone { get; }
    internal double Opacity { get; }
}
