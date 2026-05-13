using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ChartForgeX.Topology;

internal static class TopologyScenarioJson {
    public static string? ScenarioIds(TopologyChart chart) {
        return chart.Scenarios.Count == 0 ? null : string.Join(" ", chart.Scenarios.Select(scenario => scenario.Id));
    }

    public static string? Summaries(TopologyChart chart) {
        if (chart.Scenarios.Count == 0) return null;
        var builder = new StringBuilder();
        builder.Append('[');
        for (var i = 0; i < chart.Scenarios.Count; i++) {
            var scenario = chart.Scenarios[i];
            if (i > 0) builder.Append(',');
            builder.Append("{\"id\":");
            AppendJsonString(builder, scenario.Id);
            builder.Append(",\"label\":");
            AppendJsonString(builder, scenario.Label);
            if (!string.IsNullOrWhiteSpace(scenario.Description)) {
                builder.Append(",\"description\":");
                AppendJsonString(builder, scenario.Description!);
            }

            if (!string.IsNullOrWhiteSpace(scenario.Color)) {
                builder.Append(",\"color\":");
                AppendJsonString(builder, scenario.Color!.Trim());
            }

            builder.Append(",\"stepCount\":").Append(scenario.Steps.Count.ToString(CultureInfo.InvariantCulture));
            if (scenario.Metadata.Count > 0) builder.Append(",\"metadata\":").Append(Metadata(scenario.Metadata));
            builder.Append(",\"steps\":").Append(Steps(scenario));
            builder.Append('}');
        }

        builder.Append(']');
        return builder.ToString();
    }

    public static string Steps(TopologyScenario scenario) {
        var builder = new StringBuilder();
        builder.Append('[');
        for (var i = 0; i < scenario.Steps.Count; i++) {
            var step = scenario.Steps[i];
            if (i > 0) builder.Append(',');
            builder.Append("{\"index\":").Append(i.ToString(CultureInfo.InvariantCulture))
                .Append(",\"kind\":");
            AppendJsonString(builder, step.Kind.ToString());
            builder.Append(",\"id\":");
            AppendJsonString(builder, step.Id);
            if (!string.IsNullOrWhiteSpace(step.Label)) {
                builder.Append(",\"label\":");
                AppendJsonString(builder, step.Label!);
            }

            if (!string.IsNullOrWhiteSpace(step.Description)) {
                builder.Append(",\"description\":");
                AppendJsonString(builder, step.Description!);
            }

            if (step.Metadata.Count > 0) builder.Append(",\"metadata\":").Append(Metadata(step.Metadata));
            builder.Append('}');
        }

        builder.Append(']');
        return builder.ToString();
    }

    public static string? Metadata(TopologyScenario scenario) {
        return scenario.Metadata.Count == 0 ? null : Metadata(scenario.Metadata);
    }

    public static string Metadata(IReadOnlyDictionary<string, string> metadata) {
        var builder = new StringBuilder();
        builder.Append('{');
        var first = true;
        foreach (var item in metadata.OrderBy(item => item.Key, StringComparer.Ordinal)) {
            if (!first) builder.Append(',');
            first = false;
            AppendJsonString(builder, item.Key);
            builder.Append(':');
            AppendJsonString(builder, item.Value ?? string.Empty);
        }

        builder.Append('}');
        return builder.ToString();
    }

    private static void AppendJsonString(StringBuilder builder, string value) {
        builder.Append('"');
        foreach (var ch in value) {
            switch (ch) {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (char.IsControl(ch)) builder.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                    else builder.Append(ch);
                    break;
            }
        }

        builder.Append('"');
    }
}
