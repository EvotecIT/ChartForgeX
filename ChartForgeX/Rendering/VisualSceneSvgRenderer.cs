using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Serializes native scene decisions; it never lays out or parses a rendered visual.</summary>
internal static partial class VisualSceneSvgRenderer {
    internal static string Render(VisualScene scene, string? title = null, string? description = null,
        string? language = null, bool decorative = false, string? idPrefix = null, VisualSvgOptions? options = null, bool capturePngText = false, int? maximumCharacters = null) =>
        Render(scene, title, description, language, decorative, idPrefix, options, capturePngText, System.Threading.CancellationToken.None, maximumCharacters);

    internal static string Render(VisualScene scene, string? title, string? description,
        string? language, bool decorative, string? idPrefix, VisualSvgOptions? options, bool capturePngText,
        System.Threading.CancellationToken cancellation, int? maximumCharacters = null) {
        return Render(scene, title, description, language, decorative, idPrefix, options, capturePngText, cancellation, out _, out _, maximumCharacters);
    }
    /// <summary>Returns the resolved namespace from this exact emission, including automatic/native identity.</summary>
    internal static string Render(VisualScene scene, string? title, string? description, string? language,
        bool decorative, string? idPrefix, VisualSvgOptions? options, bool capturePngText, out string resolvedPrefix, int? maximumCharacters = null) =>
        Render(scene, title, description, language, decorative, idPrefix, options, capturePngText, System.Threading.CancellationToken.None, out resolvedPrefix, out _, maximumCharacters);
    internal static string RenderForHtml(VisualScene scene, string? title, string? description, string? language,
        bool decorative, string? idPrefix, VisualSvgOptions options, bool capturePngText,
        out string resolvedPrefix, out VisualSvgFontResources? fonts) =>
        Render(scene, title, description, language, decorative, idPrefix, options, capturePngText,
            System.Threading.CancellationToken.None, out resolvedPrefix, out fonts);
    private static string Render(VisualScene scene, string? title, string? description, string? language,
        bool decorative, string? idPrefix, VisualSvgOptions? options, bool capturePngText,
        System.Threading.CancellationToken cancellation, out string resolvedPrefix, out VisualSvgFontResources? fonts, int? maximumCharacters = null) {
        if (scene == null) throw new ArgumentNullException(nameof(scene));
        cancellation.ThrowIfCancellationRequested();
        var fluid = PrepareFluidLayout(scene, options);
        var imageCharacters = maximumCharacters.HasValue ? AdmitSvgImageUris(scene, maximumCharacters.Value) : 0;
        var effects = EffectPlans(scene, options, cancellation);
        var textImages = options != null && options.TextPresentation == VisualSvgTextPresentation.Automatic
            ? NativeLineInkResources.Prepare(scene, options, effects, cancellation, maximumCharacters, imageCharacters) : null;
        var surfaces = effects?.Shadows;
        var prefix = idPrefix == null ? Identity(scene, title, description, language, decorative, options) : ValidatePrefix(idPrefix);
        resolvedPrefix = prefix;
        fonts = options?.TextPresentation == VisualSvgTextPresentation.Automatic ? new VisualSvgFontResources(scene, textImages?.Lines) : null;
        string? resourceOwner = null;
        var resourceOwners = maximumCharacters.HasValue ? new Stack<string?>() : null;
        var writer = maximumCharacters.HasValue
            ? new SvgMarkupWriter(4096, maximumCharacters.Value, observed => new VisualResourceLimitException("MaximumOutputBytes", maximumCharacters.Value, observed, resourceOwner))
            : new SvgMarkupWriter(4096);
        writer.StartElement("svg").Attribute("xmlns", "http://www.w3.org/2000/svg");
        if (fluid != null) WriteFluidRootAttributes(writer, scene);
        else writer.Attribute("width", scene.Size.Width).Attribute("height", scene.Size.Height)
            .Attribute("viewBox", "0 0 " + N(scene.Size.Width) + " " + N(scene.Size.Height));
        writer.Attribute("stroke-linecap", "round").Attribute("stroke-linejoin", "round")
            .Attribute("lang", language).Attribute("xml:lang", language);
        if (fluid == null && options?.Responsive == true) writer.Attribute("style", "max-width:100%;height:auto;display:block");
        if (decorative) writer.Attribute("aria-hidden", true).Attribute("focusable", false);
        else {
            writer.Attribute("role", HasNavigation(scene) ? "group" : "img").Attribute("aria-label", title);
            if (!string.IsNullOrEmpty(title)) writer.Attribute("aria-labelledby", prefix + "-title");
            if (!string.IsNullOrEmpty(description)) writer.Attribute("aria-describedby", prefix + "-description");
        }
        var enhancePlan = WriteEnhanceRootAttributes(writer, scene, options, prefix);
        writer.EndStartElement();
        if (!decorative && !string.IsNullOrEmpty(title)) writer.StartElement("title").Attribute("id", prefix + "-title").Text(title!).EndElement();
        if (!decorative && !string.IsNullOrEmpty(description)) writer.StartElement("desc").Attribute("id", prefix + "-description").Text(description!).EndElement();
        WriteEnhanceData(writer, scene, options);
        var colorScopes = ColorScopeOptions(scene, options);
        var clipAliases = fluid == null ? WriteClips(writer, scene, prefix, options, colorScopes)
            : WriteFluidClips(writer, scene, prefix, options, colorScopes, fluid);
        WriteSurfaceFilters(writer, surfaces, prefix, options);
        WriteRetainedEffects(writer, effects, prefix, cancellation);
        WriteFontPalettes(writer, scene);
        var nativeInk = false;
        foreach (var command in scene.Nodes) if (command is VisualSceneText textCommand && textCommand.Text.NativeGlyphInk) { nativeInk = true; break; }
        var pngText = capturePngText || nativeInk || options?.TextPresentation is VisualSvgTextPresentation.Automatic or VisualSvgTextPresentation.NativeContours ? new PngTextResources(prefix) : null;
        for (var i = 0; i < scene.Nodes.Count; i++) {
            cancellation.ThrowIfCancellationRequested();
            var node = scene.Nodes[i];
            var paintOptions = colorScopes == null ? options : colorScopes[i];
            if (node is VisualSceneGroup group) {
                if (resourceOwners != null) {
                    resourceOwners.Push(resourceOwner);
                    if (group.Metadata.TryGetValue("data-node-id", out var nodeIdentity)) resourceOwner = nodeIdentity;
                }
                writer.StartElement(group.Href == null ? "g" : "a"); Semantics(writer, group, prefix, i);
                WriteGroupNavigation(writer, group, decorative, options);
                foreach (var item in group.Metadata) writer.Attribute(item.Key, item.Value);
                WriteEnhanceGroupHooks(writer, group, i, enhancePlan);
                if (group.Opacity != 1) writer.Attribute("opacity", group.Opacity);
                if (group.Metadata.TryGetValue("data-cfx-passive-overlay", out var passive) && passive == "true") writer.Attribute("pointer-events", "none");
                if (group.Metadata.TryGetValue("data-cfx-pin-state-colors", out var pin) && pin == "true") writer.Attribute("style", "forced-color-adjust:none");
                if (group.Clip.HasValue) {
                    var clipIndex = clipAliases.TryGetValue(i, out var canonicalIndex) ? canonicalIndex : i;
                    writer.Attribute("clip-path", "url(#" + prefix + "-clip-" + clipIndex.ToString(CultureInfo.InvariantCulture) + ")");
                }
                if (group.PathClip != null) {
                    var clipIndex = fluid != null && clipAliases.TryGetValue(i, out var canonicalIndex) ? canonicalIndex : i;
                    writer.Attribute("clip-path", "url(#" + prefix + "-clip-" + clipIndex.ToString(CultureInfo.InvariantCulture) + ")");
                }
                if (fluid == null && (group.Rotation.HasValue || group.Translation.HasValue)) {
                    var transform = new StringBuilder();
                    if (group.Rotation.HasValue) {
                    var rotation = group.Rotation.Value;
                    transform.Append("rotate(" + N(rotation.Degrees) + " " + N(rotation.X) + " " + N(rotation.Y) + ")");
                    }
                    if (group.Translation.HasValue) {
                        if (transform.Length > 0) transform.Append(' ');
                        var offset = group.Translation.Value;
                        transform.Append("translate(" + N(offset.X) + " " + N(offset.Y) + ")");
                    }
                    writer.Attribute("transform", transform.ToString());
                }
                if (surfaces != null && surfaces.ContainsKey(i) && options?.EffectProfile == VisualSvgEffectProfile.NativeFilters)
                    writer.Attribute("filter", "url(#" + SurfaceFilterId(prefix, i) + ")");
                if (effects != null && effects.NativeMasks.ContainsKey(i))
                    writer.Attribute("mask", "url(#" + RetainedEffectId(prefix, i) + ")");
                if (effects != null && effects.NativeAppearance.TryGetValue(i, out var appearance) && appearance.Effect != null)
                    writer.Attribute("filter", "url(#" + RetainedAppearanceId(prefix, i, appearance.Scope.Effect) + ")");
                writer.EndStartElement();
                if (effects != null && effects.BakedMasks.TryGetValue(i, out var masked)) {
                    WriteBakedRetainedEffect(writer, masked, cancellation);
                    SkipImageFallback(scene, ref i);
                    writer.EndElement();
                    continue;
                }
                if (surfaces != null && surfaces.TryGetValue(i, out var surface) && options?.EffectProfile != VisualSvgEffectProfile.NativeFilters)
                    WriteBakedSurfaceShadow(writer, surface, cancellation);
                if (options?.PatternTiles == true && WritePatternTile(writer, scene, group, prefix, ref i, paintOptions)) continue;
                if (group.Tooltip != null) writer.StartElement("title").Text(group.Tooltip).EndElement();
                if (group.ImageResource != null) {
                    WriteImageResource(writer, group.ImageResource, prefix, i);
                    SkipImageFallback(scene, ref i);
                    writer.EndElement();
                    if (resourceOwners != null) resourceOwner = resourceOwners.Pop();
                }
            } else if (node is VisualSceneEndGroup) {
                writer.EndElement();
                if (resourceOwners != null) resourceOwner = resourceOwners.Pop();
            }
            else if (fluid != null && WriteFluidNode(writer, scene, i, prefix, paintOptions, fluid, pngText, capturePngText, fonts, textImages)) { }
            else if (node is VisualSceneRectangle rect) {
                if (rect.HasPartialCorners) writer.StartElement("path").Attribute("d", RectanglePathData(rect));
                else writer.StartElement("rect").Attribute("x", rect.Bounds.X).Attribute("y", rect.Bounds.Y)
                    .Attribute("width", rect.Bounds.Width).Attribute("height", rect.Bounds.Height).Attribute("rx", rect.Radius);
                Paint(writer, rect, prefix, i, options: paintOptions); writer.EndEmptyElement();
            } else if (node is VisualSceneEllipse ellipse) {
                writer.StartElement("ellipse").Attribute("cx", ellipse.Cx).Attribute("cy", ellipse.Cy).Attribute("rx", ellipse.Rx).Attribute("ry", ellipse.Ry);
                Paint(writer, ellipse, prefix, i, options: paintOptions); writer.EndEmptyElement();
            } else if (node is VisualSceneLine line) {
                writer.StartElement("line").Attribute("x1", line.Start.X).Attribute("y1", line.Start.Y).Attribute("x2", line.End.X).Attribute("y2", line.End.Y);
                Paint(writer, line, prefix, i, options: paintOptions);
                if (line.Dash != null) {
                    writer.Attribute("stroke-dasharray", DashValue(line.Dash));
                    var phase = ChartStrokeDashes.NormalizeOffset(line.Dash, line.DashOffset);
                    if (phase != 0) writer.Attribute("stroke-dashoffset", ChartStrokeDashes.FormatNumber(phase));
                }
                writer.EndEmptyElement();
            } else if (node is VisualScenePath path) {
                writer.StartElement("path").Attribute("d", PathData(path)); Paint(writer, path, prefix, i, options: paintOptions); writer.EndEmptyElement();
            } else if (node is VisualSceneSlice slice) {
                if (slice.Outer <= 0 || slice.Sweep <= 0) continue;
                writer.StartElement("path").Attribute("d", ChartSlicePathGeometry.BuildPath(slice.Cx, slice.Cy, slice.Outer,
                    slice.Inner, slice.Start, slice.Start + slice.Sweep));
                Paint(writer, slice, prefix, i, options: paintOptions); writer.EndEmptyElement();
            } else if (node is VisualSceneProceduralSource && effects != null && effects.Procedural.TryGetValue(i, out var procedural)) {
                WriteProceduralSource(writer, procedural, prefix, i, options, cancellation);
            } else if (node is VisualSceneImage image) {
                writer.StartElement("image").Attribute("x", image.Bounds.X).Attribute("y", image.Bounds.Y)
                    .Attribute("width", image.Bounds.Width).Attribute("height", image.Bounds.Height)
                    .Attribute("preserveAspectRatio", "none").Attribute("href", image.DataUri);
                WriteImageAlphaBounds(writer, image);
                if (image.Opacity != 1) writer.Attribute("opacity", image.Opacity);
                Semantics(writer, image, prefix, i); writer.EndEmptyElement();
            } else if (node is VisualSceneGradient gradient) WriteGradientShape(writer, gradient, prefix, i, paintOptions);
            else if (node is VisualSceneText text) WriteText(writer, text, prefix, i, paintOptions, pngText, capturePngText, fonts, textImages);
        }
        pngText?.Write(writer);
        cancellation.ThrowIfCancellationRequested();
        writer.EndElement(); return writer.Build();
    }

