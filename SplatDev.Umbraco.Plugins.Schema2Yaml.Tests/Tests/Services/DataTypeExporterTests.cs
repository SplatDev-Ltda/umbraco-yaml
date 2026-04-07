using Microsoft.Extensions.Logging;
using Moq;
using SplatDev.Umbraco.Plugins.Schema2Yaml.Services;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace SplatDev.Umbraco.Plugins.Schema2Yaml.Tests.Services;

public class DataTypeExporterTests
{
    private readonly Mock<IDataTypeService> _mockDataTypeService;
    private readonly Mock<ILogger<DataTypeExporter>> _mockLogger;
    private readonly UmbracoVersionDetector _versionDetector;
    private readonly DataTypeExporter _sut;

    public DataTypeExporterTests()
    {
        _mockDataTypeService = new Mock<IDataTypeService>();
        _mockLogger = new Mock<ILogger<DataTypeExporter>>();

        var mockUmbracoVersion = new Mock<IUmbracoVersion>();
        mockUmbracoVersion.Setup(v => v.Version).Returns(new Version(13, 0, 0));
        var mockVersionLogger = new Mock<ILogger<UmbracoVersionDetector>>();
        _versionDetector = new UmbracoVersionDetector(mockUmbracoVersion.Object, mockVersionLogger.Object);

        _sut = new DataTypeExporter(_mockDataTypeService.Object, _versionDetector, _mockLogger.Object);
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

    [Fact]
    public async Task ExportAsync_WhenNoDataTypes_ReturnsEmptyList()
    {
        _mockDataTypeService.Setup(s => s.GetAll()).Returns([]);

        var result = await _sut.ExportAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task ExportAsync_MapsNameAndEditorAlias()
    {
        var mockDataType = BuildDataType("Textstring", "Umbraco.TextBox", ValueStorageType.Ntext);
        _mockDataTypeService.Setup(s => s.GetAll()).Returns([mockDataType.Object]);

        var result = await _sut.ExportAsync();

        Assert.Single(result);
        Assert.Equal("Textstring", result[0].Name);
        Assert.Equal("Umbraco.TextBox", result[0].EditorUiAlias);
    }

    [Fact]
    public async Task ExportAsync_GeneratesAlias_AsCamelCase()
    {
        var mockDataType = BuildDataType("Rich Text Editor", "Umbraco.TinyMCE", ValueStorageType.Ntext);
        _mockDataTypeService.Setup(s => s.GetAll()).Returns([mockDataType.Object]);

        var result = await _sut.ExportAsync();

        Assert.Single(result);
        Assert.Equal("richTextEditor", result[0].Alias);
    }

    [Fact]
    public async Task ExportAsync_ExportsMultipleDataTypes()
    {
        var mockDt1 = BuildDataType("Textstring", "Umbraco.TextBox", ValueStorageType.Nvarchar);
        var mockDt2 = BuildDataType("Numeric", "Umbraco.Integer", ValueStorageType.Integer);
        _mockDataTypeService.Setup(s => s.GetAll()).Returns([mockDt1.Object, mockDt2.Object]);

        var result = await _sut.ExportAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Textstring", result[0].Name);
        Assert.Equal("Numeric", result[1].Name);
    }

    [Fact]
    public async Task ExportAsync_SetsValueType_FromDatabaseType()
    {
        var mockDataType = BuildDataType("Number", "Umbraco.Integer", ValueStorageType.Integer);
        _mockDataTypeService.Setup(s => s.GetAll()).Returns([mockDataType.Object]);

        var result = await _sut.ExportAsync();

        Assert.Single(result);
        Assert.Equal("Integer", result[0].ValueType);
    }
}
