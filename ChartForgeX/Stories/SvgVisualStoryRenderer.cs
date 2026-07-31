using System;
using System.Globalization;
using System.Text;
using ChartForgeX.Raster;
using ChartForgeX.Svg;

namespace ChartForgeX.Stories;

/// <summary>Renders a resolved visual story as a self-contained, script-free animated SVG.</summary>
public sealed class SvgVisualStoryRenderer {
    private const long MaximumEmbeddedMediaCharacters = 64L * 1024 * 1024;

    /// <summary>Renders a visual story to SVG markup.</summary>
    public string Render(VisualStory story) => Render(story, string.Empty);

    /// <summary>Renders a visual story with a caller-provided deterministic ID scope.</summary>
    public string Render(VisualStory story, string idScope) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        if (idScope == null) throw new ArgumentNullException(nameof(idScope));
        story.Validate();
        var provisionalId = SvgRenderedIdentity.CreateProvisionalId(
            "cfx-story",
            idScope,
            story.Title,
            story.Width.ToString(CultureInfo.InvariantCulture),
            story.Height.ToString(CultureInfo.InvariantCulture),
            story.Scenes.Count.ToString(CultureInfo.InvariantCulture));
        var svg = RenderCore(story, provisionalId);
        return SvgRenderedIdentity.Bind(svg, provisionalId, "cfx-story", idScope);
    }

    private static string RenderCore(VisualStory story, string id) {
        var transcript = new VisualStoryTranscriptRenderer().Render(story);
        var writer = new SvgMarkupWriter(16384);
        writer.StartElement("svg")
            .Attribute("xmlns", "http://www.w3.org/2000/svg")
            .Attribute("id", id)
            .Attribute("width", story.Width)
            .Attribute("height", story.Height)
            .Attribute("viewBox", "0 0 " + story.Width.ToString(CultureInfo.InvariantCulture) + " " + story.Height.ToString(CultureInfo.InvariantCulture))
            .Attribute("role", "img")
            .Attribute("aria-labelledby", id + "-title " + id + "-desc")
            .Attribute("preserveAspectRatio", "xMidYMid meet")
            .Attribute("style", "max-width:100%;height:auto;display:block;isolation:isolate")
            .Attribute("data-cfx-story", "visual")
            .Attribute("data-cfx-motion", "scene-story")
            .Attribute(
                "data-cfx-motion-duration",
                (story.DurationSeconds + VisualStoryAnimationOptions.DefaultEndHoldSeconds)
                    .ToString("0.###", CultureInfo.InvariantCulture))
            .EndStartElement().Line()
            .StartElement("title").Attribute("id", id + "-title").Text(story.Title).EndElement().Line()
            .StartElement("desc").Attribute("id", id + "-desc").Text(transcript).EndElement().Line()
            .StartElement("defs").EndStartElement()
            .StartElement("style").EndStartElement().Raw(BuildCss(story, id)).EndElement()
            .EndElement().Line();

        var embeddedMediaCharacters = 0L;
        for (var index = 0; index < story.Scenes.Count; index++) {
            var png = PngWriter.WriteRgba(PngVisualStoryRenderer.RenderScene(
                story,
                index,
                omitVectorMedia: true));
            embeddedMediaCharacters = ReserveEmbeddedMedia(
                embeddedMediaCharacters,
                png.LongLength,
                story.Scenes[index].Id);
            writer.StartElement("g")
                .Attribute("data-cfx-role", "story-scene")
                .Attribute("data-cfx-scene", story.Scenes[index].Id)
                .Attribute("class", "cfx-story-scene cfx-story-scene-" + index.ToString(CultureInfo.InvariantCulture) + (index == story.Scenes.Count - 1 ? " cfx-story-scene-last" : string.Empty))
                .EndStartElement()
                .StartElement("image")
                .Attribute("width", story.Width)
                .Attribute("height", story.Height)
                .Attribute("preserveAspectRatio", "xMidYMid meet")
                .Attribute("href", "data:image/png;base64," + Convert.ToBase64String(png))
                .EndEmptyElement();
            WriteVectorMediaOverlays(writer, story, story.Scenes[index], ref embeddedMediaCharacters);
            writer.EndElement().Line();
        }
        writer.EndElement().Line();
        return writer.Build().Replace("\r\n", "\n");
    }

    private static void WriteVectorMediaOverlays(
        SvgMarkupWriter writer,
        VisualStory story,
        VisualStoryScene scene,
        ref long embeddedMediaCharacters) {
        var bounds = VisualStoryLayout.Panels(story, scene);
        for (var index = 0; index < scene.Panels.Count; index++) {
            var panel = scene.Panels[index];
            if (!(panel.Surface is VisualStoryMediaSurface media) || media.Svg.Length == 0) continue;
            var content = VisualStoryLayout.PanelContent(panel, bounds[index]);
            var vectorByteCount = Encoding.UTF8.GetByteCount(media.Svg);
            embeddedMediaCharacters = ReserveEmbeddedMedia(
                embeddedMediaCharacters,
                vectorByteCount,
                scene.Id);
            var vectorBytes = Encoding.UTF8.GetBytes(media.Svg);
            writer.StartElement("image")
                .Attribute("data-cfx-role", "story-vector-media")
                .Attribute("data-cfx-panel", panel.Id)
                .Attribute("x", content.X.ToString("0.###", CultureInfo.InvariantCulture))
                .Attribute("y", content.Y.ToString("0.###", CultureInfo.InvariantCulture))
                .Attribute("width", content.Width.ToString("0.###", CultureInfo.InvariantCulture))
                .Attribute("height", content.Height.ToString("0.###", CultureInfo.InvariantCulture))
                .Attribute("preserveAspectRatio", "xMidYMid meet")
                .Attribute(
                    "href",
                    "data:image/svg+xml;base64," +
                    Convert.ToBase64String(vectorBytes))
                .EndEmptyElement();
        }
    }

    internal static long ReserveEmbeddedMedia(long currentCharacters, long byteCount, string sceneId) {
        if (currentCharacters < 0) throw new ArgumentOutOfRangeException(nameof(currentCharacters));
        if (byteCount < 0) throw new ArgumentOutOfRangeException(nameof(byteCount));
        var encodedCharacters = checked(((byteCount + 2) / 3) * 4);
        var total = checked(currentCharacters + encodedCharacters);
        if (total > MaximumEmbeddedMediaCharacters) {
            throw new InvalidOperationException(
                "Visual-story SVG embedded media exceeds the " + MaximumEmbeddedMediaCharacters +
                "-character safety limit while rendering scene '" + sceneId +
                "'. Lower the size, scene count, or embedded media complexity.");
        }
        return total;
    }

    private static string BuildCss(VisualStory story, string id) {
        var css = new StringBuilder();
        css.Append('#').Append(id).Append(" .cfx-story-scene{mix-blend-mode:plus-lighter}");
        if (story.Scenes.Count == 1) {
            css.Append('#').Append(id).Append(" .cfx-story-scene{opacity:1}");
        } else {
            var total = story.DurationSeconds + VisualStoryAnimationOptions.DefaultEndHoldSeconds;
            for (var index = 0; index < story.Scenes.Count; index++) {
                var timing = VisualStoryTimeline.Timing(
                    story,
                    index,
                    VisualStoryAnimationOptions.DefaultTransitionSeconds);
                var incomingTransitionStart = index == 0
                    ? 0
                    : VisualStoryTimeline.Timing(
                        story,
                        index - 1,
                        VisualStoryAnimationOptions.DefaultTransitionSeconds).TransitionStart;
                var name = id + "-motion-scene-" + index.ToString(CultureInfo.InvariantCulture);
                css.Append("@keyframes ").Append(name).Append('{');
                if (index == 0) {
                    css.Append("0%{opacity:1}");
                } else {
                    css.Append("0%{opacity:0}");
                    AppendOpacity(css, incomingTransitionStart / total * 100, 0);
                    AppendOpacity(css, timing.Start / total * 100, 1);
                }
                if (index != story.Scenes.Count - 1) {
                    AppendOpacity(css, timing.TransitionStart / total * 100, 1);
                    AppendOpacity(css, timing.End / total * 100, 0);
                }
                AppendOpacity(css, 100, index == story.Scenes.Count - 1 ? 1 : 0);
                css.Append('}');
                css.Append('#').Append(id).Append(" .cfx-story-scene-").Append(index)
                    .Append("{opacity:").Append(index == story.Scenes.Count - 1 ? '1' : '0').Append(";animation:").Append(name).Append(' ')
                    .Append(total.ToString("0.###", CultureInfo.InvariantCulture)).Append("s linear infinite both}");
            }
        }
        css.Append("@media (prefers-reduced-motion:reduce){#").Append(id).Append(" .cfx-story-scene{display:none;opacity:1;animation:none}#")
            .Append(id).Append(" .cfx-story-scene-last{display:inline}}");
        css.Append("@media print{#").Append(id).Append(" .cfx-story-scene{display:none;opacity:1;animation:none}#")
            .Append(id).Append(" .cfx-story-scene-last{display:inline}}");
        return css.ToString();
    }

    private static void AppendOpacity(StringBuilder css, double percent, int opacity) =>
        css.Append(Percent(Math.Max(0, Math.Min(100, percent))))
            .Append("{opacity:")
            .Append(opacity)
            .Append('}');

    private static string Percent(double value) => value.ToString("0.#########", CultureInfo.InvariantCulture) + "%";
}
