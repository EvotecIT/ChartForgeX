using System;
using System.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

/// <summary>Parses genuine omitted Sequence appearance without disabling source effects.</summary>
public static class SequenceSurfaceShadowSourceRecipe {
    /// <summary>Uses the validated native source projection; pending URL diagnostics remain visible to the caller.</summary>
    public static MermaidRenderResult Parse(string source, string variant = "wide",
        VisualThemeMode mode = VisualThemeMode.Light, string? fontFamily = null) {
        if (variant is not ("compact" or "wide")) throw new ArgumentOutOfRangeException(nameof(variant));
        var result = MermaidRenderer.Render(source, new MermaidRenderOptions {
            Presentation = new MermaidPresentationOptions { ThemeMode = mode, FontFamily = fontFamily },
            Sequence = new MermaidSequenceRenderOptions { Width = variant == "compact" ? 390 : 1100, Height = 900 }
        });
        if (result.HasErrors || result.Artifact == null) {
            throw new InvalidOperationException(string.Join("; ", result.Diagnostics.Select(item => item.Message)));
        }
        return result;
    }
    /// <summary>Captures native filter output in the completed convenience frame, preserving authored participant widths and padding.</summary>
    public static ChartForgeX.Rendering.PreparedVisual PrepareCapture(MermaidRenderResult result) {
        if (result.Artifact?.Model is not SequenceArtifact model) throw new ArgumentException("A native sequence result is required.", nameof(result));
        var frame = System.Xml.Linq.XElement.Parse(result.Artifact.ToSvg()).Attribute("viewBox")?.Value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? throw new InvalidOperationException("Native SVG has no completed frame.");
        var size = new ChartForgeX.Rendering.VisualSize(double.Parse(frame[2], System.Globalization.CultureInfo.InvariantCulture),
            double.Parse(frame[3], System.Globalization.CultureInfo.InvariantCulture));
        var padding = model.Layout?.CanvasPadding ?? ChartForgeX.Primitives.ChartPadding.All(model.Padding);
        return model.Prepare(new ChartForgeX.Rendering.VisualRenderContext(new ChartForgeX.Rendering.VisualLayoutOptions(size, padding),
            model.Theme, model.ThemeMode, frame: new ChartForgeX.Rendering.VisualFrame(showLegend: false)));
    }
}