    private static Dictionary<int, int> WriteClips(SvgMarkupWriter writer, VisualScene scene, string prefix, VisualSvgOptions? options,
        VisualSvgOptions?[]? colorScopes) {
        var opened = false;
        var rectangleClips = new Dictionary<(double X, double Y, double Width, double Height), int>();
        var aliases = new Dictionary<int, int>();
        for (var i = 0; i < scene.Nodes.Count; i++) {
            var node = scene.Nodes[i];
            if (node is VisualSceneGradient gradient) {
                if (!opened) { writer.StartElement("defs").EndStartElement(); opened = true; }
                WriteGradientDefinition(writer, gradient, prefix, i, colorScopes == null ? options : colorScopes[i]);
                continue;
            }
            if (!(node is VisualSceneGroup group) || !group.Clip.HasValue && group.PathClip == null) continue;
            if (group.PathClip == null && group.Clip.HasValue) {
                var bounds = group.Clip.Value;
                var key = (bounds.X, bounds.Y, bounds.Width, bounds.Height);
                if (rectangleClips.TryGetValue(key, out var canonicalIndex)) {
                    aliases.Add(i, canonicalIndex);
                    continue;
                }
                rectangleClips.Add(key, i);
            }
            if (!opened) { writer.StartElement("defs").EndStartElement(); opened = true; }
            writer.StartElement("clipPath").Attribute("id", prefix + "-clip-" + i.ToString(CultureInfo.InvariantCulture)).Attribute("clipPathUnits", "userSpaceOnUse").EndStartElement();
            if (group.PathClip != null) writer.StartElement("path").Attribute("d", PathData(group.PathClip)).Attribute("clip-rule", "evenodd").EndEmptyElement();
            else {
                var clip = group.Clip!.Value;
                writer.StartElement("rect").Attribute("x", clip.X).Attribute("y", clip.Y).Attribute("width", clip.Width).Attribute("height", clip.Height).EndEmptyElement();
            }
            writer.EndElement();
        }
        if (opened) writer.EndElement();
        return aliases;
    }

