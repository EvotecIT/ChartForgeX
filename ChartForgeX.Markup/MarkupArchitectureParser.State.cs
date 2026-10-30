using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;

namespace ChartForgeX.Markup;

public sealed partial class MarkupArchitectureParser {
    private sealed class Frame {
        public Frame(string id, int line, PendingUseCase? useCase) { Id = id; Line = line; UseCase = useCase; }
        public string Id { get; }
        public int Line { get; }
        public PendingUseCase? UseCase { get; }
    }

    private sealed class PendingConnection {
        public PendingConnection(string from, string to, string? label, int line, int fromColumn, int toColumn) {
            From = from; To = to; Label = label; Line = line; FromColumn = fromColumn; ToColumn = toColumn;
        }
        public string From { get; }
        public string To { get; }
        public string? Label { get; }
        public int Line { get; }
        public int FromColumn { get; }
        public int ToColumn { get; }
        public string? Id { get; set; }
        public VisualLinkDirection Direction { get; set; } = VisualLinkDirection.Forward;
        public TopologyHealthStatus Status { get; set; } = TopologyHealthStatus.Unknown;
        public TopologyEdgeKind Kind { get; set; } = TopologyEdgeKind.Generic;
        public TopologyEdgeLineStyle LineStyle { get; set; } = TopologyEdgeLineStyle.Solid;
        public string? Color { get; set; }
    }

    private sealed class PendingUseCase {
        public PendingUseCase(TopologyScenario scenario, int line) { Scenario = scenario; Line = line; }
        public TopologyScenario Scenario { get; }
        public int Line { get; }
        public List<PendingStep> Steps { get; } = new();
    }

    private sealed class PendingStep {
        public PendingStep(string from, string? to, TopologyScenarioInteraction interaction, int line, int fromColumn, int toColumn) {
            From = from; To = to; Interaction = interaction; Line = line; FromColumn = fromColumn; ToColumn = toColumn;
        }
        public string From { get; }
        public string? To { get; }
        public TopologyScenarioInteraction Interaction { get; }
        public int Line { get; }
        public int FromColumn { get; }
        public int ToColumn { get; }
        public string? Label { get; set; }
        public string? Description { get; set; }
        public int? Duration { get; set; }
        public string? Payload { get; set; }
        public TopologyScenarioPayloadFormat PayloadFormat { get; set; }
    }

    private sealed class ParseState {
        private readonly MarkupParseResult<TopologyChart> _result;
        private readonly HashSet<string> _ids = new(StringComparer.Ordinal);
        private TopologyIconCatalog? _icons;

        public ParseState(MarkupParseResult<TopologyChart> result, VisualMarkupBlock block) {
            _result = result;
            FenceLine = block.FenceLine;
            Chart = TopologyChart.Create().WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight);
        }

        public TopologyChart Chart { get; }
        public int FenceLine { get; }
        public Stack<Frame> Frames { get; } = new();
        public List<PendingConnection> Connections { get; } = new();
        public List<PendingUseCase> UseCases { get; } = new();
        public Dictionary<string, int> NodeLines { get; } = new(StringComparer.Ordinal);
        public double? Width { get; set; }
        public double? Height { get; set; }
        public bool InferLegend { get; set; }
        public bool InUseCase => Frames.Count > 0 && Frames.Peek().UseCase != null;
        public string? CurrentGroupId => Frames.Count > 0 && Frames.Peek().UseCase == null ? Frames.Peek().Id : null;

        public void Error(int line, int column, string code, string message) => Add(_result, line, column, code, MarkupDiagnosticSeverity.Error, message);

        public void Warning(int line, int column, string code, string message) => Add(_result, line, column, code, MarkupDiagnosticSeverity.Warning, message);

        public void UnknownAttribute(string key, int line, int column) => Warning(line, column, MarkupArchitectureDiagnosticCodes.UnknownAttribute, "Unknown attribute '" + key + "' is ignored.");

        public bool DeclareId(string id, int line, int column) {
            if (_ids.Add(id)) return true;
            Error(line, column, MarkupArchitectureDiagnosticCodes.DuplicateId, "'" + id + "' is already declared. Node and group ids must be unique.");
            return false;
        }

        public void CheckIcon(string icon, int line, int column) {
            _icons ??= TopologyIconCatalog.Default();
            if (_icons.Resolve(icon) == null) Warning(line, column, MarkupArchitectureDiagnosticCodes.UnknownIcon, "Icon '" + icon + "' is not in the built-in icon catalog; the node kind's icon is used instead.");
        }

        public void Complete() {
            if (Chart.Nodes.Count == 0 && Connections.Count == 0) {
                Error(FenceLine, 1, MarkupArchitectureDiagnosticCodes.EmptyDiagram, "Architecture markup must declare at least one node.");
                return;
            }

            var edgeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var connection in Connections) {
                EnsureNode(connection.From, connection.Line, connection.FromColumn);
                EnsureNode(connection.To, connection.Line, connection.ToColumn);
                var id = connection.Id ?? UniqueEdgeId(connection.From + "-" + connection.To, edgeIds);
                if (!edgeIds.Add(id)) {
                    Error(connection.Line, connection.FromColumn, MarkupArchitectureDiagnosticCodes.DuplicateId, "Connection id '" + id + "' is already used.");
                    continue;
                }

                try {
                    Chart.AddEdge(id, connection.From, connection.To, connection.Label, connection.Kind, connection.Status, connection.Direction, color: connection.Color);
                    Chart.Edges[Chart.Edges.Count - 1].LineStyle = connection.LineStyle;
                } catch (ArgumentException ex) {
                    Error(connection.Line, connection.FromColumn, MarkupArchitectureDiagnosticCodes.InvalidValue, ex.Message);
                }
            }

