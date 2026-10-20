using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Localizes HTML controls independently from chart data labels and native number/date formatting.</summary>
/// <remarks>Unspecified messages use English. Templates retain named placeholders such as {count}, {current} and {total}.
/// Chart language is inherited when Language is null. Automatic direction inherits the host document.
/// Direction affects adapter chrome, not native axis order.</remarks>
public sealed partial class HtmlChartLocalization {
    private readonly Dictionary<HtmlChartText, string> _overrides = new Dictionary<HtmlChartText, string>();
    private string? _language;
    private HtmlChartTextDirection _direction;
    private static readonly IReadOnlyDictionary<HtmlChartText, string> Defaults;
    private static readonly HtmlChartText[] ChartKeys;
    private static readonly HtmlChartText[] GraphKeys;
    private static readonly HtmlChartText[] TopologyKeys;
    private static readonly HtmlChartText[] ReportKeys;

    static HtmlChartLocalization() {
        var chart = new Dictionary<HtmlChartText, string> {
        [HtmlChartText.ChartControls] = "Chart controls",
        [HtmlChartText.ZoomIn] = "Zoom in",
        [HtmlChartText.ZoomOut] = "Zoom out",
        [HtmlChartText.ZoomInButton] = "Zoom +",
        [HtmlChartText.ZoomOutButton] = "Zoom -",
        [HtmlChartText.Pan] = "Pan",
        [HtmlChartText.PanChart] = "Pan chart",
        [HtmlChartText.Brush] = "Brush",
        [HtmlChartText.BrushRegion] = "Brush select region",
        [HtmlChartText.DownloadSvg] = "Download SVG",
        [HtmlChartText.DownloadPng] = "Download PNG",
        [HtmlChartText.ResetView] = "Reset view",
        [HtmlChartText.XNavigation] = "Data range",
        [HtmlChartText.XFullRange] = "Full range",
        [HtmlChartText.XRangeStart] = "Start",
        [HtmlChartText.XRangeEnd] = "End",
        [HtmlChartText.XWindowPosition] = "Move window",
        [HtmlChartText.XRangeUpdating] = "Updating range…",
        [HtmlChartText.XRangeFailed] = "The range could not be updated.",
        [HtmlChartText.Compare] = "Compare {count}",
        [HtmlChartText.Clear] = "Clear",
        [HtmlChartText.Highlighted] = "{count} marks highlighted",
        [HtmlChartText.HighlightCleared] = "Highlight cleared",
        [HtmlChartText.ChartScenarios] = "Chart scenarios",
        [HtmlChartText.All] = "All",
        [HtmlChartText.ShowAllScenarios] = "Show all scenarios",
        [HtmlChartText.AllDataVisible] = "All chart data visible",
        [HtmlChartText.Steps] = "{count} steps",
        [HtmlChartText.StepProgress] = "Step {current} / {count}",
        [HtmlChartText.Previous] = "Prev",
        [HtmlChartText.PreviousStep] = "Previous step",
        [HtmlChartText.Play] = "Play",
        [HtmlChartText.Pause] = "Pause",
        [HtmlChartText.PlayScenario] = "Play scenario",
        [HtmlChartText.PauseScenario] = "Pause scenario",
        [HtmlChartText.Next] = "Next",
        [HtmlChartText.NextStep] = "Next step",
        [HtmlChartText.ClearStep] = "Clear step",
        [HtmlChartText.Link] = "Link",
        [HtmlChartText.CopyScenarioLink] = "Copy scenario link",
        [HtmlChartText.ScenarioTimelineStep] = "Scenario timeline step",
        [HtmlChartText.Pinned] = "Pinned",
        [HtmlChartText.ComparePrevious] = "Previous selected targets",
        [HtmlChartText.CompareNext] = "Next selected targets",
        [HtmlChartText.CompareRange] = "{first}–{last} of {count}",
        [HtmlChartText.Value] = "Value",
        [HtmlChartText.LegendMore] = "More legend entries ({count})",
        [HtmlChartText.LegendPrevious] = "Previous legend entries",
        [HtmlChartText.LegendNext] = "Next legend entries",
        [HtmlChartText.LegendRange] = "{first}–{last} of {count}",
        [HtmlChartText.LegendMute] = "Mute {label}",
        [HtmlChartText.LegendIsolate] = "Isolate {label}",
        [HtmlChartText.LegendIsolateButton] = "Isolate",
        };
        ChartKeys = chart.Keys.ToArray();
        var graph = GraphDefaults();
        var report = ReportDefaults();
        var topology = TopologyHtmlText.Defaults.ToDictionary(pair => (HtmlChartText)Enum.Parse(typeof(HtmlChartText), pair.Key), pair => pair.Value);
        var messages = new Dictionary<HtmlChartText, string>(chart);
        foreach (var pair in graph.Concat(topology).Concat(report)) messages[pair.Key] = pair.Value;
        Defaults = messages;
        GraphKeys = graph.Keys.Concat(topology.Keys.Where(key => key.ToString().StartsWith("Explorer", StringComparison.Ordinal)))
            .Concat(new[] { HtmlChartText.ZoomIn, HtmlChartText.ZoomOut, HtmlChartText.Clear, HtmlChartText.Pause }).Distinct().ToArray();
        TopologyKeys = topology.Keys.ToArray();
        ReportKeys = report.Keys.Concat(new[] { HtmlChartText.Next, HtmlChartText.ExplorerOverview }).ToArray();
    }

