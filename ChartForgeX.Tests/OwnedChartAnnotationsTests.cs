using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Exercises chart-owned annotation edits through their public authoring and native preparation boundaries.</summary>
public sealed class OwnedChartAnnotationsTests {
    private static readonly ChartColor Ink = ChartColor.FromHex("#3874CB");

    [Fact]
    public void RetainedReadOnlyViewTracksValidatedEditsWithoutExposingCollectionMutation() {
        var chart = Create();
        var view = chart.Annotations;
        var first = Guide(2, "First");
        var last = Guide(8, "Last");
        Assert.Same(chart, chart.AddAnnotation(first).AddAnnotation(last));
        Assert.Same(view, chart.Annotations);
        Assert.Equal(new[] { first, last }, view);
        var list = Assert.IsAssignableFrom<IList<ChartAnnotation>>(view);
        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Add(Guide(5, "Bypass")));

        var replacement = Guide(3, "Replaced first");
        Assert.Same(chart, chart.ReplaceAnnotation(first, replacement));
        Assert.Equal(new[] { replacement, last }, view);
        Assert.Same(chart, chart.RemoveAnnotation(last));
        Assert.Same(replacement, Assert.Single(view));
        Assert.Same(chart, chart.ClearAnnotations());
        Assert.Empty(view);
        chart.AddHorizontalLine(4, "Anonymous after clear");
        Assert.Null(Assert.Single(view).Id);
    }

    [Fact]
    public void RejectedEditsLeaveIdentityOrderAndPreparedOutputUnchanged() {
        var chart = Create();
        var first = Guide(2, "Anonymous");
        var authored = Guide(4, "Authored", new ChartAnnotationId("threshold"));
        chart.AddAnnotation(first).AddAnnotation(authored);
        var before = chart.Annotations.ToArray();
        var svg = chart.Prepare(new VisualRenderContext()).ToSvg();
        Assert.Throws<ArgumentNullException>(() => chart.AddAnnotation(null!));
        Assert.Throws<InvalidOperationException>(() => chart.AddAnnotation(first));
        Assert.Throws<InvalidOperationException>(() => chart.AddAnnotation(Guide(6, "Duplicate", new ChartAnnotationId("threshold"))));
        Assert.Throws<ArgumentException>(() => chart.ReplaceAnnotation(Guide(6, "No key")));
        Assert.Throws<ArgumentException>(() => chart.ReplaceAnnotation(Guide(6, "Missing", new ChartAnnotationId("missing"))));
        Assert.Throws<ArgumentException>(() => chart.ReplaceAnnotation(first, authored));
        Assert.Throws<ArgumentException>(() => chart.ReplaceAnnotation(authored, Guide(6, "Renamed", new ChartAnnotationId("renamed"))));
        Assert.Throws<ArgumentNullException>(() => chart.ReplaceAnnotation(first, null!));
        Assert.Throws<ArgumentException>(() => chart.ReplaceAnnotation(Guide(3, "Unattached"), Guide(3, "Replacement")));
        Assert.Throws<ArgumentException>(() => chart.RemoveAnnotation(Guide(2, "Same facts, different instance")));
        Assert.Throws<ArgumentException>(() => chart.RemoveAnnotation(new ChartAnnotationId("missing")));
        Assert.Throws<InvalidOperationException>(() => chart.RemoveAnnotation(default(ChartAnnotationId)));
        Assert.Equal(before, chart.Annotations);
        Assert.Equal(svg, chart.Prepare(new VisualRenderContext()).ToSvg());
        Assert.Same(chart, chart.ReplaceAnnotation(authored, authored));
        Assert.Equal(before, chart.Annotations);
    }

    [Fact]
    public void ExplicitSourceCommandsKeepPaintOrderAndDetachedPreparedGeometryStyleAndSemantics() {
        var chart = Create().AddVerticalLine(1, "Anonymous guide")
            .AddLineSegment(new ChartPoint(3, 4), new ChartPoint(7, 5), "Before review", id: new ChartAnnotationId("review"))
            .AddLineSegment(new ChartPoint(2, 2), new ChartPoint(8, 6), "Plan", id: new ChartAnnotationId("plan"));
        var current = chart.Annotations[1];
        current.StrokeStyle = new ChartStrokeStyle(Ink, 2);
        var prepared = chart.Prepare(new VisualRenderContext());
        var svg = prepared.ToSvg();
        var png = prepared.ToPng(new VisualRenderOptions(supersampling: 1));
        var sourceId = Assert.Single(prepared.Scene.Annotations, item => item.AnnotationId == "review").SourceId;
        current.StrokeStyle = new ChartStrokeStyle(ChartColor.FromHex("#B42393"), 8);
        Assert.NotEqual(svg, chart.Prepare(new VisualRenderContext()).ToSvg());
        var replacement = ChartAnnotation.LineSegment(new ChartPoint(7, 6), new ChartPoint(9, 7), "After review", Ink,
            new ChartAnnotationId("review"));
        chart.ReplaceAnnotation(replacement).RemoveAnnotation(new ChartAnnotationId("plan"));
        Assert.Null(chart.Annotations[0].Id);
        Assert.Same(replacement, chart.Annotations[1]);
        var next = chart.Prepare(new VisualRenderContext());
        Assert.Equal(sourceId, Assert.Single(next.Scene.Annotations, item => item.AnnotationId == "review").SourceId);
        Assert.Contains("After review", next.ToSvg());
        Assert.NotEqual(svg, next.ToSvg());
        chart.ClearAnnotations();
        Assert.Equal(svg, prepared.ToSvg());
        Assert.Equal(png, prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.Contains("Before review", prepared.ToSvg());
    }

    [Fact]
    public void FocusGuideOwnershipSurvivesReplacementRemovalAndClearWithoutRemovingManualGuides() {
        var chart = Create().AddVerticalLine(4.5, "Manual guide").WithFocusedXAxisCategory(5);
        var manual = chart.Annotations[0];
        var focus = chart.Annotations[1];
        chart.ReplaceAnnotation(focus, Guide(4.5, "Updated focus"));
        chart.RemoveAnnotation(chart.Annotations[2]);
        chart.ClearHighlightedXAxisLabels();
        Assert.Same(manual, Assert.Single(chart.Annotations));
        chart.WithFocusedXAxisCategory(5).ClearAnnotations();
        chart.WithFocusedXAxisCategory(6).ClearHighlightedXAxisLabels();
        Assert.Empty(chart.Annotations);
    }

    [Fact]
    public void SourceAnnotationEditingAllowsStagedAuthoringAndUsesCanonicalPreparationCompatibilityChecks() {
        var chart = Create();
        var id = new ChartAnnotationId("secondary-review");
        chart.AddAnnotation(ChartAnnotation.Note(new ChartPoint(3, 4), "Before series", Ink, id, ChartAxisSide.Secondary));
        Assert.Throws<InvalidOperationException>(() => chart.Prepare(new VisualRenderContext()));
        chart.ReplaceAnnotation(ChartAnnotation.Note(new ChartPoint(4, 5), "After replacement", Ink, id, ChartAxisSide.Secondary));
        chart.AddLine("Secondary", new[] { new ChartPoint(0, 1), new ChartPoint(10, 8) });
        chart.Series[1].UseSecondaryYAxis();
        var prepared = chart.Prepare(new VisualRenderContext());
        Assert.Equal(id.Value, Assert.Single(prepared.Scene.Annotations).AnnotationId);
        Assert.Contains("After replacement", prepared.ToSvg());
        chart.RemoveAnnotation(id);
        Assert.Empty(chart.Prepare(new VisualRenderContext()).Scene.Annotations);
    }

    private static ChartAnnotation Guide(double value, string caption, ChartAnnotationId? id = null) => id.HasValue
        ? new ChartAnnotation(id.Value, ChartAnnotationKind.VerticalLine, value, null, caption, Ink, 1)
        : new ChartAnnotation(ChartAnnotationKind.VerticalLine, value, null, caption, Ink, 1);

    private static Chart Create() => Chart.Create().WithSize(480, 300).WithLegend(false)
        .AddLine("Readings", new[] { new ChartPoint(0, 2), new ChartPoint(10, 8) });
}
