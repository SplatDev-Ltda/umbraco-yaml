using Microsoft.Extensions.Logging;
using Moq;
using SplatDev.Umbraco.Plugins.Schema2Yaml.Services;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace SplatDev.Umbraco.Plugins.Schema2Yaml.Tests.Services;

/// <summary>
/// Regression tests for DataTypeExporter on the Umbraco 13 branch.
/// Umbraco 13 exposes EditorAlias / Configuration (not EditorUiAlias / ConfigurationObject).
/// </summary>
public class DataTypeExporterRegressionTests
{
    private static (DataTypeExporter sut, Mock<IDataTypeService> mockService)
        CreateSut(int umbracoMajor = 13)
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

    private static Mock<IDataType> BuildDataType(string name, string editorAlias, ValueStorageType dbType, object? config = null)
    {
        var mock = new Mock<IDataType>();
        mock.Setup(dt => dt.Name).Returns(name);
        mock.Setup(dt => dt.EditorAlias).Returns(editorAlias);
        mock.Setup(dt => dt.Configuration).Returns(config);
        mock.Setup(dt => dt.DatabaseType).Returns(dbType);
        return mock;
    }

    // ── EditorAlias mapping ─────────────────────────────────────────────────

    [Fact]
    public async Task ExportAsync_ReadsEditorAlias_ForUmbraco13()
    {
        var (sut, mockService) = CreateSut(13);
        var dt = BuildDataType("Textstring", "Umbraco.TextBox", ValueStorageType.Nvarchar);
        mockService.Setup(s => s.GetAll()).Returns([dt.Object]);

        var result = await sut.ExportAsync();

        Assert.Single(result);
        Assert.Equal("Umbraco.TextBox", result[0].EditorUiAlias);
    }

    // ── Alias generation regression ──────────────────────────────────────────

    [Theory]
    [InlineData("Textstring", "textstring")]
    [InlineData("Rich Text Editor", "richTextEditor")]
    [InlineData("Image Cropper", "imageCropper")]
    [InlineData("Block List", "blockList")]
    [InlineData("My Custom Editor", "myCustomEditor")]
    public async Task ExportAsync_GeneratesCorrectCamelCaseAlias(string name, string expectedAlias)
    {
        var (sut, mockService) = CreateSut();
        var dt = BuildDataType(name, "Umbraco.TextBox", ValueStorageType.Nvarchar);
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
        var (sut, mockService) = CreateSut();
        var dt = BuildDataType("Test", "Umbraco.TextBox", dbType);
        mockService.Setup(s => s.GetAll()).Returns([dt.Object]);

        var result = await sut.ExportAsync();

        Assert.Single(result);
        Assert.Equal(expectedValueType, result[0].ValueType);
    }

    // ── Configuration extraction regression ──────────────────────────────────

    [Fact]
    public async Task ExportAsync_ReturnsEmptyConfig_WhenConfigurationIsNull()
    {
        var (sut, mockService) = CreateSut();
        var dt = BuildDataType("Simple Text", "Umbraco.TextBox", ValueStorageType.Nvarchar);
        mockService.Setup(s => s.GetAll()).Returns([dt.Object]);

        var result = await sut.ExportAsync();

        Assert.Single(result);
        Assert.Empty(result[0].Config);
    }

    [Fact]
    public async Task ExportAsync_ExtractsConfigurationProperties_WhenConfigObjectProvided()
    {
        var (sut, mockService) = CreateSut();
        var config = new { maxLength = 100, pattern = "[a-z]+" };
        var dt = BuildDataType("Validated Text", "Umbraco.TextBox", ValueStorageType.Nvarchar, config);
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
        var (sut, mockService) = CreateSut();

        var broken = new Mock<IDataType>();
        broken.Setup(d => d.Name).Returns("Broken");
        broken.Setup(d => d.DatabaseType).Throws(new InvalidOperationException("DB error"));
        broken.Setup(d => d.EditorAlias).Returns("Umbraco.TextBox");
        broken.Setup(d => d.Configuration).Returns(null as object);

        var good = BuildDataType("Textstring", "Umbraco.TextBox", ValueStorageType.Nvarchar);

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
        mockVersion.Setup(v => v.Version).Returns(new Version(13, 0, 0));
        var mockVersionLogger = new Mock<ILogger<UmbracoVersionDetector>>();
        var detector = new UmbracoVersionDetector(mockVersion.Object, mockVersionLogger.Object);
        var mockLogger = new Mock<ILogger<DataTypeExporter>>();

        Assert.Throws<ArgumentNullException>(() =>
            new DataTypeExporter(null!, detector, mockLogger.Object));
    }
}
