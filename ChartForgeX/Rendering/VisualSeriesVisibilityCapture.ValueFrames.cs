using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualSeriesVisibilityCapture {
    /// <summary>Combines actual detached value geometry with the existing native isolated series painter; no primitive correspondence is guessed.</summary>
    internal static IReadOnlyList<PreparedVisual> ValueVisibilityFrames(PreparedVisual from,PreparedVisual to,int frameCount,CancellationToken cancellationToken=default) {
        if(from==null)throw new ArgumentNullException(nameof(from));if(to==null)throw new ArgumentNullException(nameof(to));if(frameCount<2||frameCount>16)throw new ArgumentOutOfRangeException(nameof(frameCount));cancellationToken.ThrowIfCancellationRequested();
        var first=from.Scene.VisibilityCapture;var last=to.Scene.VisibilityCapture;
        if(first==null||last==null||first._appearanceKey!=last._appearanceKey||first.ReflowSource!=null||last.ReflowSource!=null||first.View.Layout!=ChartVisibilityLayout.Preserve||last.View.Layout!=ChartVisibilityLayout.Preserve
            ||first.View.LabelLayering!=last.View.LabelLayering||first._labelsEnabled!=last._labelsEnabled
            ||first._labelsEnabled&&first.View.LabelLayering!=ChartVisibilityLabelLayering.GroupWithSeries
            ||!first.SeriesKeys.SequenceEqual(last.SeriesKeys)||!SameAxes(first._basis,last._basis))
            throw new NotSupportedException("Joint values/visibility requires compatible native Line/Bar Preserve views; enabled labels require explicit GroupWithSeries and the same full authored series/axis policies.");
        var a=first._basis.Scene.ValueCapture;var b=last._basis.Scene.ValueCapture;
        if(a==null||b==null||a.Plan.Kind!="cartesian-value"||b.Plan.Kind!=a.Plan.Kind||!VisualCartesianCompiler.SupportsJointSeriesValues(a.Plan)||!VisualCartesianCompiler.SupportsJointSeriesValues(b.Plan)||!a.Plan.SamePolicy(b.Plan))
            throw new NotSupportedException("Joint values/visibility requires the same ordered captured native value structure, domains, plot, paint and presentation.");
        var before=a.Plan.Readings.Select(row=>row.Value).ToArray();var after=b.Plan.Readings.Select(row=>row.Value).ToArray();
        if(!before.Where((value,index)=>value!=after[index]).Any())throw new NotSupportedException("Joint values/visibility changes at least one captured native reading.");
        var fromOpacity=first._series.Select(row=>first.View.IsHidden(row.Key)?0d:1d).ToArray();var toOpacity=last._series.Select(row=>last.View.IsHidden(row.Key)?0d:1d).ToArray();
        if(fromOpacity.SequenceEqual(toOpacity))throw new NotSupportedException("Joint values/visibility changes at least one absolute native series target.");
        const string scope="native-joint-values-visibility";
        if(Compose(before,fromOpacity,first.View).ToSvg(scope)!=from.ToSvg(scope)||Compose(after,toOpacity,last.View).ToSvg(scope)!=to.ToSvg(scope))
            throw new NotSupportedException("Joint native value and visibility endpoints must exactly reconstruct their captured source and destination scenes.");
        var frames=new PreparedVisual[frameCount];frames[0]=from;frames[frames.Length-1]=to;
        for(var frame=1;frame<frames.Length-1;frame++) {
            cancellationToken.ThrowIfCancellationRequested();var ratio=frame/(double)(frameCount-1);var values=new double[before.Length];var opacity=new double[fromOpacity.Length];
            for(var index=0;index<values.Length;index++)values[index]=ChartMath.InterpolateRange(before[index],after[index],ratio);
            for(var index=0;index<opacity.Length;index++)opacity[index]=ChartMath.InterpolateRange(fromOpacity[index],toOpacity[index],ratio);
            frames[frame]=Compose(values,opacity,last.View);
        }
        cancellationToken.ThrowIfCancellationRequested();return Array.AsReadOnly(frames);
        PreparedVisual Compose(double[] values,double[] opacity,ChartVisibilityView view) {
            var full=a.Frame(first._basis,values);var capture=new VisualSeriesVisibilityCapture(full,view,labelsEnabled:first._labelsEnabled);
            return first.ApplyFreshVisibilityAppearance(capture.Paint(full,opacity,view));
        }
    }
}
