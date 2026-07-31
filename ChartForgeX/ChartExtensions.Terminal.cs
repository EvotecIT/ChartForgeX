using System;
using System.IO;
using System.Text;
using ChartForgeX.Terminal;

namespace ChartForgeX;

public static partial class ChartExtensions {
    /// <summary>Renders a terminal story to SVG markup.</summary>
    public static string ToSvg(this TerminalStory story) => new SvgTerminalStoryRenderer().Render(story);

    /// <summary>Renders a terminal story to SVG markup with a deterministic ID scope.</summary>
    public static string ToSvg(this TerminalStory story, string idScope) => new SvgTerminalStoryRenderer().Render(story, idScope);

    /// <summary>Renders a terminal story as an embeddable HTML fragment.</summary>
    public static string ToHtmlFragment(this TerminalStory story) => new HtmlTerminalStoryRenderer().RenderFragment(story);

    /// <summary>Renders a terminal story as an embeddable HTML fragment with a deterministic ID scope.</summary>
    public static string ToHtmlFragment(this TerminalStory story, string idScope) => new HtmlTerminalStoryRenderer().RenderFragment(story, idScope);

    /// <summary>Renders a terminal story as a complete HTML document.</summary>
    public static string ToHtmlPage(this TerminalStory story) => new HtmlTerminalStoryRenderer().RenderPage(story);

    /// <summary>Renders the completed state of a terminal story to PNG bytes.</summary>
    public static byte[] ToPng(this TerminalStory story) => new PngTerminalStoryRenderer().Render(story);

    /// <summary>Renders a terminal story as an animated GIF.</summary>
    public static byte[] ToGif(this TerminalStory story, TerminalStoryAnimationOptions? options = null) =>
        new TerminalStoryAnimatedRasterRenderer().Render(story, options, Raster.AnimatedRasterFormat.Gif);

    /// <summary>Renders a terminal story as an animated PNG.</summary>
    public static byte[] ToApng(this TerminalStory story, TerminalStoryAnimationOptions? options = null) =>
        new TerminalStoryAnimatedRasterRenderer().Render(story, options, Raster.AnimatedRasterFormat.Apng);

    /// <summary>Saves a terminal story as SVG.</summary>
    public static void SaveSvg(this TerminalStory story, string path) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        File.WriteAllText(path, story.ToSvg(), Encoding.UTF8);
    }

    /// <summary>Saves a terminal story as a complete HTML document.</summary>
    public static void SaveHtml(this TerminalStory story, string path) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        File.WriteAllText(path, story.ToHtmlPage(), Encoding.UTF8);
    }

    /// <summary>Saves the completed state of a terminal story as PNG.</summary>
    public static void SavePng(this TerminalStory story, string path) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        File.WriteAllBytes(path, story.ToPng());
    }

    /// <summary>Saves a terminal story as an animated GIF.</summary>
    public static void SaveGif(this TerminalStory story, string path, TerminalStoryAnimationOptions? options = null) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        File.WriteAllBytes(path, story.ToGif(options));
    }

    /// <summary>Saves a terminal story as an animated PNG.</summary>
    public static void SaveApng(this TerminalStory story, string path, TerminalStoryAnimationOptions? options = null) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        File.WriteAllBytes(path, story.ToApng(options));
    }
}
