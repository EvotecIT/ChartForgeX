using System.Collections.Generic;
using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

/// <summary>Validates and projects finite native paints once for topology-backed source families.</summary>
internal static partial class MermaidNativeStyles {
    internal static bool SupportsEdge(string property,string value)=>property switch {
        "stroke" or "color"=>MermaidStyleValues.IsColor(value),
        "stroke-width"=>MermaidStyleValues.TryPixels(value,out var width)&&width>0&&width<=512,
        "opacity"=>MermaidStyleValues.TryPixels(value,out var opacity,allowUnit:false)&&opacity>=0&&opacity<=1,
        "stroke-dasharray"=>MermaidStyleValues.TryNativeDash(value,out _),
        _=>SupportsText(property,value)
    };
    internal static bool SupportsSurface(string property,string value)=>property is "fill" or "stroke" or "color"
        ?MermaidStyleValues.IsColor(value):property!="opacity"&&SupportsEdge(property,value);
    internal static bool UnsupportedSurface(string property)=>property=="opacity";
    internal static void Node(TopologyNode target,IReadOnlyList<string> classes,Dictionary<string,string> styles) {
        Facts(target.Metadata,classes,styles);
        target.TextStyle=ApplyTextStyle(target.TextStyle,styles);
        if(Paint(styles,"fill") is {} fill)target.BackgroundColor=fill;
        if(Paint(styles,"stroke") is {} stroke)target.Color=stroke;
        if(Paint(styles,"color") is {} color) {
            target.LabelColor=color;foreach(var detail in target.Details)detail.Color=color;
        }
        if(styles.TryGetValue("stroke-width",out var width)&&SupportsSurface("stroke-width",width)){MermaidStyleValues.TryPixels(width,out var value);target.StrokeWidth=value;}
        if(styles.TryGetValue("stroke-dasharray",out var dash)&&MermaidStyleValues.TryNativeDash(dash,out var pattern)){target.DashPattern.Clear();target.DashPattern.AddRange(pattern);}
    }
    internal static void Group(TopologyGroup target,IReadOnlyList<string> classes,Dictionary<string,string> styles) {
        Facts(target.Metadata,classes,styles);
        target.TextStyle=ApplyTextStyle(target.TextStyle,styles);
        if(Paint(styles,"fill") is {} fill)target.BackgroundColor=fill;
        if(Paint(styles,"stroke") is {} stroke)target.BorderColor=stroke;
        if(Paint(styles,"color") is {} color)target.LabelColor=color;
        if(styles.TryGetValue("stroke-width",out var width)&&SupportsSurface("stroke-width",width)){MermaidStyleValues.TryPixels(width,out var value);target.StrokeWidth=value;}
        if(styles.TryGetValue("stroke-dasharray",out var dash)&&MermaidStyleValues.TryNativeDash(dash,out var pattern)){target.DashPattern.Clear();target.DashPattern.AddRange(pattern);}
    }
    internal static void Edge(TopologyEdge target,IReadOnlyList<string> classes,Dictionary<string,string> styles) {
        Facts(target.Metadata,classes,styles);
        target.TextStyle=ApplyTextStyle(target.TextStyle,styles);
        if(Paint(styles,"stroke") is {} stroke)target.Color=stroke;
        if(Paint(styles,"color") is {} color)target.LabelColor=color;
        if(styles.TryGetValue("stroke-width",out var width)&&SupportsEdge("stroke-width",width)) {
            MermaidStyleValues.TryPixels(width,out var parsed);target.StrokeWidth=parsed;
        }
        if(styles.TryGetValue("opacity",out var opacity)&&SupportsEdge("opacity",opacity)) {
            MermaidStyleValues.TryPixels(opacity,out var parsed,allowUnit:false);target.Opacity=parsed;
        }
        if(styles.TryGetValue("stroke-dasharray",out var dash)&&MermaidStyleValues.TryNativeDash(dash,out var pattern)) {
            target.LineStyle=pattern.Length>0?TopologyEdgeLineStyle.Dashed:TopologyEdgeLineStyle.Solid;target.DashPattern.Clear();target.DashPattern.AddRange(pattern);
        }
    }
    internal static string? Paint(Dictionary<string,string> styles,string key)=>
        styles.TryGetValue(key,out var value)&&MermaidStyleValues.IsColor(value)?value:null;
    private static void Facts(Dictionary<string,string> target,IReadOnlyList<string> classes,Dictionary<string,string> styles) {
        if(classes.Count>0)target["mermaid.classes"]=string.Join(",",classes);
        foreach(var item in styles)target["mermaid.style."+item.Key]=item.Value;
    }
}
