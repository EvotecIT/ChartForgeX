using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>Word cloud sizing, angles and density. Configure it through <see cref="ChartOptions.WordCloud"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class ChartWordCloudOptions {
    internal ChartWordCloudOptions Clone() => (ChartWordCloudOptions)MemberwiseClone();
    private double _wordCloudMinFontSize = 12;

    private double _wordCloudMaxFontSize = 46;

    private double[] _wordCloudAngles = { 0 };

    private int? _wordCloudMaximumTerms;

    private double _wordCloudDensity = 1.0;


    /// <summary>
    /// Gets or sets the smallest font size used by word cloud terms.
    /// </summary>
    public double MinFontSize {
        get => _wordCloudMinFontSize;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value <= 0 || value >= _wordCloudMaxFontSize) throw new ArgumentOutOfRangeException(nameof(value), value, "Word cloud minimum font size must be greater than zero and smaller than the maximum.");
            _wordCloudMinFontSize = value;
        }
    }


    /// <summary>
    /// Gets or sets the largest font size used by word cloud terms.
    /// </summary>
    public double MaxFontSize {
        get => _wordCloudMaxFontSize;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value <= _wordCloudMinFontSize) throw new ArgumentOutOfRangeException(nameof(value), value, "Word cloud maximum font size must be greater than the minimum.");
            _wordCloudMaxFontSize = value;
        }
    }


    /// <summary>
    /// Gets or sets the deterministic rotation angles used by word cloud terms. Terms are horizontal by default;
    /// supply a pattern such as <c>0, 90</c> for a deliberate mix of orientations.
    /// </summary>
    public double[] Angles {
        get => (double[])_wordCloudAngles.Clone();
        set {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (value.Length == 0) throw new ArgumentException("Word cloud angles must contain at least one angle.", nameof(value));
            var copy = (double[])value.Clone();
            for (var i = 0; i < copy.Length; i++) {
                ChartGuards.Finite(copy[i], nameof(value));
                if (copy[i] < -90 || copy[i] > 90) throw new ArgumentOutOfRangeException(nameof(value), copy[i], "Word cloud angles must be between -90 and 90 degrees.");
            }

            _wordCloudAngles = copy;
        }
    }


    /// <summary>
    /// Gets or sets the optional maximum number of positive-weight terms rendered in a word cloud.
    /// </summary>
    public int? MaxTerms {
        get => _wordCloudMaximumTerms;
        set {
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Word cloud maximum terms must be greater than zero.");
            _wordCloudMaximumTerms = value;
        }
    }


    /// <summary>
    /// Gets or sets the word cloud packing density from 0.5 to 2.0.
    /// </summary>
    public double Density {
        get => _wordCloudDensity;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0.5 || value > 2.0) throw new ArgumentOutOfRangeException(nameof(value), value, "Word cloud density must be between 0.5 and 2.0.");
            _wordCloudDensity = value;
        }
    }


    /// <summary>Sets the smallest and largest term font sizes together.</summary>
    /// <param name="minimum">The smallest font size.</param>
    /// <param name="maximum">The largest font size, not below <paramref name="minimum"/>.</param>
    /// <returns>The same options.</returns>
    public ChartWordCloudOptions FontRange(double minimum, double maximum) {
        ChartGuards.Finite(minimum, nameof(minimum));
        ChartGuards.Finite(maximum, nameof(maximum));
        if (minimum <= 0) throw new ArgumentOutOfRangeException(nameof(minimum), minimum, "Word cloud minimum font size must be greater than zero.");
        if (maximum <= minimum) throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Word cloud maximum font size must be greater than the minimum.");
        _wordCloudMinFontSize = minimum;
        _wordCloudMaxFontSize = maximum;
    
        return this;
    }
}
