using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Composition;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Terminal;
using ChartForgeX.Typography;

namespace ChartForgeX.Stories;

/// <summary>Renders the completed visual-story scene as a deterministic PNG.</summary>
public sealed class PngVisualStoryRenderer {
    private const int MaximumMaterializedSourceElementLength = 1024;

    /// <summary>Renders the completed story state to PNG bytes.</summary>
    public byte[] Render(VisualStory story) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        story.Validate();
        return PngWriter.WriteRgba(RenderScene(story, story.Scenes.Count - 1));
    }

    internal static RgbaImage RenderScene(
        VisualStory story,
        int sceneIndex,
        int outputScale = 1,
        bool omitVectorMedia = false) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        if (sceneIndex < 0 || sceneIndex >= story.Scenes.Count) throw new ArgumentOutOfRangeException(nameof(sceneIndex));
        var scene = story.Scenes[sceneIndex];
        var theme = story.Theme;
        var canvas = ImageComposition.CreateScaled(story.Width, story.Height, theme.Background, outputScale);
        DrawBackdrop(canvas, story);
        var outcomeOrigin = story.Width * 0.58;
        var headerTextWidth = Math.Max(1, outcomeOrigin - VisualStoryLayout.OuterPadding - 12);
        DrawHeaderText(canvas, story, story.Title, VisualStoryLayout.OuterPadding, 22, headerTextWidth, 24, theme.Text, emphasized: true);
        DrawHeaderText(canvas, story, scene.Title, VisualStoryLayout.OuterPadding, 56, headerTextWidth, 15, theme.Muted, emphasized: false);
        DrawOutcomeBadges(canvas, story, scene, sceneIndex == story.Scenes.Count - 1);

        var bounds = VisualStoryLayout.Panels(story, scene);
        for (var index = 0; index < scene.Panels.Count; index++) {
            DrawPanel(canvas, story, scene.Panels[index], bounds[index], outputScale, omitVectorMedia);
        }
        return canvas.ToImage();
    }

    internal static long MaximumFittedTerminalWorkingBytes(VisualStory story, int outputScale) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        var maximum = 0L;
        foreach (var scene in story.Scenes) {
            var bounds = VisualStoryLayout.Panels(story, scene);
            for (var index = 0; index < scene.Panels.Count; index++) {
                if (!(scene.Panels[index].Surface is VisualStoryTerminalSurface terminal)) continue;
                var content = VisualStoryLayout.PanelContent(scene.Panels[index], bounds[index]);
                maximum = Math.Max(
                    maximum,
                    Terminal.PngTerminalStoryRenderer.EstimateFittedWorkingBytes(
                        terminal.Terminal,
                        content.Width,
                        content.Height,
                        outputScale));
            }
        }
        return maximum;
    }

    private static void DrawHeaderText(
        ImageComposition canvas,
        VisualStory story,
        string text,
        double x,
        double y,
        double width,
        double fontSize,
        ChartColor color,
        bool emphasized) {
        var style = TextStyle.Create(fontSize, color);
        style.Font = FontSpec.FromFamily(story.Theme.FontFamily);
        style.Font.Weight = emphasized ? 700 : 400;
        var fitted = TerminalTextWidth.Fit(
            text,
            width,
            value => TextLayoutEngine.Measure(value, style).Width);
        canvas.DrawText(x, y, width, fitted, style, TextWrapMode.NoWrap, 1, TextTrimming.None);
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

    private static void DrawPanel(
        ImageComposition canvas,
        VisualStory story,
        VisualStoryPanel panel,
        VisualStoryBounds bounds,
        int outputScale,
        bool omitVectorMedia) {
        var theme = story.Theme;
        canvas.FillRoundedRectangle(bounds.X + 4, bounds.Y + 8, bounds.Width, bounds.Height, 18, ChartColor.Black.WithOpacity(0.18));
        canvas.FillRoundedRectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height, 18, theme.Panel);
        canvas.StrokeRoundedRectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height, 18, theme.Border, 1);
        var contentY = bounds.Y + VisualStoryLayout.PanelPadding;
        if (panel.Title.Length > 0) {
            DrawHeaderText(
                canvas,
                story,
                panel.Title,
                bounds.X + VisualStoryLayout.PanelPadding,
                contentY,
                bounds.Width - VisualStoryLayout.PanelPadding * 2,
                13,
                theme.Muted,
                emphasized: true);
        }
        var content = VisualStoryLayout.PanelContent(panel, bounds);
        if (omitVectorMedia &&
            panel.Surface is VisualStoryMediaSurface vectorMedia &&
            vectorMedia.Svg.Length > 0) {
            return;
        }
        DrawSurface(canvas, story, panel.Surface, content, outputScale);
    }

    private static void DrawSurface(ImageComposition canvas, VisualStory story, VisualStorySurface surface, VisualStoryBounds bounds, int outputScale) {
        switch (surface.Kind) {
            case VisualStorySurfaceKind.Source:
                DrawSource(canvas, story, (VisualStorySourceSurface)surface, bounds);
                break;
            case VisualStorySurfaceKind.Terminal:
                DrawTerminal(canvas, (VisualStoryTerminalSurface)surface, bounds, outputScale);
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

    private static void DrawTerminal(ImageComposition canvas, VisualStoryTerminalSurface surface, VisualStoryBounds bounds, int outputScale) {
        var terminal = new Terminal.PngTerminalStoryRenderer().RenderFitted(
            surface.Terminal,
            bounds.Width,
            bounds.Height,
            outputScale);
        canvas.DrawImage(terminal, bounds.X, bounds.Y, bounds.Width, bounds.Height, VisualCanvasImageFit.Contain);
    }

    private static void DrawText(ImageComposition canvas, VisualStory story, VisualStoryTextSurface surface, VisualStoryBounds bounds) {
        var textY = bounds.Y + Math.Max(0, bounds.Height * 0.12);
        var availableHeight = Math.Max(1, bounds.Y + bounds.Height - textY);
        var style = TextStyle.Create(surface.Emphasized ? 30 : 22, surface.Emphasized ? story.Theme.Text : story.Theme.Muted);
        style.Font = FontSpec.FromFamily(story.Theme.FontFamily);
        style.Font.Weight = surface.Emphasized ? 700 : 400;
        style.LineHeight = 1.35;
        var lineHeight = TextLayoutEngine.Measure("Ag", style).LineHeight;
        if (lineHeight > availableHeight) {
            style.FontSize = Math.Max(1, style.FontSize * availableHeight / lineHeight);
            lineHeight = TextLayoutEngine.Measure("Ag", style).LineHeight;
        }
        var maximumLines = Math.Min(8, Math.Max(1, (int)Math.Floor(availableHeight / lineHeight)));
        canvas.DrawText(bounds.X, textY, bounds.Width, surface.Text, style, TextWrapMode.Word, maximumLines);
    }

    private static void DrawSource(ImageComposition canvas, VisualStory story, VisualStorySourceSurface surface, VisualStoryBounds bounds) {
        var minimumFontSize = 10d;
        var maximumDensityLines = Math.Max(
            1,
            (int)Math.Ceiling((bounds.Height - 8) / (minimumFontSize * 1.45)));
        var lines = new List<TextLineSlice>(maximumDensityLines + 1);
        foreach (var line in TextLineScanner.Enumerate(surface.Source.Text)) {
            lines.Add(line);
            if (lines.Count > maximumDensityLines) break;
        }
        var hasAdditionalLines = lines.Count > maximumDensityLines;
        if (hasAdditionalLines) lines.RemoveAt(lines.Count - 1);
        var spans = surface.Source.Spans;
        var spanIndex = 0;
        var lineCount = Math.Max(1, lines.Count);
        var fontSize = Math.Max(minimumFontSize, Math.Min(18, (bounds.Height - 8) / (lineCount * 1.45)));
        var lineHeight = fontSize * 1.45;
        var y = bounds.Y + 4;
        var maximumLines = Math.Max(1, (int)Math.Floor((bounds.Height - 4 + 0.1) / lineHeight));
        var visibleLines = Math.Min(lines.Count, maximumLines);
        for (var lineIndex = 0; lineIndex < visibleLines; lineIndex++) {
            var line = lines[lineIndex];
            var verticallyTruncated =
                lineIndex == visibleLines - 1 &&
                (hasAdditionalLines || visibleLines < lines.Count);
            var measuredColumn = 0;
            var measurementStyle = SourceStyle(story, StorySyntaxKind.Plain, fontSize);
            FitExpandedSourceContent(
                surface.Source.Text,
                line.Start,
                line.Length,
                ref measuredColumn,
                measurementStyle,
                bounds.Width,
                out var measuredLength);
            var horizontallyTruncated = measuredLength < line.Length;
            var truncated = verticallyTruncated || horizontallyTruncated;
            var truncationReserve = truncated
                ? Math.Min(bounds.Width, SourceTruncationMarkerWidth(fontSize))
                : 0;
            var lineBounds = new VisualStoryBounds(
                bounds.X,
                bounds.Y,
                Math.Max(1, bounds.Width - truncationReserve),
                bounds.Height);
            var x = bounds.X;
            var cursor = line.Start;
            var lineEnd = line.Start + line.Length;
            var visualColumn = 0;
            while (spanIndex < spans.Count && spans[spanIndex].End <= cursor) spanIndex++;
            while (cursor < lineEnd && x < lineBounds.X + lineBounds.Width) {
                while (spanIndex < spans.Count && spans[spanIndex].End <= cursor) spanIndex++;
                var segmentEnd = lineEnd;
                var segmentKind = StorySyntaxKind.Plain;
                if (spanIndex < spans.Count) {
                    var span = spans[spanIndex];
                    if (span.Start > cursor) {
                        segmentEnd = Math.Min(lineEnd, span.Start);
                    } else if (span.End > cursor) {
                        segmentEnd = Math.Min(lineEnd, span.End);
                        segmentKind = span.Kind;
                    }
                }
                if (segmentEnd <= cursor) break;
                var style = SourceStyle(story, segmentKind, fontSize);
                var segmentStart = cursor;
                var value = FitExpandedSourceContent(
                    surface.Source.Text,
                    segmentStart,
                    segmentEnd - segmentStart,
                    ref visualColumn,
                    style,
                    lineBounds.X + lineBounds.Width - x,
                    out var consumedLength);
                if (consumedLength == 0) break;
                x += DrawSourceSegment(
                    canvas,
                    value,
                    style,
                    x,
                    y,
                    lineBounds);
                cursor = segmentStart + consumedLength;
                if (cursor < segmentEnd) break;
            }
            if (truncated) {
                DrawSourceTruncationMarker(
                    canvas,
                    story,
                    bounds.X + bounds.Width - truncationReserve,
                    y,
                    fontSize);
            }
            y += lineHeight;
        }
    }

    private static double SourceTruncationMarkerWidth(double fontSize) {
        var dotSize = Math.Max(2, fontSize * 0.18);
        var gap = Math.Max(1, dotSize * 0.6);
        return dotSize * 3 + gap * 2;
    }

    private static void DrawSourceTruncationMarker(
        ImageComposition canvas,
        VisualStory story,
        double x,
        double y,
        double fontSize) {
        var dotSize = Math.Max(2, fontSize * 0.18);
        var gap = Math.Max(1, dotSize * 0.6);
        var dotY = y + Math.Max(0, fontSize * 0.76);
        for (var index = 0; index < 3; index++) {
            canvas.FillRoundedRectangle(
                x + index * (dotSize + gap),
                dotY,
                dotSize,
                dotSize,
                dotSize / 2,
                story.Theme.Syntax.Plain);
        }
    }

    internal static string ExpandSourceTabs(string value, ref int visualColumn) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (visualColumn < 0) throw new ArgumentOutOfRangeException(nameof(visualColumn));
        if (value.IndexOf('\t') < 0) {
            visualColumn = checked(visualColumn + TerminalTextWidth.Measure(value));
            return value;
        }

        var expanded = new StringBuilder(value.Length + 8);
        var start = 0;
        for (var index = 0; index < value.Length; index++) {
            if (value[index] != '\t') continue;
            if (index > start) {
                var preceding = value.Substring(start, index - start);
                expanded.Append(preceding);
                visualColumn = checked(visualColumn + TerminalTextWidth.Measure(preceding));
            }
            var spaces = 4 - visualColumn % 4;
            expanded.Append(' ', spaces);
            visualColumn = checked(visualColumn + spaces);
            start = index + 1;
        }
        if (start < value.Length) {
            var trailing = value.Substring(start);
            expanded.Append(trailing);
            visualColumn = checked(visualColumn + TerminalTextWidth.Measure(trailing));
        }
        return expanded.ToString();
    }

    private static string FitExpandedSourceContent(
        string source,
        int start,
        int length,
        ref int visualColumn,
        TextStyle style,
        double width,
        out int consumedLength) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (start < 0 || start > source.Length) throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0 || length > source.Length - start) throw new ArgumentOutOfRangeException(nameof(length));
        if (visualColumn < 0) throw new ArgumentOutOfRangeException(nameof(visualColumn));
        consumedLength = 0;
        if (length == 0 || width <= 0) return string.Empty;

        const int maximumInitialCapacity = 256;
        var output = new StringBuilder(Math.Min(length, maximumInitialCapacity));
        var usedWidth = 0d;
        var index = start;
        var end = checked(start + length);
        while (index < end) {
            var elementStart = index;
            var elementEnd = TerminalTextWidth.NextElementBoundary(source, index);
            if (elementEnd > end) {
                throw new ArgumentException("The source range must end at a complete text element.", nameof(length));
            }
            if (elementEnd - elementStart > MaximumMaterializedSourceElementLength) {
                break;
            }
            index = elementEnd;
            var element = source.Substring(elementStart, elementEnd - elementStart);

            var rendered = element;
            var nextColumn = visualColumn;
            if (element.Length == 1 && element[0] == '\t') {
                var spaces = 4 - visualColumn % 4;
                rendered = new string(' ', spaces);
                nextColumn = checked(visualColumn + spaces);
            } else {
                nextColumn = checked(visualColumn + TerminalTextWidth.Measure(element));
            }

            var elementWidth = TextLayoutEngine.Measure(rendered, style).Width;
            if (usedWidth + elementWidth > width) {
                index = elementStart;
                break;
            }

            output.Append(rendered);
            usedWidth += elementWidth;
            visualColumn = nextColumn;
            consumedLength = index - start;
        }

        return output.ToString();
    }

    private static double DrawSourceSegment(
        ImageComposition canvas,
        string value,
        TextStyle style,
        double x,
        double y,
        VisualStoryBounds bounds) {
        if (value.Length == 0 || x >= bounds.X + bounds.Width) return 0;
        var measured = TextLayoutEngine.Measure(value, style).Width;
        canvas.DrawText(x, y, Math.Max(1, bounds.X + bounds.Width - x), value, style, TextWrapMode.NoWrap, 1, TextTrimming.None);
        return measured;
    }

    private static TextStyle SourceStyle(VisualStory story, StorySyntaxKind kind, double fontSize) {
        var style = TextStyle.Create(fontSize, story.Theme.Syntax.Resolve(kind));
        style.Font = FontSpec.FromFamily(story.Theme.MonospaceFontFamily);
        return style;
    }

}
