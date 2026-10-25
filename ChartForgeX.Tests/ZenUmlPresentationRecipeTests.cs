using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ZenUmlPresentationRecipeTests {
    [Theory]
    [InlineData("presentation","compact",false)] [InlineData("presentation","compact",true)]
    [InlineData("presentation","wide",false)] [InlineData("presentation","wide",true)]
    [InlineData("order","compact",false)] [InlineData("order","compact",true)]
    [InlineData("order","wide",false)] [InlineData("order","wide",true)]
    public void CompleteEmittedConsumerProgramUsesTheSameCompiledFiniteNativeRecipe(string family,string variant,bool dark) {
        var mode=dark?VisualThemeMode.Dark:VisualThemeMode.Light;var artifact=ZenUmlPresentationRecipe.Create(family,variant,mode);
        var program=ExampleRecipeSource.EmitZenUmlPresentation(family,variant,mode);
        Assert.Contains("var artifact=ZenUmlPresentationRecipe.Create",program,StringComparison.Ordinal);
        Assert.DoesNotContain("ChartForgeX.Examples",program,StringComparison.Ordinal);Assert.DoesNotContain("node_modules",program,StringComparison.Ordinal);
        Assert.Contains("File.WriteAllBytes",program,StringComparison.Ordinal);Assert.Contains("ToInterchangeJson",program,StringComparison.Ordinal);
        var svg=artifact.ToSvg();var png=artifact.ToPng();var json=artifact.ToInterchangeJson();var html=artifact.ToHtmlPage();
        if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is {Length:>0} output) {
            Directory.CreateDirectory(output);var path=Path.Combine(output,"zen-program-"+family+"-"+variant+(dark?"-dark":"-light"));
            File.WriteAllText(path+".Program.cs",program);File.WriteAllText(path+".expected.svg",svg);File.WriteAllBytes(path+".expected.png",png);
            File.WriteAllText(path+".expected.html",html);File.WriteAllText(path+".expected.json",json);
        }
    }
}