    private static void Paint(SvgMarkupWriter writer, VisualSceneMark mark, string prefix, int index, string? fillOverride = null, VisualSvgOptions? options = null) {
        if (mark.Opacity != 1) writer.Attribute("opacity", mark.Opacity);
        Semantics(writer, mark, prefix, index);
        // A collapsed filled slice can still have a visible separator stroke; describe fill independently.
        if (mark is VisualSceneSlice slice && !ChartSlicePathGeometry.HasEncodedFillArea(slice.Cx, slice.Cy, slice.Outer, slice.Inner, slice.Start, slice.Sweep))
            writer.Attribute("data-cfx-fill-area", "false");
        var fill = fillOverride ?? ResolvePaint(mark.Fill, mark.Paint?.Fill, options);
        writer.Attribute("fill", fill)
            .Attribute("stroke", ResolvePaint(mark.Stroke, mark.Paint?.Stroke, options)).Attribute("stroke-width", mark.StrokeWidth);
        if ((mark is VisualScenePath || mark is VisualSceneSlice) && fill != "none") writer.Attribute("fill-rule", "evenodd");
        if (mark is VisualScenePath path && path.Cap != VisualStrokeCap.Round) writer.Attribute("stroke-linecap", path.Cap.ToString().ToLowerInvariant());
        if (mark is VisualScenePath joined && joined.Join != VisualStrokeJoin.Round) writer.Attribute("stroke-linejoin", joined.Join.ToString().ToLowerInvariant());
        if (mark is VisualScenePath dashed && dashed.Dash != null) writer.Attribute("stroke-dasharray", DashValue(dashed.Dash));
        if (mark is VisualScenePath phased) {
            var phase = ChartStrokeDashes.NormalizeOffset(phased.Dash, phased.DashOffset);
            if (phase != 0) writer.Attribute("stroke-dashoffset", ChartStrokeDashes.FormatNumber(phase));
        }
    }

