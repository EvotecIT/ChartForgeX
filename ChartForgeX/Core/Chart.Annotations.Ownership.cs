using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    private readonly List<ChartAnnotation> _annotations = new();
    private IReadOnlyList<ChartAnnotation>? _annotationsView;

    /// <summary>Gets the chart's annotations in paint order through a stable, read-only view.</summary>
    /// <remarks>
    /// Use the chart's annotation methods to change membership. Annotation geometry and identity are fixed;
    /// its optional stroke style remains mutable authoring state. Preparation captures detached output.
    /// </remarks>
    public IReadOnlyList<ChartAnnotation> Annotations => _annotationsView ??= new ReadOnlyCollection<ChartAnnotation>(_annotations);

    /// <summary>Adds an annotation after validating its membership and authored identity.</summary>
    /// <remarks>
    /// An annotation instance can occur once within this chart; explicit IDs must also be unique.
    /// Anonymous guides need no generated ID. Coordinate-system compatibility is checked during preparation,
    /// after chart authoring is complete. Like other authoring methods, access must be serialized by the caller.
    /// </remarks>
    public Chart AddAnnotation(ChartAnnotation annotation) {
        ValidateAnnotationMembership(annotation);
        _annotations.Add(annotation);
        return this;
    }

    /// <summary>Replaces the annotation with the same explicit ID, preserving its paint position.</summary>
    /// <remarks>Use the two-argument overload to replace an anonymous guide by its retained instance.</remarks>
    public Chart ReplaceAnnotation(ChartAnnotation replacement) {
        if (replacement == null) throw new ArgumentNullException(nameof(replacement));
        if (!replacement.Id.HasValue)
            throw new ArgumentException("A keyed replacement requires an explicit annotation ID.", nameof(replacement));
        return ReplaceAnnotation(_annotations[OwnedAnnotationIndex(replacement.Id.Value, nameof(replacement))], replacement);
    }

    /// <summary>Replaces an attached annotation in place while preserving its explicit or anonymous identity.</summary>
    /// <remarks>
    /// The existing instance must belong to this chart. To change an authored identity, remove the old annotation
    /// and add a new one. Failed validation leaves membership, order and focus-guide ownership unchanged.
    /// </remarks>
    public Chart ReplaceAnnotation(ChartAnnotation existing, ChartAnnotation replacement) {
        var index = OwnedAnnotationIndex(existing);
        if (replacement == null) throw new ArgumentNullException(nameof(replacement));
        if (existing.Id != replacement.Id)
            throw new ArgumentException("Replacement must preserve the annotation's authored or anonymous identity.", nameof(replacement));
        ValidateAnnotationMembership(replacement, index);
        if (ReferenceEquals(existing, replacement)) return this;
        _annotations[index] = replacement;
        var focusIndex = Options.XAxisFocusGuideAnnotations.IndexOf(existing);
        if (focusIndex >= 0) Options.XAxisFocusGuideAnnotations[focusIndex] = replacement;
        return this;
    }

    /// <summary>Removes an annotation identified by an explicit, case-sensitive authored ID.</summary>
    public Chart RemoveAnnotation(ChartAnnotationId id) => RemoveAnnotation(_annotations[OwnedAnnotationIndex(id)]);

    /// <summary>Removes an attached annotation by its retained instance, including an anonymous guide.</summary>
    public Chart RemoveAnnotation(ChartAnnotation annotation) {
        var index = OwnedAnnotationIndex(annotation);
        _annotations.RemoveAt(index);
        Options.XAxisFocusGuideAnnotations.Remove(annotation);
        return this;
    }

    /// <summary>Removes all annotations while keeping the read-only collection view usable.</summary>
    public Chart ClearAnnotations() {
        _annotations.Clear();
        Options.XAxisFocusGuideAnnotations.Clear();
        return this;
    }

    private void ValidateAnnotationMembership(ChartAnnotation annotation, int replacedIndex = -1) {
        if (annotation == null) throw new ArgumentNullException(nameof(annotation));
        if (annotation.Id.HasValue) annotation.Id.Value.Validate();
        for (var index = 0; index < _annotations.Count; index++) {
            if (index == replacedIndex) continue;
            var current = _annotations[index];
            if (ReferenceEquals(current, annotation))
                throw new InvalidOperationException("An annotation instance can occur only once within a chart.");
            if (annotation.Id.HasValue && current.Id == annotation.Id)
                throw new InvalidOperationException("Annotation IDs must be unique within a chart.");
        }
    }

    private int OwnedAnnotationIndex(ChartAnnotation annotation) {
        if (annotation == null) throw new ArgumentNullException(nameof(annotation));
        var index = _annotations.IndexOf(annotation);
        if (index < 0) throw new ArgumentException("The annotation is not attached to this chart.", nameof(annotation));
        return index;
    }

    private int OwnedAnnotationIndex(ChartAnnotationId id, string parameterName = "id") {
        id.Validate();
        for (var index = 0; index < _annotations.Count; index++)
            if (_annotations[index].Id == id) return index;
        throw new ArgumentException("The chart has no annotation with ID '" + id.Value + "'.", parameterName);
    }
}
