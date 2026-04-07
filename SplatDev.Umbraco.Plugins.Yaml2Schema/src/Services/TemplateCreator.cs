using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Services;
using SplatDev.Umbraco.Plugins.Yaml2Schema.Models;

namespace SplatDev.Umbraco.Plugins.Yaml2Schema.Services
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

                    // [UPDATE] — update if exists, create if not found
                    if (yamlTemplate.Update)
                    {
                        var toUpdate = _templateService.GetAsync(yamlTemplate.Alias).GetAwaiter().GetResult();
                        if (toUpdate != null)
                        {
                            toUpdate.Content = !string.IsNullOrWhiteSpace(yamlTemplate.RazorContent)
                                ? yamlTemplate.RazorContent
                                : GenerateDefaultTemplateContent(yamlTemplate.Name, yamlTemplate.Scripts, yamlTemplate.Stylesheets);
                            _templateService.UpdateAsync(toUpdate, Guid.Empty).GetAwaiter().GetResult();
                            _logger?.LogInformation(
                                "Template '{Name}' with alias '{Alias}' updated.",
                                yamlTemplate.Name, yamlTemplate.Alias);
                            processedAliases.Add(yamlTemplate.Alias);
                            continue;
                        }
                        // Not found — fall through to creation below
                        _logger?.LogInformation(
                            "Template '{Alias}' not found during UPDATE; will create it.",
                            yamlTemplate.Alias);
                    }

                    // [REMOVE] — delete the Template if flagged
                    if (yamlTemplate.Remove)
                    {
                        var toDelete = _templateService.GetAsync(yamlTemplate.Alias).GetAwaiter().GetResult();
                        if (toDelete != null)
                        {
                            _templateService.DeleteAsync(toDelete.Key, Guid.Empty).GetAwaiter().GetResult();
                            _logger?.LogInformation(
                                "Template '{Name}' with alias '{Alias}' removed.",
                                yamlTemplate.Name, yamlTemplate.Alias);
                        }
                        else
                        {
                            _logger?.LogDebug(
                                "Template with alias '{Alias}' not found for removal. Skipping.",
                                yamlTemplate.Alias);
                        }
                        processedAliases.Add(yamlTemplate.Alias);
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

                    // Use explicit Razor content if provided, otherwise generate a default scaffold
                    var fileContent = !string.IsNullOrWhiteSpace(yamlTemplate.RazorContent)
                        ? yamlTemplate.RazorContent
                        : GenerateDefaultTemplateContent(yamlTemplate.Name, yamlTemplate.Scripts, yamlTemplate.Stylesheets);

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

        private string GenerateDefaultTemplateContent(string templateName, List<string>? scripts = null, List<string>? stylesheets = null)
        {
            var stylesheetTags = BuildStylesheetTags(stylesheets);
            var scriptTags = BuildScriptTags(scripts);

            return $$"""
@inherits Umbraco.Cms.Web.Common.Views.UmbracoViewPage
@{
    Layout = null;
}

<!DOCTYPE html>
<html>
<head>
    <title>{{templateName}}</title>
{{stylesheetTags}}</head>
<body>
    <h1>{{templateName}}</h1>
    <main>
        @Html.Raw(Model.Value("bodyText"))
    </main>
{{scriptTags}}</body>
</html>
""";
        }

        private static string BuildStylesheetTags(List<string>? stylesheets)
        {
            if (stylesheets == null || stylesheets.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var path in stylesheets.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                var href = path.StartsWith('/') ? path : $"/{path}";
                sb.AppendLine($"    <link rel=\"stylesheet\" href=\"{href}\" />");
            }
            return sb.ToString();
        }

        private static string BuildScriptTags(List<string>? scripts)
        {
            if (scripts == null || scripts.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var path in scripts.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                var src = path.StartsWith('/') ? path : $"/{path}";
                sb.AppendLine($"    <script src=\"{src}\"></script>");
            }
            return sb.ToString();
        }
    }
}
