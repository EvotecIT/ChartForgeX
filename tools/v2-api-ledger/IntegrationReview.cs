namespace ApiLedger;

internal sealed record Review(string Disposition, string Replacement, string Evidence);

/// <summary>Records source-reviewed baseline differences without claiming executed migration proof.</summary>
internal static class IntegrationReview {
    internal static Review For(ApiSymbol baseline, ApiSymbol? integrated) {
        const string oldOptions = "ChartForgeX.Markup.Mermaid.MermaidVisualMarkupRenderOptions";
        if (integrated == null && (baseline.Type == oldOptions || baseline.References.Split(';').Contains(oldOptions, StringComparer.Ordinal)))
            return new("canonical-main239-replacement", baseline.Signature.Replace(oldOptions, "ChartForgeX.Mermaid.MermaidRenderOptions", StringComparison.Ordinal),
                "Integrated ChartForgeX.Mermaid/MermaidRenderOptions.cs; Markup.Mermaid parser constructors; existing SmokeTests/MarkupMermaidTests.cs:615.");
        if (baseline.DocId == "P:ChartForgeX.Core.Chart.Options" && integrated?.Signature == "public ChartForgeX.Core.ChartOptions ChartForgeX.Core.Chart.Options { get; private set; }")
            return new("external-getter-contract-retained", "Private setter is implementation-only; callers still get the options object.", "Integrated ChartForgeX/Core/Chart.cs Options declaration.");
        if (baseline.DocId == "T:ChartForgeX.Mermaid.MermaidFlowchartDocument" && integrated != null && !integrated.Signature.Contains("sealed", StringComparison.Ordinal))
            return new("main239-inheritance-expansion", "Flowchart document becomes inheritable for domain-specific Mermaid document types.", "Integrated MermaidFlowchartDocument.cs and MermaidSwimlaneDocument.cs/MermaidUseCaseDocument.cs.");
        bool unchanged = integrated != null && baseline.Signature == integrated.Signature && baseline.Attributes == integrated.Attributes && baseline.Constant == integrated.Constant;
        return new(unchanged ? "unchanged" : "needs-review", "", "");
    }
}
