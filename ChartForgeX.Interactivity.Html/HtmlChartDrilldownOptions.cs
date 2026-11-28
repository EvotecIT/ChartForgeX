using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using PowerForge.Fluent;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Child charts that bar and line categories open in the browser, with breadcrumbs back to the parent.</summary>
/// <remarks>
/// A local target embeds its child chart in the page, prepared with the same renderer, so a saved report drills without a host.
/// An on-demand target asks the host for the child when a reader opens it: the chart section dispatches <c>cfxdrillrequest</c>
/// and the host answers through the event's <c>respond</c> callback with a section from
/// <see cref="HtmlInteractiveChartRenderer.PrepareSection(Chart, ChartCartesianViewport?, Action{HtmlChartInteractionOptions})"/>.
/// Static SVG and PNG output, and printing, keep the parent chart.
/// </remarks>
[Fluent]
public sealed class HtmlChartDrilldownOptions {
    private readonly List<HtmlChartDrilldownTarget> _targets = new();
    private string? _rootLabel;

    /// <summary>Gets the configured targets in the order they were added.</summary>
    public IReadOnlyList<HtmlChartDrilldownTarget> Targets => _targets;

    /// <summary>Gets or sets the breadcrumb label of the parent chart. Null uses the chart title.</summary>
    public string? RootLabel {
        get => _rootLabel;
        set => _rootLabel = value == null ? null : string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("A drill-down root label must not be empty.", nameof(value)) : value.Trim();
    }

    /// <summary>Opens an embedded child chart from one category.</summary>
    /// <param name="category">The category label, exactly as passed to <see cref="Chart.XLabels(string[])"/>.</param>
    /// <param name="child">The child chart, prepared with the parent's renderer when the parent section is prepared.</param>
    /// <param name="label">The breadcrumb label. Null uses the child chart's title, then the category.</param>
    /// <param name="configure">Optional interaction options for the child. The child starts from the parent's runtime,
    /// layout, toolbar, text presentation, features and localization; it may add drill-down targets of its own.</param>
    /// <returns>The current options.</returns>
    public HtmlChartDrilldownOptions Add(string category, Chart child, string? label = null, Action<HtmlChartInteractionOptions>? configure = null) {
        if (child == null) throw new ArgumentNullException(nameof(child));
        _targets.Add(new HtmlChartDrilldownTarget(NextId(category), category.Trim(), label ?? (string.IsNullOrWhiteSpace(child.Title) ? null : child.Title), child, configure));
        return this;
    }

    /// <summary>Opens a child chart that the host supplies when a reader opens the category.</summary>
    /// <param name="category">The category label, exactly as passed to <see cref="Chart.XLabels(string[])"/>.</param>
    /// <param name="label">The breadcrumb label. Null uses the category.</param>
    /// <returns>The current options.</returns>
    public HtmlChartDrilldownOptions AddOnDemand(string category, string? label = null) {
        _targets.Add(new HtmlChartDrilldownTarget(NextId(category), category.Trim(), label, null, null));
        return this;
    }

    internal bool Enabled => _targets.Count > 0;

    private string NextId(string category) {
        if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("A drill-down target requires a category.", nameof(category));
        var trimmed = category.Trim();
        if (_targets.Any(target => string.Equals(target.Category, trimmed, StringComparison.Ordinal)))
            throw new ArgumentException("Category '" + trimmed + "' already opens a drill-down target.", nameof(category));
        var slug = HtmlInteractiveChartRenderer.Slugify(trimmed);
        if (slug.Length == 0) slug = "target";
        var id = slug;
        for (var suffix = 2; _targets.Any(target => target.Id == id); suffix++) id = slug + "-" + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return id;
    }
}

/// <summary>One category that opens a child chart.</summary>
public sealed class HtmlChartDrilldownTarget {
    internal HtmlChartDrilldownTarget(string id, string category, string? label, Chart? child, Action<HtmlChartInteractionOptions>? configure) {
        Id = id; Category = category; Child = child; Configure = configure;
        Label = string.IsNullOrWhiteSpace(label) ? category : label!.Trim();
    }

    /// <summary>Gets the stable target identifier the browser events report, derived from the category.</summary>
    public string Id { get; }

    /// <summary>Gets the category label that opens the target.</summary>
    public string Category { get; }

    /// <summary>Gets the breadcrumb and announcement label.</summary>
    public string Label { get; }

    /// <summary>Gets the embedded child chart, or null when the host supplies the child on demand.</summary>
    public Chart? Child { get; }

    /// <summary>Gets whether the host supplies the child when a reader opens the target.</summary>
    public bool OnDemand => Child == null;

    internal Action<HtmlChartInteractionOptions>? Configure { get; }
}
