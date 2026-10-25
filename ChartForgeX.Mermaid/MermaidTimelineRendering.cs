using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

/// <summary>Maps parsed Mermaid Timeline notation to its reusable native diagram.</summary>
public static class MermaidTimelineRendering {
    /// <summary>Creates the native diagram, retaining authored ordering and section identity.</summary>
    public static TimelineDiagramBlock ToTimelineDiagramBlock(this MermaidTimelineDocument document, MermaidTimelineRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidTimelineRenderOptions();
        var block = TimelineDiagramBlock.Create().WithTitle(MermaidHeading.Title(document, options.Title, document.Title, "Mermaid timeline"))
            .WithSubtitle(MermaidHeading.Subtitle(options.Subtitle)).WithSize(options.Width, options.Height);
        for (var index = 0; index < document.Sections.Count; index++) block.AddSection("section-" + index.ToString(CultureInfo.InvariantCulture), document.Sections[index].Name);
        block.Direction = document.Direction == MermaidTimelineDirection.TopDown ? TimelineDiagramDirection.TopToBottom : TimelineDiagramDirection.LeftToRight;
        for (var index = 0; index < document.Periods.Count; index++) {
            var item = document.Periods[index];
            var section = item.Section == null ? -1 : document.Sections.IndexOf(item.Section);
            if (item.Section != null && section < 0) throw new ArgumentException("A timeline item references a section outside its document.", nameof(document));
            block.AddPeriod("period-" + index.ToString(CultureInfo.InvariantCulture), NormalizeBreaks(item.Text), item.Events.Select(value => NormalizeBreaks(value.Text)), section < 0 ? null : "section-" + section.ToString(CultureInfo.InvariantCulture));
        }
        return MermaidPresentation.Apply(block, document, options.Presentation);
    }

    /// <summary>Wraps the native diagram in the shared bounded Mermaid artifact policy.</summary>
    public static VisualArtifact ToVisualArtifact(this MermaidTimelineDocument document, MermaidTimelineRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidTimelineRenderOptions();
        var block = document.ToTimelineDiagramBlock(options);
        var id = string.IsNullOrWhiteSpace(options.Id) ? "mermaid-timeline" : options.Id!.Trim();
        var artifact = VisualArtifact.Create(id, VisualArtifactKind.Mermaid, block);
        artifact.SourceLanguage = VisualArtifactSourceLanguage.Mermaid; artifact.Title = block.Title; artifact.Subtitle = block.Subtitle;
        artifact.NaturalSize = new VisualArtifactSize(block.Options.Size.Width, block.Options.Size.Height);
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png;
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString(); artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.sections"] = document.Sections.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.periods"] = document.Periods.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.events"] = document.Periods.Sum(period => period.Events.Count).ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.direction"] = document.Direction.ToString();
        artifact.Metadata["render.model"] = nameof(TimelineDiagramBlock);
        MermaidCategoricalSourceFacts.Apply(artifact, document);
        return MermaidPresentation.Apply(artifact, document);
    }
    /// <summary>Renders native static SVG.</summary>
    public static string ToSvg(this MermaidTimelineDocument document, MermaidTimelineRenderOptions? options = null) => document.ToVisualArtifact(options).ToSvg();
    /// <summary>Renders native PNG from the same composed scene.</summary>
    public static byte[] ToPng(this MermaidTimelineDocument document, MermaidTimelineRenderOptions? options = null) => document.ToVisualArtifact(options).ToPng();
    private static string NormalizeBreaks(string value) => value.Replace("<br/>", "\n").Replace("<br />", "\n").Replace("<br>", "\n");
}

/// <summary>Defines the natural native diagram frame and shared presentation.</summary>
public sealed class MermaidTimelineRenderOptions : MermaidDiagramRenderOptions {
    /// <summary>Gets or sets the diagram width.</summary>
    public int Width { get; set; } = 960;
    /// <summary>Gets or sets the diagram height.</summary>
    public int Height { get; set; } = 560;
}
