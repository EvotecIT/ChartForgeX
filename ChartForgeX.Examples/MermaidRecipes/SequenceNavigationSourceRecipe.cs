using ChartForgeX.Interactivity.Html;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
/// <summary>Captures validated source navigation once and delegates HTML presentation to the optional generic adapter.</summary>
public static class SequenceNavigationSourceRecipe {
    /// <summary>Creates a static native artifact and a host page for the same parsed source.</summary>
    public static (VisualArtifact Artifact,string Html) Create(string source,string variant="wide",VisualThemeMode mode=VisualThemeMode.Light,string? fontFamily=null) {
        if(variant is not ("compact" or "wide"))throw new ArgumentOutOfRangeException(nameof(variant));
        var result=MermaidRenderer.Render(source,new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject,Presentation=new MermaidPresentationOptions{ThemeMode=mode,FontFamily=fontFamily},Sequence=new MermaidSequenceRenderOptions{Width=variant=="compact"?390:1000,Height=820}});
        if(result.HasErrors||result.Artifact==null)throw new InvalidOperationException(string.Join("; ",result.Diagnostics.Select(item=>item.Message)));
        var document=(MermaidSequenceDocument)result.Document!;var sequence=(SequenceArtifact)result.Artifact.Model!;
        return (result.Artifact,sequence.ToNavigationHtmlPage(new HtmlSequenceNavigationOptions{InitiallyExpanded=document.Configuration.SequenceForceMenus==true}));
    }
}
