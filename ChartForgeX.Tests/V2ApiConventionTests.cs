using System.Reflection;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Diagnostics;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Guards the reviewed v2 contract boundary including immutable paired themes and detached option callbacks.</summary>
public sealed class V2ApiConventionTests {
    private static readonly Type[] ImmutableContracts = {
        typeof(VisualSize), typeof(VisualLayoutOptions), typeof(VisualFrame), typeof(VisualRenderContext),
        typeof(VisualRenderOptions), typeof(PreparedVisual), typeof(VisualDiagnostic), typeof(VisualSemanticRegion),
        typeof(VisualTheme), typeof(VisualThemeColors), typeof(VisualTypography), typeof(ChartChordOrdering), typeof(ChartFunnelConnectorStyle), typeof(ChartDifferenceAlignment)
    };

    [Fact]
    public void ReviewedContracts_ExposeReadOnlyStateAndExplicitOperationRoles() {
        foreach (var type in ImmutableContracts) {
            Assert.Empty(type.GetFields(BindingFlags.Public | BindingFlags.Instance));
            Assert.All(type.GetProperties(BindingFlags.Public | BindingFlags.Instance), property => Assert.Null(property.SetMethod));
            foreach (var method in Methods(type)) {
                var factory = type == typeof(VisualTheme) && method.IsStatic && method.ReturnType == type && !method.IsSpecialName;
                var resolve = type == typeof(VisualTheme) && method.Name == "Resolve" && method.ReturnType == typeof(VisualThemeColors);
                var export = (type == typeof(PreparedVisual) || type == typeof(VisualTheme) || type == typeof(VisualTypography)) && method.Name.StartsWith("To", StringComparison.Ordinal);
                var capture = type == typeof(PreparedVisual) && method.Name == nameof(PreparedVisual.CaptureLayer)
                    && method.ReturnType == typeof(CapturedVisualLayer);
                var copy = (type == typeof(VisualTheme) || type == typeof(VisualTypography) || type == typeof(PreparedVisual)) && !method.IsStatic && method.ReturnType == type
                    && (method.Name.StartsWith("With", StringComparison.Ordinal) || method.Name == nameof(VisualTheme.Configure));
                Assert.True(factory || resolve || export || copy || capture,
                    type.Name + "." + method.Name + " needs a reviewed operation role. Immutable render contracts use constructors, factories, and copy operations; theme configuration snapshots its independent builder.");
            }
        }
        var prepare = typeof(Chart).GetMethod(nameof(Chart.Prepare), new[] { typeof(VisualRenderContext) });
        Assert.NotNull(prepare);
        Assert.Equal(typeof(PreparedVisual), prepare.ReturnType);
        Assert.Contains(typeof(IVisualRenderable), typeof(Chart).GetInterfaces());
        Assert.Equal(typeof(PreparedVisual), typeof(IVisualRenderable).GetMethod(nameof(IVisualRenderable.Prepare))!.ReturnType);
    }

    [Fact]
    public void ModelBuilderSelection_WithSetsOptions_AddAddsData_ConfigureInstallsDetachedOptions() {
        // This selection is the mutable model bridge used by Phase 1, not a blanket legacy API audit.
        var chart = Chart.Create();
        Assert.Same(chart, chart.Title("Capacity").Subtitle("Observed"));
        Assert.Empty(chart.Series);
        Assert.Equal("Capacity", chart.Title);
        var axis = chart.Options.XAxis;
        Assert.Same(chart, chart.XAxis(existing => existing.Min = 1));
        Assert.Null(axis.Min);
        Assert.Equal(1d, chart.Options.XAxis.Min);
        var configuredAxis = chart.Options.XAxis;
        Assert.Empty(chart.Series);
        Assert.Same(chart, chart.AddLine("Used", new[] { new ChartPoint(1, 3), new ChartPoint(2, 5) }));
        Assert.Single(chart.Series);
        Assert.Equal(2, chart.Series[0].Points.Count);
        Assert.Equal("Capacity", chart.Title);
        Assert.Same(configuredAxis, chart.Options.XAxis);
        var configure = typeof(ChartFluent).GetMethod(nameof(ChartFluent.XAxis), new[] { typeof(Chart), typeof(Action<ChartAxis>) })!;
        Assert.Equal(typeof(Chart), configure.ReturnType);
    }

