internal static class MermaidTreeViewExamples {
    internal const string Source = """
        ---
        config:
          treeView:
            showIcons: true
        ---
        treeView-beta
        Rendering/
        ├── Source/
        │   ├── ChartEngine.cs :::highlight ## Shared SVG and native PNG
        │   └── ColorScale.cs ## Numeric color mapping
        ├── Fixtures/ ## Deterministic example data
        └── README.md icon(file) ## Public entry point
        """;
}
