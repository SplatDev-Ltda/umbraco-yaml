using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using SplatDev.Umbraco.Plugins.Schema2Yaml.Models;

namespace SplatDev.Umbraco.Plugins.Schema2Yaml.Services;

/// <summary>
/// Exports Umbraco Dictionary Items to YAML format.
/// </summary>
public class DictionaryExporter
{
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<DictionaryExporter> _logger;

    public DictionaryExporter(
        ILocalizationService localizationService,
        ILogger<DictionaryExporter> logger)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Exports all Dictionary Items from Umbraco.
    /// </summary>
    public async Task<List<ExportDictionaryItem>> ExportAsync()
    {
        _logger.LogInformation("Starting Dictionary Item export");

        var rootItems = _localizationService.GetRootDictionaryItems();
        var exported = new List<ExportDictionaryItem>();

        foreach (var item in rootItems)
        {
            ExportDictionaryItem(item, exported);
        }

        _logger.LogInformation("Exported {Count} Dictionary Items", exported.Count);
        return await Task.FromResult(exported);
    }

    /// <summary>
    /// Recursively exports a dictionary item and its children.
    /// </summary>
    private void ExportDictionaryItem(IDictionaryItem item, List<ExportDictionaryItem> exported)
    {
        try
        {
            var translations = new Dictionary<string, string>();

            foreach (var translation in item.Translations)
            {
                if (!string.IsNullOrEmpty(translation.LanguageIsoCode))
                {
                    translations[translation.LanguageIsoCode] = translation.Value ?? string.Empty;
                }
            }

            var export = new ExportDictionaryItem
            {
                Key = item.ItemKey,
                Translations = translations
            };

            exported.Add(export);
            _logger.LogDebug("Exported Dictionary Item: {Key}", export.Key);

            // Export children recursively
            var children = _localizationService.GetDictionaryItemChildren(item.Key);
            foreach (var child in children)
            {
                ExportDictionaryItem(child, exported);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export Dictionary Item: {Key}", item.ItemKey);
        }
    }
}
