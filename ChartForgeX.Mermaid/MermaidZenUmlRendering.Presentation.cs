using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

public static partial class MermaidZenUmlRendering {
    private sealed partial class Lowering {
        private long? NextOrder()=>_presentationOrder?_order++:(long?)null;
        private static bool HasPresentation(List<MermaidZenUmlStatement> statements) {
            foreach(var item in statements) {
                if(item.Kind is MermaidZenUmlStatementKind.Reference or MermaidZenUmlStatementKind.Divider||HasPresentation(item.Children))return true;
                foreach(var branch in item.Branches)if(HasPresentation(branch.Statements))return true;
            }
            return false;
        }
        private void Presentation(MermaidZenUmlStatement statement,SequenceArtifactBlockKind kind,int depth,long? order) {
            var step=_sequence.Messages.Count;
            _sequence.AddBlock(kind,statement.Text,step,step,true,depth);
            var block=_sequence.Blocks[_sequence.Blocks.Count-1];block.SourceOrder=order;
            foreach(var id in statement.ReferenceParticipantIds)block.ParticipantIds.Add(id);
            var prefix="mermaid.zenuml.fragment."+(++_fragment).ToString(CultureInfo.InvariantCulture);
            _sequence.Metadata[prefix+".kind"]=statement.Kind.ToString();Source(_sequence.Metadata,prefix,statement.Span);
        }
    }
}
