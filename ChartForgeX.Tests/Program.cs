using ChartForgeX.Tests;

foreach (var test in SmokeTests.Tests) {
    test.Run();
    Console.WriteLine("PASS " + test.Name);
}
