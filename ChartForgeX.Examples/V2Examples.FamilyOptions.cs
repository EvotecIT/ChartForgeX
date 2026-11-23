using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

public static partial class V2Examples {
    private static readonly Lazy<IReadOnlyDictionary<string, string>> ApiSummaries = new(ReadApiSummaries);
    private static readonly string[] CommonChartMembers = { "Title", "XLabels", "AddSeries", "Legend", "DataLabels", "ValueFormat", "XAxis", "YAxis" };

    /// <summary>Describes a family's option type or the builder methods for its model.</summary>
    private static object FamilyReference(string family) {
        var type = FamilyOptionsType(family);
        if (type == null) {
            var (model, names) = family switch {
                "topology" or "org-chart" => (typeof(TopologyChart), new[] { "Title", "Theme", "LayoutMode", "AddNode", "AddEdge" }),
                "chart-grid" or "dashboard" => (typeof(ChartGrid), new[] { "Title", "Columns", "Gap", "PanelSize", "Theme", "Add", "ShareAxes" }),
                "flow" => (typeof(FlowArtifact), new[] { "Title", "Width", "Height", "AddLane", "AddStep", "AddConnector" }),
                "sequence" => (typeof(SequenceArtifact), new[] { "Title", "Width", "Height", "AddParticipant", "AddMessage", "AddBlock" }),
                _ => (typeof(Chart), CommonChartMembers)
            };
            return new {
                type = model.Name, configure = (string?)null, api = ApiUrl(model),
                options = names.Select(name => new { name, summary = MethodFor(model, name, null) is { } method ? Summary(DocumentationPrefix(method)) ?? string.Empty : string.Empty })
                    .Where(row => row.summary.Length > 0).ToArray()
            };
        }
        // Chart inlines its options, so the family's settings are configured by the generated chart.<Property>(o => ...) method.
        var property = typeof(ChartOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(candidate => candidate.PropertyType == type);
        var configure = property == null ? null : MethodFor(typeof(Chart), property.Name, typeof(Action<>).MakeGenericType(type));
        return new {
            type = type.Name, configure = configure?.Name, api = ApiUrl(type),
            options = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).OrderBy(property => property.MetadataToken)
                .Select(property => new { name = property.Name, summary = Summary("P:" + type.FullName + "." + property.Name) ?? string.Empty })
                .Where(row => row.summary.Length > 0).Take(8).ToArray()
        };
    }

    // A public instance method of the model, or a fluent extension method whose receiver is the model (generated methods are
    // generic over the receiver when the model can be derived from). With an argument type, only the overload taking it.
    private static MethodInfo? MethodFor(Type model, string name, Type? argument) {
        bool Fits(MethodInfo method, int skip) {
            var parameters = method.GetParameters();
            return argument == null || (parameters.Length == skip + 1 && parameters[skip].ParameterType == argument);
        }
        var instance = model.GetMethods(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(method => method.Name == name && Fits(method, 0));
        if (instance != null) return instance;
        return model.Assembly.GetExportedTypes().Where(candidate => candidate.IsAbstract && candidate.IsSealed)
            .SelectMany(candidate => candidate.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(method => method.Name == name && method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
                && Receives(method.GetParameters()[0].ParameterType, model) && Fits(method, 1))
            .OrderBy(method => method.GetParameters().Length).FirstOrDefault();
    }

    private static bool Receives(Type receiver, Type model) =>
        receiver == model || (receiver.IsGenericParameter && receiver.GetGenericParameterConstraints().Any(constraint => constraint.IsAssignableFrom(model)));

    // The documentation ID prefix of a method: "M:Type.Name(" or, for a generic method, "M:Type.Name``".
    private static string DocumentationPrefix(MethodInfo method) =>
        "M:" + method.DeclaringType!.FullName + "." + method.Name + (method.IsGenericMethodDefinition ? "``" : "(");

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
        return exact ?? (key.EndsWith("(", StringComparison.Ordinal) || key.EndsWith("``", StringComparison.Ordinal)
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
