using System;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
namespace ChartForgeX.VisualArtifacts;
internal sealed partial class SequencePreparedLayout {
    private sealed class FragmentHeaderAllocation {
        internal string Keyword=string.Empty,Title=string.Empty;
        internal ChartRect Tab,TitleBounds;
        internal double Height;
    }
    private FragmentHeaderAllocation? AllocateFragmentHeader(SequenceArtifactBlock block,ChartRect bounds,VisualSceneBuilder builder,double size) {
        var policy=Presentation.Layout;if(policy?.FragmentTabMinimumWidth==null&&policy?.FragmentTabMinimumHeight==null)return null;
        var padding=policy?.FragmentLabelPadding??6;var keyword=block.Kind==SequenceArtifactBlockKind.ParOver?"par":block.Kind.ToString().ToLowerInvariant();
        var label=Presentation.Measure(keyword,builder,SequenceTextRole.Fragment,size);var width=Math.Max(policy?.FragmentTabMinimumWidth??0,label.Width+padding*2);
        var tabHeight=Math.Max(policy?.FragmentTabMinimumHeight??0,label.Height+padding*2);var available=bounds.Width-width-padding*2;
        if(width>bounds.Width||block.Text.Length>0&&available<=0)throw new NotSupportedException("A measured fragment tab requires a wider native allocation for its complete title.");
        var title=block.Text.Length==0?string.Empty:Presentation.Wrap(block.Text,available,builder,SequenceTextRole.Fragment,size);
        var titleHeight=title.Length==0?0:Presentation.Measure(title,builder,SequenceTextRole.Fragment,size).Height;var height=Math.Max(tabHeight,titleHeight+padding*2);
        return new FragmentHeaderAllocation{Keyword=keyword,Title=title,Height=height,Tab=new ChartRect(bounds.X,bounds.Y,width,tabHeight),TitleBounds=new ChartRect(bounds.X+width+padding,bounds.Y+(height-titleHeight)/2,Math.Max(0,available),titleHeight)};
    }
}
