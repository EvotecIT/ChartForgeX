using System;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

internal static class ExampleInteractiveScenarios {
    public static void ConfigureDomainSecurity(ChartInteractionOptions interaction, ChartSeriesId passedId, ChartSeriesId warningsId, ChartSeriesId failedId) {
        if (interaction == null) throw new ArgumentNullException(nameof(interaction));
        interaction
            .AddScenario("healthy-trend", "Healthy trend", scenario => scenario
                .WithColor("#22C55E")
                .WithDescription("Focus on successful checks and the weekly control target.")
                .WithPlayback(1200)
                .WithMetadata("view", "operations")
                .AddSeriesStep(passedId, "Passed checks", configure: step => step.WithDuration(1600).WithMetadata("signal", "success")))
            .AddScenario("risk-review", "Risk review", scenario => scenario
                .WithColor("#F97316")
                .WithDescription("Review warnings and remaining failures before escalation.")
                .WithPlayback(1100)
                .WithMetadata("view", "risk")
                .AddSeriesStep(warningsId, "Warnings", "Inspect the warning trend before escalation.")
                .AddSeriesStep(failedId, "Failures", "Confirm whether failures are recovering."))
            .WithActiveScenario("risk-review")
            .WithDeepLinkState();
    }
}
