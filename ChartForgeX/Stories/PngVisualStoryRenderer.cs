using System;
using System.Collections.Generic;
using ChartForgeX.Composition;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Terminal;
using ChartForgeX.Typography;

namespace ChartForgeX.Stories;

/// <summary>Renders the completed visual-story scene as a deterministic PNG.</summary>
public sealed class PngVisualStoryRenderer {
    /// <summary>Renders the completed story state to PNG bytes.</summary>
    public byte[] Render(VisualStory story) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        story.Validate();
        return PngWriter.WriteRgba(RenderScene(story, story.Scenes.Count - 1));
    }

    internal static RgbaImage RenderScene(VisualStory story, int sceneIndex) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        if (sceneIndex < 0 || sceneIndex >= story.Scenes.Count) throw new ArgumentOutOfRangeException(nameof(sceneIndex));
        var scene = story.Scenes[sceneIndex];
        var theme = story.Theme;
        var canvas = ImageComposition.Create(story.Width, story.Height, theme.Background);
        DrawBackdrop(canvas, story);
        canvas.DrawText(VisualStoryLayout.OuterPadding, 22, story.Width * 0.58, story.Title, 24, theme.Text, emphasized: true);
        canvas.DrawText(VisualStoryLayout.OuterPadding, 56, story.Width * 0.58, scene.Title, 15, theme.Muted);
        DrawOutcomeBadges(canvas, story, scene, sceneIndex == story.Scenes.Count - 1);

        var bounds = VisualStoryLayout.Panels(story, scene);
        for (var index = 0; index < scene.Panels.Count; index++) {
            DrawPanel(canvas, story, scene.Panels[index], bounds[index]);
        }
        return canvas.ToImage();
    }

    private static void DrawBackdrop(ImageComposition canvas, VisualStory story) {
        var accent = story.Theme.Accent.WithOpacity(0.05);
        canvas.FillRoundedRectangle(-story.Width * 0.08, -story.Height * 0.18, story.Width * 0.5, story.Height * 0.7, story.Width * 0.22, accent);
        canvas.FillRoundedRectangle(story.Width * 0.68, story.Height * 0.62, story.Width * 0.42, story.Height * 0.52, story.Width * 0.18, story.Theme.Success.WithOpacity(0.035));
    }

    private static void DrawOutcomeBadges(ImageComposition canvas, VisualStory story, VisualStoryScene scene, bool completed) {
        var revealed = new List<string>();
        foreach (var outcome in story.Outcomes) {
            foreach (var panel in scene.Panels) {
                if (string.Equals(panel.Id, outcome.PanelId, StringComparison.Ordinal)) {
                    revealed.Add((completed ? "✓ " : "→ ") + outcome.Label);
                    break;
                }
            }
        }
        if (revealed.Count == 0) return;
        var x = story.Width * 0.58;
        var width = story.Width * 0.38;
        var style = TextStyle.Create(13, completed ? story.Theme.Success : story.Theme.Accent);
        style.Font = FontSpec.FromFamily(story.Theme.FontFamily);
        style.Font.Weight = 700;
        style.Alignment = TextAlignment.Left;
        var label = TerminalTextWidth.Fit(
            string.Join("   ", revealed),
            width,
            value => TextLayoutEngine.Measure(value, style).Width);
        canvas.DrawText(x, 28, width, label, style, TextWrapMode.NoWrap, 1, TextTrimming.None);
    }

    private static void DrawPanel(ImageComposition canvas, VisualStory story, VisualStoryPanel panel, VisualStoryBounds bounds) {
        var theme = story.Theme;
        canvas.FillRoundedRectangle(bounds.X + 4, bounds.Y + 8, bounds.Width, bounds.Height, 18, ChartColor.Black.WithOpacity(0.18));
        canvas.FillRoundedRectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height, 18, theme.Panel);
        canvas.StrokeRoundedRectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height, 18, theme.Border, 1);
        var contentY = bounds.Y + VisualStoryLayout.PanelPadding;
        if (panel.Title.Length > 0) {
            canvas.DrawText(bounds.X + VisualStoryLayout.PanelPadding, contentY, bounds.Width - VisualStoryLayout.PanelPadding * 2, panel.Title, 13, theme.Muted, emphasized: true);
        }
        var content = VisualStoryLayout.PanelContent(panel, bounds);
        DrawSurface(canvas, story, panel.Surface, content);
    }

    private static void DrawSurface(ImageComposition canvas, VisualStory story, VisualStorySurface surface, VisualStoryBounds bounds) {
        switch (surface.Kind) {
            case VisualStorySurfaceKind.Source:
                DrawSource(canvas, story, (VisualStorySourceSurface)surface, bounds);
                break;
            case VisualStorySurfaceKind.Terminal:
                DrawTerminal(canvas, (VisualStoryTerminalSurface)surface, bounds);
                break;
            case VisualStorySurfaceKind.Media:
                canvas.DrawImage(((VisualStoryMediaSurface)surface).Raster, bounds.X, bounds.Y, bounds.Width, bounds.Height, VisualCanvasImageFit.Contain);
                break;
            case VisualStorySurfaceKind.Text:
                DrawText(canvas, story, (VisualStoryTextSurface)surface, bounds);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(surface));
        }
    }

    private static void DrawTerminal(ImageComposition canvas, VisualStoryTerminalSurface surface, VisualStoryBounds bounds) {
        var png = new Terminal.PngTerminalStoryRenderer().Render(surface.Terminal);
        canvas.DrawImageBytes(png, bounds.X, bounds.Y, bounds.Width, bounds.Height, VisualCanvasImageFit.Contain);
    }

    private static void DrawText(ImageComposition canvas, VisualStory story, VisualStoryTextSurface surface, VisualStoryBounds bounds) {
        var style = TextStyle.Create(surface.Emphasized ? 30 : 22, surface.Emphasized ? story.Theme.Text : story.Theme.Muted);
        style.Font = FontSpec.FromFamily(story.Theme.FontFamily);
        style.Font.Weight = surface.Emphasized ? 700 : 400;
        style.LineHeight = 1.35;
        canvas.DrawText(bounds.X, bounds.Y + Math.Max(0, bounds.Height * 0.12), bounds.Width, surface.Text, style, TextWrapMode.Word, maximumLines: 8);
    }

    private static void DrawSource(ImageComposition canvas, VisualStory story, VisualStorySourceSurface surface, VisualStoryBounds bounds) {
        var lines = SourceLines(surface.Source.Text);
        var lineCount = Math.Max(1, lines.Count);
        var fontSize = Math.Max(10, Math.Min(18, (bounds.Height - 8) / (lineCount * 1.45)));
        var lineHeight = fontSize * 1.45;
        var y = bounds.Y + 4;
        foreach (var line in lines) {
            if (y + lineHeight > bounds.Y + bounds.Height + 0.1) break;
            var x = bounds.X;
            var segmentStart = line.Start;
            var segmentKind = line.Length == 0 ? StorySyntaxKind.Plain : surface.Source.KindAt(line.Start);
            for (var offset = 0; offset <= line.Length; offset++) {
                var absolute = line.Start + offset;
                var kind = offset == line.Length ? (StorySyntaxKind)(-1) : surface.Source.KindAt(absolute);
                if (kind == segmentKind) continue;
                var value = surface.Source.Text.Substring(segmentStart, absolute - segmentStart);
                x += DrawSourceSegment(canvas, story, value, segmentKind, x, y, bounds, fontSize);
                segmentStart = absolute;
                segmentKind = kind;
            }
            y += lineHeight;
        }
    }

    private static double DrawSourceSegment(ImageComposition canvas, VisualStory story, string value, StorySyntaxKind kind, double x, double y, VisualStoryBounds bounds, double fontSize) {
        if (value.Length == 0 || x >= bounds.X + bounds.Width) return 0;
        var style = TextStyle.Create(fontSize, story.Theme.Syntax.Resolve(kind));
        style.Font = FontSpec.FromFamily(story.Theme.MonospaceFontFamily);
        var measured = TextLayoutEngine.Measure(value, style).Width;
        if (x + measured > bounds.X + bounds.Width) {
            value = FitSource(value, style, bounds.X + bounds.Width - x);
            measured = TextLayoutEngine.Measure(value, style).Width;
        }
        canvas.DrawText(x, y, Math.Max(1, bounds.X + bounds.Width - x), value, style, TextWrapMode.NoWrap, 1, TextTrimming.None);
        return measured;
    }

    private static string FitSource(string value, TextStyle style, double width) {
        return TerminalTextWidth.Fit(
            value,
            width,
            candidate => TextLayoutEngine.Measure(candidate, style).Width);
    }

    private static List<SourceLine> SourceLines(string text) {
        var lines = new List<SourceLine>();
        var start = 0;
        for (var index = 0; index < text.Length; index++) {
            if (text[index] != '\r' && text[index] != '\n') continue;
            lines.Add(new SourceLine(start, index - start));
            if (text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n') index++;
            start = index + 1;
        }
        lines.Add(new SourceLine(start, text.Length - start));
        return lines;
    }

    private readonly struct SourceLine {
        public SourceLine(int start, int length) {
            Start = start;
            Length = length;
        }
        public int Start { get; }
        public int Length { get; }
    }
}
