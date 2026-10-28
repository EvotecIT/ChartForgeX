using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
/// <summary>Parses the genuine omitted-preset source through the canonical adapter and reports pending effect diagnostics.</summary>
public static class SequencePresetAppearanceSourceRecipe {
    /// <summary>Creates natural scrollable native output; the host does not replace source minima or disable default source effects.</summary>
    public static VisualArtifact Create(string source,string variant="wide",VisualThemeMode mode=VisualThemeMode.Light,string? family=null) {
        if(variant is not ("compact" or "wide"))throw new ArgumentOutOfRangeException(nameof(variant));var result=MermaidRenderer.Render(source,new MermaidRenderOptions{Presentation=new MermaidPresentationOptions{ThemeMode=mode,FontFamily=family},Sequence=new MermaidSequenceRenderOptions{Width=variant=="compact"?390:1100,Height=860}});if(result.HasErrors||result.Artifact==null)throw new InvalidOperationException(string.Join("; ",result.Diagnostics.Select(d=>d.Message)));result.Artifact.HtmlSizing=VisualArtifactHtmlSizing.PreserveSize;return result.Artifact;
    }
}
