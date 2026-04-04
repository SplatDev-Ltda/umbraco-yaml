using Microsoft.Extensions.Logging;
using Moq;
using SplatDev.Umbraco.Plugins.Schema2Yaml.Services;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace SplatDev.Umbraco.Plugins.Schema2Yaml.Tests.Services;

public class DictionaryExporterTests
{
    private readonly Mock<ILocalizationService> _mockLocalizationService;
    private readonly Mock<ILogger<DictionaryExporter>> _mockLogger;
    private readonly DictionaryExporter _sut;

    public DictionaryExporterTests()
    {
        _mockLocalizationService = new Mock<ILocalizationService>();
        _mockLogger = new Mock<ILogger<DictionaryExporter>>();

        _sut = new DictionaryExporter(_mockLocalizationService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ExportAsync_WhenNoRootItems_ReturnsEmptyList()
    {
        _mockLocalizationService.Setup(s => s.GetRootDictionaryItems()).Returns([]);

        var result = await _sut.ExportAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task ExportAsync_ExportsRootItemWithTranslations()
    {
        var mockTranslation = new Mock<IDictionaryTranslation>();
        mockTranslation.Setup(t => t.LanguageIsoCode).Returns("en-US");
        mockTranslation.Setup(t => t.Value).Returns("Welcome");

        var mockItem = new Mock<IDictionaryItem>();
        mockItem.Setup(i => i.ItemKey).Returns("general.welcome");
        mockItem.Setup(i => i.Key).Returns(Guid.NewGuid());
        mockItem.Setup(i => i.Translations).Returns([mockTranslation.Object]);

        _mockLocalizationService.Setup(s => s.GetRootDictionaryItems())
            .Returns([mockItem.Object]);
        _mockLocalizationService.Setup(s => s.GetDictionaryItemChildren(It.IsAny<Guid>()))
            .Returns([]);

        var result = await _sut.ExportAsync();

        Assert.Single(result);
        Assert.Equal("general.welcome", result[0].Key);
        Assert.True(result[0].Translations.ContainsKey("en-US"));
        Assert.Equal("Welcome", result[0].Translations["en-US"]);
    }

    [Fact]
    public async Task ExportAsync_ExportsChildItems_FlattenedIntoList()
    {
        var parentKey = Guid.NewGuid();

        var mockParent = new Mock<IDictionaryItem>();
        mockParent.Setup(i => i.ItemKey).Returns("nav");
        mockParent.Setup(i => i.Key).Returns(parentKey);
        mockParent.Setup(i => i.Translations).Returns([]);

        var mockChild = new Mock<IDictionaryItem>();
        mockChild.Setup(i => i.ItemKey).Returns("nav.home");
        mockChild.Setup(i => i.Key).Returns(Guid.NewGuid());
        mockChild.Setup(i => i.Translations).Returns([]);

        _mockLocalizationService.Setup(s => s.GetRootDictionaryItems())
            .Returns([mockParent.Object]);
        _mockLocalizationService.Setup(s => s.GetDictionaryItemChildren(parentKey))
            .Returns([mockChild.Object]);
        _mockLocalizationService.Setup(s => s.GetDictionaryItemChildren(It.Is<Guid>(g => g != parentKey)))
            .Returns([]);

        var result = await _sut.ExportAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("nav", result[0].Key);
        Assert.Equal("nav.home", result[1].Key);
    }

    [Fact]
    public async Task ExportAsync_SkipsTranslation_WhenLanguageIsNull()
    {
        var mockTranslation = new Mock<IDictionaryTranslation>();
        mockTranslation.Setup(t => t.LanguageIsoCode).Returns(string.Empty);
        mockTranslation.Setup(t => t.Value).Returns("Welcome");

        var mockItem = new Mock<IDictionaryItem>();
        mockItem.Setup(i => i.ItemKey).Returns("general.welcome");
        mockItem.Setup(i => i.Key).Returns(Guid.NewGuid());
        mockItem.Setup(i => i.Translations).Returns([mockTranslation.Object]);

        _mockLocalizationService.Setup(s => s.GetRootDictionaryItems())
            .Returns([mockItem.Object]);
        _mockLocalizationService.Setup(s => s.GetDictionaryItemChildren(It.IsAny<Guid>()))
            .Returns([]);

        var result = await _sut.ExportAsync();

        Assert.Single(result);
        Assert.Empty(result[0].Translations);
    }
}
