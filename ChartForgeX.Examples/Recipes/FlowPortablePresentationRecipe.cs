using System;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

/// <summary>Shows an editable native flow retaining its source, caller frame and complete presentation.</summary>
public static class FlowPortablePresentationRecipe {
    /// <summary>Creates a static workflow. An optional caller-owned native font uses the existing FontSpec owner.</summary>
    public static VisualArtifact Create(string variant, VisualThemeMode mode, FontSpec? font = null) {
        if (variant is not ("compact" or "wide")) throw new ArgumentException("Unknown flow recipe size.", nameof(variant));
        var width = variant == "compact" ? 640 : 1000; var height = variant == "compact" ? 520 : 620;
        var flow = FlowArtifact.Create("portable-review-flow").WithTitle("Request review").WithSubtitle("One editable workflow")
            .WithSize(width,height).AddLane("review","Review").AddLane("delivery","Delivery")
            .AddStep("receive","Receive request",FlowArtifactStepKind.Start,"review")
            .AddStep("check","Check policy",FlowArtifactStepKind.Process,"review",VisualStatus.Positive)
            .AddStep("publish","Publish result",FlowArtifactStepKind.End,"delivery")
            .AddConnector("receive","check","Review",FlowArtifactConnectorKind.Flow,VisualLinkDirection.Forward)
            .AddConnector("check","publish","Approved",FlowArtifactConnectorKind.Flow,VisualLinkDirection.Forward);
        flow.ConfigureStep("check",step=>{step.Width=200;step.Height=96;step.Subtitle="Keep the source facts";});
        var theme=VisualTheme.Graphite();var frame=new VisualFrame(title:"Review workflow",subtitle:"The caller heading stays separate from the model title",showLegend:false,transparentBackground:true);
        var context=new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width,height),new ChartPadding(30,26,34,28)),theme,mode,frame,font??theme.Typography.ToFontSpec());
        return flow.Prepare(context).ToArtifact();
    }
}
