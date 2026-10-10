using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Accessibility;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;
using ChartForgeX.Terminal;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Stories;

/// <summary>Produces common static artifacts from completed story states.</summary>
public static class StoryArtifactExtensions {
    /// <summary>Captures the completed visual story while retaining story metadata and transcript accessibility.</summary>
    public static VisualArtifact ToVisualArtifact(this VisualStory story, string? id = null,
        VisualArtifactSourceLanguage sourceLanguage = VisualArtifactSourceLanguage.Native) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        var prepared = story.Prepare();
        var source = new StoryStaticSource(prepared.PrepareFrame(prepared.ContentDuration).ToSvg("story-artifact"),
            prepared.RenderAt(prepared.ContentDuration));
        var artifact = Create(story, source, id, "visual-story", story.Title, story.Description,
            story.Width, story.Height, sourceLanguage);
        artifact.Accessibility.Name = story.Title;
        artifact.Accessibility.Description = new VisualStoryTranscriptRenderer().Render(story);
        artifact.Metadata["visual-story.scenes"] = story.Scenes.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["visual-story.outcomes"] = story.Outcomes.Count.ToString(CultureInfo.InvariantCulture);
        return artifact;
    }

    /// <summary>Captures the completed terminal display and its command/output transcript.</summary>
    public static VisualArtifact ToVisualArtifact(this TerminalStory story, string? id = null,
        VisualArtifactSourceLanguage sourceLanguage = VisualArtifactSourceLanguage.Native) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        var layout = TerminalStoryLayout.Build(story);
        var raster = RasterImageDecoder.Decode(story.ToPng());
        var transcript = story.ToTranscript();
        var builder = new VisualSceneBuilder(new VisualSize(layout.Width, layout.Height), FontSpec.FromFamily(story.Theme.FontFamily));
        builder.Image(raster, new ChartRect(0, 0, layout.Width, layout.Height), role: "terminal-completed");
        var prepared = new PreparedVisual(builder.Build(), new VisualAccessibility { Name = story.Title, Description = transcript });
        var source = new StoryStaticSource(prepared.ToSvg("terminal-artifact"), raster);
        var artifact = Create(story, source, id, "terminal-story", story.Title, string.Empty,
            layout.Width, layout.Height, sourceLanguage);
        artifact.Accessibility.Name = story.Title; artifact.Accessibility.Description = transcript;
        artifact.Metadata["terminal-story.tabs"] = story.Tabs.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["terminal-story.steps"] = story.Steps.Count.ToString(CultureInfo.InvariantCulture);
        return artifact;
    }

    private static VisualArtifact Create(object model, IStaticVisualSource source, string? id, string defaultId,
        string title, string subtitle, double width, double height, VisualArtifactSourceLanguage language) {
        var artifact = VisualArtifact.Create(string.IsNullOrWhiteSpace(id) ? defaultId : id!.Trim(), VisualArtifactKind.Story, model);
        artifact.RenderSource = source; artifact.SourceLanguage = language;
        artifact.Title = title; artifact.Subtitle = subtitle;
        artifact.NaturalSize = new VisualArtifactSize(width, height);
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png
            | VisualArtifactExportFormat.Html | VisualArtifactExportFormat.Office;
        artifact.Metadata["render.model"] = model.GetType().Name;
        return artifact;
    }
}

internal sealed class StoryStaticSource : IStaticVisualSource {
    private readonly string _svg;
    private readonly string? _rootId;
    private readonly RgbaImage _raster;
    internal StoryStaticSource(string svg, RgbaImage raster) {
        _svg = svg; _rootId = (string?)XDocument.Parse(svg).Root!.Attribute("id");
        _raster = new RgbaImage(raster.Width, raster.Height, (byte[])raster.Pixels.Clone());
    }
    public string RenderSvg(string idScope) {
        var id = SvgRenderedIdentity.CreateProvisionalId("cfx-story-artifact", idScope, _svg);
        if (_rootId != null) return SvgRenderedIdentity.RebindGeneratedId(_svg, _rootId, id);
        // Prepared SVG keeps IDs below its root. Re-export namespaces by prefixing each generated identity.
        var result = _svg;
        foreach (var oldId in XDocument.Parse(_svg).Descendants().Attributes("id").Select(attribute => attribute.Value)
            .Distinct(StringComparer.Ordinal).OrderByDescending(value => value.Length))
            result = SvgRenderedIdentity.RebindGeneratedId(result, oldId, id + "-" + oldId);
        return result;
    }
    public RgbaImage RenderRgba() => new(_raster.Width, _raster.Height, (byte[])_raster.Pixels.Clone());
}
