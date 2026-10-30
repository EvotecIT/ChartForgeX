using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.Markup;

public sealed partial class MarkupArchitectureParser {
    private static void ParseUseCaseHeader(ParseState state, List<MarkupArchitectureToken> tokens, int line) {
        if (state.Frames.Count > 0) {
            state.Error(line, tokens[0].Column, MarkupArchitectureDiagnosticCodes.InvalidStatement, "Declare use cases at the top level, outside groups.");
            return;
        }

        if (tokens.Count < 2) {
            state.Error(line, tokens[0].Column, MarkupArchitectureDiagnosticCodes.InvalidStatement, "Write a use case as 'usecase <id> \"Label\"', then its steps, then 'end'.");
            return;
        }

        string id;
        string label;
        int next;
        if (tokens[1].Quoted) {
            label = tokens[1].Text;
            id = Slug(label);
            next = 2;
        } else {
            id = tokens[1].Text;
            label = tokens.Count > 2 && tokens[2].Quoted ? tokens[2].Text : id;
            next = tokens.Count > 2 && tokens[2].Quoted ? 3 : 2;
        }

        var scenario = new TopologyScenario { Id = SafeToken(id), Label = label, Spotlight = true };
        foreach (var (key, value, column) in Attributes(tokens, next)) {
            switch (key) {
                case "description": scenario.Description = value; break;
                case "color": scenario.Color = value; break;
                case "delay":
                case "duration": scenario.PlaybackDelayMilliseconds = VisualMarkupFenceOptions.ParseInt32(value, key); break;
                case "loop": scenario.LoopPlayback = VisualMarkupFenceOptions.ParseBoolean(value, key); break;
                case "autoplay": scenario.AutoPlay = VisualMarkupFenceOptions.ParseBoolean(value, key); break;
                case "spotlight": scenario.Spotlight = VisualMarkupFenceOptions.ParseBoolean(value, key); break;
                default: state.UnknownAttribute(key, line, column); break;
            }
        }

        if (state.UseCases.Any(item => string.Equals(item.Scenario.Id, scenario.Id, StringComparison.Ordinal))) {
            state.Error(line, tokens[1].Column, MarkupArchitectureDiagnosticCodes.DuplicateId, "Use case '" + scenario.Id + "' is declared more than once.");
            return;
        }

        var useCase = new PendingUseCase(scenario, line);
        state.UseCases.Add(useCase);
        state.Frames.Push(new Frame(scenario.Id, line, useCase));
    }

    private static void ParseUseCaseLine(ParseState state, List<MarkupArchitectureToken> tokens, string text, string[] lines, ref int index, int line) {
        var useCase = state.Frames.Peek().UseCase!;
        var command = tokens[0].Quoted ? string.Empty : tokens[0].Text.ToLowerInvariant();
        if (command == "end" || command == "}") {
            state.Frames.Pop();
            if (useCase.Steps.Count == 0) state.Warning(useCase.Line, 1, MarkupArchitectureDiagnosticCodes.EmptyUseCase, "Use case '" + useCase.Scenario.Id + "' has no steps.");
            return;
        }

        try {
            if (tokens.Count >= 3 && !tokens[1].Quoted && StepArrows.TryGetValue(tokens[1].Text, out var interaction)) {
                if (tokens[0].Quoted || tokens[2].Quoted) throw new FormatException("Steps join node ids, not quoted text.");
                var step = new PendingStep(tokens[0].Text, tokens[2].Text, interaction, line, tokens[0].Column, tokens[2].Column);
                ReadStepTail(state, step, tokens, 3, text, lines, ref index, line);
                useCase.Steps.Add(step);
                return;
            }

            if (command == "visit" || command == "at") {
                if (tokens.Count < 2 || tokens[1].Quoted) throw new FormatException("Write a node step as 'visit <node> \"what happens\"'.");
                var visit = new PendingStep(tokens[1].Text, null, TopologyScenarioInteraction.None, line, tokens[1].Column, 0);
                ReadStepTail(state, visit, tokens, 2, text, lines, ref index, line);
                useCase.Steps.Add(visit);
                return;
            }

            if (tokens.Count >= 3 && IsConnectionArrow(tokens[1])) {
                throw new FormatException("Use case steps use '->' (request), '->>' (message), '-->' (response) or '-x' (failure).");
            }

            state.Error(line, tokens[0].Column, MarkupArchitectureDiagnosticCodes.InvalidStatement, "Expected a step like 'client -> api \"POST /orders\"', a 'visit <node>' step, or 'end'.");
        } catch (FormatException ex) {
            state.Error(line, tokens[0].Column, MarkupArchitectureDiagnosticCodes.InvalidStatement, ex.Message);
        } catch (ArgumentException ex) {
            state.Error(line, tokens[0].Column, MarkupArchitectureDiagnosticCodes.InvalidValue, ex.Message);
        }
    }

