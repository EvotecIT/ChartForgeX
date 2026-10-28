using System;
using System.IO;
using System.Linq;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Real public source fixtures; tiny existing document budgets bound the uncorrected recursive path.</summary>
public sealed class SvgRasterPatternRecursionCausalTests {
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void SelfAndMutualPaintReferencesFinishAtALocatedCycleRatherThanExhaustingTheCanvasBudget(bool mutual) {
        var source=PatternRecursionFixture.Cycle(mutual);
        PatternRecursionFixture.SaveSource("pattern-cycle-"+mutual,source);
        // The uncorrected path reaches the80-byte canvas ceiling after a small number of1x1 tiles;
        // it cannot run to an unbounded stack during the before proof.
        var result=SvgRasterizer.Rasterize(source,filters:new SvgRasterFilterOptions(maximumLiveBytes:80,maximumWorkSamples:4096));
        Assert.Contains(result.Diagnostics,item=>item.Code=="SFR001"&&item.Message.Contains("pattern",StringComparison.OrdinalIgnoreCase));
        Assert.All(result.Image.Pixels.Where((_,index)=>index%4==3),alpha=>Assert.Equal((byte)0,alpha));
    }
}

internal static class PatternRecursionFixture {
    internal static string Cycle(bool mutual)=>Wrap("<pattern id='a' patternUnits='userSpaceOnUse' width='1' height='1'><rect width='1' height='1' fill='url(#"+(mutual?"b":"a")+")'/></pattern>"
        +(mutual?"<pattern id='b' patternUnits='userSpaceOnUse' width='1' height='1'><rect width='1' height='1' fill='url(#a)'/></pattern>":""));
    internal static string Wrap(string definitions)=>"<svg xmlns='http://www.w3.org/2000/svg' width='2' height='2'><defs>"+definitions+"</defs><rect width='2' height='2' fill='url(#a)'/></svg>";
    internal static void SaveSource(string stem,string source) {
        var directory=Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");if(string.IsNullOrWhiteSpace(directory))return;
        Directory.CreateDirectory(directory);File.WriteAllText(Path.Combine(directory,stem+".source.svg"),source);
    }
}
