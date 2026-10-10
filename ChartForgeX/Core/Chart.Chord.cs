using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Adds copied, directed flows with explicit node and link IDs as a weighted chord chart.</summary>
    /// <remarks>Reciprocal, parallel, cyclic, and self flows remain separate. Zero flows retain semantics without positive geometry.</remarks>
    public Chart AddChord(string name, IEnumerable<ChartNode> nodes, IEnumerable<ChartFlowLink> links, ChartColor? color = null) {
        EnsureCanAddSeries();
        var series = new ChartSeries(name, ChartSeriesKind.Chord, Array.Empty<ChartPoint>()) { Color = color };
        series.SetRelationships(ChartRelationshipIndex.Chord(nodes, links));
        AppendSeries(series);
        return this;
    }

    /// <summary>Configures the circular allocation, ribbons, and labels used by native chord exports.</summary>
    public Chart ConfigureChord(Action<ChartChordOptions> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        configure(Options.Chord);
        return this;
    }
}
