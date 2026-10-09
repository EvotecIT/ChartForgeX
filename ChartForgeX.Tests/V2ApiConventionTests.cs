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

/// <summary>Guards the reviewed v2 contract boundary without applying new rules to unmigrated legacy APIs.</summary>
public sealed class V2ApiConventionTests {
    private static readonly Type[] ImmutableContracts = {
        typeof(VisualSize), typeof(VisualLayoutOptions), typeof(VisualFrame), typeof(VisualRenderContext),
        typeof(VisualRenderOptions), typeof(PreparedVisual), typeof(VisualDiagnostic), typeof(VisualSemanticRegion),
        typeof(VisualTheme), typeof(VisualThemeColors), typeof(VisualTypography)
    };

    [Fact]
    public void ReviewedContracts_ExposeReadOnlyStateAndExplicitOperationRoles() {
        foreach (var type in ImmutableContracts) {
            Assert.Empty(type.GetFields(BindingFlags.Public | BindingFlags.Instance));
            Assert.All(type.GetProperties(BindingFlags.Public | BindingFlags.Instance), property => Assert.Null(property.SetMethod));
            foreach (var method in Methods(type)) {
                var factory = type == typeof(VisualTheme) && method.IsStatic && method.ReturnType == type && (method.Name is "Graphite" or "FromJson" or "FromThemeJson");
                var resolve = type == typeof(VisualTheme) && method.Name == "Resolve" && method.ReturnType == typeof(VisualThemeColors);
                var export = (type == typeof(PreparedVisual) || type == typeof(VisualTheme)) && method.Name.StartsWith("To", StringComparison.Ordinal);
                var copy = type == typeof(VisualTheme) && !method.IsStatic && method.Name == nameof(VisualTheme.WithTypography)
                    && method.ReturnType == type && method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(new[] { typeof(VisualTypography) });
                Assert.True(factory || resolve || export || copy,
                    type.Name + "." + method.Name + " needs a reviewed operation role. Immutable render contracts use constructors, factories, and explicitly reviewed copy operations; Add/Configure belong to mutable model builders.");
            }
        }
        var prepare = typeof(Chart).GetMethod(nameof(Chart.Prepare), new[] { typeof(VisualRenderContext) });
        Assert.NotNull(prepare);
        Assert.Equal(typeof(PreparedVisual), prepare.ReturnType);
        Assert.Contains(typeof(IVisualRenderable), typeof(Chart).GetInterfaces());
        Assert.Equal(typeof(PreparedVisual), typeof(IVisualRenderable).GetMethod(nameof(IVisualRenderable.Prepare))!.ReturnType);
    }

    [Fact]
    public void ModelBuilderSelection_WithConfigures_AddAddsData_ConfigureEditsExistingSubObject() {
        // This selection is the mutable model bridge used by Phase 1, not a blanket legacy API audit.
        var chart = Chart.Create();
        Assert.Same(chart, chart.WithTitle("Capacity").WithSubtitle("Observed"));
        Assert.Empty(chart.Series);
        Assert.Equal("Capacity", chart.Title);
        var axis = chart.Options.XAxis;
        Assert.Same(chart, chart.ConfigureXAxis(existing => existing.Minimum = 1));
        Assert.Same(axis, chart.Options.XAxis);
        Assert.Equal(1d, axis.Minimum);
        Assert.Empty(chart.Series);
        Assert.Same(chart, chart.AddLine("Used", new[] { new ChartPoint(1, 3), new ChartPoint(2, 5) }));
        Assert.Single(chart.Series);
        Assert.Equal(2, chart.Series[0].Points.Count);
        Assert.Equal("Capacity", chart.Title);
        Assert.Same(axis, chart.Options.XAxis);
        var configure = typeof(Chart).GetMethod(nameof(Chart.ConfigureXAxis))!;
        Assert.Equal(typeof(Action<ChartAxis>), Assert.Single(configure.GetParameters()).ParameterType);
        Assert.Equal(typeof(Chart), configure.ReturnType);
    }

