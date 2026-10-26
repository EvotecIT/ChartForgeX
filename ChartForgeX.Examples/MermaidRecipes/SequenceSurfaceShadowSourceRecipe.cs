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
}