    private static string DashValue(IReadOnlyList<double> lengths) =>
        lengths.Count == 0 ? "none" : string.Join(" ", System.Linq.Enumerable.Select(lengths, ChartStrokeDashes.FormatNumber));

    private static void Semantics(SvgMarkupWriter writer, VisualSceneNode node, string prefix, int index) {
        if (node.Id != null) {
            writer.Attribute("id", prefix + "-node-" + index.ToString(CultureInfo.InvariantCulture));
            if (!(node is VisualSceneGroup group) || !group.Metadata.ContainsKey("data-cfx-source-id"))
                writer.Attribute("data-cfx-source-id", node.Id);
        }
        writer.Attribute("data-cfx-role", node.Role);
    }

    private static void WriteText(SvgMarkupWriter writer, VisualSceneText node, string prefix, int index, VisualSvgOptions? options, PngTextResources? pngText, bool capturePngText, VisualSvgFontResources? fonts, NativeLineInkResources? textImages) {
        var prepared = node.Text; var style = prepared.Style;
        writer.StartElement("g"); Semantics(writer, node, prefix, index);
        WritePageFontAttributes(writer, options);
        if (textImages?.ForNode(node) is {} capturedImage) {
            var caption = string.Join("\n", prepared.Lines.Select(line => line.Text));
            writer.Attribute("aria-label", caption).Attribute("data-cfx-caption", caption).EndStartElement();
            writer.StartElement("g").Attribute("aria-hidden", "true").Attribute("data-cfx-native-text-ink", "true")
                .Attribute("data-cfx-native-raster-text-ink", "true").EndStartElement();
            writer.StartElement("image").Attribute("x", capturedImage.Bounds.X).Attribute("y", capturedImage.Bounds.Y)
                .Attribute("width", capturedImage.Bounds.Width).Attribute("height", capturedImage.Bounds.Height)
                .Attribute("preserveAspectRatio", "none").Attribute("href", capturedImage.Uri);
            WriteImageAlphaBounds(writer, capturedImage.Data.AlphaBounds, capturedImage.Data.Image.Width, capturedImage.Data.Image.Height);
            writer.EndEmptyElement();
            writer.EndElement();
            writer.EndElement(); return;
        }
        writer.EndStartElement();
        for (var i = 0; i < prepared.Lines.Count; i++) {
            var line = prepared.Lines[i];
            var delivered = fonts?.ForLine(node, i);
            var nativeInk = prepared.NativeGlyphInk || options?.TextPresentation is VisualSvgTextPresentation.Automatic or VisualSvgTextPresentation.NativeContours;
            if (nativeInk && pngText != null && pngText.TryCapture(node, line, i, true, out var glyphs)) {
                writer.StartElement("g").Attribute("aria-hidden", "true").Attribute("data-cfx-native-text-ink", "true")
                    .Attribute("fill", ResolvePaint(node.Color, node.Paint, options)).Attribute("fill-rule", "nonzero");
                if (delivered != null) writer.Attribute("data-cfx-owned-font-native", delivered.Alias);
                if (node.Stroke.HasValue && node.StrokeWidth > 0) writer.Attribute("stroke", ResolvePaint(node.Stroke, node.StrokePaint, options))
                    .Attribute("stroke-width", node.StrokeWidth).Attribute("stroke-linejoin", "round").Attribute("paint-order", "stroke");
                writer.EndStartElement();
                foreach (var glyph in glyphs) writer.StartElement("use").Attribute("href", "#" + glyph.Id)
                    .Attribute("transform", "matrix(1 0 0 1 " + glyph.X.ToString("R", CultureInfo.InvariantCulture) + " " + glyph.Baseline.ToString("R", CultureInfo.InvariantCulture) + ")").EndEmptyElement();
                writer.EndElement();
                if (delivered == null) continue;
            } else if (nativeInk) throw new NotSupportedException(prepared.NativeGlyphInk
                ? "Structured SVG caption ink requires native outline or built-in TinyFont geometry; color/bitmap-only glyph faces are unsupported."
                : "Captured SVG text ink requires native outline or built-in TinyFont geometry; color/bitmap-only glyph faces are unsupported.");
            writer.StartElement("text").Attribute("x", node.LineLeft(line)).Attribute("y", node.Baseline + i * prepared.Metrics.LineHeight);
            WriteTextFontAttributes(writer, delivered?.Alias ?? style.Font.Family, prepared.Size, options);
            writer.Attribute("font-weight", style.Font.Weight)
                .Attribute("font-style", style.Font.Italic ? "italic" : "normal").Attribute("fill", ResolvePaint(node.Color, node.Paint, options))
                .Attribute("xml:space", "preserve");
            if (delivered != null) writer.Attribute("data-cfx-owned-font-text", delivered.Alias).Attribute("display", "none");
            if (capturePngText && pngText != null) {
                if (pngText.TryCapture(node, line, i, out var references)) writer.Attribute("data-cfx-png-glyphs", references);
                else writer.Attribute("data-cfx-png-font-fallback", "browser-text");
            }
            if (node.Stroke.HasValue && node.StrokeWidth > 0) writer.Attribute("stroke", ResolvePaint(node.Stroke, node.StrokePaint, options))
                .Attribute("stroke-width", node.StrokeWidth).Attribute("stroke-linejoin", "round").Attribute("paint-order", "stroke");
            var css = "white-space:pre";
            if (style.LetterSpacing != 0) css += ";letter-spacing:" + style.LetterSpacing.ToString("R", CultureInfo.InvariantCulture) + "px";
            if (style.Font.Variations.Count > 0) css += ";font-variation-settings:" + style.Font.Variations.Css;
            css += style.OpenTypeLanguageTag == null ? ";font-language-override:normal" : ";font-language-override:'" + style.OpenTypeLanguageTag + "'";
            css += ";font-palette:" + (style.Font.ColorPaletteIndex == 0 ? "normal" : ChartForgeX.Typography.TypographyPaletteCss.Name(style.Font.ColorPaletteIndex));
            if (ChartDomRoles.IsAxisTick(node.Role)) css += ";font-variant-numeric:tabular-nums"; // design rule 7: axis numbers align
            writer.Attribute("style", css).Text(line.Text).EndElement();
        }
        writer.EndElement();
    }

