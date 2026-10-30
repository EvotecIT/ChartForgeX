using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Compiles the sequence model directly to native scene primitives and a detached semantic projection.</summary>
internal static partial class SequencePreparedCompiler {
    internal static PreparedVisual Prepare(SequenceArtifact model, VisualRenderContext context) => Prepare(model, context, false);

    internal static PreparedVisual PrepareDefault(SequenceArtifact model) {
        if (model == null) throw new ArgumentNullException(nameof(model));
        var original = PrepareNatural(model, model.Theme);
        var authoredSize = model.Theme.Typography.DataLabelSize;
        if (authoredSize <= VisualTextFitting.MaximumOrdinaryFontSize || FitsDefaultRaster(original)) return original;
        var minimum = VisualTextFitting.MaximumOrdinaryFontSize;
        var fitted = PrepareNatural(model, FitSequenceTheme(model.Theme, minimum));
        // Huge ordinary diagrams retain the existing raster guard. This policy only
        // bounds the extra canvas growth caused by oversized authored typography.
        if (!FitsDefaultRaster(fitted)) return original;
        var maximum = authoredSize;
        for (var pass = 0; pass < 12; pass++) {
            var size = (minimum + maximum) / 2;
            var candidate = PrepareNatural(model, FitSequenceTheme(model.Theme, size));
            if (FitsDefaultRaster(candidate)) { minimum = size; fitted = candidate; }
            else maximum = size;
        }
        return fitted;
    }

    private static bool FitsDefaultRaster(PreparedVisual visual) {
        var options = new VisualRenderOptions();
        try {
            VisualSceneRasterRenderer.CalculateAllocation(visual.Size,
                options.Scale, options.Supersampling, options.PixelBudget);
            return true;
        } catch (ArgumentOutOfRangeException exception) when (exception.ParamName is "pixelBudget" or "width") {
            return false;
        }
    }

    private static VisualTheme FitSequenceTheme(VisualTheme theme, double size) {
        var typography = theme.Typography;
        var factor = size / typography.DataLabelSize;
        return theme.WithTypography(new VisualTypography(typography.Family,
            typography.TitleSize * factor, typography.SubtitleSize * factor,
            typography.AxisSize * factor, typography.LegendSize * factor, size,
            typography.ScalarValueSize * factor, typography.CenterValueSize * factor,
            tileLabelSize: typography.TileLabelSize, tileValueSize: typography.TileValueSize,
            tileDetailSize: typography.TileDetailSize));
    }

