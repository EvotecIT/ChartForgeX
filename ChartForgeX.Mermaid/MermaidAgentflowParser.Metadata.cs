using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidAgentflowParser {
    private static void ApplyAttachments(MermaidAgentflowDocument document, List<Attachment> attachments, MermaidParseResult<MermaidDocument> result) {
        foreach (var attachment in attachments) {
            MermaidResourceGuard.Checkpoint();
            var flow = document.Flows.Find(item => item.Id == attachment.Id);
            var edge = document.Edges.Find(item => item.Id == attachment.Id);
            var node = document.Nodes.Find(item => item.Id == attachment.Id);
            var values = flow?.Metadata ?? edge?.Metadata ?? node?.Metadata;
            if (values == null) continue;
            foreach (var pair in attachment.Data.Properties) {
                values[pair.Key] = pair.Value;
                var scalar = pair.Value.Kind == MermaidMetadataKind.String ? pair.Value.Text : null;
                switch (pair.Key) {
                    case "shape":
                        if (node == null || flow != null || edge != null) Unsupported("Agentflow shape metadata applies only to nodes.");
                        else if (scalar != null && MermaidAgentflowShapes.IsRemoved(scalar))
                            MermaidParserUtilities.Add(result, pair.Value.Span, MermaidDiagnosticSeverity.Error,
                                "Agentflow shape '" + scalar + "' was removed from the family and is retained without application.", MermaidDiagnosticCodes.InvalidStatement);
                        else if (scalar != null && (scalar != scalar.ToLowerInvariant() || scalar.IndexOf('_') >= 0))
                            MermaidParserUtilities.Add(result, pair.Value.Span, MermaidDiagnosticSeverity.Error,
                                "Authored Agentflow shape names must be lowercase and must not contain underscores.", MermaidDiagnosticCodes.InvalidStatement);
                        else if (scalar == null || !MermaidAgentflowShapes.TryResolve(scalar, out _)) Unsupported("This Agentflow shape is outside the family catalog and is retained without application.");
                        else node.Shape = scalar;
                        break;
                    case "label":
                        if (scalar == null) Unsupported("Agentflow labels require text metadata.");
                        else if (flow != null) flow.Label = scalar;
                        else if (edge != null) edge.Label = scalar;
                        else if (node != null && attachment.Span.Line >= node.LabelSpan.Line) node.Label = scalar;
                        break;
                    case "view":
                        if (flow == null || scalar is not ("collapsed" or "expanded")) Unsupported("Agentflow view metadata requires an expanded or collapsed flow.");
                        else flow.Collapsed = scalar == "collapsed";
                        break;
                    case "labelType":
                        if (scalar != "text" && scalar != "string") Unsupported("Agentflow rich label parsing is retained as literal native text.");
                        break;
                    case "algorithm": case "curve": case "animate": case "animation":
                        Unsupported("Agentflow layout and animation metadata is retained; native static layout does not execute it."); break;
                }
            }
            void Unsupported(string message) => MermaidParserUtilities.Add(result, attachment.Span, MermaidDiagnosticSeverity.Warning,
                message, MermaidDiagnosticCodes.UnsupportedStatement);
        }
    }
}
