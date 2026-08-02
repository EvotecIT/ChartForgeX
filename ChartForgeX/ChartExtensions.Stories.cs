using System;
using System.IO;
using System.Text;
using ChartForgeX.Stories;

namespace ChartForgeX;

public static partial class ChartExtensions {
    /// <summary>Renders a visual story to self-contained animated SVG.</summary>
    public static string ToSvg(this VisualStory story) => new SvgVisualStoryRenderer().Render(story);

    /// <summary>Renders a visual story to self-contained animated SVG with a deterministic ID scope.</summary>
    public static string ToSvg(this VisualStory story, string idScope) => new SvgVisualStoryRenderer().Render(story, idScope);

    /// <summary>Renders a visual story as an embeddable HTML fragment.</summary>
    public static string ToHtmlFragment(this VisualStory story) => new HtmlVisualStoryRenderer().RenderFragment(story);

    /// <summary>Renders a visual story as a complete HTML document.</summary>
    public static string ToHtmlPage(this VisualStory story) => new HtmlVisualStoryRenderer().RenderPage(story);

    /// <summary>Renders the completed visual-story scene to PNG.</summary>
    public static byte[] ToPng(this VisualStory story) => new PngVisualStoryRenderer().Render(story);

    /// <summary>Renders a visual story to animated GIF.</summary>
    public static byte[] ToGif(this VisualStory story, VisualStoryAnimationOptions? options = null) =>
        new VisualStoryAnimatedRasterRenderer().Render(story, options, Raster.AnimatedRasterFormat.Gif);

    /// <summary>Renders a visual story to animated PNG.</summary>
    public static byte[] ToApng(this VisualStory story, VisualStoryAnimationOptions? options = null) =>
        new VisualStoryAnimatedRasterRenderer().Render(story, options, Raster.AnimatedRasterFormat.Apng);

    /// <summary>Renders a portable plain-text transcript.</summary>
    public static string ToTranscript(this VisualStory story) => new VisualStoryTranscriptRenderer().Render(story);

    /// <summary>Saves a visual story as SVG.</summary>
    public static void SaveSvg(this VisualStory story, string path) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        File.WriteAllText(path, story.ToSvg(), Encoding.UTF8);
    }

    /// <summary>Saves the completed visual-story scene as PNG.</summary>
    public static void SavePng(this VisualStory story, string path) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        File.WriteAllBytes(path, story.ToPng());
    }

    /// <summary>Saves a visual story as animated GIF.</summary>
    public static void SaveGif(this VisualStory story, string path, VisualStoryAnimationOptions? options = null) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        File.WriteAllBytes(path, story.ToGif(options));
    }

    /// <summary>Saves a visual story as animated PNG.</summary>
    public static void SaveApng(this VisualStory story, string path, VisualStoryAnimationOptions? options = null) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        File.WriteAllBytes(path, story.ToApng(options));
    }

    /// <summary>Saves a portable plain-text transcript.</summary>
    public static void SaveTranscript(this VisualStory story, string path) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        File.WriteAllText(path, story.ToTranscript(), Encoding.UTF8);
    }
}