    [Fact]
    public void FamilyConfigurationCallbacksApplyOptionsAndReturnTheirBuilderWithoutChangingData() {
        var chart = Chart.Create().AddRadar("Signal", new[] { new ChartPoint(1, 50), new ChartPoint(2, 70) });
        Assert.Same(chart, chart.Funnel(options => {
            options.Orientation = ChartOrientation.Horizontal;
        }));
        Assert.Same(chart, chart.Pyramid(options => {
            options.ValueEncoding = ChartPyramidValueEncoding.Area;
        }));
        Assert.Same(chart, chart.Chord(options => {
            options.RibbonOpacity = .2;
        }));
        Assert.Same(chart, chart.Sankey(options => {
            options.NodeWidth = 18;
        }));
        Assert.Equal(ChartOrientation.Horizontal, chart.Options.Funnel.Orientation);
        Assert.Equal(ChartPyramidValueEncoding.Area, chart.Options.Pyramid.ValueEncoding);
        Assert.Equal(.2, chart.Options.Chord.RibbonOpacity);
        Assert.Equal(18, chart.Options.Sankey.NodeWidth);

        var series = chart.Series[0];
        Assert.Same(series, series.Markers(options => {
            options.Radius = 7;
        }));
        Assert.Same(series, series.Radar(options => {
            options.FillOpacity = .4;
        }));
        Assert.Equal(7d, series.Markers.Radius);
        Assert.Equal(.4, series.Radar.FillOpacity);
        Assert.Single(chart.Series);
        Assert.Equal(2, series.Points.Count);
    }

