using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    /// <summary>Restores detached editable sequence facts from a validated typed portable envelope without consulting SVG.</summary>
    /// <param name="envelope">A Sequence-family envelope with authoritative participant IDs, message order and typed annotations.</param>
    /// <returns>A new native sequence with detached mutable records, metadata, presentation and source-ordered instantaneous notations.</returns>
    /// <remarks>Resolved geometry is an output observation; a new export reflows the editable facts through the existing native owner.
    /// Carried native padding and typography facts are restored. Legacy envelopes without those authoring facts retain the native constructor defaults;
    /// missing source text, arbitrary host layout policy and uncarried font resources are not reconstructed or claimed byte-identical.
    /// Envelope IDs remain authoritative; opaque source metadata never replaces endpoint identity.</remarks>
    /// <exception cref="NotSupportedException">A carried field has no faithful editable sequence representation.</exception>
    public static SequenceArtifact ToSequenceArtifact(this VisualArtifactInterchangeEnvelope envelope) {
        if(envelope==null)throw new ArgumentNullException(nameof(envelope));
        envelope.Validate();
        RequireEditableSourcePresentation(envelope);
        if(envelope.Family!=VisualArtifactInterchangeFamily.Sequence||envelope.Sequence==null)throw new NotSupportedException("Native sequence restoration requires a typed Sequence envelope.");
        if(envelope.Scenarios.Count>0||envelope.Presentation?.MapViewport!=null||envelope.Presentation?.Legend!=null)
            throw new NotSupportedException("Sequence reconstruction cannot reinterpret topology scenarios, map viewports or legends.");
        var nodes=envelope.Nodes.OrderBy(item=>item.Sequence!.Order).ToArray();
        if(!nodes.Select(item=>item.Sequence!.Order).SequenceEqual(Enumerable.Range(0,nodes.Length)))throw new NotSupportedException("Sequence participant order must be unique and contiguous.");
        if(nodes.Length==0)throw new NotSupportedException("A native sequence requires an existing participant.");
        var messages=envelope.Edges.OrderBy(item=>item.Order).ToArray();
        if(!messages.Select(item=>item.Order).SequenceEqual(Enumerable.Range(0,messages.Length)))throw new NotSupportedException("Sequence message order must be unique and contiguous.");
        ValidateSequenceRestorationIdentities(envelope,nodes,messages);
        var sequence=SequenceArtifact.Create(envelope.Id).WithTitle(envelope.Title).WithSubtitle(envelope.Subtitle)
            .WithSize(SourceBound(envelope,"width",envelope.Width),SourceBound(envelope,"height",envelope.Height));
        sequence.Padding=NativeNumber(envelope.Extensions,"padding",sequence.Padding,false);
        sequence.Layout=envelope.Sequence.Layout;sequence.Typography=envelope.Sequence.Typography;
        if(envelope.Sequence.Theme!=null) {
            sequence.Theme=envelope.Sequence.Theme;sequence.ThemeMode=envelope.Sequence.ThemeMode!.Value;
        } else {
        if(envelope.Presentation?.Theme is {} theme)sequence.Theme=RestoreTopologyTheme(theme);
        var ordinary=sequence.Theme.Typography;
        sequence.Theme=sequence.Theme.WithTypography(new VisualTypography(ordinary.Family,
            NativeNumber(envelope.Extensions,"titleSize",ordinary.TitleSize),NativeNumber(envelope.Extensions,"subtitleSize",ordinary.SubtitleSize),
            ordinary.AxisSize,ordinary.LegendSize,NativeNumber(envelope.Extensions,"dataLabelSize",ordinary.DataLabelSize),ordinary.ScalarValueSize,ordinary.CenterValueSize,
            tileLabelSize:ordinary.TileLabelSize,tileValueSize:ordinary.TileValueSize,tileDetailSize:ordinary.TileDetailSize));
        }
        sequence.Accessibility.Name=envelope.AccessibleName;sequence.Accessibility.Description=envelope.AccessibleDescription;
        sequence.Accessibility.Language=envelope.Language;sequence.Accessibility.IsDecorative=envelope.IsDecorative;
        CopyFacts(envelope.Extensions,sequence.Metadata);RestoreSequencePaint(sequence,envelope.Extensions);
        sequence.Paint.LifelineStroke=envelope.Sequence.LifelineStroke;
        sequence.Paint.NoteShadow = envelope.Sequence.NoteShadow;
        sequence.Paint.FragmentTabShadow = envelope.Sequence.FragmentTabShadow;
        sequence.Paint.BackgroundShadow = envelope.Sequence.BackgroundShadow;
        foreach(var source in nodes) {
            if(source.Kind!=source.Sequence!.Kind.ToString()||source.Subtitle!=null||source.Status!=null||source.Details.Count>0||source.Ports.Count>0||source.Metrics.Count>0
                ||source.IconId!=null||source.Symbol!=null||source.Badge!=null||source.Color!=null||source.BackgroundColor!=null||source.LabelColor!=null||source.Tooltip!=null)
                throw new NotSupportedException("A sequence participant carries unsupported or contradictory common presentation fields.");
            sequence.AddParticipant(source.Id,source.Label,source.Sequence.Kind);
            var target=sequence.Participants[sequence.Participants.Count-1];target.Href=source.Href;target.IsImplicit=source.Sequence.IsImplicit;
            target.NavigationLinks=source.Sequence.NavigationLinks;
            target.CreatedAtMessageIndex=source.Sequence.CreatedAtMessageIndex;target.DestroyedAtMessageIndex=source.Sequence.DestroyedAtMessageIndex;
            target.Stereotype=source.Sequence.Stereotype;target.Symbol=source.Sequence.Symbol;target.MinimumWidth=source.Sequence.MinimumWidth;
            target.Card = source.Sequence.Card?.Copy();
            target.Fill=source.Sequence.Fill==null?null:PortableColor(source.Sequence.Fill);
            target.SurfaceShadow = source.Sequence.SurfaceShadow;
            target.Border=source.Sequence.Border==null?null:PortableColor(source.Sequence.Border);target.ActivationFill=source.Sequence.ActivationFill==null?null:PortableColor(source.Sequence.ActivationFill);target.ActivationBorder=source.Sequence.ActivationBorder==null?null:PortableColor(source.Sequence.ActivationBorder);
            CopyFacts(source.Extensions,target.Metadata);
        }
        foreach(var source in messages) {
            if(source.Kind!="SequenceMessage"||source.Status!=null||source.SecondaryLabel!=null||source.TertiaryLabel!=null||source.SourceLabel!=null||source.TargetLabel!=null
                ||source.SourcePortId!=null||source.TargetPortId!=null||source.LabelColor!=null||source.Href!=null||source.Tooltip!=null||source.Metrics.Count>0)
                throw new NotSupportedException("A sequence message carries unrepresentable common fields.");
            sequence.AddMessage(source.SourceId,source.TargetId,source.Label,source.Sequence!.LineStyle,source.Sequence.Kind);
            var target=sequence.Messages[sequence.Messages.Count-1];target.ActivatesTarget=source.Sequence.ActivatesTarget;target.Deactivates=source.Sequence.Deactivates;
            target.Color=source.Color==null?null:PortableColor(source.Color);
            CopyFacts(source.Extensions,target.Metadata);
        }
        foreach(var group in envelope.Groups) {
            if(group.Kind!="SequenceParticipantGroup"||group.Subtitle!=null||group.Status!=null||group.Href!=null||group.Tooltip!=null||group.BackgroundColor!=null||group.BorderColor!=null||group.LabelColor!=null)
                throw new NotSupportedException("A sequence group carries unrepresentable common presentation.");
            var members=nodes.Select((node,index)=>new {node,index}).Where(item=>item.node.GroupId==group.Id).Select(item=>item.index).ToArray();
            if(members.Length==0||members[members.Length-1]-members[0]+1!=members.Length)throw new NotSupportedException("Sequence groups require contiguous existing participant columns.");
            sequence.AddParticipantGroup(group.Id,group.Label,members[0],members[members.Length-1],group.Color==null?null:PortableColor(group.Color));
            CopyFacts(group.Extensions,sequence.ParticipantGroups[sequence.ParticipantGroups.Count-1].Metadata);
        }
        foreach(var source in envelope.Annotations) {
            var native=source.Sequence!;
            var expectedKind=source.Role switch {
                VisualArtifactInterchangeAnnotationRole.SequenceNote=>"SequenceNote",
                VisualArtifactInterchangeAnnotationRole.SequenceActivation=>native.ActivationState==true?"SequenceActivation":"SequenceDeactivation",
                VisualArtifactInterchangeAnnotationRole.SequenceBlock=>"SequenceBlock:"+native.BlockKind,
                VisualArtifactInterchangeAnnotationRole.SequenceBranch=>"SequenceBranch:"+native.BranchKind,_=>string.Empty};
            var expectedPlacement=source.Role==VisualArtifactInterchangeAnnotationRole.SequenceNote?native.NotePlacement?.ToString()
                :source.Role==VisualArtifactInterchangeAnnotationRole.SequenceBranch?native.ParentBlockKind?.ToString():null;
            if(source.Kind!=expectedKind||source.Placement!=expectedPlacement||source.Role==VisualArtifactInterchangeAnnotationRole.SequenceActivation&&source.Text.Length>0)
                throw new NotSupportedException("Sequence annotation tokens contradict their typed native role.");
            if(source.Role!=VisualArtifactInterchangeAnnotationRole.SequenceActivation&&source.Extensions.Keys.Any(key=>
                !key.StartsWith("chartforgex.bounds.",StringComparison.Ordinal)&&key!="chartforgex.sequence.sourceOrder"
                &&key!="chartforgex.sequence.endSourceOrder"&&key!="chartforgex.sequence.closeStep"&&key!="chartforgex.sequence.background"))
                throw new NotSupportedException("Native note, block and branch records cannot preserve additional annotation extension bags.");
            if(source.Role==VisualArtifactInterchangeAnnotationRole.SequenceNote) {
                if(source.TargetIds.Count==0)throw new NotSupportedException("A targetless sequence note cannot be placed by the native owner.");
                sequence.AddNote(native.NotePlacement!.Value,source.TargetIds,source.Text);
                var target=sequence.Notes[sequence.Notes.Count-1];target.StepIndex=source.StartIndex!.Value;target.SourceOrder=SourceOrder(source,"sourceOrder");
            } else if(source.Role==VisualArtifactInterchangeAnnotationRole.SequenceActivation) {
                sequence.AddActivation(source.TargetIds[0],native.ActivationState!.Value,source.StartIndex!.Value);
                var target=sequence.Activations[sequence.Activations.Count-1];target.SourceOrder=SourceOrder(source,"sourceOrder");
                CopyFacts(source.Extensions,target.Metadata);
            } else if(source.Role==VisualArtifactInterchangeAnnotationRole.SequenceBlock) {
                sequence.AddBlock(native.BlockKind!.Value,source.Text,source.StartIndex!.Value,source.EndIndex??source.StartIndex.Value,native.IsEmpty,native.Depth);
                var target=sequence.Blocks[sequence.Blocks.Count-1];target.ParticipantIds.AddRange(source.TargetIds);
                target.SourceOrder=SourceOrder(source,"sourceOrder");target.EndSourceOrder=SourceOrder(source,"endSourceOrder");target.CloseStepIndex=CloseStep(source);
                if(source.Extensions.TryGetValue("chartforgex.sequence.background",out var color))target.BackgroundColor=PortableColor(color);
            } else if(source.Role==VisualArtifactInterchangeAnnotationRole.SequenceBranch) {
                sequence.AddBranch(native.ParentBlockKind!.Value,native.BranchKind!,source.Text,source.StartIndex!.Value,source.EndIndex??source.StartIndex.Value,native.Depth,native.IsEmpty);
                var target=sequence.Branches[sequence.Branches.Count-1];target.SourceOrder=SourceOrder(source,"sourceOrder");target.EndSourceOrder=SourceOrder(source,"endSourceOrder");target.CloseStepIndex=CloseStep(source);
            } else throw new NotSupportedException("A sequence annotation lacks a supported typed native role.");
        }
        return sequence;
    }
    private static void ValidateSequenceRestorationIdentities(VisualArtifactInterchangeEnvelope envelope,VisualArtifactInterchangeNode[] nodes,VisualArtifactInterchangeEdge[] messages) {
        var scope=new InterchangeIdScope();
        foreach(var node in nodes)if(scope.AddNodeOccurrence(node.Id)!=node.Id)throw new NotSupportedException("Sequence participant IDs must remain native portable identities.");
        foreach(var group in envelope.Groups)if(scope.AddGroup(group.Id)!=group.Id)throw new NotSupportedException("Sequence group IDs must remain native portable identities.");
        for(var index=0;index<messages.Length;index++)if(scope.AddEdge("message-"+(index+1).ToString(CultureInfo.InvariantCulture))!=messages[index].Id)
            throw new NotSupportedException("The native sequence message record cannot preserve an edited portable message identity.");
        foreach(var role in new[]{VisualArtifactInterchangeAnnotationRole.SequenceNote,VisualArtifactInterchangeAnnotationRole.SequenceActivation,VisualArtifactInterchangeAnnotationRole.SequenceBlock,VisualArtifactInterchangeAnnotationRole.SequenceBranch}) {
            var prefix=role==VisualArtifactInterchangeAnnotationRole.SequenceNote?"note":role==VisualArtifactInterchangeAnnotationRole.SequenceActivation?"activation":role==VisualArtifactInterchangeAnnotationRole.SequenceBlock?"block":"branch";
            var index=0;foreach(var item in envelope.Annotations.Where(a=>a.Role==role))
                if(scope.AddAnnotation(prefix+"-"+(++index).ToString(CultureInfo.InvariantCulture))!=item.Id)throw new NotSupportedException("Native sequence annotations cannot preserve edited portable annotation identities.");
        }
    }
    private static void CopyFacts(IReadOnlyDictionary<string,string> source,Dictionary<string,string> target) {foreach(var pair in source)target[pair.Key]=pair.Value;}
    private static ChartColor PortableColor(string value) => SvgRasterColor.TryParse(value,out var color)?color:throw new NotSupportedException("Sequence restoration requires literal native colors.");
    private static double SourceBound(VisualArtifactInterchangeEnvelope envelope,string name,double? fallback) {
        if(envelope.Extensions.TryGetValue("chartforgex.source."+name,out var value)) {
            if(double.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out var number)&&!double.IsNaN(number)&&!double.IsInfinity(number)&&number>0)return number;
            throw new NotSupportedException("Invalid carried native sequence source "+name+".");
        }
        return fallback??throw new NotSupportedException("Sequence restoration requires its authored or captured dimensions.");
    }
    private static double NativeNumber(IReadOnlyDictionary<string,string> values,string name,double fallback,bool positive=true) {
        if(!values.TryGetValue("chartforgex.sequence."+name,out var text))return fallback;
        if(!double.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out var value)||double.IsNaN(value)||double.IsInfinity(value)||(positive?value<=0:value<0))
            throw new NotSupportedException("Invalid carried native sequence "+name+".");
        return value;
    }
    private static long? SourceOrder(VisualArtifactInterchangeAnnotation source,string name) {
        if(!source.Extensions.TryGetValue("chartforgex.sequence."+name,out var text))return null;
        if(!long.TryParse(text,NumberStyles.None,CultureInfo.InvariantCulture,out var value)||value<0)throw new NotSupportedException("Invalid carried sequence occurrence order.");
        return value;
    }
    private static int? CloseStep(VisualArtifactInterchangeAnnotation source) {
        var value=SourceOrder(source,"closeStep");
        if(value>100000)throw new NotSupportedException("Sequence close occurrence exceeds the native step bound.");
        return value.HasValue?(int)value.Value:(int?)null;
    }
    private static void RestoreSequencePaint(SequenceArtifact sequence,IReadOnlyDictionary<string,string> values) {
        ChartColor? Get(string name)=>values.TryGetValue("chartforgex.sequence.paint."+name,out var text)?PortableColor(text):(ChartColor?)null;
        var paint=sequence.Paint;
        paint.MessageLabelFill=Get("messageLabelFill");paint.MessageLabelBorder=Get("messageLabelBorder");
        paint.ParticipantFill=Get("participantFill");paint.ParticipantBorder=Get("participantBorder");paint.ParticipantText=Get("participantText");paint.Lifeline=Get("lifeline");
        paint.MessageLine=Get("messageLine");paint.MessageText=Get("messageText");paint.NumberText=Get("numberText");paint.NumberFill=Get("numberFill");paint.NoteFill=Get("noteFill");paint.NoteBorder=Get("noteBorder");paint.NoteText=Get("noteText");
        paint.ActivationFill=Get("activationFill");paint.ActivationBorder=Get("activationBorder");paint.FragmentFill=Get("fragmentFill");paint.FragmentBorder=Get("fragmentBorder");paint.FragmentText=Get("fragmentText");paint.FragmentTitleText=Get("fragmentTitleText");
    }
}
