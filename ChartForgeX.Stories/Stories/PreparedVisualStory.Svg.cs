using System;
using System.Globalization;
using System.Text;
using System.Threading;
using ChartForgeX.Svg;
using ChartForgeX.Accessibility;

namespace ChartForgeX.Stories;

public sealed partial class PreparedVisualStory {
    /// <summary>Exports a script-free HTML page using the captured story and prepared playback clock.</summary>
    public string ToHtmlPage(VisualStoryFrameOptions? options = null, CancellationToken cancellationToken = default) =>
        new HtmlVisualStoryRenderer().RenderPage(_story, ToAnimatedSvg(options, cancellationToken: cancellationToken));

    /// <summary>Exports sampled native frames as a self-contained, script-free SVG animation using the prepared playback clock.</summary>
    /// <remarks>Reduced motion and print show the completed poster. The complete SVG document has a 64 MiB character budget, including escaped text and embedded frames.</remarks>
    public string ToAnimatedSvg(VisualStoryFrameOptions? options = null, string idScope = "", CancellationToken cancellationToken = default) {
        if (idScope == null) throw new ArgumentNullException(nameof(idScope));
        var sampling = options ?? DefaultSvgSampling(); var count = FrameCount(sampling);
        var chapterSamples = new TimeSpan[Chapters.Count];
        EnsureSceneCoverage(count, index => TimeSpan.FromTicks(SampleTicks(index, sampling.FramesPerSecond)), chapterSamples: chapterSamples);
        var provisional = SvgRenderedIdentity.CreateProvisionalId("cfx-story", idScope, Title, Width.ToString(CultureInfo.InvariantCulture), Height.ToString(CultureInfo.InvariantCulture));
        var writer = new SvgMarkupWriter(16384, SvgVisualStoryRenderer.MaximumDocumentCharacters);
        writer.StartElement("svg").Attribute("xmlns", "http://www.w3.org/2000/svg").Attribute("id", provisional)
            .Attribute("width", Width).Attribute("height", Height).Attribute("viewBox", "0 0 " + Width + " " + Height)
            .Attribute("role", "img").Attribute("aria-labelledby", provisional + "-title " + provisional + "-desc")
            .Attribute("style", "max-width:100%;height:auto;display:block")
            .Attribute("data-cfx-story", "visual").Attribute("data-cfx-motion", "scene-story")
            .Attribute("data-cfx-motion-duration", Duration.TotalSeconds.ToString("0.#########", CultureInfo.InvariantCulture))
            .Attribute("data-cfx-motion-plays", Playback.PlayCount).EndStartElement()
            .StartElement("title").Attribute("id", provisional + "-title").Text(Title).EndElement()
            .StartElement("desc").Attribute("id", provisional + "-desc").Text(ToTranscript()).EndElement();
        for (var chapter = 0; chapter < Chapters.Count; chapter++) {
            writer.StartElement("g").Attribute("data-cfx-chapter-id", Chapters[chapter].Id)
                .Attribute("data-cfx-chapter-title", Chapters[chapter].Title)
                .Attribute("data-cfx-chapter-start", Chapters[chapter].Start.TotalSeconds.ToString("0.#########", CultureInfo.InvariantCulture))
                .Attribute("data-cfx-chapter-frame-time", chapterSamples[chapter].TotalSeconds.ToString("0.#########", CultureInfo.InvariantCulture)).EndEmptyElement();
        }
        var css = new StringBuilder(); var total = Duration.Ticks;
        long embedded = 0;
        for (var index = 0; index < count; index++) {
            cancellationToken.ThrowIfCancellationRequested();
            var start = SampleTicks(index, sampling.FramesPerSecond);
            var end = index == count - 1 ? total : SampleTicks(index + 1, sampling.FramesPerSecond);
            var timestamp = index == count - 1 ? ContentDuration : TimeSpan.FromTicks(start);
            var frame = PrepareFrame(timestamp, sampling.OutputScale).ToSvg(new VisualAccessibility { IsDecorative = true }, "story-frame");
            embedded = SvgVisualStoryRenderer.ReserveEmbeddedMedia(embedded, Encoding.UTF8.GetByteCount(frame), _story.Scenes[0].Id);
            var chapter = FindChapter(timestamp < ContentDuration ? timestamp : ContentDuration);
            var name = provisional + "-motion-frame-" + index;
            var last = index == count - 1;
            writer.StartElement("g").Attribute("class", "cfx-story-frame cfx-story-frame-" + index + (last ? " cfx-story-frame-last" : ""))
                .Attribute("data-cfx-scene", _story.Scenes[chapter].Id)
                .Attribute("data-cfx-scene-title", _story.Scenes[chapter].Title)
                .Attribute("data-cfx-chapter-start", Chapters[chapter].Start.TotalSeconds.ToString("0.#########", CultureInfo.InvariantCulture))
                .Attribute("data-cfx-frame-time", (start / (double)TimeSpan.TicksPerSecond).ToString("0.#########", CultureInfo.InvariantCulture)).EndStartElement()
                .StartElement("image").Attribute("width", Width).Attribute("height", Height)
                .Attribute("href", "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(frame))).EndEmptyElement().EndElement();
            css.Append('@').Append("keyframes ").Append(name).Append("{0%{opacity:").Append(index == 0 ? '1' : '0').Append('}');
            if (index > 0) css.Append(Percent(start, total)).Append("{opacity:1}");
            if (!last) css.Append(Percent(end, total)).Append("{opacity:0}");
            css.Append("100%{opacity:").Append(last ? '1' : '0').Append("}}");
            css.Append('#').Append(provisional).Append(" .cfx-story-frame-").Append(index).Append("{opacity:").Append(last ? '1' : '0')
                .Append(";animation:").Append(name).Append(' ').Append(Duration.TotalSeconds.ToString("0.#########", CultureInfo.InvariantCulture))
                .Append("s steps(1,end) ").Append(Playback.PlayCount == 0 ? "infinite" : Playback.PlayCount.ToString(CultureInfo.InvariantCulture)).Append(" both}");
        }
        css.Append("@media (prefers-reduced-motion:reduce){#").Append(provisional).Append(" .cfx-story-frame{display:none;animation:none}#")
            .Append(provisional).Append(" .cfx-story-frame-last{display:inline;opacity:1}}");
        css.Append("@media print{#").Append(provisional).Append(" .cfx-story-frame{display:none;animation:none}#")
            .Append(provisional).Append(" .cfx-story-frame-last{display:inline;opacity:1}}");
        writer.StartElement("style").EndStartElement().Raw(css.ToString()).EndElement().EndElement();
        cancellationToken.ThrowIfCancellationRequested();
        var result = SvgRenderedIdentity.Bind(writer.Build(), provisional, "cfx-story", idScope);
        if (result.Length > SvgVisualStoryRenderer.MaximumDocumentCharacters)
            throw new InvalidOperationException("Story SVG document exceeds the 64 MiB character budget. Reduce text or embedded media.");
        return result;
    }
    private static string Percent(long ticks, long total) => (ticks * 100d / total).ToString("0.#########", CultureInfo.InvariantCulture) + "%";
}
