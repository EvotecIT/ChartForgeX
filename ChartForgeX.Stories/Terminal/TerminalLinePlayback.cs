using System;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Terminal;

/// <summary>Shared timestamp observation for whole-window raster and fixed native terminal viewports.</summary>
internal static class TerminalLinePlayback {
    internal static TerminalLineState At(TerminalRenderedLine line, double? elapsedSeconds) {
        if (!elapsedSeconds.HasValue) return new TerminalLineState(true, 1, 1, 0);
        var elapsed = elapsedSeconds.Value;
        if (!StoryPlaybackClock.Started(elapsed, line.StartSeconds)) return new TerminalLineState(false, 0, 0, 0);
        var progress = StoryPlaybackClock.Progress(elapsed, line.StartSeconds, line.DurationSeconds);
        if (line.IsCommand) return new TerminalLineState(true, progress, 1, 0);
        var eased = 1 - Math.Pow(1 - progress, 3);
        return new TerminalLineState(true, progress, eased, (1 - eased) * 3);
    }
    internal static string CommandText(TerminalRenderedLine line, double elapsed) {
        var elements = TerminalTextWidth.VisibleElements(line.Text).ToArray();
        var count = StoryPlaybackClock.Elements(elements.Length, elapsed, line.StartSeconds, line.DurationSeconds);
        return string.Concat(elements.Take(count));
    }
    internal static ChartColor ToneColor(TerminalTheme theme, TerminalTextTone tone) => tone switch {
        TerminalTextTone.Default => theme.Text,
        TerminalTextTone.Muted => theme.Muted,
        TerminalTextTone.Accent => theme.Accent,
        TerminalTextTone.Success => theme.Success,
        TerminalTextTone.Warning => theme.Warning,
        TerminalTextTone.Error => theme.Error,
        _ => throw new ArgumentOutOfRangeException(nameof(tone))
    };
}
internal readonly struct TerminalLineState {
    internal TerminalLineState(bool visible, double progress, double opacity, double translateY) {
        Visible = visible; Progress = progress; Opacity = opacity; TranslateY = translateY;
    }
    internal bool Visible { get; }
    internal double Progress { get; }
    internal double Opacity { get; }
    internal double TranslateY { get; }
}
