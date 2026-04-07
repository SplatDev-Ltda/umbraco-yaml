using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using UmbracoYaml.Models;

namespace UmbracoYaml.Services
{
    public class TemplateCreator
    {
        private readonly ITemplateService _templateService;
        private readonly ILogger<TemplateCreator>? _logger;

        public TemplateCreator(ITemplateService templateService, ILogger<TemplateCreator>? logger = null)
        {
            _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
            _logger = logger;
        }

        public void CreateTemplates(List<YamlTemplate> templates)
        {
            if (templates == null)
            {
                throw new ArgumentNullException(nameof(templates));
            }

            var processedAliases = new HashSet<string>();

            foreach (var yamlTemplate in templates)
            {
                try
                {
                    // Skip if alias has already been processed in this batch
                    if (processedAliases.Contains(yamlTemplate.Alias))
                    {
                        _logger?.LogWarning(
                            "Template with alias '{Alias}' is a duplicate and will be skipped.",
                            yamlTemplate.Alias
                        );
                        continue;
                    }

                    // Check if Template already exists in the system
                    var existingTemplate = _templateService.GetByAlias(yamlTemplate.Alias);
                    if (existingTemplate != null)
                    {
                        _logger?.LogInformation(
                            "Template with alias '{Alias}' already exists. Skipping.",
                            yamlTemplate.Alias
                        );
                        processedAliases.Add(yamlTemplate.Alias);
                        continue;
                    }

                    // Resolve master template if specified
                    ITemplate? masterTemplate = null;
                    if (!string.IsNullOrEmpty(yamlTemplate.MasterTemplate))
                    {
                        masterTemplate = _templateService.GetByAlias(yamlTemplate.MasterTemplate);
                        if (masterTemplate == null)
                        {
                            _logger?.LogWarning(
                                "Master template with alias '{MasterTemplateAlias}' not found. Template '{TemplateAlias}' will be created without a master.",
                                yamlTemplate.MasterTemplate,
                                yamlTemplate.Alias
                            );
                        }
                    }

                    // Create new Template
                    var template = new Template(masterTemplate)
                    {
                        Name = yamlTemplate.Name,
                        Alias = yamlTemplate.Alias,
                        Path = yamlTemplate.Path ?? yamlTemplate.Alias
                    };

                    // Create template file in Views directory if not exists
                    var viewsDirectory = Path.Combine(
                        AppContext.BaseDirectory,
                        "Views",
                        string.IsNullOrEmpty(yamlTemplate.Path) ? "" : yamlTemplate.Path
                    );

                    if (!Directory.Exists(viewsDirectory))
                    {
                        Directory.CreateDirectory(viewsDirectory);
                    }

                    var templateFilePath = Path.Combine(viewsDirectory, $"{yamlTemplate.Alias}.cshtml");
                    if (!File.Exists(templateFilePath))
                    {
                        var defaultContent = GenerateDefaultTemplateContent(yamlTemplate.Name);
                        File.WriteAllText(templateFilePath, defaultContent);
                    }

                    // Save template via service
                    _templateService.Save(template);
                    _logger?.LogInformation(
                        "Template '{Name}' with alias '{Alias}' created successfully.",
                        yamlTemplate.Name,
                        yamlTemplate.Alias
                    );

                    processedAliases.Add(yamlTemplate.Alias);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(
                        ex,
                        "Error creating Template '{Name}' with alias '{Alias}'.",
                        yamlTemplate.Name,
                        yamlTemplate.Alias
                    );
                    throw;
                }
            }
        }

        private string GenerateDefaultTemplateContent(string templateName)
        {
            return $"""
@inherits Umbraco.Cms.Web.Common.Views.UmbracoViewPage
@{{
    Layout = null;
}}

<!DOCTYPE html>
<html>
<head>
    <title>{templateName}</title>
</head>
<body>
    <h1>{templateName}</h1>
    <main>
        @Html.Raw(Model.Value("bodyText"))
    </main>
</body>
</html>
""";
        }
    }
}