    /// <summary>Gets or sets the HTML language tag, or null to inherit the chart's accessibility language.</summary>
    public string? Language {
        get => _language;
        set {
            if (value != null && (string.IsNullOrWhiteSpace(value) || value.Any(char.IsControl)))
                throw new ArgumentException("A language tag must be nonempty and contain no control characters.", nameof(value));
            _language = value?.Trim();
        }
    }

    /// <summary>Gets or sets text direction for the HTML controls. Native plot and axis directions remain authored.</summary>
    public HtmlChartTextDirection Direction {
        get => _direction;
        set {
            if (value < HtmlChartTextDirection.Automatic || value > HtmlChartTextDirection.RightToLeft)
                throw new ArgumentOutOfRangeException(nameof(value));
            _direction = value;
        }
    }

    /// <summary>Gets or replaces one message. Dynamic messages use the documented named placeholders.</summary>
    /// <param name="message">The adapter message to translate.</param>
    /// <returns>The translated message, or its English default.</returns>
    public string this[HtmlChartText message] {
        get => _overrides.TryGetValue(message, out var text) ? text : Defaults[message];
        set {
            if (!Defaults.ContainsKey(message)) throw new ArgumentOutOfRangeException(nameof(message));
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A translated message must be nonempty.", nameof(value));
            _overrides[message] = value;
        }
    }

    internal string? DirectionAttribute => Direction == HtmlChartTextDirection.RightToLeft ? "rtl"
        : Direction == HtmlChartTextDirection.LeftToRight ? "ltr" : null;
    internal string ResolveLanguage(string? inherited) => Language ?? (string.IsNullOrWhiteSpace(inherited) ? "en" : inherited!);
    internal string Format(HtmlChartText message, string name, int count) => Format(message, (name, count.ToString(CultureInfo.InvariantCulture)));
    internal string Format(HtmlChartText message, params (string Name, string Value)[] values) => TopologyHtmlText.FormatTemplate(this[message], values);
    internal string ToJson() => ToJson(ChartKeys);
    internal string ToGraphJson() => ToJson(GraphKeys);
    internal string ToReportJson() => ToJson(ReportKeys);
    internal TopologyHtmlText ForTopology(string? inheritedLanguage) => new TopologyHtmlText(
        TopologyKeys.ToDictionary(key => key.ToString(), key => this[key]), ToJson(TopologyKeys), ResolveLanguage(inheritedLanguage), DirectionAttribute);
    private string ToJson(IEnumerable<HtmlChartText> keys) => "{" + string.Join(",", keys.Select(key => HtmlJsonString.Encode(key.ToString())
        + ":" + HtmlJsonString.Encode(this[key]))) + "}";
    internal void CopyTo(HtmlChartLocalization target) {
        target.Language = Language; target.Direction = Direction;
        foreach (var item in _overrides) target[item.Key] = item.Value;
    }
}

/// <summary>Controls the HTML adapter's text and chrome direction without reversing native chart axes.</summary>
public enum HtmlChartTextDirection {
    /// <summary>Inherits the host document's direction; standalone pages use the browser's default.</summary>
    Automatic,
    /// <summary>Uses left-to-right HTML controls.</summary>
    LeftToRight,
    /// <summary>Uses right-to-left HTML controls.</summary>
    RightToLeft
}
