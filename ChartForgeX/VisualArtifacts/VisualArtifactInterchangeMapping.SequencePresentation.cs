using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    private static void MapSequenceOccurrence(VisualArtifactInterchangeAnnotation target,long? order,long? end=null,int? close=null) {
        if(order.HasValue)target.Extensions["chartforgex.sequence.sourceOrder"]=order.Value.ToString(CultureInfo.InvariantCulture);
        if(end.HasValue)target.Extensions["chartforgex.sequence.endSourceOrder"]=end.Value.ToString(CultureInfo.InvariantCulture);
        if(close.HasValue&&end.HasValue)target.Extensions["chartforgex.sequence.closeStep"]=close.Value.ToString(CultureInfo.InvariantCulture);
    }
    private static bool HasSequenceCapturedPolicy(SequenceArtifact source) => SequencePresentationPolicy.IsCaptured(source);
    private static bool HasSequenceNativePresentation(SequenceArtifact source)=>
        HasSequenceCapturedPolicy(source)
        ||source.Participants.Any(item=>item.Card!=null||item.Stereotype!=null||item.Symbol!=null||item.Fill.HasValue||item.MinimumWidth.HasValue)
        ||source.Blocks.Any(item=>SequencePreparedLayout.IsInstant(item.Kind));
    private static void MapSequenceNativePresentation(VisualArtifactInterchangeEnvelope target,SequenceArtifact source,VisualRenderContext? context=null) {
        if(!HasSequenceNativePresentation(source))return;
        if(context!=null&&HasSequenceCapturedPolicy(source)) {
            var captured = new SequencePreparedPresentation(source,context);
            var presentation=target.Sequence??throw new InvalidOperationException("Sequence presentation requires its existing typed root.");
            presentation.Theme=context.Theme;presentation.ThemeMode=context.ThemeMode;
            presentation.Layout=(source.Layout??new SequenceLayoutPolicy()).WithCanvasPadding(context.Layout.PaddingEdges);
            presentation.Typography=captured.CaptureTypography(source.Typography);
        }
        var typography=(context?.Theme??source.Theme).Typography;
        target.Extensions["chartforgex.sequence.padding"]=(context?.Layout.Padding??source.Padding).ToString("R",CultureInfo.InvariantCulture);
        target.Extensions["chartforgex.sequence.dataLabelSize"]=typography.DataLabelSize.ToString("R",CultureInfo.InvariantCulture);
        target.Extensions["chartforgex.sequence.titleSize"]=typography.TitleSize.ToString("R",CultureInfo.InvariantCulture);
        target.Extensions["chartforgex.sequence.subtitleSize"]=typography.SubtitleSize.ToString("R",CultureInfo.InvariantCulture);
    }
}
