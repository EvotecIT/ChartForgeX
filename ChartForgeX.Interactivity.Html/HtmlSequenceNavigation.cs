using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.Html;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
namespace ChartForgeX.Interactivity.Html;
/// <summary>Composes native sequence scenes with ordinary HTML participant navigation.</summary>
public static class HtmlSequenceNavigation {
    /// <summary>Creates a self-contained page from one native preparation and detached participant link facts.</summary>
    /// <param name="sequence">The reusable sequence source.</param>
    /// <param name="options">Optional host-only initial disclosure state, captured before preparation.</param>
    /// <param name="context">Optional explicit native viewport/theme request; omitted uses the normal natural sequence owner.</param>
    /// <returns>Native script-free SVG plus escaped details/link controls, or the ordinary static page when no visible participant has links.</returns>
    public static string ToNavigationHtmlPage(this SequenceArtifact sequence,HtmlSequenceNavigationOptions? options=null,VisualRenderContext? context=null) {
        if(sequence==null)throw new ArgumentNullException(nameof(sequence));
        var expanded=options?.InitiallyExpanded==true;
        var prepared=context==null?SequencePreparedCompiler.PrepareDefault(sequence):sequence.Prepare(context);
        var envelope=prepared.ToArtifact(sequence.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope();
        var menus=prepared.Regions.Where(region=>region.Role=="sequence-participant").Select(region=>new {Region=region,Node=envelope.Nodes.Single(node=>node.Id==region.Id)})
            .Where(item=>item.Node.Sequence!.NavigationLinks.Count>0).ToArray();
        var title=sequence.Title.Length==0?sequence.Id:sequence.Title;var language=prepared.Accessibility.Language;var svg=prepared.ToSvg();
        if(menus.Length==0)return VisualArtifactRendering.WrapSvgPage(title,svg,language,sizing:VisualArtifactHtmlSizing.PreserveSize);
        var writer=new HtmlMarkupWriter();
        // Fixed host CSS only; all geometry is the captured native participant region, never recomputed text or sequence layout.
        writer.StartElement("style").RawTrusted(".cfx-sequence-navigation{display:grid;grid-template-columns:minmax(0,1fr);isolation:isolate}.cfx-sequence-navigation>svg{grid-area:1/1;display:block;max-width:none!important;height:auto}.cfx-sequence-navigation>details{grid-area:1/1;align-self:start;justify-self:start;z-index:1;box-sizing:border-box;min-width:0;color:CanvasText;background:Canvas;border:1px solid currentColor;border-radius:4px;font:14px/1.4 system-ui;overflow-wrap:anywhere}.cfx-sequence-navigation summary{cursor:pointer;padding:4px 8px}.cfx-sequence-navigation nav{display:grid;padding:4px 8px;gap:4px}.cfx-sequence-navigation a{color:LinkText}.cfx-sequence-navigation :focus-visible{outline:2px solid currentColor;outline-offset:2px}").EndElement();
        writer.StartElement("div").Attribute("class","cfx-sequence-navigation").Attribute("data-cfx-role","sequence-navigation").Attribute("style","width:"+F(prepared.Size.Width)+"px;color-scheme:"+((context?.ThemeMode??sequence.ThemeMode)==ChartForgeX.Themes.VisualThemeMode.Dark?"dark":"light")).EndStartElement().RawTrusted(svg);
        foreach(var item in menus) {
            var b=item.Region.Bounds;
            writer.StartElement("details").Attribute("data-source-id",item.Node.Id).BooleanAttribute("open",expanded)
                .Attribute("style","margin-left:"+F(b.Left)+"px;margin-top:"+F(b.Bottom)+"px;max-width:"+F(prepared.Size.Width-b.Left)+"px").EndStartElement();
            writer.StartElement("summary").Text(item.Node.Label+" links").EndElement();
            writer.StartElement("nav").Attribute("aria-label",item.Node.Label+" links").EndStartElement();
            foreach(var link in item.Node.Sequence!.NavigationLinks)writer.StartElement("a").Attribute("href",link.Href).Text(link.Label).EndElement();
            writer.EndElement().EndElement();
        }
        writer.EndElement();return VisualArtifactRendering.WrapSvgPage(title,writer.Build(),language,sizing:VisualArtifactHtmlSizing.PreserveSize);
    }
    private static string F(double value)=>value.ToString("R",CultureInfo.InvariantCulture);
}
