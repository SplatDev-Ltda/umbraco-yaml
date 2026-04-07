using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Models;
using SplatDev.Umbraco.Plugins.Schema2Yaml.Models;
using SplatDev.Umbraco.Plugins.Schema2Yaml.Configuration;

namespace SplatDev.Umbraco.Plugins.Schema2Yaml.Services;

/// <summary>
/// Exports Umbraco Content nodes to YAML format.
/// </summary>
public class ContentExporter
{
    private readonly IContentService _contentService;
    private readonly IFileService _fileService;
    private readonly Schema2YamlOptions _options;
    private readonly ILogger<ContentExporter> _logger;

    public ContentExporter(
        IContentService contentService,
        IFileService fileService,
        IOptions<Schema2YamlOptions> options,
        ILogger<ContentExporter> logger)
    {
        _contentService = contentService ?? throw new ArgumentNullException(nameof(contentService));
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Exports all Content nodes from Umbraco.
    /// </summary>
    public async Task<List<ExportContent>> ExportAsync()
    {
        if (!_options.IncludeContent)
        {
            _logger.LogInformation("Content export is disabled in configuration");
            return [];
        }

        _logger.LogInformation("Starting Content export");

        var roots = _contentService.GetRootContent();
        var exported = new List<ExportContent>();

        foreach (var root in roots)
        {
            ExportNode(root, exported, 0);
        }

        _logger.LogInformation("Exported {Count} Content nodes", exported.Count);
        return await Task.FromResult(exported);
    }

    /// <summary>
    /// Recursively exports a content node and its children.
    /// </summary>
    private void ExportNode(IContent content, List<ExportContent> exported, int depth)
    {
        if (depth >= _options.MaxHierarchyDepth)
        {
            _logger.LogWarning("Max hierarchy depth reached at node: {Name}", content.Name);
            return;
        }

        try
        {
            var export = new ExportContent
            {
                Name = content.Name ?? string.Empty,
                DocumentType = content.ContentType.Alias,
                Template = GetTemplateName(content),
                SortOrder = content.SortOrder,
                IsPublished = content.Published,
                Properties = ExportProperties(content),
                Children = []
            };

            exported.Add(export);
            _logger.LogDebug("Exported Content: {Name} ({DocumentType})", export.Name, export.DocumentType);

            // Export children recursively
            var children = _contentService.GetPagedChildren(content.Id, 0, int.MaxValue, out _);
            foreach (var child in children.OrderBy(c => c.SortOrder))
            {
                ExportNode(child, export.Children, depth + 1);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export Content node: {Name}", content.Name);
        }
    }

    /// <summary>
    /// Gets the template name for a content node.
    /// </summary>
    private string? GetTemplateName(IContent content)
    {
        if (!content.TemplateId.HasValue)
        {
            return null;
        }

        try
        {
            var template = _fileService.GetTemplate(content.TemplateId.Value);
            return template?.Alias;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Exports all property values from a content node.
    /// </summary>
    private Dictionary<string, object> ExportProperties(IContent content)
    {
        var properties = new Dictionary<string, object>();

        foreach (var property in content.Properties)
        {
            try
            {
                var value = property.GetValue();
                if (value != null)
                {
                    properties[property.Alias] = ConvertPropertyValue(value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to export property: {Alias} on {ContentName}", 
                    property.Alias, content.Name);
            }
        }

        return properties;
    }

    /// <summary>
    /// Converts property values to YAML-serializable format.
    /// </summary>
    private object ConvertPropertyValue(object value)
    {
        // Handle null
        if (value == null)
        {
            return string.Empty;
        }

        // Handle common types
        return value switch
        {
            string str => str,
            int or long or decimal or double or float => value,
            bool b => b,
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
            _ => value.ToString() ?? string.Empty
        };
    }
}
