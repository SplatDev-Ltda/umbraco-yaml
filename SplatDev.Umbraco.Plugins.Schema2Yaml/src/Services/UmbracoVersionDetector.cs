using Umbraco.Cms.Core.Configuration;
using Microsoft.Extensions.Logging;

namespace SplatDev.Umbraco.Plugins.Schema2Yaml.Services;

/// <summary>
/// Supported Umbraco versions for export (Umbraco 13 only branch).
/// </summary>
public enum UmbracoVersion
{
    V13,
    Unknown
}

/// <summary>
/// Detects the current Umbraco version to handle version-specific export logic.
/// </summary>
public class UmbracoVersionDetector
{
    private readonly IUmbracoVersion _umbracoVersion;
    private readonly ILogger<UmbracoVersionDetector> _logger;
    private UmbracoVersion? _cachedVersion;

    public UmbracoVersionDetector(
        IUmbracoVersion umbracoVersion,
        ILogger<UmbracoVersionDetector> logger)
    {
        _umbracoVersion = umbracoVersion ?? throw new ArgumentNullException(nameof(umbracoVersion));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the current Umbraco version.
    /// </summary>
    public UmbracoVersion GetVersion()
    {
        if (_cachedVersion.HasValue)
        {
            return _cachedVersion.Value;
        }

        var version = _umbracoVersion.Version;
        _logger.LogDebug("Detecting Umbraco version: {Version}", version);

        _cachedVersion = version.Major == 13 ? UmbracoVersion.V13 : UmbracoVersion.Unknown;

        if (_cachedVersion == UmbracoVersion.Unknown)
        {
            _logger.LogWarning(
                "Umbraco version {Version} is not supported by this package. Use SplatDev.Umbraco.Plugins.Schema2Yaml v2.x for Umbraco 14–17.",
                version);
        }
        else
        {
            _logger.LogInformation("Detected Umbraco version: {Version}", _cachedVersion);
        }

        return _cachedVersion.Value;
    }

    /// <summary>
    /// Gets the version string for export metadata.
    /// </summary>
    public string GetVersionString()
    {
        return _umbracoVersion.Version.ToString();
    }

    /// <summary>
    /// Determines if the current version supports the new editor UI alias format (V14+).
    /// Always false on this Umbraco 13 branch.
    /// </summary>
    public bool SupportsEditorUiAlias() => false;

    /// <summary>
    /// Determines if the current version uses legacy property editor aliases (V13).
    /// Always true on this Umbraco 13 branch.
    /// </summary>
    public bool UsesLegacyEditorAlias() => true;
}