    private static PreparedVisual PrepareNatural(SequenceArtifact model, VisualTheme theme) {
        var measurements = new VisualSceneBuilder(new VisualSize(1, 1), new FontSpec { Family = theme.Typography.Family });
        var presentation = new SequencePreparedPresentation(model, new VisualRenderContext(new VisualLayoutOptions(new VisualSize(1, 1), 0), theme, model.ThemeMode));
        var policy = presentation.Layout;
        var padding = policy?.CanvasPadding ?? ChartPadding.All(model.Padding);
        var minimumLabelWidth = presentation.MinimumLabelWidth(model, measurements, theme.Typography.DataLabelSize);
        var noteWidth = presentation.NoteWidth(minimumLabelWidth);
        double nesting = Math.Max(model.Blocks.Select(b => b.Depth).DefaultIfEmpty(0).Max(), model.Branches.Select(b => b.Depth).DefaultIfEmpty(0).Max());
        double gap = Math.Max(10, theme.Spacing), stroke = Math.Max(2, theme.SeriesStrokeWidth);
        double inset = (policy?.FragmentMargin ?? gap) + nesting * (policy?.FragmentMargin ?? 10) + stroke + (model.Notes.Any(n => n.Placement == SequenceArtifactNotePlacement.Over) ? 32 : 0);
        double noteReserve = (model.Notes.Any(n => n.Placement == SequenceArtifactNotePlacement.LeftOf) ? noteWidth + (policy?.NoteSpacing ?? gap) : 0)
            + (model.Notes.Any(n => n.Placement == SequenceArtifactNotePlacement.RightOf) ? noteWidth + (policy?.NoteSpacing ?? gap) : 0);
        double width = Math.Max(model.Width, padding.Left + padding.Right + inset * 2 + noteReserve
            + (model.Messages.Any(m => m.SourceId == m.TargetId) ? presentation.SelfMessageReserve(model, minimumLabelWidth) : 0)
            + presentation.VisibleParticipantIndices.Count * presentation.MinimumLane(model, Math.Max(140, minimumLabelWidth
                + Math.Max(28, presentation.WrapSides * 2 + (policy?.ParticipantSpacing ?? 12)))));
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, Math.Max(model.Height, padding.Top + padding.Bottom + 512)), padding),
            theme, model.ThemeMode, frame: new VisualFrame(showLegend: false));
        return Prepare(model, context, true);
    }

    private static PreparedVisual Prepare(SequenceArtifact model, VisualRenderContext context, bool fitContent) {
        if (context == null) throw new ArgumentNullException(nameof(context));
        Validate(model);
        context = VisualDiagramPrimitives.WithFrame(context, model.Title, model.Subtitle);
        var builder = new VisualSceneBuilder(context.Layout.Size, context.Font);
        var plot = VisualFrameLayout.Build(builder, context, Array.Empty<VisualLegendEntry>());
        var semantics = VisualArtifactInterchangeMapping.FromPreparedSequence(model, context);
        var layout = SequencePreparedLayout.Calculate(model, semantics, builder, context, plot, fitContent);
        if (fitContent) {
            // Height growth can expose additional heading lines. Recalculate the plot and
            // sequence together; the common frame has at most four heading lines to expose.
            for (var pass = 0; pass < 5; pass++) {
                var height = Math.Max(model.Height, layout.Bottom + context.Layout.PaddingEdges.Bottom + Math.Max(2, context.Theme.SeriesStrokeWidth));
                if (pass > 0) height = Math.Max(context.Layout.Size.Height, height);
                if (Math.Abs(height - context.Layout.Size.Height) < 0.001) break;
                var resolvedSize = new VisualSize(context.Layout.Size.Width, height);
                context = new VisualRenderContext(new VisualLayoutOptions(resolvedSize, context.Layout.PaddingEdges), context.Theme, context.ThemeMode, context.Frame, context.Font);
                builder = new VisualSceneBuilder(resolvedSize, context.Font);
                plot = VisualFrameLayout.Build(builder, context, Array.Empty<VisualLegendEntry>());
                semantics = VisualArtifactInterchangeMapping.FromPreparedSequence(model, context);
                layout = SequencePreparedLayout.Calculate(model, semantics, builder, context, plot, true);
            }
        }
        semantics.Extensions["chartforgex.source.width"] = model.Width.ToString("R", CultureInfo.InvariantCulture);
        semantics.Extensions["chartforgex.source.height"] = model.Height.ToString("R", CultureInfo.InvariantCulture);
        var colors = context.Theme.Resolve(context.ThemeMode);
        double stroke = context.Theme.AxisStrokeWidth;
        var paint = model.Paint;
        ParticipantGroups(builder, model, layout, semantics, context);
        foreach (var item in layout.Blocks.Where(item => model.Blocks[item.Index].BackgroundColor.HasValue).OrderBy(item => model.Blocks[item.Index].Depth)) {
            var block = model.Blocks[item.Index];
            var backgroundOnly = block.Kind == SequenceArtifactBlockKind.Rect;
            using (builder.PushGroup(backgroundOnly ? item.Id : item.Id + "-background", backgroundOnly ? "sequence-block" : "sequence-background",
                new Dictionary<string, string> { ["data-label"] = block.Text })) {
                using (Surface(builder, paint.BackgroundShadow, item.Bounds, 0)) {
                    builder.Rect(item.Bounds, block.BackgroundColor, role: "sequence-block-background");
                }
            }
        }
        foreach (var item in layout.Blocks.OrderBy(i => model.Blocks[i.Index].Depth)) {
            var backgroundOnly = model.Blocks[item.Index].Kind == SequenceArtifactBlockKind.Rect && model.Blocks[item.Index].BackgroundColor.HasValue;
            if (SequencePreparedLayout.IsInstant(model.Blocks[item.Index].Kind)) {
                InstantFragment(builder, model.Blocks[item.Index], item, layout, colors, context.Theme, paint);
                Region(builder, item, "sequence-block", model.Blocks[item.Index].Text); AnnotationBounds(semantics, item); continue;
            }
            if (!backgroundOnly) using (Group(builder, item, "sequence-block", model.Blocks[item.Index].Text)) {
                builder.Rect(item.Bounds, null, paint.FragmentBorder ?? colors.Border, stroke, role: "sequence-fragment-frame");
                if(item.FragmentTabBounds is ChartRect tab) FragmentHeader(builder,item,layout,tab,colors,paint,stroke);
                else {
                var header = new ChartRect(item.Bounds.X, item.Bounds.Y, item.Bounds.Width, item.Y);
                using (Surface(builder, paint.FragmentTabShadow, header, stroke)) {
                    builder.Rect(header, paint.FragmentFill ?? colors.Surface, paint.FragmentBorder ?? colors.Border, stroke);
                }
                var labelPadding = layout.Presentation.Layout?.FragmentLabelPadding;
                Text(builder, item.Text, new ChartRect(item.Bounds.X + (labelPadding ?? 8), item.Bounds.Y + (labelPadding ?? 6),
                    item.Bounds.Width - (labelPadding.HasValue ? labelPadding.Value * 2 : 16), item.Y - (labelPadding.HasValue ? labelPadding.Value * 2 : 10)),
                    layout, SequenceTextRole.Fragment, paint.FragmentText ?? colors.Foreground);
                }
            }
            Region(builder, item, "sequence-block", model.Blocks[item.Index].Text); AnnotationBounds(semantics, item);
        }
        foreach (var item in layout.Branches) {
            if (item.Y > 0) using (Group(builder, item, "sequence-branch", model.Branches[item.Index].Text)) {
                builder.Line(item.Bounds.X, item.Bounds.Y, item.Bounds.Right, item.Bounds.Y, paint.FragmentBorder ?? colors.Border, stroke, dash: new[] { 5d, 4d });
                builder.Rect(new ChartRect(item.Bounds.X, item.Bounds.Y + 1, item.Bounds.Width, item.Y - 2), paint.FragmentFill ?? colors.Surface);
                var labelPadding = layout.Presentation.Layout?.FragmentLabelPadding;
                Text(builder, item.Text, new ChartRect(item.Bounds.X + (labelPadding ?? 6), item.Bounds.Y + (labelPadding ?? 5),
                    item.Bounds.Width - (labelPadding.HasValue ? labelPadding.Value * 2 : 12), item.Y - (labelPadding.HasValue ? labelPadding.Value * 2 : 8)),
                    layout, SequenceTextRole.Fragment, paint.FragmentTitleText ?? paint.FragmentText ?? colors.Foreground);
            }
            Region(builder, item, "sequence-branch", model.Branches[item.Index].Text); AnnotationBounds(semantics, item);
        }
        foreach (var item in layout.Participants) {
            var participant = model.Participants[item.Index]; double x = layout.Lanes[participant.Id];
            double bottom = layout.ParticipantLifelineEnd(model, item);
            DrawLifeline(builder,item,x,bottom,paint,colors,stroke);
            if (participant.DestroyedAtMessageIndex.HasValue && !layout.Presentation.MirrorsParticipants) {
                using (builder.PushGroup(item.Id + "-destruction", "sequence-destruction")) {
                    builder.Line(x - 7, bottom - 7, x + 7, bottom + 7, colors.Accent, Math.Max(2, stroke), "sequence-destruction-cross");
                    builder.Line(x + 7, bottom - 7, x - 7, bottom + 7, colors.Accent, Math.Max(2, stroke), "sequence-destruction-cross");
                }
            }
        }
        foreach (var item in layout.Activations) {
            var owner=item.ActivationParticipantId==null?null:model.Participants.First(participant=>participant.Id==item.ActivationParticipantId);
            using (Group(builder, item, "sequence-activation", item.Text))
                builder.Rect(item.Bounds, owner?.ActivationFill ?? paint.ActivationFill ?? colors.Surface, owner?.ActivationBorder ?? paint.ActivationBorder ?? colors.Accent, stroke, role: "sequence-activation-bar");
            Region(builder, item, "sequence-activation", item.Text);
            if (item.Index >= 0) AnnotationBounds(semantics, item);
        }
        foreach (var item in layout.Participants) {
            var participant = model.Participants[item.Index];
            using (Group(builder, item, "sequence-participant", participant.Label)) {
                IDisposable? link = participant.Href == null ? null : builder.PushLink(participant.Href);
                try { Participant(builder, participant, item, layout, colors, context.Theme, paint); }
                finally { link?.Dispose(); }
            }
            Region(builder, item, "sequence-participant", participant.Label);
            var node = semantics.Nodes[item.Index];
            node.X = item.Bounds.X; node.Y = item.Bounds.Y; node.Width = item.Bounds.Width; node.Height = item.Bounds.Height;
        }
        PaintParticipantFooters(builder, model, layout, colors, context.Theme, paint);
        foreach (var item in layout.Messages) {
            var message = model.Messages[item.Index];
            bool self = message.SourceId == message.TargetId;
            double endY = item.Y + (self ? item.SelfDrop : 0);
            double loopX = item.PresentedLoopX ?? Math.Max(item.X1, item.X2) + 56;
            var points = item.CapturedRoute?.Terminals ?? (self ? new[] { new ChartPoint(item.X1, item.Y), new ChartPoint(loopX, item.Y), new ChartPoint(loopX, endY), new ChartPoint(item.X2, endY) }
                : new[] { new ChartPoint(item.X1, item.Y), new ChartPoint(item.X2, item.Y) });
            using (Group(builder, item, "sequence-message", message.Text, MessageAttributes(message))) {
                builder.Path(item.CapturedRoute?.Path ?? new ChartPath(points.Select((p, i) => i == 0 ? ChartPathCommand.MoveTo(p.X, p.Y) : ChartPathCommand.LineTo(p.X, p.Y)).ToArray()),
                    stroke: paint.MessageLine ?? colors.Accent, strokeWidth: context.Theme.SeriesStrokeWidth, role: "sequence-message-line", dash: message.LineStyle == SequenceArtifactMessageLineStyle.Dashed ? new[] { 6d, 5d } : null);
                MessageArrows(builder, message, points, colors, context.Theme.SeriesStrokeWidth, paint.MessageLine ?? colors.Accent,
                    item.CapturedRoute?.Curved == true, item.CapturedRoute?.Center);
                if (model.ParticipantGroups.Count > 0 && item.Text.Length > 0)
                    builder.Rect(new ChartRect(item.LabelBounds.X - 3, item.LabelBounds.Y - 1, item.LabelBounds.Width + 6, item.LabelBounds.Height + 2), colors.Surface);
                if (item.NumberBadgeBounds.HasValue) {
                    PaintNumberBadge(builder,layout,item,paint,colors);
                    Text(builder,item.Text,item.LabelBounds,layout,SequenceTextRole.Message,paint.MessageText??colors.Foreground,center:!self);
                } else if (item.NumberText.Length == 0) Text(builder, item.Text, item.LabelBounds, layout, SequenceTextRole.Message, paint.MessageText ?? colors.Foreground, center: !self);
                else {
                    Text(builder, item.NumberText, new ChartRect(item.LabelBounds.X, item.LabelBounds.Y, item.NumberWidth, item.LabelBounds.Height),
                        layout, SequenceTextRole.Number, paint.MessageText ?? colors.Foreground, nativeRole: "sequence-number");
                    Text(builder, item.Text, new ChartRect(item.LabelBounds.X + item.NumberWidth, item.LabelBounds.Y,
                        item.LabelBounds.Width - item.NumberWidth, item.LabelBounds.Height), layout, SequenceTextRole.Message, paint.MessageText ?? colors.Foreground);
                }
            }
            Region(builder, item, "sequence-message", message.Text);
            foreach (var point in item.CapturedRoute?.Observed ?? points) semantics.Edges[item.Index].ResolvedRoute.Add(new VisualArtifactInterchangePoint { X = point.X, Y = point.Y });
            semantics.Edges[item.Index].ResolvedLabelBounds = item.LabelBounds;
        }
        foreach (var item in layout.Notes) {
            using (Group(builder, item, "sequence-note", model.Notes[item.Index].Text)) {
                var b = item.Bounds; const double fold = 9;
                using (Surface(builder, paint.NoteShadow, b, stroke)) {
                    builder.Path(new ChartPath(new[] { ChartPathCommand.MoveTo(b.X, b.Y), ChartPathCommand.LineTo(b.Right - fold, b.Y), ChartPathCommand.LineTo(b.Right, b.Y + fold), ChartPathCommand.LineTo(b.Right, b.Bottom), ChartPathCommand.LineTo(b.X, b.Bottom) }), paint.NoteFill ?? colors.Surface, paint.NoteBorder ?? colors.Accent, stroke, close: true);
                    builder.Line(b.Right - fold, b.Y, b.Right - fold, b.Y + fold, paint.NoteBorder ?? colors.Accent, stroke);
                    builder.Line(b.Right - fold, b.Y + fold, b.Right, b.Y + fold, paint.NoteBorder ?? colors.Accent, stroke);
                }
                var margin = layout.Presentation.Layout?.NoteSpacing;
                Text(builder, item.Text, new ChartRect(b.X + (margin ?? 10), b.Y + (margin ?? 8),
                    b.Width - (margin.HasValue ? margin.Value * 2 + 14 : 24), b.Height - (margin.HasValue ? margin.Value * 2 : 16)),
                    layout, SequenceTextRole.Note, paint.NoteText ?? colors.Foreground);
            }
            Region(builder, item, "sequence-note", model.Notes[item.Index].Text); AnnotationBounds(semantics, item);
        }
        var accessibility = model.Accessibility.Clone();
        if (string.IsNullOrWhiteSpace(accessibility.Name)) accessibility.Name = !string.IsNullOrWhiteSpace(context.Frame.Title) ? context.Frame.Title
            : !string.IsNullOrWhiteSpace(model.Title) ? model.Title : !string.IsNullOrWhiteSpace(model.Id) ? model.Id : "Sequence";
        if (string.IsNullOrWhiteSpace(accessibility.Description)) accessibility.Description = context.Frame.Subtitle;
        semantics.AccessibleName = accessibility.Name; semantics.AccessibleDescription = accessibility.Description;
        semantics.Language = accessibility.Language; semantics.IsDecorative = accessibility.IsDecorative;
        return new PreparedVisual(builder.Build(), accessibility, semanticInterchange: semantics);
    }

    private static Dictionary<string, string> MessageAttributes(SequenceArtifactMessage message) {
        var attributes = new Dictionary<string, string> { ["data-source"] = message.SourceId, ["data-target"] = message.TargetId, ["data-kind"] = message.Kind.ToString() };
        if (message.Metadata.TryGetValue(ChartForgeX.Topology.TopologyChartExtensions.ScenarioStepMetadataKey, out var step)) attributes["data-cfx-scenario-step"] = step;
        return attributes;
    }

    private static IDisposable Group(VisualSceneBuilder builder, SequencePreparedLayout.Item item, string role, string label, Dictionary<string, string>? metadata = null) {
        metadata ??= new Dictionary<string, string>(); metadata["data-label"] = label;
        return builder.PushGroup(item.Id, role, metadata);
    }
    private static void Region(VisualSceneBuilder builder, SequencePreparedLayout.Item item, string role, string label) => builder.AddRegion(new VisualSemanticRegion(item.Id, role, item.Bounds, label));
    private static void AnnotationBounds(VisualArtifactInterchangeEnvelope envelope, SequencePreparedLayout.Item item) {
        var annotation = envelope.Annotations.First(a => a.Id == item.Id);
        annotation.Extensions["chartforgex.bounds.x"] = item.Bounds.X.ToString("R", CultureInfo.InvariantCulture);
        annotation.Extensions["chartforgex.bounds.y"] = item.Bounds.Y.ToString("R", CultureInfo.InvariantCulture);
        annotation.Extensions["chartforgex.bounds.width"] = item.Bounds.Width.ToString("R", CultureInfo.InvariantCulture);
        annotation.Extensions["chartforgex.bounds.height"] = item.Bounds.Height.ToString("R", CultureInfo.InvariantCulture);
    }
    private static void Text(VisualSceneBuilder builder, string text, ChartRect bounds, double size, ChartColor color, bool center = false) {
        builder.Text(text, center ? bounds.X + bounds.Width / 2 : bounds.X, bounds.Y + builder.TextAscent(size), size, color,
            role: "sequence-label", alignment: center ? TextAlignment.Center : TextAlignment.Left);
    }

    private static void Validate(SequenceArtifact model) {
        if (model.Participants.Count == 0) throw new InvalidOperationException("A sequence requires at least one participant.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var participant in model.Participants) {
            if (!ids.Add(participant.Id)) throw new InvalidOperationException("Duplicate sequence participant: " + participant.Id);
            if (!Enum.IsDefined(typeof(SequenceArtifactParticipantKind), participant.Kind)) throw new ArgumentOutOfRangeException(nameof(model), "Unknown sequence participant kind.");
        }
        ValidateParticipantLifetimesAndGroups(model);
        foreach (var message in model.Messages) {
            RequireId(message.SourceId); RequireId(message.TargetId);
            if (!Enum.IsDefined(typeof(SequenceArtifactMessageKind), message.Kind) || !Enum.IsDefined(typeof(SequenceArtifactMessageLineStyle), message.LineStyle)) throw new ArgumentOutOfRangeException(nameof(model), "Unknown sequence message notation.");
        }
        foreach (var note in model.Notes) {
            Step(note.StepIndex);
            if (note.ParticipantIds.Count == 0) throw new InvalidOperationException("A sequence note requires participant targets.");
            foreach (var id in note.ParticipantIds) RequireId(id);
            if (!Enum.IsDefined(typeof(SequenceArtifactNotePlacement), note.Placement)) throw new ArgumentOutOfRangeException(nameof(model), "Unknown sequence note placement.");
        }
        foreach (var activation in model.Activations) { RequireId(activation.ParticipantId); Step(activation.StepIndex); }
        foreach (var block in model.Blocks) {
            Span(block.StartStepIndex, block.EndStepIndex, block.Depth, block.IsEmpty);
            if (!Enum.IsDefined(typeof(SequenceArtifactBlockKind), block.Kind)) throw new ArgumentOutOfRangeException(nameof(model));
            if (SequencePreparedLayout.IsInstant(block.Kind) && (!block.IsEmpty || block.StartStepIndex < 0 || block.StartStepIndex != block.EndStepIndex))
                throw new ArgumentException("Reference and Divider require one empty instantaneous step.", nameof(model));
            if (block.Kind != SequenceArtifactBlockKind.Reference && block.ParticipantIds.Count > 0)
                throw new ArgumentException("Only Reference may carry participant targets.", nameof(model));
            var targets = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in block.ParticipantIds) {
                RequireId(id); if (!targets.Add(id)) throw new ArgumentException("Reference targets must be distinct.", nameof(model));
                var participant=model.Participants.First(item=>item.Id==id);
                if (participant.CreatedAtMessageIndex is int born && block.StartStepIndex <= born
                    || participant.DestroyedAtMessageIndex is int died && block.StartStepIndex > died)
                    throw new ArgumentException("Reference targets must exist at their source occurrence.", nameof(model));
            }
        }
        foreach (var branch in model.Branches) { if (SequencePreparedLayout.IsInstant(branch.ParentKind)) throw new ArgumentException("Instantaneous fragments cannot own branches.", nameof(model)); Span(branch.StartStepIndex, branch.EndStepIndex, branch.Depth, branch.IsEmpty); if (!Enum.IsDefined(typeof(SequenceArtifactBlockKind), branch.ParentKind)) throw new ArgumentOutOfRangeException(nameof(model)); }
        void RequireId(string id) { if (!ids.Contains(id)) throw new InvalidOperationException("Sequence target does not exist: " + id); }
        void Step(int step) { if (step > 100000) throw new ArgumentOutOfRangeException(nameof(model), "Sequence step indices must not exceed 100000; negative indices normalize to zero."); }
        void Span(int start, int end, int depth, bool empty) {
            Step(start); Step(end);
            if (depth < 0 || depth > 100) throw new ArgumentOutOfRangeException(nameof(model), "Invalid sequence fragment nesting depth.");
        }
    }
}
