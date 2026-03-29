using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Services;
using Umbraco.Plugins.Yaml2Schema.Models;

namespace Umbraco.Plugins.Yaml2Schema.Services
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
                    var existingTemplate = _templateService.GetAsync(yamlTemplate.Alias).GetAwaiter().GetResult();
                    if (existingTemplate != null)
                    {
                        _logger?.LogInformation(
                            "Template with alias '{Alias}' already exists. Skipping.",
                            yamlTemplate.Alias
                        );
                        processedAliases.Add(yamlTemplate.Alias);
                        continue;
                    }

                    // Generate default template content
                    var fileContent = GenerateDefaultTemplateContent(yamlTemplate.Name);

                    // Create new Template via service
                    _templateService.CreateAsync(yamlTemplate.Name, yamlTemplate.Alias, fileContent, Guid.Empty, null)
                        .GetAwaiter().GetResult();

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
            return $$"""
@inherits Umbraco.Cms.Web.Common.Views.UmbracoViewPage
@{
    Layout = null;
}

<!DOCTYPE html>
<html>
<head>
    <title>{{templateName}}</title>
</head>
<body>
    <h1>{{templateName}}</h1>
    <main>
        @Html.Raw(Model.Value("bodyText"))
    </main>
</body>
</html>
""";
        }
    }
}
