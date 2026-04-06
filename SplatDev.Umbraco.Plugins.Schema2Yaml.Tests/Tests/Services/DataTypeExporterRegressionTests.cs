using Microsoft.Extensions.Logging;
using Moq;
using SplatDev.Umbraco.Plugins.Schema2Yaml.Services;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace SplatDev.Umbraco.Plugins.Schema2Yaml.Tests.Services;

/// <summary>
/// Regression tests for DataTypeExporter.
/// On net8.0 the exporter compiles against Umbraco 13 which exposes EditorAlias / Configuration.
/// On net9.0/net10.0 it compiles against Umbraco 16/17 which expose EditorUiAlias / ConfigurationObject.
/// These tests assert correct behaviour for every TFM and guard against regressions when
/// switching Umbraco major versions.
/// </summary>
public class DataTypeExporterRegressionTests
{
    private static (DataTypeExporter sut, Mock<IDataTypeService> mockService)
        CreateSut(int umbracoMajor)
    {
        var mockService = new Mock<IDataTypeService>();
        var mockUmbracoVersion = new Mock<IUmbracoVersion>();
        mockUmbracoVersion.Setup(v => v.Version).Returns(new Version(umbracoMajor, 0, 0));
        var mockVersionLogger = new Mock<ILogger<UmbracoVersionDetector>>();
        var mockLogger = new Mock<ILogger<DataTypeExporter>>();
        var detector = new UmbracoVersionDetector(mockUmbracoVersion.Object, mockVersionLogger.Object);
        var sut = new DataTypeExporter(mockService.Object, detector, mockLogger.Object);
        return (sut, mockService);
    }

    private static Mock<IDataType> BuildDataType(string name, string editorAlias, ValueStorageType dbType)
    {
        var mock = new Mock<IDataType>();
        mock.Setup(dt => dt.Name).Returns(name);
        mock.Setup(dt => dt.DatabaseType).Returns(dbType);
#if NET8_0
        mock.Setup(dt => dt.EditorAlias).Returns(editorAlias);
        mock.Setup(dt => dt.Configuration).Returns(null as object);
#else
        mock.Setup(dt => dt.EditorUiAlias).Returns(editorAlias);
        mock.Setup(dt => dt.ConfigurationObject).Returns(null as object);
#endif
        return mock;
    }

    // ── TFM × Umbraco version matrix ────────────────────────────────────────

    #if NET8_0
    [Theory]
    [InlineData(13)]
    [InlineData(14)]
    public async Task ExportAsync_OnNet8TFM_ReadsEditorAlias_ForUmbraco13And14(int umbracoMajor)
    {
        var (sut, mockService) = CreateSut(umbracoMajor);
        var dt = BuildDataType("Textstring", "Umb.PropertyEditorUi.TextBox", ValueStorageType.Nvarchar);
        mockService.Setup(s => s.GetAll()).Returns([dt.Object]);

        var result = await sut.ExportAsync();

        Assert.Single(result);
        Assert.Equal("Umb.PropertyEditorUi.TextBox", result[0].EditorUiAlias);
    }
#endif

#if !NET8_0
    [Theory]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    public async Task ExportAsync_OnNet9OrNet10TFM_ReadsEditorUiAlias_ForUmbraco15To17(int umbracoMajor)
    {
        var (sut, mockService) = CreateSut(umbracoMajor);
        var dt = BuildDataType("Rich Text", "Umb.PropertyEditorUi.TinyMce", ValueStorageType.Ntext);
        mockService.Setup(s => s.GetAll()).Returns([dt.Object]);

        var result = await sut.ExportAsync();

        Assert.Single(result);
        Assert.Equal("Umb.PropertyEditorUi.TinyMce", result[0].EditorUiAlias);
    }

    [Fact]
    public async Task ExportAsync_OnNet9OrNet10TFM_FallsBackToEditorAlias_WhenEditorUiAliasIsNull()
    {
        var (sut, mockService) = CreateSut(13);
        var dt = new Mock<IDataType>();
        dt.Setup(d => d.Name).Returns("Legacy Editor");
        dt.Setup(d => d.DatabaseType).Returns(ValueStorageType.Ntext);
        dt.Setup(d => d.EditorUiAlias).Returns((string?)null);
        dt.Setup(d => d.EditorAlias).Returns("Umbraco.TinyMCE");
        dt.Setup(d => d.ConfigurationObject).Returns(null as object);
        mockService.Setup(s => s.GetAll()).Returns([dt.Object]);

        var result = await sut.ExportAsync();

        Assert.Single(result);
        Assert.Equal("Umbraco.TinyMCE", result[0].EditorUiAlias);
    }
#endif

    // ── Alias generation regression ──────────────────────────────────────────