            foreach (var useCase in UseCases) {
                try {
                    CompleteUseCase(useCase, edgeIds);
                } catch (ArgumentException ex) {
                    Error(useCase.Line, 1, MarkupArchitectureDiagnosticCodes.InvalidValue, ex.Message);
                }
            }
            if (InferLegend) Chart.Legend = TopologyLegend.Infer(Chart);
            var (width, height) = MarkupArchitectureViewport.Estimate(Chart);
            Chart.WithViewport(Width ?? width, Height ?? height, 28);
        }

        private void CompleteUseCase(PendingUseCase useCase, HashSet<string> edgeIds) {
            var known = new HashSet<string>(Chart.Nodes.Select(node => node.Id), StringComparer.Ordinal);
            var open = new List<(string From, string To)>();
            foreach (var step in useCase.Steps) {
                if (!known.Contains(step.From)) {
                    Error(step.Line, step.FromColumn, MarkupArchitectureDiagnosticCodes.UnknownReference, "Use case '" + useCase.Scenario.Id + "' refers to unknown node '" + step.From + "'.");
                    continue;
                }

                if (step.To == null) {
                    useCase.Scenario.AddNodeStep(step.From, step.Label, step.Description, configured => Configure(configured, step));
                    continue;
                }

                if (!known.Contains(step.To)) {
                    Error(step.Line, step.ToColumn, MarkupArchitectureDiagnosticCodes.UnknownReference, "Use case '" + useCase.Scenario.Id + "' refers to unknown node '" + step.To + "'.");
                    continue;
                }

                var edge = Chart.Edges.FirstOrDefault(candidate => candidate.SourceNodeId == step.From && candidate.TargetNodeId == step.To);
                var reverse = false;
                if (edge == null) {
                    edge = Chart.Edges.FirstOrDefault(candidate => candidate.SourceNodeId == step.To && candidate.TargetNodeId == step.From);
                    reverse = edge != null;
                }

                if (edge == null) {
                    Warning(step.Line, step.FromColumn, MarkupArchitectureDiagnosticCodes.ImplicitConnection,
                        "No connection joins '" + step.From + "' and '" + step.To + "'. A dashed connection was added; declare '" + step.From + " -> " + step.To + "' to keep the architecture complete.");
                    Chart.AddEdge(UniqueEdgeId(step.From + "-" + step.To, edgeIds), step.From, step.To, null, direction: VisualLinkDirection.Forward);
                    edge = Chart.Edges[Chart.Edges.Count - 1];
                    edge.LineStyle = TopologyEdgeLineStyle.Dashed;
                    edgeIds.Add(edge.Id);
                }

                if (step.Interaction == TopologyScenarioInteraction.Response) {
                    var match = open.FindLastIndex(item => item.From == step.To && item.To == step.From);
                    if (match < 0) Warning(step.Line, step.FromColumn, MarkupArchitectureDiagnosticCodes.UnansweredResponse, "This response from '" + step.From + "' answers no earlier request from '" + step.To + "'.");
                    else open.RemoveAt(match);
                } else if (step.Interaction == TopologyScenarioInteraction.Request) {
                    open.Add((step.From, step.To));
                }

                useCase.Scenario.AddMessageStep(edge.Id, step.Interaction, step.Label, reverse, configured => {
                    configured.Description = step.Description;
                    Configure(configured, step);
                });
            }

            if (useCase.Scenario.Steps.Count > 0) Chart.Scenarios.Add(useCase.Scenario);
        }

        private static void Configure(TopologyScenarioStep configured, PendingStep step) {
            if (step.Duration.HasValue) configured.DurationMilliseconds = step.Duration.Value;
            if (step.Payload != null) configured.WithPayload(step.Payload, step.PayloadFormat);
        }

        private void EnsureNode(string id, int line, int column) {
            if (Chart.Nodes.Any(node => node.Id == id)) return;
            if (Chart.Groups.Any(group => group.Id == id)) {
                Error(line, column, MarkupArchitectureDiagnosticCodes.UnknownReference, "'" + id + "' is a group. Connect nodes; connections to groups are not supported yet.");
                return;
            }

            Warning(line, column, MarkupArchitectureDiagnosticCodes.ImplicitNode, "Node '" + id + "' is not declared, so it was added as a generic node. Declare it with a type such as 'service " + id + " \"Name\"'.");
            Chart.AddAutoNode(id, id);
            _ids.Add(id);
        }

        private static string UniqueEdgeId(string baseId, HashSet<string> used) {
            if (!used.Contains(baseId)) return baseId;
            for (var i = 2; ; i++) {
                var candidate = baseId + "-" + i.ToString(CultureInfo.InvariantCulture);
                if (!used.Contains(candidate)) return candidate;
            }
        }
    }
}
