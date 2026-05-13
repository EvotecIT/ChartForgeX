using System;
using ChartForgeX.Interactivity;

internal static class ExampleInteractiveScenarios {
    public static void ConfigureDomainSecurity(ChartInteractionOptions interaction) {
        if (interaction == null) throw new ArgumentNullException(nameof(interaction));
        interaction
            .AddScenario("healthy-trend", "Healthy trend", scenario => scenario
                .WithColor("#22C55E")
                .WithDescription("Focus on successful checks and the weekly control target.")
                .WithMetadata("view", "operations")
                .AddSeriesStep("0", "Passed checks", configure: step => step.WithMetadata("signal", "success")))
            .AddScenario("risk-review", "Risk review", scenario => scenario
                .WithColor("#F97316")
                .WithDescription("Review warnings and remaining failures before escalation.")
                .WithMetadata("view", "risk")
                .AddSeriesStep("1", "Warnings")
                .AddSeriesStep("2", "Failures"))
            .WithActiveScenario("risk-review")
            .WithDeepLinkState();
    }
}
