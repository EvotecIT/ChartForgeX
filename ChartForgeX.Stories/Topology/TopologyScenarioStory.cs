using System;
using System.Globalization;
using ChartForgeX.Stories;

namespace ChartForgeX.Topology;

/// <summary>
/// Options for turning a topology scenario into a step-by-step visual story.
/// </summary>
public sealed class TopologyScenarioStoryOptions {
    private double _stepSeconds = 2.4;

    /// <summary>Gets or sets how long each step stays on screen when the step declares no duration of its own.</summary>
    public double StepSeconds {
        get => _stepSeconds;
        set => _stepSeconds = value >= 0.5 && value <= 60 ? value : throw new ArgumentOutOfRangeException(nameof(value), "Story steps last 0.5 to 60 seconds.");
    }

    /// <summary>Gets or sets render options for the topology frames. The caller's instance is not modified.</summary>
    public TopologyRenderOptions? RenderOptions { get; set; }

    /// <summary>Gets or sets an optional sharing format; omit it to size the story from the topology.</summary>
    public VisualStoryFormat? Format { get; set; }
}

/// <summary>
/// Turns topology scenarios into visual stories: one scene per step, with the step highlighted on the topology and
/// its request or response payload beside it. The stories export to SVG, PNG per step, GIF, APNG, and HTML.
/// </summary>
public static class TopologyScenarioStoryExtensions {
    /// <summary>Visual stories hold at most this many scenes, so longer scenarios are rejected.</summary>
    public const int MaximumSteps = 24;

    /// <summary>
    /// Builds a visual story with one scene per scenario step.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="scenarioId">The scenario id.</param>
    /// <param name="options">Optional story options.</param>
    /// <returns>A visual story whose chapters are the scenario steps.</returns>
    public static VisualStory ToScenarioStory(this TopologyChart chart, string scenarioId, TopologyScenarioStoryOptions? options = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        options ??= new TopologyScenarioStoryOptions();
        var resolved = chart.ResolveScenario(scenarioId);
        if (resolved.Steps.Count == 0) throw new ArgumentException("Scenario '" + resolved.Id + "' has no steps.", nameof(scenarioId));
        if (resolved.Steps.Count > MaximumSteps) {
            throw new ArgumentException("Scenario '" + resolved.Id + "' has " + resolved.Steps.Count.ToString(CultureInfo.InvariantCulture) +
                " steps; a story holds at most " + MaximumSteps.ToString(CultureInfo.InvariantCulture) + ". Split the scenario.", nameof(scenarioId));
        }

        var story = VisualStory.Create(chart.Title is { Length: > 0 } title ? title + " · " + resolved.Label : resolved.Label)
            .WithDescription(string.IsNullOrWhiteSpace(resolved.Scenario.Description) ? resolved.Label : resolved.Scenario.Description!.Trim());
        PreparedTopology? first = null;
        var digits = resolved.Steps.Count.ToString(CultureInfo.InvariantCulture).Length;
        foreach (var step in resolved.Steps) {
            var frame = chart.PrepareScenarioStep(resolved.Id, step.Index, options.RenderOptions);
            first ??= frame;
            var wide = frame.Width / Math.Max(1, frame.Height) > 1.8;
            var number = (step.Index + 1).ToString(CultureInfo.InvariantCulture);
            var seconds = step.Step.DurationMilliseconds.HasValue ? Math.Max(0.5, step.Step.DurationMilliseconds.Value / 1000.0) : options.StepSeconds;
            var scene = story.Scene("step-" + number.PadLeft(digits, '0'), number + ". " + step.Summary, seconds, wide ? VisualStorySceneLayout.Stacked : VisualStorySceneLayout.Split);
            scene.Panel("topology", new VisualStoryMediaSurface(frame.Visual, resolved.Label + ", step " + number + ": " + step.Summary), weight: wide ? 2.2 : 2.4);
            scene.Panel("step", StepSurface(step), Heading(step), weight: wide ? 0.8 : 1);
        }

        if (options.Format.HasValue) story.WithFormat(options.Format.Value);
        else story.WithSize(StoryWidth(first!), StoryHeight(first!));
        story.Outcome("flow", resolved.Label, "topology");
        return story;
    }

    private static VisualStorySurface StepSurface(TopologyResolvedScenarioStep step) {
        if (string.IsNullOrEmpty(step.Payload)) return new VisualStoryTextSurface(step.Label, emphasized: step.IsFailure);
        var language = step.PayloadFormat switch {
            TopologyScenarioPayloadFormat.Json => "json",
            TopologyScenarioPayloadFormat.Xml => "xml",
            _ => "text"
        };
        return new VisualStorySourceSurface(StorySourceText.Create(step.Payload!, language), step.Label);
    }

    private static string Heading(TopologyResolvedScenarioStep step) {
        var kind = step.Interaction switch {
            TopologyScenarioInteraction.Request => "Request",
            TopologyScenarioInteraction.Message => "Message",
            TopologyScenarioInteraction.Response => "Response",
            TopologyScenarioInteraction.Failure => "Failed call",
            _ => step.TargetNodeId == null ? "At " + step.SourceLabel : "Route"
        };
        return step.TargetNodeId == null ? kind : kind + ": " + step.SourceLabel + " → " + step.TargetLabel;
    }

    private static int StoryWidth(PreparedTopology frame) => (int)Math.Max(960, Math.Min(1920, frame.Width + 80));

    private static int StoryHeight(PreparedTopology frame) {
        var wide = frame.Width / Math.Max(1, frame.Height) > 1.8;
        var width = StoryWidth(frame);
        var topology = frame.Height * Math.Min(1, (width - 80) / Math.Max(1, frame.Width));
        return (int)Math.Max(540, Math.Min(1200, wide ? topology * 1.6 + 220 : topology + 160));
    }
}
