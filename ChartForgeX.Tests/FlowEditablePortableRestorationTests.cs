using ChartForgeX.Topology;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class FlowEditablePortableRestorationTests : IDisposable {
    [Theory]
    [InlineData(FlowArtifactLayoutMode.Layered,VisualThemeMode.Light)]
    [InlineData(FlowArtifactLayoutMode.Dense,VisualThemeMode.Dark)]
    [InlineData(FlowArtifactLayoutMode.Force,VisualThemeMode.Dark)]
    public void OrderedNativeFlowRestoresSourceDimensionsCompleteThemeAndFourInsetCallerFrame(FlowArtifactLayoutMode mode,VisualThemeMode themeMode) {
        TopologyOwnedFontPortableTests.Register();var flow=Source();flow.LayoutMode=mode;flow.Direction=FlowArtifactDirection.TopToBottom;
        var theme=VisualTheme.Graphite().WithFontFamily(TopologyOwnedFontPortableTests.Primary).Configure(builder=>{builder.Spacing=14;builder.AxisStrokeWidth=2.25;builder.Dark.Background=ChartColor.FromHex("#172431");builder.Light.Border=ChartColor.FromHex("#224466");});
        var role=new TextStyle{Font=FontSpec.FromFamily(TopologyOwnedFontPortableTests.Primary),FontSize=21,Color=ChartColor.FromHex("#447788"),TextCase=TextCaseTransform.Uppercase};
        var frame=new VisualFrame(title:"Caller H",subtitle:"Caller O",showLegend:false,transparentBackground:true,titleStyle:role,subtitleStyle:new TextStyle{Font=role.Font,FontSize=13});
        var context=new VisualRenderContext(new VisualLayoutOptions(new VisualSize(900,620),new ChartPadding(37,29,43,31)),theme,themeMode,frame);
        var before=flow.Prepare(context);TopologyPortableFontObservation.Capture("flow-"+mode+"-direct",before);TopologyOwnedFontPortableTests.Capture("flow-"+mode+"-direct",before);var saved=before.ToArtifact().ToInterchangeEnvelope();var json=saved.ToJson();
        FontRegistry.Clear();var restored=VisualArtifactInterchangeEnvelope.FromJson(json).ToFlowArtifact();var after=restored.ToVisualArtifact();Capture("flow-"+mode+"-restored",after);
        Assert.Equal("Source H",saved.Title);Assert.Equal("Source O",saved.Subtitle);Assert.Equal(flow.Title,restored.Title);Assert.Equal(flow.Subtitle,restored.Subtitle);Assert.Equal(mode,restored.LayoutMode);Assert.Equal(flow.Direction,restored.Direction);
        Assert.Equal(flow.Lanes.Select(lane=>lane.Id),restored.Lanes.Select(lane=>lane.Id));Assert.Equal(flow.Steps.Select(step=>(step.Id,step.Width,step.Height,step.Kind,step.LaneId)),restored.Steps.Select(step=>(step.Id,step.Width,step.Height,step.Kind,step.LaneId)));
        Assert.Equal(flow.Connectors.Select(edge=>(edge.Id,edge.SourceId,edge.TargetId,edge.Label,edge.Kind,edge.Direction,edge.Status)),restored.Connectors.Select(edge=>(edge.Id,edge.SourceId,edge.TargetId,edge.Label,edge.Kind,edge.Direction,edge.Status)));
        Assert.All(saved.Edges, edge => { Assert.Null(edge.SecondaryLabel); Assert.Null(edge.TertiaryLabel); });
        Assert.Equal(flow.ExportFormats,restored.ExportFormats);Assert.Equal("flow-value",restored.Metadata["flow-key"]);Assert.Equal("step-value",restored.Steps[0].Metadata["step-key"]);Assert.Equal("edge-value",restored.Connectors[0].Metadata["edge-key"]);
        Assert.Equal(theme.ToJson(),saved.Presentation!.Theme!.Definition!.ToJson());Assert.Equal(themeMode,saved.Presentation.Theme.Mode??VisualThemeMode.Light);
        if(themeMode==VisualThemeMode.Light)Assert.Null(saved.Presentation.Theme.Mode);else Assert.Equal(themeMode,saved.Presentation.Theme.Mode);
        Assert.Equal(themeMode,restored.PreparedPresentation!.Mode);Assert.True(frame.SameRequest(saved.Presentation.Frame!));
        Assert.Equal(new ChartPadding(37,29,43,31),saved.Flow!.ViewportPadding);Assert.NotNull(saved.Nodes[0].Flow!.AuthoredWidth);Assert.Equal(2,saved.Version);
        Assert.Equal(TopologyStaticPaintObservation.Paint(before.ToSvg()),TopologyStaticPaintObservation.Paint(after.ToSvg()));Assert.Equal(before.ToPng(),after.ToPng());
        role.FontSize=40;role.Font.Family="Changed caller";Assert.Equal(json,saved.ToJson());Assert.Equal(before.ToPng(),after.ToPng());
        // Clearing the explicit400 frame creates a new default700 heading request. Editable
        // continuation may resolve new requests from the host; explicitly retain this fixture's
        // bounded host policy rather than require arbitrary installed roots to fit the wire cap.
        TopologyOwnedFontPortableTests.Register();
        var edited=VisualArtifactInterchangeEnvelope.FromJson(json);edited.Presentation!.Frame=null;var withoutCaller=edited.ToFlowArtifact().ToVisualArtifact();Capture("flow-"+mode+"-source-heading",withoutCaller);
        Assert.Contains("Source H",withoutCaller.ToSvg(),StringComparison.Ordinal);Assert.DoesNotContain("CALLER H",withoutCaller.ToSvg(),StringComparison.Ordinal);
    }
    [Fact]
    public void FlowRequestLayoutInvalidatesAfterSizePaddingEditsAndExplicitContextRemainsAuthoritative() {
        TopologyOwnedFontPortableTests.Register();var flow=Source();var theme=VisualTheme.Graphite().WithFontFamily(TopologyOwnedFontPortableTests.Primary);
        var before=flow.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(900,620),new ChartPadding(37,29,43,31)),theme,frame:new VisualFrame(title:"Caller H",showLegend:false)));
        var restored=VisualArtifactInterchangeEnvelope.FromJson(before.ToArtifact().ToInterchangeJson()).ToFlowArtifact();restored.Width=1100;restored.Height=720;restored.Padding=40;
        var edited=restored.ToVisualArtifact();Capture("flow-layout-edited",edited);var saved=edited.ToInterchangeEnvelope();Assert.Equal(1100,saved.Width);Assert.Equal(720,saved.Height);Assert.Equal(ChartPadding.All(40),saved.Flow!.ViewportPadding);
        var context=new VisualRenderContext(new VisualLayoutOptions(new VisualSize(1000,700),new ChartPadding(32,26,38,28)),theme,frame:new VisualFrame(title:"Explicit H",showLegend:false,transparentBackground:true));
        var overridden=restored.Prepare(context);TopologyOwnedFontPortableTests.Capture("flow-explicit-context",overridden);Assert.Contains(overridden.Scene.Nodes.OfType<VisualSceneText>().SelectMany(text=>text.Text.Lines),line=>line.Text=="Explicit H");Assert.DoesNotContain(overridden.Scene.Nodes,mark=>mark.Role=="background");
    }
    [Fact]
    public void FlowPortableAdmissionRejectsRealUnrepresentableFactsAndMalformedSourceDimensions() {
        var flow=Source();var saved=flow.ToVisualArtifact().ToInterchangeEnvelope();var json=saved.ToJson();Capture("flow-admission-base",flow.ToVisualArtifact());
        var bad=VisualArtifactInterchangeEnvelope.FromJson(json);bad.Nodes[0].Href="https://example.invalid/step";Assert.Throws<NotSupportedException>(()=>bad.ToFlowArtifact());
        bad=VisualArtifactInterchangeEnvelope.FromJson(json);bad.Edges[0].Order=bad.Edges[1].Order;Assert.Throws<NotSupportedException>(()=>bad.ToFlowArtifact());
        bad=VisualArtifactInterchangeEnvelope.FromJson(json);bad.Edges[0].SecondaryLabel="unsupported authored caption";Assert.Throws<NotSupportedException>(()=>bad.ToFlowArtifact());
        bad=VisualArtifactInterchangeEnvelope.FromJson(json);bad.Edges[0].TertiaryLabel="unsupported authored caption";Assert.Throws<NotSupportedException>(()=>bad.ToFlowArtifact());
        var raw=System.Text.Json.Nodes.JsonNode.Parse(json)!;raw["nodes"]![0]!["flow"]!["authoredWidth"]=-1;var malformed=raw.ToJsonString();Assert.ThrowsAny<ArgumentException>(()=>VisualArtifactInterchangeEnvelope.FromJson(malformed));
        var chart=TopologyOwnedFontPortableTests.Source();Assert.Throws<NotSupportedException>(()=>chart.Prepare().ToInterchangeEnvelope().ToFlowArtifact());
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void ExistingWrapperAccessibilityFactsStayDetachedAndFollowTheCurrentTopologyEditSemantics(bool decorative) {
        var flow=Source();var ordinary=flow.ToVisualArtifact();var ordinaryFacts=ordinary.ToInterchangeEnvelope();Capture("flow-accessibility-ordinary",ordinary);
        var artifact=flow.ToVisualArtifact();artifact.Accessibility.WithTextAlternative("Caller accessible name","Caller long description","pl-PL");artifact.Accessibility.IsDecorative=decorative;
        var saved=artifact.ToInterchangeEnvelope();var json=saved.ToJson();Capture("flow-accessibility-wrapper-"+decorative,artifact);
        var restored=VisualArtifactInterchangeEnvelope.FromJson(json).ToFlowArtifact();var after=restored.ToVisualArtifact();Capture("flow-accessibility-restored-"+decorative,after);var facts=after.ToInterchangeEnvelope();
        Assert.Equal("Caller accessible name",facts.AccessibleName);Assert.Equal("Caller long description",facts.AccessibleDescription);Assert.Equal("pl-PL",facts.Language);Assert.Equal(decorative,facts.IsDecorative);
        Assert.Equal(artifact.ToPng(),after.ToPng());Assert.NotEqual(saved.AccessibleName,ordinaryFacts.AccessibleName);Assert.Null(ordinaryFacts.Language);Assert.False(ordinaryFacts.IsDecorative);
        saved.AccessibleName="Changed detached envelope";restored.Title="Edited source H";var edited=restored.ToVisualArtifact();Capture("flow-accessibility-edited-"+decorative,edited);var editedFacts=edited.ToInterchangeEnvelope();
        Assert.Equal("Edited source H",editedFacts.Title);Assert.Equal("Caller accessible name",editedFacts.AccessibleName);Assert.Equal("Caller long description",editedFacts.AccessibleDescription);Assert.Equal("pl-PL",editedFacts.Language);Assert.Equal(decorative,editedFacts.IsDecorative);
        after.Accessibility.WithTextAlternative("Later wrapper name","Later wrapper description","en-GB");after.Accessibility.IsDecorative=!decorative;
        Capture("flow-accessibility-later-wrapper-"+decorative,after);var wrapperFacts=after.ToInterchangeEnvelope();
        Assert.Equal("Later wrapper name",wrapperFacts.AccessibleName);Assert.Equal("Later wrapper description",wrapperFacts.AccessibleDescription);Assert.Equal("en-GB",wrapperFacts.Language);Assert.Equal(!decorative,wrapperFacts.IsDecorative);
        var independent=restored.ToVisualArtifact().ToInterchangeEnvelope();Assert.Equal("Caller accessible name",independent.AccessibleName);Assert.Equal(decorative,independent.IsDecorative);
        Assert.Equal(json,VisualArtifactInterchangeEnvelope.FromJson(json).ToJson());
    }
    private static FlowArtifact Source() {
        var flow=FlowArtifact.Create("flow-restoration").WithTitle("Source H").WithSubtitle("Source O").WithSize(900,620)
            .AddLane("lane_a","H",VisualStatus.Positive,"#287A5F").AddLane("lane_b","O",VisualStatus.Warning,"#B58422")
            .AddStep("start","H",FlowArtifactStepKind.Start,"lane_a").AddStep("process","O",FlowArtifactStepKind.Process,"lane_a").AddStep("end","H",FlowArtifactStepKind.End,"lane_b")
            .AddConnector("start","process","H",FlowArtifactConnectorKind.Data,VisualLinkDirection.Forward,VisualStatus.Positive,"#245C86")
            .AddConnector("process","end","O",FlowArtifactConnectorKind.Flow,VisualLinkDirection.Bidirectional);
        flow.ConfigureStep("start",step=>{step.Width=160;step.Height=74;step.FitLabelToContent=false;step.Metadata["step-key"]="step-value";});
        flow.ConfigureStep("process",step=>{step.Width=200;step.Height=100;step.Subtitle="H";});flow.ConfigureConnector(0,edge=>{edge.Id="source_edge";edge.Metadata["edge-key"]="edge-value";});
        flow.Metadata["flow-key"]="flow-value";flow.ExportFormats=VisualArtifactExportFormat.Svg|VisualArtifactExportFormat.Png|VisualArtifactExportFormat.Json;return flow;
    }
    private static void Capture(string name,VisualArtifact value){
        if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not{Length:>0} output)return;
        Directory.CreateDirectory(output);var stem=Path.Combine(output,name);File.WriteAllText(stem+".svg",value.ToSvg());File.WriteAllBytes(stem+".png",value.ToPng());
        if(value.Model is FlowArtifact flow)TopologyPortableFontObservation.Capture(name,flow.Prepare(VisualExportRequest.ForFlow(flow).Context));
        File.WriteAllText(stem+".json",value.ToInterchangeJson());
    }
    public void Dispose(){FontRegistry.Clear();_=BrowserProofFonts.Family;}
}