    [Fact]
    public void FamilyConfigurationCallbacks_EditExistingOptionsAndReturnTheirBuilder() {
        var chart = Chart.Create().AddRadar("Signal", new[] { new ChartPoint(1, 50), new ChartPoint(2, 70) });
        var funnel = chart.Options.Funnel;
        var pyramid = chart.Options.Pyramid;
        var chord = chart.Options.Chord;
        var sankey = chart.Options.Sankey;
        Assert.Same(chart, chart.ConfigureFunnel(options => {
            Assert.Same(funnel, options);
            options.Orientation = ChartOrientation.Horizontal;
        }));
        Assert.Same(chart, chart.ConfigurePyramid(options => {
            Assert.Same(pyramid, options);
            options.ValueEncoding = ChartPyramidValueEncoding.Area;
        }));
        Assert.Same(chart, chart.ConfigureChord(options => {
            Assert.Same(chord, options);
            options.RibbonOpacity = .2;
        }));
        Assert.Same(chart, chart.ConfigureSankey(options => {
            Assert.Same(sankey, options);
            options.NodeWidth = 18;
        }));
        Assert.Same(funnel, chart.Options.Funnel);
        Assert.Same(pyramid, chart.Options.Pyramid);
        Assert.Same(chord, chart.Options.Chord);
        Assert.Same(sankey, chart.Options.Sankey);
        Assert.Equal(ChartOrientation.Horizontal, chart.Options.Funnel.Orientation);
        Assert.Equal(ChartPyramidValueEncoding.Area, chart.Options.Pyramid.ValueEncoding);
        Assert.Equal(.2, chart.Options.Chord.RibbonOpacity);
        Assert.Equal(18, chart.Options.Sankey.NodeWidth);

        var series = chart.Series[0];
        var markers = series.Markers;
        var radar = series.Radar;
        Assert.Same(series, series.ConfigureMarkers(options => {
            Assert.Same(markers, options);
            options.Radius = 7;
        }));
        Assert.Same(series, series.ConfigureRadar(options => {
            Assert.Same(radar, options);
            options.FillOpacity = .4;
        }));
        Assert.Same(markers, series.Markers);
        Assert.Same(radar, series.Radar);
        Assert.Equal(7d, series.Markers.Radius);
        Assert.Equal(.4, series.Radar.FillOpacity);
        Assert.Single(chart.Series);
        Assert.Equal(2, series.Points.Count);
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
        Assert.Same(card, card.WithMiniSparklineStyle(series.Radar.Form));
        Assert.Equal(ChartLineAreaForm.Line, card.MiniSparklineStyle);
    }

    [Fact]
    public void ContextThemeAndPreparedVisual_AreDetachedFromMutableInputsAndReturnedCopies() {
        var light = new VisualDesignTokens();
        var dark = VisualDesignTokens.Dark();
        var theme = new VisualTheme(light, dark);
        var originalPalette = theme.Resolve(VisualThemeMode.Light).Palette.ToArray();
        var font = FontSpec.SystemSans();
        var originalFamily = font.Family;
        var context = new VisualRenderContext(theme: theme, font: font, frame: new VisualFrame(showLegend: false));
        font.Family = "Changed input family";
        context.Font.Family = "Changed returned family";
        light.Palette = new[] { ChartColor.FromHex("#112233") };
        Assert.Equal(originalFamily, context.Font.Family);
        Assert.Equal(originalPalette, theme.Resolve(VisualThemeMode.Light).Palette);
        var colors = theme.Resolve(VisualThemeMode.Light);
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
        foreach (var method in Methods(typeof(PreparedVisual))) {
            Assert.StartsWith("To", method.Name, StringComparison.Ordinal);
            Assert.Contains(method.ReturnType, new[] { typeof(string), typeof(byte[]), typeof(RgbaImage) });
            Assert.All(method.GetParameters(), parameter => {
                if (method.Name == nameof(PreparedVisual.ToSvg)) {
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
