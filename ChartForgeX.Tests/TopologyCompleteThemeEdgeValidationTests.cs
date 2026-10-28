using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyCompleteThemeEdgeValidationTests {
    [Theory] [InlineData("series")] [InlineData("axis")] [InlineData("label")]
    public void CapturedDerivedInkBoundsUseAuthoritativeThemeValuesRatherThanOrdinaryFallbacks(string field) {
        var prepared=Source();var envelope=prepared.ToArtifact().ToInterchangeEnvelope();var svg=prepared.ToSvg();var png=prepared.ToPng();
        TopologyCompleteThemePortableTests.Capture("theme-edge-limit-"+field,svg,png,envelope.ToJson());
        Assert.NotNull(envelope.Edges[0].Topology!.PreparedEdgePlan);
        envelope.Presentation!.Theme!.Definition=VisualTheme.Graphite().Configure(theme=>{
            if(field=="series")theme.SeriesStrokeWidth=1e12;if(field=="axis")theme.AxisStrokeWidth=1e12;
            if(field=="label")theme.Typography=new VisualTypography(dataLabelSize:1e12);
        });
        // This legacy observation remains small: a complete native definition is authoritative.
        envelope.Presentation.Theme.DataLabelSize=12;
        Assert.Throws<ArgumentOutOfRangeException>(()=>envelope.Validate());Assert.Equal(svg,prepared.ToSvg());Assert.Equal(png,prepared.ToPng());
    }

    [Fact]
    public void NullAndExplicitOrdinaryThemeDefinitionsKeepTheExistingCompletedEdgeAdmission() {
        var prepared=Source();var ordinary=prepared.ToArtifact().ToInterchangeEnvelope();ordinary.Validate();var json=ordinary.ToJson();var png=prepared.ToPng();
        TopologyCompleteThemePortableTests.Capture("theme-edge-limit-ordinary",prepared.ToSvg(),png,json);
        var explicitTheme=VisualArtifactInterchangeEnvelope.FromJson(json);explicitTheme.Presentation!.Theme!.Definition=VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family);
        explicitTheme.Validate();Assert.NotEmpty(explicitTheme.ToJson());Assert.Equal(json,ordinary.ToJson());Assert.Equal(png,prepared.ToPng());
    }

    private static PreparedVisual Source() {
        var chart=TopologyExplicitLabelMarkerTests.Source().WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family));
        chart.Edges[0].TextStyle=null;chart.Edges[0].SourceMarkerStyle=null;chart.Edges[0].TargetMarkerStyle=null;chart.Edges[0].StrokeWidth=null;
        return chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(400,280),20),chart.Theme),
            new TopologyRenderOptions{IncludeTitle=false,IncludeLegend=false,FitContentToViewport=true});
    }
}