    private static void ReadStepTail(ParseState state, PendingStep step, List<MarkupArchitectureToken> tokens, int start, string text, string[] lines, ref int index, int line) {
        var (label, next) = Label(tokens, start);
        step.Label = label;
        for (; next < tokens.Count; next++) {
            var token = tokens[next];
            if (IsPayloadStart(token)) break;
            if (!MarkupArchitectureLine.TryAttribute(token, out var key, out var value)) throw new FormatException("Expected an attribute, a payload, or the end of the step but found '" + token.Text + "'.");
            switch (key) {
                case "duration":
                    var duration = VisualMarkupFenceOptions.ParseInt32(value, key);
                    step.Duration = duration >= 200 && duration <= 60_000 ? duration : throw new ArgumentException("Step duration must be between 200 and 60000 milliseconds.");
                    break;
                case "description": step.Description = value; break;
                default: state.UnknownAttribute(key, line, token.Column); break;
            }
        }

        if (next >= tokens.Count) return;
        var payloadToken = tokens[next];
        var keyword = payloadToken.Quoted ? string.Empty : payloadToken.Text.ToLowerInvariant();
        TopologyScenarioPayloadFormat format;
        string payload;
        if (keyword == "json" || keyword == "xml" || keyword == "text") {
            format = keyword == "json" ? TopologyScenarioPayloadFormat.Json : keyword == "xml" ? TopologyScenarioPayloadFormat.Xml : TopologyScenarioPayloadFormat.Text;
            if (next + 1 >= tokens.Count) throw new FormatException("'" + keyword + "' needs the payload after it.");
            var body = tokens[next + 1];
            payload = format == TopologyScenarioPayloadFormat.Text && body.Quoted && next + 2 == tokens.Count ? body.Text : text.Substring(body.Start).Trim();
        } else {
            format = payloadToken.Text.StartsWith("<", StringComparison.Ordinal) ? TopologyScenarioPayloadFormat.Xml : TopologyScenarioPayloadFormat.Json;
            payload = text.Substring(payloadToken.Start).Trim();
        }

        if (format == TopologyScenarioPayloadFormat.Json) {
            var balance = MarkupArchitectureLine.JsonBalance(payload, 0);
            var continuation = new List<string>();
            while (balance > 0 && index + 1 < lines.Length) {
                var raw = lines[++index];
                continuation.Add(raw);
                balance = MarkupArchitectureLine.JsonBalance(raw, balance);
            }

            if (continuation.Count > 0) payload += "\n" + Dedent(continuation);
            if (balance != 0) {
                state.Error(line, payloadToken.Column, MarkupArchitectureDiagnosticCodes.InvalidPayload, "The JSON payload's brackets do not balance" + (balance > 0 ? " before the end of the block." : "."));
                return;
            }
        }

        if (payload.Length > TopologyScenarioStep.MaximumPayloadLength) {
            state.Error(line, payloadToken.Column, MarkupArchitectureDiagnosticCodes.InvalidPayload, "The payload is longer than " + TopologyScenarioStep.MaximumPayloadLength.ToString(CultureInfo.InvariantCulture) + " characters.");
            return;
        }

        step.Payload = payload;
        step.PayloadFormat = format;
    }

    private static bool IsPayloadStart(MarkupArchitectureToken token) {
        if (token.Quoted) return false;
        var text = token.Text;
        if (text.Length == 0) return false;
        var lower = text.ToLowerInvariant();
        return lower == "json" || lower == "xml" || lower == "text" || text[0] == '{' || text[0] == '[' || text[0] == '<' && text != "<-" && text != "<->";
    }

    private static string Dedent(List<string> lines) {
        var indent = lines.Where(line => line.Trim().Length > 0).Select(line => line.Length - line.TrimStart().Length).DefaultIfEmpty(0).Min();
        return string.Join("\n", lines.Select(line => (line.Length >= indent ? line.Substring(indent) : line.TrimStart()).TrimEnd()));
    }

    private static string Slug(string label) {
        var chars = label.Trim().ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray();
        var slug = string.Join("-", new string(chars).Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries));
        return slug.Length == 0 ? "usecase" : slug;
    }

    private static string SafeToken(string id) {
        foreach (var ch in id) {
            if (!(char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == '.')) throw new ArgumentException("Use case ids may contain only letters, digits, dots, underscores, and hyphens.");
        }

        return id;
    }

    private static readonly Dictionary<string, TopologyScenarioInteraction> StepArrows = new(StringComparer.Ordinal) {
        ["->"] = TopologyScenarioInteraction.Request,
        ["->>"] = TopologyScenarioInteraction.Message,
        ["-->"] = TopologyScenarioInteraction.Response,
        ["-x"] = TopologyScenarioInteraction.Failure
    };
}
