using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidAgentflowParser {
    private static bool TryStyle(MermaidAgentflowDocument document,Dictionary<string,MermaidAgentflowNode> nodes,
        List<MermaidFlowchartLinkStyle> links,string text,MermaidSourceSpan span,MermaidParseResult<MermaidDocument> result) {
        if(MermaidStyleParser.TryDefinition(text,span,result,document.ClassDefinitions,"Agentflow",MermaidNativeStyles.SupportsEdge))return true;
        var isClass=MermaidParserUtilities.StartsStatement(text,"class");
        var isStyle=MermaidParserUtilities.StartsStatement(text,"style");
        var isLink=MermaidParserUtilities.StartsStatement(text,"linkStyle");
        if(!isClass&&!isStyle&&!isLink)return false;
        var keyword=isLink?"linkStyle":isClass?"class":"style";
        if(!MermaidStyleParser.SplitHead(text.Substring(keyword.Length).Trim(),out var head,out var body)) {
            Error("Agentflow "+keyword+" requires a selector and value.");return true;
        }
        if(isLink) {
            var link=new MermaidFlowchartLinkStyle(head,span);
            if(head!="default")foreach(var value in head.Split(','))
                if(!int.TryParse(value,NumberStyles.None,CultureInfo.InvariantCulture,out var index)||index>=document.Edges.Count) {
                    Error("Agentflow linkStyle must select an existing zero-based edge index or 'default'.");return true;
                }
            Read(link.Styles,body,true);links.Add(link);return true;
        }
        var names=MermaidStyleParser.Names(head);
        foreach(var id in names) {
            var flow=document.Flows.Find(item=>item.Id==id);var edge=document.Edges.Find(item=>item.Id==id);nodes.TryGetValue(id,out var node);
            if(isClass) {
                var classes=MermaidStyleParser.Names(body);
                if(flow!=null)Add(flow.Classes,classes);
                if(edge!=null)Add(edge.Classes,classes);
                if(node!=null)Add(node.Classes,classes);
                continue;
            }
            if(edge!=null) {
                MermaidParserUtilities.Add(result,span,MermaidDiagnosticSeverity.Warning,
                    "Agentflow named-edge style is retained without application; use class or linkStyle for edge paints.",MermaidDiagnosticCodes.UnsupportedStatement);
                continue;
            }
            if(flow!=null){Read(flow.Styles,body,false,flow);continue;}
            if(node==null) {
                node=new MermaidAgentflowNode(id,span);nodes.Add(id,node);document.Nodes.Add(node);MermaidResourceGuard.ReserveElements(1,span);
            }
            Read(node.Styles,body,false,node);
        }
        return true;
        void Read(Dictionary<string,string> styles,string declarations,bool edge,MermaidAstNode? target=null)=>MermaidStyleParser.Read(styles,declarations,span,result,"Agentflow",edge,
            nativeProperty:(property,value)=> {
                if (!edge && property == "opacity" && target != null) target.SurfaceOpacitySpan = span;
                return edge ? MermaidNativeStyles.SupportsEdge(property,value) : MermaidTopologySurfaceElements.DeferredSurface(property,value);
            });
        void Add(List<string> target,List<string> classes){MermaidResourceGuard.ReserveElements(classes.Count,span);target.AddRange(classes);}
        void Error(string message)=>MermaidParserUtilities.Add(result,span,MermaidDiagnosticSeverity.Error,message,MermaidDiagnosticCodes.InvalidStatement);
    }

    private static void ApplyStyles(MermaidAgentflowDocument document,List<MermaidFlowchartLinkStyle> links,MermaidParseResult<MermaidDocument> result) {
        var definitions=MermaidStyleParser.Definitions(document.ClassDefinitions);
        foreach(var node in document.Nodes)Apply(node,node.Classes,node.Styles,true,MermaidTopologySurfaceElements.Supports(document,node));
        foreach(var flow in document.Flows)Apply(flow,flow.Classes,flow.Styles,false,false);
        foreach(var edge in document.Edges) {
            var resolved=MermaidStyleParser.Resolve(definitions,edge.Classes,edge.Styles,edge.Span,includeDefault:false);
            edge.Styles.Clear();MermaidStyleParser.Copy(edge.Styles,resolved);
        }
        MermaidFlowchartLinkStyle? defaults=null;var selected=new Dictionary<int,MermaidFlowchartLinkStyle>();
        foreach(var link in links) {
            if(link.Selector=="default"){defaults=link;continue;}
            foreach(var value in link.Selector.Split(','))selected[int.Parse(value,NumberStyles.None,CultureInfo.InvariantCulture)]=link;
        }
        for(var index=0;index<document.Edges.Count;index++) {
            var edge=document.Edges[index];if(defaults!=null)MermaidStyleParser.Copy(edge.Styles,defaults.Styles,edge.Span);
            if(selected.TryGetValue(index,out var link))MermaidStyleParser.Copy(edge.Styles,link.Styles,edge.Span);
        }
        void Apply(MermaidAstNode target,List<string> classes,Dictionary<string,string> styles,bool defaultsForNode,bool supported) {
            var resolved=MermaidStyleParser.Resolve(definitions,classes,styles,target.Span,defaultsForNode);styles.Clear();MermaidStyleParser.Copy(styles,resolved);
            MermaidTopologySurfaceElements.Diagnose(target,styles,supported,"Agentflow",result);
        }
    }
}