    private static void WriteFontPalettes(SvgMarkupWriter writer, VisualScene scene) {
        var styles = new System.Collections.Generic.List<ChartForgeX.Typography.TextStyleOverride>();
        foreach (var node in scene.Nodes) if (node is VisualSceneText text) {
            var font = text.Text.Style.Font;
            if (font.ColorPaletteIndex == 0) continue;
            styles.Add(new ChartForgeX.Typography.TextStyleOverride { FontFamily = font.Family, ColorPaletteIndex = font.ColorPaletteIndex });
        }
        if (styles.Count == 0) return;
        var rules = ChartForgeX.Typography.TypographyPaletteCss.Rules(string.Empty, styles.ToArray());
        if (rules.Length > 0) writer.StartElement("style").Text(rules).EndElement();
    }

    private static string PathData(VisualScenePath path) => new SvgPathDataBuilder().Append(path.Commands, path.Close).Build();
    private static string RectanglePathData(VisualSceneRectangle rect) => new SvgPathDataBuilder().Append(rect.Outline().Commands, true).Build();

    private static string N(double value) => SvgMarkupWriter.FormatNumber(value);

    /// <summary>Hashes detached typed content for prepared exports without SVG intermediates or random identities.</summary>
    internal static string Identity(VisualScene scene, string? title, string? description, string? language, bool decorative, VisualSvgOptions? options = null) {
        AdmitImageIdentityWork(scene);
        using var hash = SHA256.Create();
        using var stream = new CryptoStream(Stream.Null, hash, CryptoStreamMode.Write);
        // Batch primitive writes without changing the identity byte stream. Dense scenes otherwise
        // send thousands of tiny updates through the crypto transform while exporting SVG.
        using var buffered = new BufferedStream(stream, 16384);
        using (var writer = new BinaryWriter(buffered, Encoding.UTF8, leaveOpen: true)) {
            writer.Write("cfx-native-svg-2");
            if (options?.TextPresentation is VisualSvgTextPresentation.Automatic or VisualSvgTextPresentation.NativeContours) {
                writer.Write("captured-svg-text"); writer.Write((int)options.TextPresentation);
            }
            writer.Write(scene.Size.Width); writer.Write(scene.Size.Height);
            Text(writer, title); Text(writer, description); Text(writer, language); writer.Write(decorative);
            writer.Write(scene.Nodes.Count);
            foreach (var node in scene.Nodes) {
                writer.Write(node switch {
                    VisualSceneGroup => 1, VisualSceneEndGroup => 2, VisualSceneRectangle => 3, VisualSceneEllipse => 4,
                    VisualSceneLine => 5, VisualScenePath => 6, VisualSceneSlice => 7, VisualSceneText => 8, VisualSceneGradient => 9, VisualSceneImage => 10,
                    VisualSceneProceduralSource => 11,
                    _ => throw new NotSupportedException("Unknown native scene command.")
                });
                Text(writer, node.Id); Text(writer, node.Role);
                if (node is VisualSceneMark mark) { Color(writer, mark.Fill); Color(writer, mark.Stroke); writer.Write(mark.StrokeWidth); PaintIdentity(writer, mark.Paint); }
                if (node is VisualSceneMark opacityMark) WriteOpacityIdentity(writer, opacityMark.Opacity);
                if (node is VisualSceneGroup group) {
                    WriteOpacityIdentity(writer, group.Opacity);
                    SurfaceIdentity(writer, group.SurfaceShadow);
                    MaskIdentity(writer, group.RetainedEffect);
                    if (group.ColorScope != null) writer.Write("svg-color-scope");
                    writer.Write(group.Clip.HasValue);
                    if (group.Clip.HasValue) Rectangle(writer, group.Clip.Value);
                    writer.Write(group.PathClip != null);
                    if (group.PathClip != null) WritePathIdentity(writer, group.PathClip);
                    writer.Write(group.Rotation.HasValue);
                    if (group.Rotation.HasValue) {
                        var rotation = group.Rotation.Value;
                        writer.Write(rotation.Degrees); writer.Write(rotation.X); writer.Write(rotation.Y);
                    }
                    Text(writer, group.Href); Text(writer, group.Tooltip);
                    writer.Write(group.Translation.HasValue);
                    if (group.Translation.HasValue) Point(writer, group.Translation.Value);
                    writer.Write(group.ImageResource != null);
                    if (group.ImageResource != null) {
                        var resource = group.ImageResource;
                        Text(writer, resource.Href); Text(writer, resource.PreserveAspectRatio); Rectangle(writer, resource.Bounds);
                        writer.Write(resource.Opacity); Text(writer, resource.Role); Text(writer, resource.Id);
                    }
                    writer.Write(group.Metadata.Count);
                    // The immutable scene sorts metadata with ordinal keys before export.
                    foreach (var item in group.Metadata) { writer.Write(item.Key); writer.Write(item.Value); }
                } else if (node is VisualSceneEndGroup) { }
                else if (node is VisualSceneRectangle rect) { Rectangle(writer, rect.Bounds); writer.Write(rect.Radius); writer.Write((int)rect.Corners); }
                else if (node is VisualSceneEllipse ellipse) {
                    writer.Write(ellipse.Cx); writer.Write(ellipse.Cy); writer.Write(ellipse.Rx); writer.Write(ellipse.Ry);
                } else if (node is VisualSceneLine line) {
                    Point(writer, line.Start); Point(writer, line.End);
                    writer.Write(line.Dash?.Count ?? 0);
                    if (line.Dash != null) foreach (var length in line.Dash) writer.Write(length);
                    WriteDashOffsetIdentity(writer, line.DashOffset);
                } else if (node is VisualScenePath path) {
                    WritePathIdentity(writer, path);
                } else if (node is VisualSceneSlice slice) {
                    writer.Write(slice.Cx); writer.Write(slice.Cy); writer.Write(slice.Outer);
                    writer.Write(slice.Inner); writer.Write(slice.Start); writer.Write(slice.Sweep);
                } else if (node is VisualSceneProceduralSource procedural) {
                    TurbulenceIdentity(writer, procedural);
                } else if (node is VisualSceneImage image) {
                    Rectangle(writer, image.Bounds); writer.Write(image.Opacity); writer.Write(image.Image.Width); writer.Write(image.Image.Height);
                    writer.Write(image.Image.Pixels.Length); writer.Write(image.Image.Pixels);
                } else if (node is VisualSceneGradient gradient) {
                    WriteGradientIdentity(writer, gradient);
                } else if (node is VisualSceneText text) {
                    writer.Write(text.X); writer.Write(text.Baseline); writer.Write((int)text.Alignment); Color(writer, text.Color); Text(writer, text.Paint?.Value);
                    Color(writer, text.Stroke); writer.Write(text.StrokeWidth); Text(writer, text.StrokePaint?.Value);
                    var prepared = text.Text; var style = prepared.Style; var font = style.Font;
                    writer.Write(prepared.Size); writer.Write(prepared.Ascent); writer.Write(prepared.Metrics.Width);
                    writer.Write(prepared.Metrics.Height); writer.Write(prepared.Metrics.LineHeight);
                    writer.Write(font.Family); Text(writer, font.FilePath); Text(writer, font.FaceName);
                    writer.Write(font.CollectionIndex ?? -1); writer.Write(font.Weight); writer.Write(font.Italic);
                    writer.Write(font.Variations.Css); writer.Write(font.ColorPaletteIndex);
                    writer.Write(style.FontSize); writer.Write(style.LineHeight); Color(writer, style.Color);
                    if (style.LetterSpacing != 0) { writer.Write("letter-spacing"); writer.Write(style.LetterSpacing); }
                    writer.Write((int)style.Alignment); writer.Write((int)style.UnderlineStyle); writer.Write((int)style.StrikethroughStyle);
                    writer.Write((int)style.Baseline); writer.Write((int)style.TextCase); writer.Write((int)style.Hinting); Text(writer, style.OpenTypeLanguageTag);
                    writer.Write(prepared.Lines.Count);
                    foreach (var preparedLine in prepared.Lines) { writer.Write(preparedLine.Text); writer.Write(preparedLine.Width); }
                    // Preserve every ordinary identity byte; structured glyph ink has a distinct export contract.
                    if (prepared.NativeGlyphInk) writer.Write("native-inline-glyph-ink");
                } else throw new NotSupportedException("Unknown native scene command.");
            }
        }
        buffered.Flush();
        stream.FlushFinalBlock();
        var result = new StringBuilder("cfx-v2-");
        foreach (var value in hash.Hash!) result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
        return ExportIdentity(scene, result.ToString(), options);
    }

