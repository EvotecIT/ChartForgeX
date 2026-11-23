using System;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

internal static class ExampleInteractiveScenarios {
    public static void ConfigureDomainSecurity(ChartInteractionOptions interaction, ChartSeriesId passedId, ChartSeriesId warningsId, ChartSeriesId failedId) {
        if (interaction == null) throw new ArgumentNullException(nameof(interaction));
        interaction
            .AddScenario("healthy-trend", "Healthy trend", scenario => scenario
                .Color("#22C55E")
                .Description("Focus on successful checks and the weekly control target.")
                .PlaybackDelayMilliseconds(1200).LoopPlayback(false).AutoPlay(false)
                .Metadata("view", "operations")
                .AddSeriesStep(passedId, "Passed checks", configure: step => step.DurationMilliseconds(1600).Metadata("signal", "success")))
            .AddScenario("risk-review", "Risk review", scenario => scenario
                .Color("#F97316")
                .Description("Review warnings and remaining failures before escalation.")
                .PlaybackDelayMilliseconds(1100).LoopPlayback(false).AutoPlay(false)
                .Metadata("view", "risk")
                .AddSeriesStep(warningsId, "Warnings", "Inspect the warning trend before escalation.")
                .AddSeriesStep(failedId, "Failures", "Confirm whether failures are recovering."))
            .ActiveScenario("risk-review")
            .DeepLinkState();
    }
}
