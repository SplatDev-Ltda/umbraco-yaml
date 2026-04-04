using Umbraco.Cms.Core.Configuration;
using Microsoft.Extensions.Logging;

namespace SplatDev.Umbraco.Plugins.Schema2Yaml.Services;

/// <summary>
/// Supported Umbraco versions for export.
/// </summary>
public enum UmbracoVersion
{
    V13,
    V14,
    V15,
    V16,
    V17,
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

        _cachedVersion = version.Major switch
        {
            13 => UmbracoVersion.V13,
            14 => UmbracoVersion.V14,
            15 => UmbracoVersion.V15,
            16 => UmbracoVersion.V16,
            17 => UmbracoVersion.V17,
            _ => UmbracoVersion.Unknown
        };

        if (_cachedVersion == UmbracoVersion.Unknown)
        {
            _logger.LogWarning(
                "Umbraco version {Version} is not explicitly supported. Export may have compatibility issues.",
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
    /// </summary>
    public bool SupportsEditorUiAlias()
    {
        var version = GetVersion();
        return version is UmbracoVersion.V14 or UmbracoVersion.V15 or UmbracoVersion.V16 or UmbracoVersion.V17;
    }

    /// <summary>
    /// Determines if the current version uses legacy property editor aliases (V13).
    /// </summary>
    public bool UsesLegacyEditorAlias()
    {
        return GetVersion() == UmbracoVersion.V13;
    }
}
