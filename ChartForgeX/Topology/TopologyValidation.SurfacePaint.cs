namespace ChartForgeX.Topology;

public sealed partial class TopologyChartValidator {
    internal TopologyValidationResult ValidateSurfacePaintInputs(TopologyChart chart) {
        var result=new TopologyValidationResult();ValidateSurfacePaints(chart,result);return result;
    }
    private static void ValidateSurfacePaints(TopologyChart chart,TopologyValidationResult result) {
        foreach(var node in chart.Nodes)if(TopologySurfacePaintRules.Error(node.StrokeWidth,node.Opacity,node.SurfaceOpacity,node.DashPattern) is {} error)
            Add(result,"node-surface-paint","Node '"+node.Id+"' "+error+".",node.Id);
        foreach(var node in chart.Nodes) if (TopologySurfacePaintRules.BorderColorError(node.BorderColor) is {} colorError)
            Add(result,"node-border-color","Node '"+node.Id+"' "+colorError+".",node.Id);
        foreach(var group in chart.Groups) {
            if(TopologySurfacePaintRules.Error(group.StrokeWidth,group.Opacity,group.SurfaceOpacity,group.DashPattern) is {} error)
                Add(result,"group-surface-paint","Group '"+group.Id+"' "+error+".",group.Id);
            if(TopologySurfacePaintRules.HasOverrides(group.StrokeWidth,group.Opacity,group.SurfaceOpacity,group.DashPattern)
                &&(chart.LayoutMode==TopologyLayoutMode.Geographic||group.Presentation==TopologyGroupPresentation.Compartment))
                Add(result,"group-surface-paint-presentation","Nondefault group surface paints require a standard container outside Geographic layout.",group.Id);
        }
        foreach(var edge in chart.Edges)if(TopologySurfacePaintRules.DashError(edge.DashPattern) is {} error)
            Add(result,"edge-"+TopologySurfacePaintRules.DashCode(edge.DashPattern),"Edge '"+edge.Id+"' "+error+".",edge.Id);
    }
}
