using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Services;
using SplatDev.Umbraco.Plugins.Schema2Yaml.Models;

namespace SplatDev.Umbraco.Plugins.Schema2Yaml.Services;

/// <summary>
/// Exports Umbraco Languages to YAML format.
/// </summary>
public class LanguageExporter
{
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<LanguageExporter> _logger;

    public LanguageExporter(
        ILocalizationService localizationService,
        ILogger<LanguageExporter> logger)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Exports all Languages from Umbraco.
    /// </summary>
    public async Task<List<ExportLanguage>> ExportAsync()
    {
        _logger.LogInformation("Starting Language export");

        var languages = _localizationService.GetAllLanguages();
        var exported = new List<ExportLanguage>();

        foreach (var language in languages)
        {
            try
            {
                var export = new ExportLanguage
                {
                    IsoCode = language.IsoCode,
                    CultureName = language.CultureName,
                    IsDefault = language.IsDefault,
                    IsMandatory = language.IsMandatory
                };

                exported.Add(export);
                _logger.LogDebug("Exported Language: {CultureName} ({IsoCode})", export.CultureName, export.IsoCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export Language: {IsoCode}", language.IsoCode);
            }
        }

        _logger.LogInformation("Exported {Count} Languages", exported.Count);
        return await Task.FromResult(exported);
    }
}
