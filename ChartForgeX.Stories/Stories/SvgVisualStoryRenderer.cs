using System;
using ChartForgeX.Svg;

namespace ChartForgeX.Stories;

/// <summary>Renders script-free SVG using the common prepared playback clock.</summary>
public sealed class SvgVisualStoryRenderer {
    internal const int MaximumDocumentCharacters = 64 * 1024 * 1024;
    /// <summary>Renders a visual story to animated SVG markup.</summary>
    public string Render(VisualStory story) => Render(story, string.Empty);
    /// <summary>Renders animated SVG with a deterministic identifier scope.</summary>
    public string Render(VisualStory story, string idScope) =>
        (story ?? throw new ArgumentNullException(nameof(story))).Prepare().ToAnimatedSvg(idScope: idScope);
    /// <summary>Renders the completed native frame as a static vector SVG.</summary>
    public string RenderStatic(VisualStory story, string idScope = "") {
        if (story == null) throw new ArgumentNullException(nameof(story));
        if (idScope == null) throw new ArgumentNullException(nameof(idScope));
        var prepared = story.Prepare();
        return prepared.PrepareFrame(prepared.ContentDuration).ToSvg(idScope);
    }

    internal static long ReserveEmbeddedMedia(long currentCharacters, long byteCount, string sceneId) {
        if (currentCharacters < 0) throw new ArgumentOutOfRangeException(nameof(currentCharacters));
        if (byteCount < 0) throw new ArgumentOutOfRangeException(nameof(byteCount));
        var encodedCharacters = checked(((byteCount + 2) / 3) * 4);
        var total = checked(currentCharacters + encodedCharacters);
        if (total > MaximumDocumentCharacters) {
            throw new InvalidOperationException(
                "Visual-story SVG embedded media exceeds the " + MaximumDocumentCharacters +
                "-character safety limit while rendering scene '" + sceneId +
                "'. Lower the size, scene count, or embedded media complexity.");
        }
        return total;
    }

}
