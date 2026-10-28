/// <summary>A complete source-ordered cause tree renders through the native Fishbone owner.</summary>
public static class IshikawaDepthRecipe {
    /// <summary>Creates complete measured cause captions; a compact host may scroll the natural frame.</summary>
    public static FishboneDiagramBlock Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact")) throw new ArgumentOutOfRangeException(nameof(variant));
        var block = FishboneDiagramBlock.Create("Delayed service release").WithTitle("Measured cause tree")
            .WithTheme(VisualTheme.Graphite(), mode).WithPadding(28);
        var cause0 = block.AddCause("People and ownership handoffs");
        var cause1 = cause0.AddChild("Regional review coordination");
        cause1.AddChild("Missing reviewer availability");
        cause1.AddChild("Unclear escalation owner");
        var cause4 = cause0.AddChild("Onboarding and knowledge transfer");
        cause4.AddChild("Missing operational runbooks");
        cause4.AddChild("Incomplete service ownership notes");
        var cause7 = block.AddCause("Process and approval checkpoints");
        var cause8 = cause7.AddChild("Change approval scheduling");
        cause8.AddChild("Conflicting deployment windows");
        cause8.AddChild("Late security review evidence");
        var cause11 = cause7.AddChild("Release communication quality");
        cause11.AddChild("Missing rollback decision record");
        cause11.AddChild("Ambiguous completion checklist");
        var cause14 = block.AddCause("Platform and build reliability");
        var cause15 = cause14.AddChild("Repeatable build environment");
        cause15.AddChild("Unpinned operating environment");
        cause15.AddChild("Incomplete dependency inventory");
        var cause18 = cause14.AddChild("Test feedback and diagnostics");
        cause18.AddChild("Missing failure artifact links");
        cause18.AddChild("Slow feedback triage routing");
        var cause21 = block.AddCause("Measurement and delivery facts");
        var cause22 = cause21.AddChild("Readiness data collection");
        cause22.AddChild("Incomplete acceptance evidence");
        cause22.AddChild("Missing deployment status owner");
        var cause25 = cause21.AddChild("Outcome and follow-up tracking");
        cause25.AddChild("Unclear customer impact window");
        cause25.AddChild("Missing next checkpoint context");
        return block.WithNaturalSize(variant == "compact" ? 390 : 1100, 520);
    }
}
