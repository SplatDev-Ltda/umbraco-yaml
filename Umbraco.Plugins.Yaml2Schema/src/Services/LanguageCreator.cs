using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Plugins.Yaml2Schema.Models;

namespace Umbraco.Plugins.Yaml2Schema.Services
{
    public class LanguageCreator
    {
        private readonly ILanguageService _languageService;
        private readonly ILogger<LanguageCreator>? _logger;

        public LanguageCreator(ILanguageService languageService, ILogger<LanguageCreator>? logger = null)
        {
            _languageService = languageService ?? throw new ArgumentNullException(nameof(languageService));
            _logger = logger;
        }

        public void CreateLanguages(List<YamlLanguage> languages)
        {
            if (languages == null) throw new ArgumentNullException(nameof(languages));

            var processedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var yamlLang in languages)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(yamlLang.IsoCode))
                    {
                        _logger?.LogWarning("Language entry is missing isoCode. Skipping.");
                        continue;
                    }

                    if (processedCodes.Contains(yamlLang.IsoCode))
                    {
                        _logger?.LogWarning("Language '{IsoCode}' is a duplicate and will be skipped.", yamlLang.IsoCode);
                        continue;
                    }

                    // [REMOVE]
                    if (yamlLang.Remove)
                    {
                        var toDelete = _languageService.GetAsync(yamlLang.IsoCode).GetAwaiter().GetResult();
                        if (toDelete != null)
                        {
                            _languageService.DeleteAsync(yamlLang.IsoCode, Constants.Security.SuperUserKey)
                                .GetAwaiter().GetResult();
                            _logger?.LogInformation("Language '{IsoCode}' removed.", yamlLang.IsoCode);
                        }
                        else
                        {
                            _logger?.LogWarning("Language '{IsoCode}' not found for removal. Skipping.", yamlLang.IsoCode);
                        }
                        processedCodes.Add(yamlLang.IsoCode);
                        continue;
                    }

                    var existing = _languageService.GetAsync(yamlLang.IsoCode).GetAwaiter().GetResult();

                    // [UPDATE]
                    if (yamlLang.Update && existing != null)
                    {
                        existing.IsDefault = yamlLang.IsDefault;
                        existing.IsMandatory = yamlLang.IsMandatory;
                        _languageService.UpdateAsync(existing, Constants.Security.SuperUserKey).GetAwaiter().GetResult();
                        _logger?.LogInformation("Language '{IsoCode}' updated.", yamlLang.IsoCode);
                        processedCodes.Add(yamlLang.IsoCode);
                        continue;
                    }

                    if (existing != null)
                    {
                        _logger?.LogInformation("Language '{IsoCode}' already exists. Skipping.", yamlLang.IsoCode);
                        processedCodes.Add(yamlLang.IsoCode);
                        continue;
                    }

                    // Create
                    var cultureName = yamlLang.CultureName
                        ?? CultureInfo.GetCultureInfo(yamlLang.IsoCode).DisplayName;

                    var language = new Language(yamlLang.IsoCode, cultureName)
                    {
                        IsDefault = yamlLang.IsDefault,
                        IsMandatory = yamlLang.IsMandatory
                    };

                    _languageService.CreateAsync(language, Constants.Security.SuperUserKey).GetAwaiter().GetResult();
                    _logger?.LogInformation("Language '{IsoCode}' created.", yamlLang.IsoCode);
                    processedCodes.Add(yamlLang.IsoCode);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error processing language '{IsoCode}'.", yamlLang.IsoCode);
                    throw;
                }
            }
        }
    }
}
