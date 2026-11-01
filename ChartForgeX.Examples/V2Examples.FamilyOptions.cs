using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

public static partial class V2Examples {
    private static readonly Lazy<IReadOnlyDictionary<string, string>> ApiSummaries = new(ReadApiSummaries);
    private static readonly string[] CommonChartMembers = { "WithTitle", "WithXLabels", "AddSeries", "WithLegend", "WithDataLabels", "WithValueFormat", "ConfigureXAxis", "ConfigureYAxis" };

    /// <summary>Describes a family's option type or the builder methods for its model.</summary>
    private static object FamilyReference(string family) {
        var type = FamilyOptionsType(family);
        if (type == null) {
            var (model, members, names) = family switch {
                "topology" or "org-chart" => (typeof(TopologyChart), typeof(TopologyChartExtensions), new[] { "WithTitle", "WithTheme", "WithLayout", "AddNode", "AddEdge" }),
                "chart-grid" or "dashboard" => (typeof(ChartGrid), typeof(ChartGrid), new[] { "WithTitle", "WithColumns", "WithGap", "WithPanelSize", "WithTheme", "Add", "WithSharedAxes" }),
                "flow" => (typeof(FlowArtifact), typeof(FlowArtifact), new[] { "WithTitle", "WithSize", "AddLane", "AddStep", "AddConnector" }),
                "sequence" => (typeof(SequenceArtifact), typeof(SequenceArtifact), new[] { "WithTitle", "WithSize", "AddParticipant", "AddMessage", "AddBlock" }),
                _ => (typeof(Chart), typeof(Chart), CommonChartMembers)
            };
            return new {
                type = model.Name, configure = (string?)null, api = ApiUrl(model),
                options = names.Select(name => new { name, summary = Summary("M:" + members.FullName + "." + name + "(") ?? string.Empty })
                    .Where(row => row.summary.Length > 0).ToArray()
            };
        }
        var configure = typeof(Chart).GetMethods(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(method => method.Name.StartsWith("Configure", StringComparison.Ordinal)
            && method.GetParameters() is { Length: 1 } parameters && parameters[0].ParameterType == typeof(Action<>).MakeGenericType(type));
        return new {
            type = type.Name, configure = configure?.Name, api = ApiUrl(type),
            options = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).OrderBy(property => property.MetadataToken)
                .Select(property => new { name = property.Name, summary = Summary("P:" + type.FullName + "." + property.Name) ?? string.Empty })
                .Where(row => row.summary.Length > 0).Take(8).ToArray()
        };
    }

    private static Type? FamilyOptionsType(string family) => family switch {
        "box-plot" => typeof(ChartBoxPlotOptions), "bubble" => typeof(ChartBubbleOptions),
        "bullet" => typeof(ChartBulletOptions), "calendar-heatmap" => typeof(ChartCalendarOptions), "chord" => typeof(ChartChordOptions),
        "difference-area" => typeof(ChartDifferenceAreaOptions), "dumbbell" => typeof(ChartDumbbellOptions),
        "candlestick" or "ohlc" => typeof(ChartFinancialOptions), "funnel" or "cone-funnel" => typeof(ChartFunnelOptions),
        "gauge" or "linear-gauge" => typeof(ChartGaugeOptions), "polar" or "polar-area" => typeof(ChartPolarOptions),
        "pyramid" => typeof(ChartPyramidOptions), "radar" => typeof(ChartRadarOptions), "range-bar" => typeof(ChartRangeBarOptions),
        "sankey" => typeof(ChartSankeyOptions), "sunburst" => typeof(ChartSunburstOptions), "treemap" => typeof(ChartTreemapOptions),
        "trend-line" => typeof(ChartTrendLineOptions), "waterfall" => typeof(ChartWaterfallOptions),
        _ => null
    };

    private static string ApiUrl(Type type) => "/api/" + type.FullName!.Replace('.', '-').ToLowerInvariant() + "/";

    private static string? Summary(string key) {
        var exact = ApiSummaries.Value.TryGetValue(key, out var value) ? value : null;
        return exact ?? (key.EndsWith("(", StringComparison.Ordinal)
            ? ApiSummaries.Value.Where(row => row.Key.StartsWith(key, StringComparison.Ordinal)).OrderBy(row => row.Key.Length).Select(row => row.Value).FirstOrDefault()
            : null);
    }

    private static IReadOnlyDictionary<string, string> ReadApiSummaries() {
        var path = Path.Combine(AppContext.BaseDirectory, "ChartForgeX.xml");
        if (!File.Exists(path)) return new Dictionary<string, string>();
        return XDocument.Load(path).Descendants("member")
            .Select(member => (Key: (string?)member.Attribute("name"), Summary: member.Element("summary")))
            .Where(row => row.Key != null && row.Summary != null)
            .GroupBy(row => row.Key!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => PlainSummary(group.First().Summary!), StringComparer.Ordinal);
    }

    private static string PlainSummary(XElement summary) {
        var text = string.Concat(summary.Nodes().Select(node => node switch {
            XText value => value.Value,
            XElement element when element.Attribute("cref") is { } cref => cref.Value.Substring(cref.Value.LastIndexOf('.') + 1),
            XElement element when element.Attribute("langword") is { } word => word.Value,
            XElement element => element.Value,
            _ => string.Empty
        }));
        return Regex.Replace(text, @"\s+", " ").Trim();
    }
}
