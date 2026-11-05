using System.Collections.Generic;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlChartLocalization {
    private static Dictionary<HtmlChartText, string> DiagramDefaults() => new() {
        [HtmlChartText.DiagramPlayback] = "Message playback",
        [HtmlChartText.DiagramHint] = "Hover or tap a shape, or use Tab and the arrow keys. Escape clears.",
        [HtmlChartText.DiagramRoute] = "{source} → {target}",
        [HtmlChartText.DiagramIncoming] = "From {items}",
        [HtmlChartText.DiagramOutgoing] = "To {items}",
        [HtmlChartText.DiagramMessage] = "Message {current} of {count}",
        [HtmlChartText.DiagramParticipantMessages] = "{sent} sent · {received} received",
    };

    // Playback reuses the scenario step controls' messages so translated pages stay consistent.
    private static readonly HtmlChartText[] DiagramPlaybackKeys = {
        HtmlChartText.Steps, HtmlChartText.StepProgress, HtmlChartText.Previous, HtmlChartText.PreviousStep, HtmlChartText.Play,
        HtmlChartText.Pause, HtmlChartText.Next, HtmlChartText.NextStep
    };
}
