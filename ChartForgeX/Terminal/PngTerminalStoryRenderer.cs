using System;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;

namespace ChartForgeX.Terminal;

/// <summary>
/// Renders the completed state of a terminal story as a dependency-free PNG image.
/// </summary>
public sealed class PngTerminalStoryRenderer {
    /// <summary>Renders a terminal story to PNG bytes.</summary>
    public byte[] Render(TerminalStory story) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        return Render(story, story.PngOutputScale);
    }

    internal byte[] Render(TerminalStory story, int outputScale) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        if (outputScale < 1 || outputScale > 4) throw new ArgumentOutOfRangeException(nameof(outputScale));
        var theme = story.Theme;
        var outlineFont = TrueTypeFont.TryLoadForFamily(theme.FontFamily, out _) ?? TrueTypeFont.TryLoadDefault();
        var tableFont = ResolveTableFont(theme, outlineFont);
        return Render(story, outlineFont, tableFont, outputScale);
    }

    internal RgbaImage RenderFitted(
        TerminalStory story,
        double targetWidth,
        double targetHeight,
        int outputScale) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        if (targetWidth <= 0) throw new ArgumentOutOfRangeException(nameof(targetWidth));
        if (targetHeight <= 0) throw new ArgumentOutOfRangeException(nameof(targetHeight));
        if (outputScale < 1 || outputScale > 4) throw new ArgumentOutOfRangeException(nameof(outputScale));
        var theme = story.Theme;
        var outlineFont = TrueTypeFont.TryLoadForFamily(theme.FontFamily, out _) ?? TrueTypeFont.TryLoadDefault();
        var tableFont = ResolveTableFont(theme, outlineFont);
        string PreserveText(string value) => TerminalPngTextPreserver.Preserve(value, outlineFont);
        string PreserveTableText(string value) => TerminalPngTextPreserver.Preserve(value, tableFont);
        var layout = TerminalStoryLayout.Build(story, PreserveText, outlineFont, PreserveTableText);
        var fittedScale = Math.Min(targetWidth / layout.Width, targetHeight / layout.Height);
        var requiredScale = Math.Ceiling(outputScale * fittedScale);
        if (requiredScale > int.MaxValue) {
            throw new InvalidOperationException("The fitted terminal render scale exceeds the supported raster range.");
        }
        var renderScale = Math.Max(1, (int)requiredScale);
        return RenderImage(story, layout, outlineFont, tableFont, renderScale, null);
    }

    internal byte[] Render(TerminalStory story, TrueTypeFont? outlineFont) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        var tableFont = ResolveTableFont(story.Theme, outlineFont);
        return Render(story, outlineFont, tableFont, story.PngOutputScale);
    }

    private static byte[] Render(TerminalStory story, TrueTypeFont? outlineFont, TrueTypeFont? tableFont, int outputScale) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        string PreserveText(string value) => TerminalPngTextPreserver.Preserve(value, outlineFont);
        string PreserveTableText(string value) => TerminalPngTextPreserver.Preserve(value, tableFont);
        var layout = TerminalStoryLayout.Build(story, PreserveText, outlineFont, PreserveTableText);
        var image = RenderImage(story, layout, outlineFont, tableFont, outputScale, null);
        return PngWriter.WriteRgba(image.Width, image.Height, image.Pixels);
    }

    internal RgbaImage RenderImage(TerminalStory story, TerminalStoryLayout layout, TrueTypeFont? outlineFont, int outputScale, double? elapsedSeconds) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        var tableFont = ResolveTableFont(story.Theme, outlineFont);
        return RenderImage(story, layout, outlineFont, tableFont, outputScale, elapsedSeconds);
    }

    internal static RgbaImage RenderImage(TerminalStory story, TerminalStoryLayout layout, TrueTypeFont? outlineFont, TrueTypeFont? tableFont, int outputScale, double? elapsedSeconds) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        if (layout == null) throw new ArgumentNullException(nameof(layout));
        var theme = story.Theme;
        var canvas = new RgbaCanvas(layout.Width, layout.Height, 2, outlineFont, outputScale, useDefaultOutlineFont: false);
        canvas.Clear(theme.PageBackground);
        canvas.FillRoundedRect(12, 18, layout.Width - 24, layout.Height - 24, 16, ChartColor.Black.WithOpacity(0.18));
        canvas.FillRoundedRect(10, 14, layout.Width - 20, layout.Height - 20, 15, ChartColor.Black.WithOpacity(0.12));
        canvas.FillRoundedRect(8, 8, layout.Width - 16, layout.Height - 16, 14, theme.Background);
        canvas.StrokeRoundedRect(8, 8, layout.Width - 16, layout.Height - 16, 14, theme.Border, 1.2);
        canvas.FillRoundedRect(8, 8, layout.Width - 16, layout.HeaderHeightValue, 14, theme.HeaderBackground);
        canvas.FillRect(8, layout.HeaderHeightValue, layout.Width - 16, 8, theme.HeaderBackground);
        canvas.DrawLine(8, layout.HeaderHeightValue + 8, layout.Width - 8, layout.HeaderHeightValue + 8, theme.Border, 1);
        canvas.DrawCircle(29, 29, 5.5, ChartColor.FromHex("#FF5F57"));
        canvas.DrawCircle(49, 29, 5.5, ChartColor.FromHex("#FEBC2E"));
        canvas.DrawCircle(69, 29, 5.5, ChartColor.FromHex("#28C840"));
        var visibleTitle = TerminalStoryLayout.FitTitle(TerminalPngTextPreserver.Preserve(story.Title, outlineFont), layout.Width);
        var titleWidth = TerminalPngTextPreserver.Measure(visibleTitle, canvas, 12);
        TerminalPngTextPreserver.Draw(canvas, (layout.Width - titleWidth) / 2, 19, visibleTitle, theme.Muted, 12);

        for (var index = 0; index < layout.Lines.Count; index++) {
            var line = layout.Lines[index];
            var state = VisibleState(line, elapsedSeconds);
            if (!state.Visible) continue;
            var y = layout.ContentTop + index * story.LineHeight + state.TranslateY;
            var visibleText = line.IsCommand && elapsedSeconds.HasValue ? VisibleCommand(line, state.Progress) : line.Text;
            if (line.IsCommand) {
                var promptLength = Math.Min(line.PromptLength, visibleText.Length);
                var prompt = visibleText.Substring(0, promptLength);
                var command = visibleText.Substring(promptLength);
                var promptWidth = TerminalPngTextPreserver.MeasureEmphasized(prompt, canvas, story.FontSize);
                TerminalPngTextPreserver.DrawEmphasized(canvas, layout.ContentX, y, prompt, theme.Accent, story.FontSize);
                TerminalPngTextPreserver.Draw(canvas, layout.ContentX + promptWidth, y, command, theme.Text, story.FontSize);
            } else {
                TerminalPngTextPreserver.Draw(canvas, layout.ContentX, y, visibleText, WithOpacity(ToneColor(theme, line.Tone), state.Opacity), story.FontSize, line.IsTable ? tableFont : outlineFont);
            }

            if (story.ShowFinalPrompt && index == layout.Lines.Count - 1 && CursorVisible(layout, line, elapsedSeconds)) {
                var visibleWidth = line.IsCommand
                    ? TerminalPngTextPreserver.MeasureEmphasized(visibleText, canvas, story.FontSize)
                    : TerminalPngTextPreserver.Measure(visibleText, canvas, story.FontSize);
                var cursorX = layout.ContentX + visibleWidth + 2;
                canvas.FillRoundedRect(cursorX, y + 2, Math.Max(7, story.FontSize * 0.55), story.FontSize + 2, 1, theme.Cursor);
            }
        }

        return canvas.ToImage();
    }

    internal static TrueTypeFont? ResolveTableFont(TerminalTheme theme, TrueTypeFont? outlineFont) {
        if (theme == null) throw new ArgumentNullException(nameof(theme));
        if (outlineFont == null || TrueTypeFont.IsMonospaceFamily(theme.FontFamily)) return outlineFont;
        return TrueTypeFont.TryLoadForFamily(ChartFontStacks.Mono, out _);
    }

    private static VisibleLineState VisibleState(TerminalRenderedLine line, double? elapsedSeconds) {
        if (!elapsedSeconds.HasValue) return new VisibleLineState(true, 1, 1, 0);
        var elapsed = elapsedSeconds.Value;
        if (elapsed < line.StartSeconds) return new VisibleLineState(false, 0, 0, 0);
        if (line.IsCommand) {
            var progress = line.DurationSeconds <= 0 ? 1 : Unit((elapsed - line.StartSeconds) / line.DurationSeconds);
            return new VisibleLineState(true, progress, 1, 0);
        }

        var reveal = line.DurationSeconds <= 0 ? 1 : Unit((elapsed - line.StartSeconds) / line.DurationSeconds);
        var eased = 1 - Math.Pow(1 - reveal, 3);
        return new VisibleLineState(true, reveal, eased, (1 - eased) * 3);
    }

    private static string VisibleCommand(TerminalRenderedLine line, double progress) {
        if (progress >= 1) return line.Text;
        var elements = TerminalTextWidth.VisibleElements(line.Text).ToArray();
        var count = Math.Max(0, Math.Min(elements.Length, (int)Math.Floor(elements.Length * progress)));
        return string.Concat(elements.Take(count));
    }

    internal static bool CursorVisible(TerminalStoryLayout layout, TerminalRenderedLine line, double? elapsedSeconds) {
        if (!elapsedSeconds.HasValue) return true;
        if (elapsedSeconds.Value < line.StartSeconds) return false;
        return (elapsedSeconds.Value - line.StartSeconds) % 1 < 0.47;
    }

    private static ChartColor WithOpacity(ChartColor color, double opacity) {
        var alpha = (byte)Math.Round(color.A * Unit(opacity));
        return color.WithAlpha(alpha);
    }

    private static double Unit(double value) => Math.Max(0, Math.Min(1, value));

    private static ChartColor ToneColor(TerminalTheme theme, TerminalTextTone tone) {
        switch (tone) {
            case TerminalTextTone.Default: return theme.Text;
            case TerminalTextTone.Muted: return theme.Muted;
            case TerminalTextTone.Accent: return theme.Accent;
            case TerminalTextTone.Success: return theme.Success;
            case TerminalTextTone.Warning: return theme.Warning;
            case TerminalTextTone.Error: return theme.Error;
            default: throw new ArgumentOutOfRangeException(nameof(tone));
        }
    }

    private readonly struct VisibleLineState {
        public readonly bool Visible;
        public readonly double Progress;
        public readonly double Opacity;
        public readonly double TranslateY;

        public VisibleLineState(bool visible, double progress, double opacity, double translateY) {
            Visible = visible;
            Progress = progress;
            Opacity = opacity;
            TranslateY = translateY;
        }
    }
}
