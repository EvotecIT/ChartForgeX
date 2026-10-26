using System;
using System.Linq;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>After-only unbounded-options controls; never run these against the unguarded before candidate.</summary>
public sealed class SvgRasterPatternRecursionPostTests {
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void OrdinaryNullOptionsAlsoRejectCyclicPaintAndStrictModeReportsItsLocatedLoss(bool mutual) {
        var source=PatternRecursionFixture.Cycle(mutual);var result=SvgRasterizer.Rasterize(source);
        Assert.Contains(result.Diagnostics,item=>item.Code=="SFR001");
        Assert.All(result.Image.Pixels.Where((_,index)=>index%4==3),alpha=>Assert.Equal((byte)0,alpha));
        Assert.Throws<NotSupportedException>(()=>SvgRasterizer.Rasterize(source,strict:true));
    }

    [Theory]
    [InlineData(8,true)] [InlineData(9,false)]
    public void ExistingResourceDepthBoundsNestedPaintWhileAcyclicReuseReleasesItsActiveScope(int count,bool valid) {
        var patterns=string.Concat(Enumerable.Range(0,count).Select(index=>
            "<pattern id='"+(index==0?"a":"p"+index)+"' patternUnits='userSpaceOnUse' width='1' height='1'><rect width='1' height='1' fill='"
            +(index+1==count?"red":"url(#p"+(index+1)+")")+"'/></pattern>"));
        var source=PatternRecursionFixture.Wrap(patterns);var result=SvgRasterizer.Rasterize(source);
        PatternRecursionFixture.SaveSource("pattern-depth-"+count,source);
        if(valid) {
            Assert.Empty(result.Diagnostics);
            for(var pixel=0;pixel<4;pixel++)Assert.Equal(new byte[]{255,0,0,255},result.Image.Pixels.Skip(pixel*4).Take(4));
            var reused=source.Replace("</svg>","<rect width='1' height='1' fill='url(#a)'/></svg>",StringComparison.Ordinal);
            Assert.Empty(SvgRasterizer.Rasterize(reused,strict:true).Diagnostics);
        } else Assert.Contains(result.Diagnostics,item=>item.Code=="SFR001");
    }
}
