using System;
using System.Globalization;
using System.Text;
using ChartForgeX.Raster;
using ChartForgeX.Svg;

namespace ChartForgeX.Stories;

/// <summary>Renders a resolved visual story as a self-contained, script-free animated SVG.</summary>
public sealed class SvgVisualStoryRenderer {
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
            .Attribute("style", "max-width:100%;height:auto;display:block")
            .Attribute("data-cfx-story", "visual")
            .Attribute("data-cfx-motion", "scene-story")
            .Attribute("data-cfx-motion-duration", story.DurationSeconds.ToString("0.###", CultureInfo.InvariantCulture))
            .EndStartElement().Line()
            .StartElement("title").Attribute("id", id + "-title").Text(story.Title).EndElement().Line()
            .StartElement("desc").Attribute("id", id + "-desc").Text(transcript).EndElement().Line()
            .StartElement("defs").EndStartElement()
            .StartElement("style").EndStartElement().Raw(BuildCss(story, id)).EndElement()
            .EndElement().Line();

        for (var index = 0; index < story.Scenes.Count; index++) {
            var png = PngWriter.WriteRgba(PngVisualStoryRenderer.RenderScene(story, index));
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
            WriteVectorMediaOverlays(writer, story, story.Scenes[index]);
            writer.EndElement().Line();
        }
        writer.EndElement().Line();
        return writer.Build().Replace("\r\n", "\n");
    }

    private static void WriteVectorMediaOverlays(
        SvgMarkupWriter writer,
        VisualStory story,
        VisualStoryScene scene) {
        var bounds = VisualStoryLayout.Panels(story, scene);
        for (var index = 0; index < scene.Panels.Count; index++) {
            var panel = scene.Panels[index];
            if (!(panel.Surface is VisualStoryMediaSurface media) || media.Svg.Length == 0) continue;
            var content = VisualStoryLayout.PanelContent(panel, bounds[index]);
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
                    Convert.ToBase64String(Encoding.UTF8.GetBytes(media.Svg)))
                .EndEmptyElement();
        }
    }

    private static string BuildCss(VisualStory story, string id) {
        var css = new StringBuilder();
        if (story.Scenes.Count == 1) {
            css.Append('#').Append(id).Append(" .cfx-story-scene{opacity:1}");
        } else {
            var elapsed = 0d;
            var total = story.DurationSeconds;
            for (var index = 0; index < story.Scenes.Count; index++) {
                var scene = story.Scenes[index];
                var start = elapsed / total * 100;
                elapsed += scene.DurationSeconds;
                var end = elapsed / total * 100;
                var fadePercent = Math.Min(1.2, (end - start) * 0.12);
                var name = id + "-motion-scene-" + index.ToString(CultureInfo.InvariantCulture);
                css.Append("@keyframes ").Append(name).Append('{');
                if (index == 0) {
                    css.Append("0%{opacity:1}");
                } else {
                    css.Append("0%,").Append(Percent(start)).Append("{opacity:0}")
                        .Append(Percent(Math.Min(end, start + fadePercent))).Append("{opacity:1}");
                }
                css.Append(Percent(end)).Append("{opacity:1}");
                if (index != story.Scenes.Count - 1) {
                    css.Append(Percent(Math.Min(100, end + fadePercent))).Append("{opacity:0}");
                }
                css
                    .Append("100%{opacity:").Append(index == story.Scenes.Count - 1 ? '1' : '0').Append("}}");
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

    private static string Percent(double value) => value.ToString("0.###", CultureInfo.InvariantCulture) + "%";
}
