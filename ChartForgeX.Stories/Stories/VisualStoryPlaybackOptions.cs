using System;

namespace ChartForgeX.Stories;

/// <summary>Immutable timing shared by timestamp rendering, browser playback and animation export.</summary>
public sealed class VisualStoryPlaybackOptions {
    /// <summary>Creates playback timing. Zero plays repeats indefinitely; one plays once.</summary>
    public VisualStoryPlaybackOptions(TimeSpan? endHold = null, TimeSpan? transition = null, int playCount = 0) {
        EndHold = endHold ?? TimeSpan.FromSeconds(1.5);
        Transition = transition ?? TimeSpan.FromSeconds(0.24);
        if (EndHold < TimeSpan.Zero || EndHold > TimeSpan.FromSeconds(10)) throw new ArgumentOutOfRangeException(nameof(endHold));
        if (Transition < TimeSpan.Zero || Transition > TimeSpan.FromSeconds(1)) throw new ArgumentOutOfRangeException(nameof(transition));
        if (playCount < 0 || playCount > 65536) throw new ArgumentOutOfRangeException(nameof(playCount));
        PlayCount = playCount;
    }

    /// <summary>Gets the completed-state hold after the authored scenes.</summary>
    public TimeSpan EndHold { get; }
    /// <summary>Gets the cross-fade occupying the end of each non-final scene.</summary>
    public TimeSpan Transition { get; }
    /// <summary>Gets the total number of plays, or zero for indefinite repetition.</summary>
    public int PlayCount { get; }
}

/// <summary>Immutable sampling and resource limits, independent of playback timing.</summary>
public sealed class VisualStoryFrameOptions {
    /// <summary>Creates bounded frame sampling options.</summary>
    public VisualStoryFrameOptions(int framesPerSecond = 6, int outputScale = 1, int maximumFrames = 600) {
        if (framesPerSecond < 2 || framesPerSecond > 60) throw new ArgumentOutOfRangeException(nameof(framesPerSecond));
        if (outputScale < 1 || outputScale > 4) throw new ArgumentOutOfRangeException(nameof(outputScale));
        if (maximumFrames < 2 || maximumFrames > 3600) throw new ArgumentOutOfRangeException(nameof(maximumFrames));
        FramesPerSecond = framesPerSecond; OutputScale = outputScale; MaximumFrames = maximumFrames;
    }
    /// <summary>Gets the requested rational frame cadence.</summary>
    public int FramesPerSecond { get; }
    /// <summary>Gets pixels per logical unit.</summary>
    public int OutputScale { get; }
    /// <summary>Gets the maximum number of samples permitted before rendering begins.</summary>
    public int MaximumFrames { get; }
}

/// <summary>An immutable authored scene boundary for seeking and chapter navigation.</summary>
public sealed class VisualStoryChapter {
    internal VisualStoryChapter(string id, string title, TimeSpan start, TimeSpan duration) {
        Id = id; Title = title; Start = start; Duration = duration;
    }
    /// <summary>Gets the stable scene identifier.</summary>
    public string Id { get; }
    /// <summary>Gets the chapter heading.</summary>
    public string Title { get; }
    /// <summary>Gets the start on the common story clock.</summary>
    public TimeSpan Start { get; }
    /// <summary>Gets the authored scene duration, excluding the final hold.</summary>
    public TimeSpan Duration { get; }
}
