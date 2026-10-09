using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Accessibility;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Terminal;
using ChartForgeX.Typography;

namespace ChartForgeX.Stories;

internal static partial class NativeVisualStoryRenderer {
    internal static PreparedVisual Prepare(VisualStory story, int sceneIndex, double? elapsed, int outputScale, string transcript) {
        var scene = story.Scenes[sceneIndex]; var theme = story.Theme;
        var builder = new VisualSceneBuilder(new VisualSize(story.Width, story.Height), FontSpec.FromFamily(theme.FontFamily));
        builder.Rect(new ChartRect(0, 0, story.Width, story.Height), theme.Background);
        builder.Ellipse(story.Width * 0.15, 0, story.Width * 0.38, story.Height * 0.28, theme.Accent.WithOpacity(0.04));
        var padding = VisualStoryLayout.OuterPadding;
        var titleWidth = story.Width - padding * 2;
        FitText(builder, story.Title, padding, 45, titleWidth, 25, theme.Text, 700);
        FitText(builder, scene.Title, padding, 74, titleWidth, 15, theme.Muted);
        var bounds = VisualStoryLayout.Panels(story, scene);
        for (var i = 0; i < scene.Panels.Count; i++) {
            var panel = scene.Panels[i]; var box = Rect(bounds[i]);
            using (builder.PushGroup(panel.Id, "story-panel", new Dictionary<string, string> { ["data-cfx-panel"] = panel.Id })) {
                builder.Rect(new ChartRect(box.X + 3, box.Y + 7, box.Width, box.Height), ChartColor.Black.WithOpacity(0.16), radius: 18);
                builder.Rect(box, theme.Panel, theme.Border, radius: 18);
                if (panel.Title.Length > 0) FitText(builder, panel.Title, box.X + 20, box.Y + 34, box.Width - 40, 13, theme.Muted, 700);
                var content = Rect(VisualStoryLayout.PanelContent(panel, bounds[i]));
                builder.AddRegion(new VisualSemanticRegion(panel.Id, panel.Surface.Kind.ToString().ToLowerInvariant(), content, panel.Surface.AccessibleText));
                using (builder.PushClip(content)) {
                    switch (panel.Surface) {
                        case VisualStorySourceSurface source: DrawSource(builder, story, source, content, elapsed); break;
                        case VisualStoryTerminalSurface terminal:
                            builder.Image(new PngTerminalStoryRenderer().RenderFitted(terminal.Terminal, content.Width, content.Height, outputScale, elapsed), content,
                                role: "story-terminal", preserveAspectRatio: "xMidYMid meet");
                            break;
                        case VisualStoryMediaSurface media:
                            if (media.Svg.Length > 0) {
                                using (builder.PushEmbeddedSvg(media.Svg,
                                    content, "xMidYMid meet", role: "story-vector-media")) {
                                    builder.Image(media.Raster, content, preserveAspectRatio: "xMidYMid meet");
                                }
                            } else builder.Image(media.Raster, content, role: "story-media", preserveAspectRatio: "xMidYMid meet");
                            break;
                        case VisualStoryTextSurface text: DrawProse(builder, story, text, content); break;
                    }
                }
            }
        }
        FitText(builder, (sceneIndex + 1).ToString(CultureInfo.InvariantCulture) + " / " + story.Scenes.Count.ToString(CultureInfo.InvariantCulture),
            padding, story.Height - 13, story.Width * 0.25, 11, theme.Muted);
        if (sceneIndex == story.Scenes.Count - 1 && (!elapsed.HasValue || elapsed.Value >= scene.DurationSeconds)) {
            var label = "✓ " + string.Join(" · ", story.Outcomes.Select(outcome => outcome.Label));
            var labelWidth = Math.Min(story.Width * 0.7, builder.MeasureText(label, 11, 700).Width);
            FitText(builder, label, story.Width - padding - labelWidth, story.Height - 13, labelWidth, 11, theme.Success, 700);
        }
        return new PreparedVisual(builder.Build(), new VisualAccessibility {
            Name = story.Title + " — " + scene.Title, Description = transcript, Language = "en"
        });
    }

    private static ChartRect Rect(VisualStoryBounds bounds) => new(bounds.X, bounds.Y, bounds.Width, bounds.Height);
    private static void FitText(VisualSceneBuilder builder, string text, double x, double baseline, double width,
        double size, ChartColor color, int weight = 400) {
        builder.Text(TerminalTextWidth.Fit(text, width, value => builder.MeasureText(value, size, weight).Width), x, baseline, size, color, weight);
    }
    private static void DrawProse(VisualSceneBuilder builder, VisualStory story, VisualStoryTextSurface surface, ChartRect bounds) {
        var style = TextStyle.Create(surface.Emphasized ? 28 : 21, surface.Emphasized ? story.Theme.Text : story.Theme.Muted);
        style.Font = FontSpec.FromFamily(story.Theme.FontFamily); style.Font.Weight = surface.Emphasized ? 700 : 400;
        style.LineHeight = 1.35;
        var lineHeight = TextLayoutEngine.Measure("Ag", style).LineHeight;
        var maxLines = Math.Max(1, (int)(bounds.Height / lineHeight));
        var layout = TextLayoutEngine.Layout(surface.Text, bounds.Width, style, TextWrapMode.Word, maxLines);
        var y = bounds.Y + builder.TextAscent(style) + Math.Max(0, (bounds.Height - layout.Metrics.Height) * 0.35);
        foreach (var line in layout.Lines) { builder.Text(line.Text, bounds.X, y, style); y += lineHeight; }
    }
}
