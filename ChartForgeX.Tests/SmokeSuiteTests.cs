using System;
using System.Collections.Generic;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SmokeSuiteTests {
    public static IEnumerable<object[]> SmokeCases {
        get {
            foreach (var test in SmokeTests.Tests) {
                yield return new object[] { test.Name, test.Run };
            }
        }
    }

    [Theory]
    [MemberData(nameof(SmokeCases))]
    public void SmokeCasePasses(string name, Action run) {
        Assert.False(string.IsNullOrWhiteSpace(name));
        Assert.NotNull(run);
        run();
    }
}
