using System;
using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

/// <summary>Owns common bounded source graph spacing and native text policy without choosing a layout algorithm.</summary>
internal sealed class MermaidGraphSettings {
    private static readonly string[] Keys={"diagramPadding","nodeSpacing","rankSpacing","wrappingWidth","minNodeWidth","padding","useMaxWidth"};
    private readonly string _scope;
    private MermaidGraphSettings(string scope){_scope=scope;}
    internal double? Padding,NodeSpacing,RankSpacing,WrappingWidth,MinimumWidth,ContentPadding;
    internal bool? UseMaxWidth;
    internal bool HasNativeSettings=>Padding.HasValue||NodeSpacing.HasValue||RankSpacing.HasValue||WrappingWidth.HasValue||MinimumWidth.HasValue||ContentPadding.HasValue||UseMaxWidth.HasValue;
    internal static bool IsScalarPath(string path)=>IsScalarPath(path,"flowchart")||IsScalarPath(path,"agentflow");
    internal static bool IsScalarPath(string path,string scope) {
        foreach(var key in Keys)if(path==scope+"."+key)return true;return false;
    }
    internal bool Handles(string path) {
        foreach(var key in Keys) {
            var root=_scope+"."+key;if(path==root||path.StartsWith(root+".",StringComparison.Ordinal))return true;
        }
        return false;
    }
    internal static MermaidGraphSettings Read(MermaidSourceConfiguration source,string scope,Action<MermaidConfigurationSetting,string> error) {
        var result=new MermaidGraphSettings(scope) {
            Padding=Number("diagramPadding",false),NodeSpacing=Number("nodeSpacing",false),RankSpacing=Number("rankSpacing",false),
            WrappingWidth=Number("wrappingWidth",true),MinimumWidth=Number("minNodeWidth",false),ContentPadding=Number("padding",false)
        };
        var maximum=source.Last(scope+".useMaxWidth");
        if(maximum!=null) {
            if(!maximum.IsString&&bool.TryParse(maximum.Value,out var value))result.UseMaxWidth=value;
            else error(maximum,"Mermaid configuration '"+maximum.Path+"' must be a boolean.");
        }
        return result;
        double? Number(string key,bool positive) {
            var setting=source.Last(scope+"."+key);if(setting==null)return null;
            if(setting.TryGetNumber(out var value)&&(positive?value>0:value>=0))return value;
            error(setting,"Mermaid configuration '"+setting.Path+"' must be finite and "+(positive?"positive":"non-negative")+".");return null;
        }
    }
    internal void Apply(TopologyChart chart,MermaidPresentationOptions? host) {
        if(!HasNativeSettings)return;
        chart.DefaultRenderOptions??=new TopologyRenderOptions();var render=chart.DefaultRenderOptions;
        if(NodeSpacing.HasValue)render.LayeredNodeSpacing=NodeSpacing;
        if(RankSpacing.HasValue)render.LayeredRankSpacing=RankSpacing;
        if(UseMaxWidth.HasValue&&host?.TopologySizing==null)render.FitContentToViewport=UseMaxWidth.Value;
        if(WrappingWidth.HasValue||MinimumWidth.HasValue) {
            render.AllowMultilineNodeLabels=true;render.WrapNodeLabels=true;render.MaxNodeLabelLines=32;render.MaxNodeSubtitleLines=32;
        }
        foreach(var node in chart.Nodes) {
            if(!node.ShowLabel)continue;
            if(ContentPadding.HasValue)node.ContentPadding=ContentPadding;
            var inset=node.ContentPadding??12;
            if(MinimumWidth.HasValue)node.Width=Math.Max(node.Width,MinimumWidth.Value+inset*2);
            if(WrappingWidth.HasValue)node.MaximumTextWidth=Math.Max(WrappingWidth.Value,MinimumWidth??0);
        }
    }
}
