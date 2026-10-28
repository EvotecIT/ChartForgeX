using System;
using System.Linq;
using System.Globalization;
using ChartForgeX.SvgRaster;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeValidation {
    private static void ValidateSequencePresentation(VisualArtifactInterchangeEnvelope envelope) {
        var sequence = envelope.Sequence;
        if(sequence!=null) {
            if((sequence.Theme!=null)!=sequence.ThemeMode.HasValue)throw new ArgumentException("Sequence theme capture requires its matching palette mode.");
            if(sequence.ThemeMode.HasValue&&!Enum.IsDefined(typeof(ChartForgeX.Themes.VisualThemeMode),sequence.ThemeMode.Value))throw new ArgumentOutOfRangeException(nameof(sequence));
            if(sequence.Theme!=null && !SequencePresentationPolicy.IsCaptured(envelope))
                throw new ArgumentException("Sequence native theme capture requires a nonempty presentation policy.");
        }
        var typography = sequence?.Typography;
        if (typography == null) return;
        ValidateDetailTextStyle(typography.Participant); ValidateDetailTextStyle(typography.Message); ValidateDetailTextStyle(typography.Note);
        ValidateDetailTextStyle(typography.Fragment); ValidateDetailTextStyle(typography.Number);
        // Immutable allocation constructors and canonical text setters own finite/positive/enum validation.
    }

    private static void ValidateSequenceParticipantPresentation(VisualArtifactInterchangeSequenceNode node) {
        foreach(var link in node.NavigationLinks) {
            if(link==null)throw new ArgumentException("Sequence navigation cannot contain null records.");
            Text(link.Label,"sequence navigation label");Text(link.Href,"sequence navigation target");
        }
        OptionalText(node.Stereotype,"sequence stereotype");OptionalText(node.Symbol,"sequence symbol");OptionalText(node.Fill,"sequence fill");
        PositiveOptional(node.MinimumWidth,"sequence minimum participant width");
        foreach(var paint in new[]{node.Border,node.ActivationFill,node.ActivationBorder}){OptionalText(paint,"sequence participant paint");if(paint!=null&&!SvgRasterColor.TryParse(paint,out _))throw new ArgumentException("Sequence participant paint requires a literal native color.");}
        if(node.Fill!=null&&!SvgRasterColor.TryParse(node.Fill,out _))throw new ArgumentException("Sequence participant fill requires a literal supported native color.");
    }
    private static void ValidateSequenceOccurrence(VisualArtifactInterchangeAnnotation item,VisualArtifactInterchangeEnvelope envelope) {
        long? Read(string key) {
            if(!item.Extensions.TryGetValue("chartforgex.sequence."+key,out var text))return null;
            if(!long.TryParse(text,NumberStyles.None,CultureInfo.InvariantCulture,out var value)||value<0)throw new ArgumentException("Invalid sequence occurrence order.",nameof(envelope));
            return value;
        }
        var start=Read("sourceOrder");var end=Read("endSourceOrder");var close=Read("closeStep");
        if(end.HasValue!=close.HasValue||end.HasValue&&(!start.HasValue||end<=start)||close>100000||close<item.StartIndex)
            throw new ArgumentException("Sequence closing occurrence must follow its opening occurrence within native bounds.",nameof(envelope));
        if(end.HasValue&&item.Role is not (VisualArtifactInterchangeAnnotationRole.SequenceBlock or VisualArtifactInterchangeAnnotationRole.SequenceBranch)
            ||end.HasValue&&item.Sequence?.BlockKind is SequenceArtifactBlockKind.Reference or SequenceArtifactBlockKind.Divider)
            throw new ArgumentException("Instantaneous sequence annotations cannot carry closing occurrences.",nameof(envelope));
    }
    private static void ValidateInstantSequenceAnnotation(VisualArtifactInterchangeAnnotation item,VisualArtifactInterchangeEnvelope envelope) {
        var kind=item.Sequence!.BlockKind;
        if(kind is SequenceArtifactBlockKind.Reference or SequenceArtifactBlockKind.Divider) {
            if(!item.Sequence.IsEmpty||item.EndIndex!=null&&item.EndIndex!=item.StartIndex)
                throw new ArgumentException("Instantaneous sequence presentation requires an empty single-step annotation.",nameof(envelope));
            if(item.TargetIds.Count!=item.TargetIds.Distinct(StringComparer.Ordinal).Count())throw new ArgumentException("Reference targets must be distinct.",nameof(envelope));
            if(kind==SequenceArtifactBlockKind.Reference)foreach(var id in item.TargetIds) {
                var participant=envelope.Nodes.FirstOrDefault(node=>node.Id==id)?.Sequence;
                if(participant==null||participant.CreatedAtMessageIndex is int born&&item.StartIndex<=born||participant.DestroyedAtMessageIndex is int died&&item.StartIndex>died)
                    throw new ArgumentException("Reference targets must be sequence participants alive at the reference occurrence.",nameof(envelope));
            }
        }
        if(kind!=SequenceArtifactBlockKind.Reference&&item.TargetIds.Count>0)throw new ArgumentException("Only reference blocks may carry participant targets.",nameof(envelope));
    }
}
