using Microsoft.Extensions.Logging;
using Moq;
using SplatDev.Umbraco.Plugins.Schema2Yaml.Services;
using Umbraco.Cms.Core.Configuration;
using UmbracoVersion = SplatDev.Umbraco.Plugins.Schema2Yaml.Services.UmbracoVersion;

namespace SplatDev.Umbraco.Plugins.Schema2Yaml.Tests.Services;

public class UmbracoVersionDetectorTests
{
    private static UmbracoVersionDetector CreateDetector(int major, int minor = 0, int patch = 0)
    {
        var mockVersion = new Mock<IUmbracoVersion>();
        mockVersion.Setup(v => v.Version).Returns(new Version(major, minor, patch));
        var mockLogger = new Mock<ILogger<UmbracoVersionDetector>>();
        return new UmbracoVersionDetector(mockVersion.Object, mockLogger.Object);
    }

    // ── GetVersion ──────────────────────────────────────────────────────────

    [Fact]
    public void GetVersion_ReturnsV13_ForMajor13()
    {
        var sut = CreateDetector(13);

        var result = sut.GetVersion();

        Assert.Equal(UmbracoVersion.V13, result);
    }

    [Theory]
    [InlineData(12)]
    [InlineData(14)]
    [InlineData(15)]
    [InlineData(17)]
    [InlineData(18)]
    [InlineData(99)]
    public void GetVersion_ReturnsUnknown_ForNonV13Majors(int major)
    {
        var sut = CreateDetector(major);

        var result = sut.GetVersion();

        Assert.Equal(UmbracoVersion.Unknown, result);
    }

    [Fact]
    public void GetVersion_ReturnsCachedResult_OnSecondCall()
    {
        var mockVersion = new Mock<IUmbracoVersion>();
        mockVersion.Setup(v => v.Version).Returns(new Version(13, 0, 0));
        var mockLogger = new Mock<ILogger<UmbracoVersionDetector>>();
        var sut = new UmbracoVersionDetector(mockVersion.Object, mockLogger.Object);

        sut.GetVersion();
        sut.GetVersion();

        // Version property should be read exactly once due to caching
        mockVersion.Verify(v => v.Version, Times.Once);
    }

    // ── GetVersionString ─────────────────────────────────────────────────────

    [Fact]
    public void GetVersionString_ReturnsVersionAsDotSeparatedString()
    {
        var sut = CreateDetector(13, 7, 2);

        var result = sut.GetVersionString();

        Assert.Equal("13.7.2", result);
    }

    // ── SupportsEditorUiAlias ────────────────────────────────────────────────

    [Theory]
    [InlineData(13)]
    [InlineData(14)]
    [InlineData(12)]
    public void SupportsEditorUiAlias_AlwaysReturnsFalse_OnV13Branch(int major)
    {
        var sut = CreateDetector(major);

        Assert.False(sut.SupportsEditorUiAlias());
    }

    // ── UsesLegacyEditorAlias ────────────────────────────────────────────────

    [Theory]
    [InlineData(13)]
    [InlineData(14)]
    [InlineData(12)]
    public void UsesLegacyEditorAlias_AlwaysReturnsTrue_OnV13Branch(int major)
    {
        var sut = CreateDetector(major);

        Assert.True(sut.UsesLegacyEditorAlias());
    }

    // ── Constructor guard ────────────────────────────────────────────────────

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenUmbracoVersionIsNull()
    {
        var mockLogger = new Mock<ILogger<UmbracoVersionDetector>>();

        Assert.Throws<ArgumentNullException>(() =>
            new UmbracoVersionDetector(null!, mockLogger.Object));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        var mockVersion = new Mock<IUmbracoVersion>();
        mockVersion.Setup(v => v.Version).Returns(new Version(13, 0, 0));

        Assert.Throws<ArgumentNullException>(() =>
            new UmbracoVersionDetector(mockVersion.Object, null!));
    }
}
