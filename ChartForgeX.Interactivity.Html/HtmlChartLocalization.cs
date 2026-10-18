using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Localizes HTML controls independently from chart data labels and native number/date formatting.</summary>
/// <remarks>Unspecified messages use English. Templates retain named placeholders such as {count}, {current} and {total}.
/// Chart language is inherited when Language is null. Direction affects adapter chrome, not native axis order.</remarks>
public sealed class HtmlChartLocalization {
    private readonly Dictionary<HtmlChartText, string> _overrides = new Dictionary<HtmlChartText, string>();
    private string? _language;
    private HtmlChartTextDirection _direction;
    private static readonly IReadOnlyDictionary<HtmlChartText, string> Defaults = new Dictionary<HtmlChartText, string> {
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
    };

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

    internal string DirectionAttribute => Direction == HtmlChartTextDirection.RightToLeft ? "rtl"
        : Direction == HtmlChartTextDirection.LeftToRight ? "ltr" : "auto";
    internal string ResolveLanguage(string? inherited) => Language ?? inherited ?? "en";
    internal string Format(HtmlChartText message, string name, int count) => this[message]
        .Replace("{" + name + "}", count.ToString(CultureInfo.InvariantCulture));
    internal string ToJson() => "{" + string.Join(",", Defaults.Select(item => HtmlJsonString.Encode(item.Key.ToString())
        + ":" + HtmlJsonString.Encode(this[item.Key]))) + "}";
    internal void CopyTo(HtmlChartLocalization target) {
        target.Language = Language; target.Direction = Direction;
        foreach (var item in _overrides) target[item.Key] = item.Value;
    }
}

/// <summary>Controls the HTML adapter's text and chrome direction without reversing native chart axes.</summary>
public enum HtmlChartTextDirection {
    /// <summary>Lets the browser determine direction from the displayed text.</summary>
    Automatic,
    /// <summary>Uses left-to-right HTML controls.</summary>
    LeftToRight,
    /// <summary>Uses right-to-left HTML controls.</summary>
    RightToLeft
}
