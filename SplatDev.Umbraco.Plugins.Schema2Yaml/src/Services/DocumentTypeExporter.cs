using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Models;
using SplatDev.Umbraco.Plugins.Schema2Yaml.Models;

namespace SplatDev.Umbraco.Plugins.Schema2Yaml.Services;

/// <summary>
/// Exports Umbraco DocumentTypes to YAML format.
/// </summary>
public class DocumentTypeExporter
{
    private readonly IContentTypeService _contentTypeService;
    private readonly IDataTypeService _dataTypeService;
    private readonly ILogger<DocumentTypeExporter> _logger;

    public DocumentTypeExporter(
        IContentTypeService contentTypeService,
        IDataTypeService dataTypeService,
        ILogger<DocumentTypeExporter> logger)
    {
        _contentTypeService = contentTypeService ?? throw new ArgumentNullException(nameof(contentTypeService));
        _dataTypeService = dataTypeService ?? throw new ArgumentNullException(nameof(dataTypeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Exports all DocumentTypes from Umbraco.
    /// </summary>
    public async Task<List<ExportDocumentType>> ExportAsync()
    {
        _logger.LogInformation("Starting DocumentType export");

        var contentTypes = _contentTypeService.GetAll();
        var exported = new List<ExportDocumentType>();

        foreach (var contentType in contentTypes)
        {
            try
            {
                var export = new ExportDocumentType
                {
                    Alias = contentType.Alias,
                    Name = contentType.Name ?? string.Empty,
                    Icon = contentType.Icon,
                    IsElement = contentType.IsElement,
                    AllowAsRoot = contentType.AllowedAsRoot,
                    AllowedChildTypes = contentType.AllowedContentTypes?
                        .Select(x => x.Alias)
                        .ToList() ?? [],
                    Compositions = contentType.ContentTypeComposition
                        .Where(c => c.Id != contentType.Id) // Exclude self
                        .Select(c => c.Alias)
                        .ToList(),
                    AllowedTemplates = contentType.AllowedTemplates?
                        .Select(t => t.Alias)
                        .ToList() ?? [],
                    DefaultTemplate = contentType.DefaultTemplate?.Alias,
                    Tabs = ExportTabs(contentType)
                };

                exported.Add(export);
                _logger.LogDebug("Exported DocumentType: {Name} ({Alias})", export.Name, export.Alias);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export DocumentType: {Name}", contentType.Name);
            }
        }

        _logger.LogInformation("Exported {Count} DocumentTypes", exported.Count);
        return await Task.FromResult(exported);
    }

    /// <summary>
    /// Exports property tabs from a content type.
    /// </summary>
    private List<ExportTab> ExportTabs(IContentType contentType)
    {
        var tabs = new List<ExportTab>();
        var propertyGroups = contentType.PropertyGroups;

        foreach (var group in propertyGroups.OrderBy(g => g.SortOrder))
        {
            var tab = new ExportTab
            {
                Name = group.Name ?? string.Empty,
                SortOrder = group.SortOrder,
                Properties = ExportProperties(group.PropertyTypes ?? Enumerable.Empty<IPropertyType>())
            };

            tabs.Add(tab);
        }

        // Handle properties without a tab (generic properties)
        var genericProperties = contentType.PropertyTypes
            .Where(p => string.IsNullOrEmpty(p.PropertyGroupId?.ToString()))
            .ToList();

        if (genericProperties.Any())
        {
            tabs.Add(new ExportTab
            {
                Name = "Generic",
                SortOrder = 999,
                Properties = ExportProperties(genericProperties)
            });
        }

        return tabs;
    }

    /// <summary>
    /// Exports properties from a collection of property types.
    /// </summary>
    private List<ExportProperty> ExportProperties(IEnumerable<IPropertyType> propertyTypes)
    {
        var properties = new List<ExportProperty>();

        foreach (var prop in propertyTypes.OrderBy(p => p.SortOrder))
        {
            try
            {
                var dataType = _dataTypeService.GetDataType(prop.DataTypeId);
                var dataTypeName = dataType?.Name ?? "Unknown";

                var exportProp = new ExportProperty
                {
                    Alias = prop.Alias,
                    Name = prop.Name,
                    DataType = dataTypeName,
                    Required = prop.Mandatory,
                    Description = prop.Description,
                    SortOrder = prop.SortOrder
                };

                properties.Add(exportProp);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to export property: {Alias}", prop.Alias);
            }
        }

        return properties;
    }
}
