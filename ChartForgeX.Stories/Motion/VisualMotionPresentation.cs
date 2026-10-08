using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Motion;

/// <summary>Adds a script-free timeline to named targets in a common static visual.</summary>
/// <remarks>The completed raster remains the producer's static output. Target motion is an SVG/HTML presentation policy.</remarks>
public sealed class VisualMotionPresentation : IStaticVisualSource {
    private readonly string _svg;
    private readonly RgbaImage _raster;
    private readonly VisualMotionTimeline _timeline;

    private VisualMotionPresentation(string svg, RgbaImage raster, VisualMotionTimeline timeline) {
        _svg = svg ?? throw new ArgumentNullException(nameof(svg));
        _raster = new RgbaImage(raster.Width, raster.Height, (byte[])raster.Pixels.Clone());
        _timeline = VisualMotionTimeline.Create();
        if (timeline == null) throw new ArgumentNullException(nameof(timeline));
        timeline.Validate();
        foreach (var cue in timeline.Cues)
            _timeline.Add(cue.TargetId, cue.Effect, cue.DelaySeconds, cue.DurationSeconds, cue.Easing, cue.DistancePixels);
        ValidateTargets(ReadDocument(_svg));
    }

    /// <summary>Captures a producer's static output and applies a detached copy of the timeline.</summary>
    public static VisualMotionPresentation Create(IStaticVisualSource source, VisualMotionTimeline timeline) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        return new VisualMotionPresentation(source.RenderSvg("motion-input"), source.RenderRgba(), timeline);
    }

    /// <summary>Applies a timeline to source identities already retained in a prepared native scene.</summary>
    public static VisualMotionPresentation Create(PreparedVisual prepared, VisualMotionTimeline timeline) {
        if (prepared == null) throw new ArgumentNullException(nameof(prepared));
        return new VisualMotionPresentation(prepared.ToSvg("motion-input"), prepared.ToRgba(), timeline);
    }

    /// <summary>Exports animated SVG with deterministic scoped IDs and a reduced-motion/print fallback.</summary>
    public string ToSvg(string idScope = "") {
        if (idScope == null) throw new ArgumentNullException(nameof(idScope));
        var identity = SvgRenderedIdentity.CreateProvisionalId("cfx-motion", idScope,
            _svg, string.Join("|", _timeline.Cues.Select(cue => cue.TargetId + ":" + cue.Effect + ":" +
                cue.DelaySeconds.ToString("R", CultureInfo.InvariantCulture) + ":" + cue.DurationSeconds.ToString("R", CultureInfo.InvariantCulture)
                + ":" + cue.Easing + ":" + cue.DistancePixels.ToString("R", CultureInfo.InvariantCulture))));
        var document = ReadDocument(ScopeStatic(identity));
        var root = document.Root!;
        root.SetAttributeValue("id", identity);
        root.SetAttributeValue("data-cfx-motion", "timeline");
        root.SetAttributeValue("data-cfx-motion-duration", VisualMotionCss.Duration(_timeline).ToString("0.###", CultureInfo.InvariantCulture));
        var description = root.Elements(root.Name.Namespace + "desc").FirstOrDefault();
        if (description != null) description.Value += " Motion is decorative and has a static reduced-motion fallback.";
        foreach (var cue in _timeline.Cues) {
            var target = root.DescendantsAndSelf().Single(element => TargetId(element) == cue.TargetId);
            target.SetAttributeValue("data-cfx-motion-target", cue.TargetId);
        }
        root.AddFirst(new XElement(root.Name.Namespace + "style", VisualMotionCss.Build("#" + identity, _timeline, identity)));
        return SvgRenderedIdentity.Bind(document.ToString(SaveOptions.DisableFormatting), identity, "cfx-motion", idScope);
    }

    /// <summary>Exports an embeddable animated SVG wrapper.</summary>
    public string ToHtmlFragment(string idScope = "") => "<div class=\"chartforgex-visual-motion\">" + ToSvg(idScope) + "</div>";
    /// <summary>Exports a complete HTML page containing the animated SVG.</summary>
    public string ToHtmlPage() => VisualArtifactRendering.WrapSvgPage("Visual motion", ToSvg());
    /// <summary>Exports the completed static pixels without animation artifacts.</summary>
    public RgbaImage ToRgbaImage() => new(_raster.Width, _raster.Height, (byte[])_raster.Pixels.Clone());
    /// <summary>Exports the completed static output as PNG.</summary>
    public byte[] ToPng() => PngWriter.WriteRgba(_raster);

    string IStaticVisualSource.RenderSvg(string idScope) => ScopeStatic(
        SvgRenderedIdentity.CreateProvisionalId("cfx-motion-still", idScope, _svg));
    RgbaImage IStaticVisualSource.RenderRgba() => ToRgbaImage();

    private void ValidateTargets(XDocument document) {
        foreach (var cue in _timeline.Cues) {
            var count = document.Root!.DescendantsAndSelf().Count(element => TargetId(element) == cue.TargetId);
            if (count != 1) throw new InvalidOperationException("Visual motion target '" + cue.TargetId
                + "' must identify exactly one element in the static visual.");
        }
    }

    private static string? TargetId(XElement element) =>
        (string?)element.Attribute("data-cfx-target") ?? (string?)element.Attribute("data-cfx-motion-target")
            ?? (string?)element.Attribute("data-cfx-source-id");

    private string ScopeStatic(string identity) {
        var source = ReadDocument(_svg);
        var scoped = source.ToString(SaveOptions.DisableFormatting);
        foreach (var oldId in source.Descendants().Attributes("id").Select(attribute => attribute.Value)
                     .Distinct(StringComparer.Ordinal).OrderByDescending(value => value.Length))
            scoped = SvgRenderedIdentity.RebindGeneratedId(scoped, oldId, identity + "-input-" + oldId);
        var document = ReadDocument(scoped);
        var rootId = (string?)document.Root!.Attribute("id");
        if (rootId != null) scoped = SvgRenderedIdentity.RebindGeneratedId(scoped, rootId, identity);
        else { document.Root.SetAttributeValue("id", identity); scoped = document.ToString(SaveOptions.DisableFormatting); }
        return scoped;
    }

    private static XDocument ReadDocument(string svg) {
        using var text = new StringReader(svg);
        using var reader = XmlReader.Create(text, new XmlReaderSettings {
            DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 64L * 1024 * 1024
        });
        var document = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
        if (document.Root?.Name != XName.Get("svg", "http://www.w3.org/2000/svg"))
            throw new ArgumentException("A static SVG root is required.", nameof(svg));
        return document;
    }
}