    [Fact]
    public void FluentMethods_AreNamedAfterTheirPropertiesAndReturnTheirReceiver() {
        var types = new[] { typeof(Chart).Assembly, typeof(VisualGrid).Assembly }.SelectMany(assembly => assembly.GetExportedTypes()).ToArray();
        // Callbacks are named after the settings they configure; only the immutable theme keeps Configure, which returns a new theme.
        Assert.DoesNotContain(types.SelectMany(Methods), method => method.Name.StartsWith("Configure", StringComparison.Ordinal) && method.DeclaringType != typeof(VisualTheme));
        var fluent = types.Where(type => type.Name.EndsWith("Fluent", StringComparison.Ordinal) && type.IsAbstract && type.IsSealed)
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)).ToArray();
        Assert.NotEmpty(fluent);
        foreach (var method in fluent) {
            Assert.True(method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), inherit: false), method.DeclaringType!.Name + "." + method.Name);
            var receiver = method.GetParameters()[0].ParameterType;
            Assert.True(receiver == method.ReturnType, method.DeclaringType!.Name + "." + method.Name + " must return its receiver.");
        }
    }

    [Fact]
    public void ContractSignatures_ReuseCanonicalColorSeverityAndCoreOnlyOwnership() {
        var colors = typeof(VisualThemeColors).GetProperties().Where(property => property.PropertyType.IsValueType && !property.PropertyType.IsEnum);
        Assert.NotEmpty(colors);
        Assert.All(colors, property => Assert.Equal(typeof(ChartColor), property.PropertyType));
        Assert.Equal(typeof(IReadOnlyList<ChartColor>), typeof(VisualThemeColors).GetProperty(nameof(VisualThemeColors.Palette))!.PropertyType);
        Assert.Equal(typeof(VisualDiagnosticSeverity), typeof(VisualDiagnostic).GetProperty(nameof(VisualDiagnostic.Severity))!.PropertyType);
        var core = typeof(Chart).Assembly;
        foreach (var type in ImmutableContracts.Append(typeof(IVisualRenderable))) {
            foreach (var signatureType in SignatureTypes(type).SelectMany(Flatten)) {
                var assemblyName = signatureType.Assembly.GetName().Name ?? string.Empty;
                if (assemblyName.StartsWith("ChartForgeX", StringComparison.Ordinal)) Assert.Same(core, signatureType.Assembly);
                Assert.False(signatureType.Name.StartsWith("VisualScene", StringComparison.Ordinal), "The display-list implementation must remain internal.");
            }
        }
        Assert.DoesNotContain(core.GetReferencedAssemblies(), reference =>
            reference.Name != "ChartForgeX" && (reference.Name?.StartsWith("ChartForgeX.", StringComparison.Ordinal) ?? false));
    }

    [Fact]
    public void LineAreaFormKeepsSharedNumericValuesAndRadarAndMetricDefaults() {
        Assert.Equal(0, (int)ChartLineAreaForm.Area);
        Assert.Equal(1, (int)ChartLineAreaForm.Line);
        var series = Chart.Create().AddRadar("Signal", new[] { new ChartPoint(1, 50), new ChartPoint(2, 70) }).Series[0];
        var card = MetricCard.Create();
        Assert.Equal(ChartLineAreaForm.Area, series.Radar.Form);
        Assert.Equal(ChartLineAreaForm.Area, card.MiniSparklineStyle);
        series.Radar.Form = ChartLineAreaForm.Line;
        Assert.Same(card, card.MiniSparklineStyle(series.Radar.Form));
        Assert.Equal(ChartLineAreaForm.Line, card.MiniSparklineStyle);
    }

    [Fact]
    public void ContextThemeAndPreparedVisual_AreDetachedFromMutableInputsAndReturnedCopies() {
        var light = new VisualDesignTokens();
        var dark = VisualDesignTokens.Dark();
        var theme = new VisualTheme(light, dark);
        var originalPalette = theme.Resolve(ThemeMode.Light).Palette.ToArray();
        var font = FontSpec.SystemSans();
        var originalFamily = font.Family;
        var context = new VisualRenderContext(theme: theme, font: font, frame: new VisualFrame(showLegend: false));
        font.Family = "Changed input family";
        context.Font.Family = "Changed returned family";
        light.Palette = new[] { ChartColor.FromHex("#112233") };
        Assert.Equal(originalFamily, context.Font.Family);
        Assert.Equal(originalPalette, theme.Resolve(ThemeMode.Light).Palette);
        var colors = theme.Resolve(ThemeMode.Light);
        var statusFill = colors.Status.Critical.Fill;
        colors.Status.Critical = colors.Status.Pass;
        Assert.Equal(statusFill, colors.Status.Critical.Fill);
        Assert.Throws<NotSupportedException>(() => ((IList<ChartColor>)colors.Palette)[0] = ChartColor.Black);

        var chart = Chart.Create().AddDonut("Capacity", new[] { new ChartPoint(1, 60), new ChartPoint(2, 40) });
        chart.Accessibility.Name = "Storage capacity";
        var prepared = chart.Prepare(context);
        var svg = prepared.ToSvg();
        var png = prepared.ToPng();
        chart.Series[0].Points.Clear();
        chart.Accessibility.Name = "Changed source alternative";
        prepared.Accessibility.Name = "Changed returned alternative";
        Assert.Equal("Storage capacity", prepared.Accessibility.Name);
        Assert.Equal(svg, prepared.ToSvg());
        Assert.Equal(png, prepared.ToPng());
    }

    [Fact]
    public void PreparedToMethods_ReturnInMemoryValuesWithTheResolvedViewport() {
        var exports = Methods(typeof(PreparedVisual)).Where(method => method.Name.StartsWith("To", StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(exports);
        foreach (var method in exports) {
            Assert.StartsWith("To", method.Name, StringComparison.Ordinal);
            Assert.Contains(method.ReturnType, new[] { typeof(string), typeof(byte[]), typeof(RgbaImage) });
            Assert.All(method.GetParameters(), parameter => {
                if (parameter.ParameterType == typeof(CancellationToken)) {
                    Assert.Equal("cancellationToken", parameter.Name);
                } else if (method.Name == nameof(PreparedVisual.ToSvg)) {
                    if (parameter.ParameterType == typeof(VisualSvgOptions)) Assert.Equal("options", parameter.Name);
                    else {
                        Assert.Equal("idPrefix", parameter.Name);
                        Assert.Equal(typeof(string), parameter.ParameterType);
                    }
                } else Assert.Equal(typeof(VisualRenderOptions), parameter.ParameterType);
            });
        }
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(360, 240)), frame: new VisualFrame(showLegend: false));
        var prepared = Chart.Create().AddPie("Count", new[] { new ChartPoint(1, 3) }).Prepare(context);
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Equal("360", (string?)svg.Root!.Attribute("width"));
        Assert.Equal("240", (string?)svg.Root.Attribute("height"));
        var image = PngReader.Decode(prepared.ToPng(new VisualRenderOptions(scale: 2)));
        Assert.Equal(720, image.Width);
        Assert.Equal(480, image.Height);
        Assert.Equal(360, prepared.Size.Width);
        Assert.Equal(240, prepared.Size.Height);
    }

    private static IEnumerable<MethodInfo> Methods(Type type) => type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
        .Where(method => !method.IsSpecialName);

    private static IEnumerable<Type> SignatureTypes(Type type) => type.GetProperties().Select(property => property.PropertyType)
        .Concat(type.GetConstructors().SelectMany(constructor => constructor.GetParameters().Select(parameter => parameter.ParameterType)))
        .Concat(Methods(type).SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType)));

    private static IEnumerable<Type> Flatten(Type type) {
        yield return type;
        if (type.HasElementType) foreach (var element in Flatten(type.GetElementType()!)) yield return element;
        if (type.IsGenericType) foreach (var argument in type.GetGenericArguments().SelectMany(Flatten)) yield return argument;
    }
}