    [Theory]
    [InlineData("Textstring", "textstring")]
    [InlineData("Rich Text Editor", "richTextEditor")]
    [InlineData("Image Cropper", "imageCropper")]
    [InlineData("Block List", "blockList")]
    [InlineData("My Custom Editor", "myCustomEditor")]
    public async Task ExportAsync_GeneratesCorrectCamelCaseAlias(string name, string expectedAlias)
    {
        var (sut, mockService) = CreateSut(17);
        var dt = BuildDataType(name, "Umb.PropertyEditorUi.TextBox", ValueStorageType.Nvarchar);
        mockService.Setup(s => s.GetAll()).Returns([dt.Object]);

        var result = await sut.ExportAsync();

        Assert.Single(result);
        Assert.Equal(expectedAlias, result[0].Alias);
    }

    // ── ValueType / DatabaseType mapping regression ──────────────────────────

    [Theory]
    [InlineData(ValueStorageType.Nvarchar, "Nvarchar")]
    [InlineData(ValueStorageType.Ntext, "Ntext")]
    [InlineData(ValueStorageType.Integer, "Integer")]
    [InlineData(ValueStorageType.Decimal, "Decimal")]
    [InlineData(ValueStorageType.Date, "Date")]
    public async Task ExportAsync_MapsAllDatabaseTypes(ValueStorageType dbType, string expectedValueType)
    {
        var (sut, mockService) = CreateSut(17);
        var dt = BuildDataType("Test", "Umb.PropertyEditorUi.TextBox", dbType);
        mockService.Setup(s => s.GetAll()).Returns([dt.Object]);

        var result = await sut.ExportAsync();

        Assert.Single(result);
        Assert.Equal(expectedValueType, result[0].ValueType);
    }

    // ── Configuration extraction regression ──────────────────────────────────

    [Fact]
    public async Task ExportAsync_ReturnsEmptyConfig_WhenConfigurationIsNull()
    {
        var (sut, mockService) = CreateSut(17);
        var dt = BuildDataType("Simple Text", "Umb.PropertyEditorUi.TextBox", ValueStorageType.Nvarchar);
        mockService.Setup(s => s.GetAll()).Returns([dt.Object]);

        var result = await sut.ExportAsync();

        Assert.Single(result);
        Assert.Empty(result[0].Config);
    }

    [Fact]
    public async Task ExportAsync_ExtractsConfigurationProperties_WhenConfigObjectProvided()
    {
        var (sut, mockService) = CreateSut(17);
        var config = new { maxLength = 100, pattern = "[a-z]+" };
        var dt = new Mock<IDataType>();
        dt.Setup(d => d.Name).Returns("Validated Text");
        dt.Setup(d => d.DatabaseType).Returns(ValueStorageType.Nvarchar);
#if NET8_0
        dt.Setup(d => d.EditorAlias).Returns("Umb.PropertyEditorUi.TextBox");
        dt.Setup(d => d.Configuration).Returns(config);
#else
        dt.Setup(d => d.EditorUiAlias).Returns("Umb.PropertyEditorUi.TextBox");
        dt.Setup(d => d.ConfigurationObject).Returns(config);
#endif
        mockService.Setup(s => s.GetAll()).Returns([dt.Object]);

        var result = await sut.ExportAsync();

        Assert.Single(result);
        Assert.True(result[0].Config.ContainsKey("maxLength"));
        Assert.True(result[0].Config.ContainsKey("pattern"));
    }

    // ── Error resilience regression ───────────────────────────────────────────

    [Fact]
    public async Task ExportAsync_SkipsFailingDataType_AndContinuesExport()
    {
        var (sut, mockService) = CreateSut(17);

        // Throw on DatabaseType (inside the try block; not accessed by the catch logger)
        var broken = new Mock<IDataType>();
        broken.Setup(d => d.Name).Returns("Broken");
        broken.Setup(d => d.DatabaseType).Throws(new InvalidOperationException("DB error"));
#if NET8_0
        broken.Setup(d => d.EditorAlias).Returns("Umb.PropertyEditorUi.TextBox");
        broken.Setup(d => d.Configuration).Returns(null as object);
#else
        broken.Setup(d => d.EditorUiAlias).Returns("Umb.PropertyEditorUi.TextBox");
        broken.Setup(d => d.ConfigurationObject).Returns(null as object);
#endif

        var good = BuildDataType("Textstring", "Umb.PropertyEditorUi.TextBox", ValueStorageType.Nvarchar);

        mockService.Setup(s => s.GetAll()).Returns([broken.Object, good.Object]);

        var result = await sut.ExportAsync();

        Assert.Single(result);
        Assert.Equal("Textstring", result[0].Name);
    }

    // ── Constructor null guard regression ────────────────────────────────────

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenDataTypeServiceIsNull()
    {
        var mockVersion = new Mock<IUmbracoVersion>();
        mockVersion.Setup(v => v.Version).Returns(new Version(17, 0, 0));
        var mockVersionLogger = new Mock<ILogger<UmbracoVersionDetector>>();
        var detector = new UmbracoVersionDetector(mockVersion.Object, mockVersionLogger.Object);
        var mockLogger = new Mock<ILogger<DataTypeExporter>>();

        Assert.Throws<ArgumentNullException>(() =>
            new DataTypeExporter(null!, detector, mockLogger.Object));
    }
}
