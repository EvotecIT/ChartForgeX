using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class ZenUmlPresentationDepthTests {
    internal static string Fixture(string name)=>File.ReadAllText(Path.Combine(TestRepository.Root,"tests","mermaid-conformance","external-plugins","zenuml",name+".mmd"));
    internal static MermaidRenderOptions Options(int width,bool dark)=>new(){Sequence=new MermaidSequenceRenderOptions{Width=width,Height=900},
        Presentation=new MermaidPresentationOptions{FontFamily=BrowserProofFonts.Family,ThemeMode=dark?VisualThemeMode.Dark:VisualThemeMode.Light}};
    internal static SequenceArtifact Model(string fixture,int width=960,bool dark=false) {
        var result=MermaidRenderer.Render((dark?"%%{init: {'theme':'dark'}}%%\n":"")+Fixture(fixture),Options(width,dark));
        Assert.Empty(result.Diagnostics);return Assert.IsType<SequenceArtifact>(result.Artifact!.Model);
    }
    [Fact]
    public void FiniteSourcePresentationAndReferenceTargetsLowerWithoutNoteFallbackOrSourceIdentityLoss() {
        var parsed=new MermaidParser().Parse(Fixture("zenuml-native-presentation"));Assert.Empty(parsed.Diagnostics);
        var document=Assert.IsType<MermaidZenUmlDocument>(parsed.Document);
        var source=document.Participants.Single(item=>item.Id=="API");Assert.Equal(3,source.Span.Line);
        var sequence=document.ToSequenceArtifact();var api=sequence.Participants.Single(item=>item.Id=="API");
        Assert.Equal("Service",api.Stereotype);Assert.Equal("🚀",api.Symbol);Assert.Equal(176,api.MinimumWidth);Assert.Equal("#336699",api.Fill!.Value.ToCss());
        Assert.Equal("API",api.Metadata["mermaid.id"]);Assert.Equal("3",api.Metadata["mermaid.source.line"]);
        Assert.Equal(new[]{"API","Store"},sequence.Blocks.Single(item=>item.Kind==SequenceArtifactBlockKind.Reference).ParticipantIds);
        Assert.Contains(sequence.Blocks,item=>item.Kind==SequenceArtifactBlockKind.Divider&&item.Text=="Next phase");
        Assert.DoesNotContain(sequence.Notes,item=>item.Text.Contains("Login",StringComparison.Ordinal)||item.Text=="Next phase");
        Assert.Equal(8,(int)SequenceArtifactBlockKind.ParOver);Assert.Equal(9,(int)SequenceArtifactBlockKind.Reference);Assert.Equal(10,(int)SequenceArtifactBlockKind.Divider);
        source.Attributes["width"]="999";source.Attributes["stereotype"]="Mutated";
        Assert.Equal(176,api.MinimumWidth);Assert.Equal("Service",api.Stereotype);
    }
    [Fact]
    public void UnresolvedShortcodesRemainLocatedLiteralDiagnosticsAndStrictPolicyRejects() {
        const string source="zenuml\nparticipant [unlisted_native_symbol] A\nA.work()";
        var parsed=new MermaidParser().Parse(source);Assert.False(parsed.HasErrors);
        var diagnostic=Assert.Single(parsed.Diagnostics);Assert.Equal(2,diagnostic.Span.Line);Assert.Equal(MermaidDiagnosticCodes.UnsupportedStatement,diagnostic.Code);
        Assert.Equal("unlisted_native_symbol",Assert.IsType<MermaidZenUmlDocument>(parsed.Document).ToSequenceArtifact().Participants.Single(item=>item.Id=="A").Symbol);
        Assert.Null(MermaidRenderer.Render(source,new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject}).Artifact);
    }
    [Theory]
    [InlineData("ref(Login,A,A)")] [InlineData("ref(Login,,A)")] [InlineData("participant A 0")]
    public void ContradictoryFiniteSourceFactsFailWithLocatedDiagnostics(string body) {
        var result=new MermaidParser().Parse("zenuml\n"+body);Assert.True(result.HasErrors);Assert.Contains(result.Diagnostics,item=>item.Span.Line==2);
    }
    [Theory]
    [InlineData("zenuml-native-presentation",640,false)] [InlineData("zenuml-native-presentation",640,true)]
    [InlineData("zenuml-native-presentation",1100,false)] [InlineData("zenuml-native-presentation",1100,true)]
    [InlineData("zenuml-native-presentation-order",640,false)] [InlineData("zenuml-native-presentation-order",640,true)]
    [InlineData("zenuml-native-presentation-order",1100,false)] [InlineData("zenuml-native-presentation-order",1100,true)]
    public void NativeSvgPngAndPortableFactsSharePresentationBounds(string fixture,int width,bool dark) {
        var model=Model(fixture,width,dark);var artifact=model.ToVisualArtifact();var svg=artifact.ToSvg();var png=artifact.ToPng();var json=artifact.ToInterchangeJson();
        Capture("zen-native-"+fixture+"-"+width+(dark?"-dark":"-light"),svg,png,json);
        var xml=XDocument.Parse(svg);Assert.Contains(xml.Descendants(),item=>(string?)item.Attribute("data-cfx-role")=="sequence-reference");
        Assert.Contains(xml.Descendants(),item=>(string?)item.Attribute("data-cfx-role")=="sequence-divider");
        Assert.DoesNotContain(xml.Descendants(),item=>item.Name.LocalName is "script" or "foreignObject");
        var shadows=xml.Descendants().Where(item=>item.Name.LocalName=="image").ToArray();
        Assert.NotEmpty(shadows);
        Assert.All(shadows,item=>{Assert.Equal("true",item.Attribute("aria-hidden")?.Value);Assert.Equal("none",item.Attribute("pointer-events")?.Value);Assert.StartsWith("data:image/png;base64,",item.Attribute("href")!.Value);});
        var nativeSvg=SequencePreparedCompiler.PrepareDefault(model).ToSvg(new VisualSvgOptions(effectProfile:VisualSvgEffectProfile.NativeFilters));
        Capture("zen-native-filter-"+fixture+"-"+width+(dark?"-dark":"-light"),nativeSvg,png,json);
        var native=XDocument.Parse(nativeSvg);
        Assert.DoesNotContain(native.Descendants(),item=>item.Name.LocalName=="image");
        Assert.Equal(xml.Descendants().Count(item=>item.Name.LocalName=="text"),native.Descendants().Count(item=>item.Name.LocalName=="text"));
        Assert.True(png.Length>64);
        var prepared=model.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(Math.Max(width,1100),1600),model.Padding),model.Theme,model.ThemeMode));
        var facts=prepared.ToArtifact().ToInterchangeEnvelope();var api=facts.Nodes.Single(item=>item.Extensions.TryGetValue("mermaid.id",out var id)&&id=="API");
        Assert.True(api.Width>=api.Sequence!.MinimumWidth);Assert.NotNull(api.Sequence.Fill);Assert.Equal(2,facts.Version);
    }
    [Fact]
    public void EqualMessageBoundaryKeepsInsideReferenceThenDeactivationThenOutsideDividerAndReference() {
        var model=Model("zenuml-native-presentation-order");var facts=model.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(1200,1600)),model.Theme,model.ThemeMode)).ToArtifact().ToInterchangeEnvelope();
        var inner=facts.Annotations.Single(item=>item.Text=="Inside call");var outer=facts.Annotations.Single(item=>item.Text=="Outside call");
        var divider=facts.Annotations.Single(item=>item.Text=="After return");
        var end=facts.Annotations.First(item=>item.Sequence?.ActivationState==false&&item.Extensions["mermaid.source.line"]=="4");
        double Y(VisualArtifactInterchangeAnnotation a)=>double.Parse(a.Extensions["chartforgex.bounds.y"],CultureInfo.InvariantCulture);
        double Height(VisualArtifactInterchangeAnnotation a)=>double.Parse(a.Extensions["chartforgex.bounds.height"],CultureInfo.InvariantCulture);
        Assert.Equal(inner.StartIndex,end.StartIndex);Assert.Equal(end.StartIndex,divider.StartIndex);Assert.Equal(divider.StartIndex,outer.StartIndex);
        Assert.True(Y(inner)+Height(inner)<=Y(end)+2);Assert.True(Y(end)+2<Y(divider));Assert.True(Y(divider)+Height(divider)<Y(outer));
        var restored=VisualArtifactInterchangeEnvelope.FromJson(facts.ToJson()).ToSequenceArtifact();
        var again=restored.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(1200,1600)),restored.Theme,restored.ThemeMode)).ToArtifact().ToInterchangeEnvelope();
        Assert.Equal(facts.Annotations.Select(a=>a.Extensions.GetValueOrDefault("chartforgex.sequence.sourceOrder")),again.Annotations.Select(a=>a.Extensions.GetValueOrDefault("chartforgex.sequence.sourceOrder")));
        Assert.Equal(facts.Annotations.Select(Y),again.Annotations.Select(Y));
    }
    [Fact]
    public void ExplicitUndersizedContextRejectsRatherThanSilentlyShrinkingTheAuthoredMinimum() {
        var model=Model("zenuml-native-presentation");model.Participants.Single(item=>item.Id=="API").MinimumWidth=640;
        Assert.Throws<NotSupportedException>(()=>model.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(600,1600)),model.Theme)));
        Assert.Contains("sequence-reference",model.ToSvg(),StringComparison.Ordinal);
    }
    internal static void Capture(string name,string svg,byte[] png,string json) {
        if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not {Length:>0} output)return;
        Directory.CreateDirectory(output);var path=Path.Combine(output,name);File.WriteAllText(path+".svg",svg);File.WriteAllBytes(path+".png",png);File.WriteAllText(path+".json",json);
    }
}
