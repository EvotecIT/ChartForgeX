using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

/// <summary>Native source presentation with no browser engine or plugin dependency.</summary>
public static class ZenUmlPresentationRecipe {
    public static VisualArtifact Create(string family,string variant,VisualThemeMode mode,string? fontFamily=null) {
        var source=family=="order"?Order:Presentation;
        var result=MermaidRenderer.Render(source,new MermaidRenderOptions{
            Sequence=new MermaidSequenceRenderOptions{Width=variant=="compact"?640:1100,Height=900},
            Presentation=new MermaidPresentationOptions{ThemeMode=mode,FontFamily=fontFamily}});
        if(result.HasErrors||result.Diagnostics.Count>0||result.Artifact==null)throw new InvalidOperationException("The finite Zen source recipe must render without diagnostics.");
        return result.Artifact;
    }
    private const string Presentation="zenuml\ntitle Native presentation\nparticipant <<Service>> [rocket] API 176 as \"API service\" #336699\nparticipant <<Store>> 🔒 Store 144 as \"Stored records\" #ccddaa\nAPI.work() {\n  Store.read()\n  return ready\n}\nref(Login, API, Store)\n== Next phase ==\nAPI -> Store: ready\n";
    private const string Order="zenuml\nparticipant <<Service>> [rocket] API 176 as \"API service\" #336699\nparticipant Store\nAPI.work() {\n  ref(\"Inside call\", API, Store)\n}\n== After return ==\nref(\"Outside call\", API, Store)\nAPI -> Store: next message\n";
}