    internal static string ValidatePrefix(string idPrefix) {
        if (idPrefix == null) throw new ArgumentNullException(nameof(idPrefix));
        if (idPrefix.Length == 0 || !Letter(idPrefix[0])) throw new ArgumentException("SVG ID prefixes must begin with an ASCII letter.", nameof(idPrefix));
        foreach (var c in idPrefix) if (!(Letter(c) || c >= '0' && c <= '9' || c == '-' || c == '_' || c == '.'))
            throw new ArgumentException("SVG ID prefixes may contain only ASCII letters, digits, hyphens, underscores and periods.", nameof(idPrefix));
        return idPrefix;
    }

    private static bool Letter(char value) => value >= 'a' && value <= 'z' || value >= 'A' && value <= 'Z';
    private static void Text(BinaryWriter writer, string? value) { writer.Write(value != null); if (value != null) writer.Write(value); }
    private static void Point(BinaryWriter writer, ChartPoint value) { writer.Write(value.X); writer.Write(value.Y); writer.Write(value.BreakBefore); }
    private static void Rectangle(BinaryWriter writer, ChartRect value) { writer.Write(value.X); writer.Write(value.Y); writer.Write(value.Width); writer.Write(value.Height); }
    private static void Color(BinaryWriter writer, ChartColor? value) {
        writer.Write(value.HasValue);
        if (value.HasValue) {
            writer.Write(value.Value.R); writer.Write(value.Value.G); writer.Write(value.Value.B); writer.Write(value.Value.A);
            WriteColorVariableIdentity(writer, value.Value);
        }
    }
}
