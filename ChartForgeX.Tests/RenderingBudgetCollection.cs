using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Runs rendering budget measurements outside parallel test collections.</summary>
[CollectionDefinition(nameof(RenderingBudgetCollection), DisableParallelization = true)]
public sealed class RenderingBudgetCollection {
}
