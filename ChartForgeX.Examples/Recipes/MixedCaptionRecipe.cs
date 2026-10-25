/// <summary>Ordinary Core-only editable native strong/italic captions with script-free SVG/PNG exports.</summary>
public static class MixedCaptionRecipe {
    /// <summary>Creates one compact/wide light/dark native artifact without a parser or adapter dependency.</summary>
    public static VisualArtifact Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light, string? fontFamily = null) {
        if (variant is not ("wide" or "compact")) throw new ArgumentOutOfRangeException(nameof(variant));
        var width = variant == "compact" ? 390 : 1100;
        var chart = TopologyChart.Create().WithViewport(width, 420).WithTheme(VisualTheme.Report(), mode);
        if (fontFamily != null) chart.WithTheme(chart.Theme.WithFontFamily(fontFamily), mode);
        var node = new TopologyNode { Id = "review", X = 30, Y = 40, Width = width - 70, Height = 280, ShowStatusBadge = false, FitLabelToContent = true,
            LabelContent = TextContent.FromRuns(new[] { new TextContentRun("Review "), new TextContentRun("carefully", TextInlineEmphasis.Bold), new TextContentRun(" and "), new TextContentRun("record", TextInlineEmphasis.Italic), new TextContentRun("\naccepted work") }) };
        node.Details.Add(new TopologyNodeDetail { Label = "Status:", ValueContent = TextContent.FromRuns(new[] { new TextContentRun("Ready "), new TextContentRun("for approval", TextInlineEmphasis.Bold | TextInlineEmphasis.Italic) }) });
        chart.Nodes.Add(node);
        chart.DefaultRenderOptions = new TopologyRenderOptions { AllowMultilineNodeLabels = true, WrapNodeLabels = true, MaxNodeLabelLines = 5 };
        return chart.ToVisualArtifact();
    }
}
