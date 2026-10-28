using System.Globalization;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

public static partial class MermaidZenUmlRendering {
    private sealed partial class Lowering {
        private void Occurrence(MermaidZenUmlStatement statement, int depth) {
            var start = _sequence.Messages.Count; var targetId = statement.TargetId ?? _external;
            if (statement.Kind == MermaidZenUmlStatementKind.Create) { _participants[targetId].CreatedAtMessageIndex = start; _notCreated.Remove(targetId); }
            _sequence.AddMessage(statement.CallerId ?? _external, targetId,
                statement.Assignment == null ? statement.Text : statement.Assignment + " = " + statement.Text);
            var message = _sequence.Messages[_sequence.Messages.Count - 1]; Source(message.Metadata, statement.Span);
            message.Metadata["mermaid.zenuml.kind"] = statement.Kind.ToString();
            message.Metadata["mermaid.zenuml.occurrence"] = statement.HasBody ? "true" : "false";
            if (statement.CallerId == null || statement.TargetId == null) message.Metadata["mermaid.zenuml.external-context"] = "true";
            if (statement.Assignment != null) message.Metadata["mermaid.zenuml.assignment"] = statement.Assignment;
            if (statement.Kind == MermaidZenUmlStatementKind.Create) message.ActivatesTarget = true;
            else {
                _sequence.AddActivation(targetId, true, start);
                _sequence.Activations[_sequence.Activations.Count-1].SourceOrder=NextOrder();
            Source(_sequence.Activations[_sequence.Activations.Count - 1].Metadata, statement.Span);
            }
            Statements(statement.Children, depth);
            if (statement.Assignment != null && !MermaidZenUmlModel.HasReturn(statement.Children)) {
                _sequence.AddMessage(targetId, statement.CallerId ?? _external, statement.Assignment, SequenceArtifactMessageLineStyle.Dashed, SequenceArtifactMessageKind.Return);
                var response = _sequence.Messages[_sequence.Messages.Count - 1]; Source(response.Metadata, statement.Span);
                response.Metadata["mermaid.zenuml.assignment"] = statement.Assignment;
            }
            _sequence.AddActivation(targetId, false, _sequence.Messages.Count);
            _sequence.Activations[_sequence.Activations.Count-1].SourceOrder=NextOrder();
            Source(_sequence.Activations[_sequence.Activations.Count - 1].Metadata, statement.Span);
        }

        private void Fragment(MermaidZenUmlStatement statement, int depth,long? order) {
            var kind = statement.Kind switch {
                MermaidZenUmlStatementKind.Conditional => SequenceArtifactBlockKind.Alt,
                MermaidZenUmlStatementKind.Loop => SequenceArtifactBlockKind.Loop,
                MermaidZenUmlStatementKind.Optional => SequenceArtifactBlockKind.Opt,
                MermaidZenUmlStatementKind.Parallel => SequenceArtifactBlockKind.Par,
                MermaidZenUmlStatementKind.Critical => SequenceArtifactBlockKind.Critical,
                MermaidZenUmlStatementKind.Try => SequenceArtifactBlockKind.Try,
                _ => SequenceArtifactBlockKind.Rect
            };
            var start = _sequence.Messages.Count;
            var primaryOrder=NextOrder();
            Statements(statement.Children, depth + 1);
            if (statement.Kind is MermaidZenUmlStatementKind.Conditional or MermaidZenUmlStatementKind.Try) {
                Branch(kind, "Primary", statement.Text, start, depth,primaryOrder);
                foreach (var branch in statement.Branches) {
                    var first = _sequence.Messages.Count; var branchOrder=NextOrder(); Statements(branch.Statements, depth + 1);
                    Branch(kind, branch.Kind, branch.Text, first, depth,branchOrder);
                }
            }
            var end = _sequence.Messages.Count;
            _sequence.AddBlock(kind, statement.Text, start, end == start ? start : end - 1, end == start, depth);
            var block=_sequence.Blocks[_sequence.Blocks.Count-1];block.SourceOrder=order;block.EndSourceOrder=NextOrder();block.CloseStepIndex=end;
            var prefix = "mermaid.zenuml.fragment." + (++_fragment).ToString(CultureInfo.InvariantCulture);
            _sequence.Metadata[prefix + ".kind"] = statement.Kind.ToString();
            _sequence.Metadata[prefix + ".keyword"] = statement.Keyword ?? string.Empty;
            _sequence.Metadata[prefix + ".source.line"] = statement.Span.Line.ToString(CultureInfo.InvariantCulture);
            _sequence.Metadata[prefix + ".source.column"] = statement.Span.Column.ToString(CultureInfo.InvariantCulture);
        }
        private void Branch(SequenceArtifactBlockKind kind, string label, string text, int start, int depth,long? order) {
            var end = _sequence.Messages.Count;
            _sequence.AddBranch(kind, label, text, start, end == start ? start : end - 1, depth, end == start);
            var branch=_sequence.Branches[_sequence.Branches.Count-1];branch.SourceOrder=order;branch.EndSourceOrder=NextOrder();branch.CloseStepIndex=end;
        }
    }
}
