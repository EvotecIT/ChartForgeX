using System;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Terminal;

/// <summary>
/// Renders the completed state of a terminal story as a dependency-free PNG image.
/// </summary>
public sealed class PngTerminalStoryRenderer {
    /// <summary>Renders a terminal story to PNG bytes.</summary>
    public byte[] Render(TerminalStory story) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        var layout = TerminalStoryLayout.Build(story);
        var theme = story.Theme;
        var canvas = new RgbaCanvas(layout.Width, layout.Height, 2, TrueTypeFont.TryLoadForFamily(theme.FontFamily, out _), story.PngOutputScale);
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
        var titleWidth = canvas.MeasureTextWidth(story.Title, 12);
        canvas.DrawText((layout.Width - titleWidth) / 2, 19, story.Title, theme.Muted, 12);

        for (var index = 0; index < layout.Lines.Count; index++) {
            var line = layout.Lines[index];
            var y = layout.ContentTop + index * story.LineHeight;
            if (line.IsCommand) {
                var prompt = line.Text.Substring(0, line.PromptLength);
                var command = line.Text.Substring(line.PromptLength);
                canvas.DrawText(layout.ContentX, y, prompt, theme.Accent, story.FontSize);
                canvas.DrawText(layout.ContentX + canvas.MeasureTextWidth(prompt, story.FontSize), y, command, theme.Text, story.FontSize);
            } else {
                canvas.DrawText(layout.ContentX, y, line.Text, ToneColor(theme, line.Tone), story.FontSize);
            }

            if (story.ShowFinalPrompt && index == layout.Lines.Count - 1) {
                var cursorX = layout.ContentX + canvas.MeasureTextWidth(line.Text, story.FontSize) + 2;
                canvas.FillRoundedRect(cursorX, y + 2, Math.Max(7, story.FontSize * 0.55), story.FontSize + 2, 1, theme.Cursor);
            }
        }

        return PngWriter.WriteRgba(canvas.OutputWidth, canvas.OutputHeight, canvas.ToOutputPixels());
    }

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
}
