namespace ApiLedger;

internal sealed record Mapping(string Owner, string Disposition, string Phase, string Replacement, string RequiredProof, string Rule);

/// <summary>Assigns capability ownership; planned breaking changes are never reported as implemented.</summary>
internal static class Ownership {
    private static readonly string[] Diagrams = ["Wardley", "Venn", "Packet", "GitGraph", "Fishbone", "BlockLayout"];
    private static readonly string[] SharedBlocks = ["IVisualBlock", "VisualBlock", "VisualBlockOptions", "VisualBlockLayout", "VisualBlockKind", "VisualBlockRendering", "SvgVisualBlockRenderer", "PngVisualBlockRenderer", "HtmlVisualBlockRenderer"];
    private static readonly string[] Chrome = ["WithMenu", "ShowMenu", "WithSelectionControls", "ShowSelectionControls", "WithNavigation", "WithNavigationSymbols", "ShowNavigation", "PreviousNavigationSymbol", "NextNavigationSymbol", "WithAction", "WithActionStyle", "ActionLabel", "ActionSymbol", "ActionUrl", "ActionBackground", "ActionForeground"];

    internal static Mapping For(ApiSymbol api) {
        const string oldOptions = "ChartForgeX.Markup.Mermaid.MermaidVisualMarkupRenderOptions";
        if (api.Type == oldOptions || api.References.Split(';').Contains(oldOptions, StringComparer.Ordinal))
            return New(api.Type == oldOptions ? "ChartForgeX.Mermaid" : api.Assembly, "replaced-in-integrated-baseline", "integrated-main239",
                api.Signature.Replace(oldOptions, "ChartForgeX.Mermaid.MermaidRenderOptions", StringComparison.Ordinal),
                "SmokeTests/MarkupMermaidTests.cs:615 per-diagram options; verify same named properties and parser construction with MermaidRenderOptions.", "mermaid-main239-canonical-options");
        if (api.Assembly != "ChartForgeX") return New(api.Assembly, "retained", "existing-adapter", "Retain typed adapter/parser API; consume common core contracts when applicable.", "Adapter parse/render or HTML interaction fixture; framework/package closure.", "existing-adapter");
        if (api.Namespace == "ChartForgeX.VisualBlocks" && Chrome.Contains(api.Name, StringComparer.Ordinal))
            return New("removed-static-chrome", "removal-planned", "later-family-migration", "Omit decorative controls in static output; real controls belong to HTML/host adapters. Retain factual checked/completed markers.", "Consumer-member audit and before/after static fixture before deletion.", "explicit-static-control-member");
        if (api.Namespace.Contains(".Stories", StringComparison.Ordinal) || api.Namespace.Contains(".Terminal", StringComparison.Ordinal) || api.Namespace.Contains(".Motion", StringComparison.Ordinal) ||
            api.Source.EndsWith("/ChartExtensions.Stories.cs", StringComparison.Ordinal) || api.Source.EndsWith("/ChartExtensions.Terminal.cs", StringComparison.Ordinal) ||
            api.Type.Contains("TopologyMotion", StringComparison.Ordinal) || api.Name is "ToGif" or "ToApng" or "SaveGif" or "SaveApng" or "WriteGif" or "WriteApng" ||
            api.Type.Contains("VisualGridMotion", StringComparison.Ordinal))
            return New("ChartForgeX.Stories", "move-planned", "later-package-extraction", "Stories namespace/extensions; accept common static renderables without a Visuals assembly reference.", "GIF/APNG decoded frame, timing/loop/alpha and deterministic terminal/topology story fixtures.", "animation-and-story-ownership");
        if (api.Type.EndsWith("TopologyRenderOptions", StringComparison.Ordinal) && api.Name == "Motion")
            return New("ChartForgeX.Stories", "replacement-planned", "later-package-extraction", "Separate Stories topology animation options; static render options remain in core.", "Static topology package works alone; animated route/scenario fixture with Stories.", "mixed-static-options-motion-member");
        if (api.Type.Contains("VisualWatermark", StringComparison.Ordinal) || api.Name == "Watermarks")
            return New("ChartForgeX.Visuals", "replacement-planned", "later-package-extraction", "Visuals watermark decorator over a common renderable/prepared visual; remove watermark field from core render options.", "SVG/PNG text/image watermark, anchors, repeats, alpha; PowerBGInfo asset import.", "watermark-composition");
        if (api.Namespace.Contains(".Composition", StringComparison.Ordinal) || api.Namespace.Contains(".VisualCanvas", StringComparison.Ordinal) || api.Receiver.StartsWith("VisualCanvas", StringComparison.Ordinal) ||
            api.Receiver.StartsWith("ImageComposition", StringComparison.Ordinal) || api.Receiver == "VisualGrid" || api.Type.EndsWith("TableArtifactRendering", StringComparison.Ordinal))
            return Visuals("canvas-composition-or-table-display");
        if (api.Type.EndsWith("CompositeArtifactRendering", StringComparison.Ordinal)) {
            if (api.Receiver == "VisualStory") return New("ChartForgeX.Stories", "move-planned", "later-package-extraction", "Story-owned artifact producer.", "Story artifact static export and semantic interchange.", "artifact-producer-story");
            if (api.Receiver == "VisualCanvas") return Visuals("artifact-producer-canvas");
            return CoreReplacement("Common artifact producer contract; keep core grid and diagram semantics, optional producers in their owning packages.", "Core/Visuals/Stories artifact creation with no core optional-package reference.", "mixed-artifact-producers");
        }
        if (api.Namespace == "ChartForgeX.VisualBlocks") {
            if (Diagrams.Any(prefix => SimpleType(api).StartsWith(prefix, StringComparison.Ordinal))) return Core("genuine-diagram");
            if (SharedBlocks.Any(name => SimpleType(api) == name || SimpleType(api).StartsWith(name + "<", StringComparison.Ordinal)))
                return CoreReplacement("Common renderable/frame contract in core; diagram renderers remain core and factual composition rendering moves to Visuals.", "Mermaid six diagram-family fixtures; Visuals factual tiles; package-cycle check.", "mixed-visual-block-contract");
            if (api.Source.Contains("VisualGridMotion", StringComparison.Ordinal)) return New("ChartForgeX.Stories", "move-planned", "later-package-extraction", "Stories grid animation over common static renderables.", "Grid animation frame/timing fixture.", "grid-animation");
            return Visuals("factual-block-or-heterogeneous-composition");
        }
        if (api.Type.EndsWith("VisualArtifactRendering", StringComparison.Ordinal) || api.Type.EndsWith("VisualArtifactRenderOptions", StringComparison.Ordinal))
            return CoreReplacement("Common renderable dispatch and backend options; optional package producers/decorators own their behavior.", "Core chart/diagram and optional visual/story artifact export, semantic interchange and package graph.", "artifact-dispatch-options");
        if (api.Type.EndsWith(".VisualArtifact", StringComparison.Ordinal) && api.Name is "Model" or "Create")
            return CoreReplacement("Typed/common renderable handoff while retaining neutral semantic envelope data.", "Core/Visuals/Stories artifact creation and Office semantic interchange roundtrip.", "artifact-model-handoff");
        if (api.Type.EndsWith("VisualArtifactSize", StringComparison.Ordinal) || api.Type.EndsWith("ChartSize", StringComparison.Ordinal) ||
            api.Type.EndsWith("ChartTheme", StringComparison.Ordinal) || api.Type.EndsWith("VisualDesignTokens", StringComparison.Ordinal))
            return CoreReplacement("Shared VisualSize/VisualTheme contracts; migrate actual calls after API spike, preserving explicit caller overrides.", "Shared size/theme/frame consistency; consumer old-to-new compile fixtures.", "shared-size-theme-contract");
        return Core("retained-core-capability");
    }

    private static string SimpleType(ApiSymbol api) => api.Type[(api.Type.LastIndexOf('.') + 1)..];
    private static Mapping Core(string rule) => New("ChartForgeX", "retained", "phase1-or-later-family", "Retain capability; use common size/frame/theme/pipeline during its family migration.", "Existing family contract tests plus v2 SVG/PNG/shared-contract fixtures during migration.", rule);
    private static Mapping CoreReplacement(string replacement, string proof, string rule) => New("ChartForgeX", "replacement-planned", "phase1-contract-or-later-family", replacement, proof, rule);
    private static Mapping Visuals(string rule) => New("ChartForgeX.Visuals", "move-planned", "later-package-extraction", "Visuals-owned static composition/factual presentation over core renderables; migrate package/namespace intentionally.", "PowerBGInfo canvas/composition scripts, ImagePlayground cmdlets and relevant existing SVG/PNG fixtures; compiling migration example.", rule);
    private static Mapping New(string owner, string disposition, string phase, string replacement, string proof, string rule) => new(owner, disposition, phase, replacement, proof, rule);
}
