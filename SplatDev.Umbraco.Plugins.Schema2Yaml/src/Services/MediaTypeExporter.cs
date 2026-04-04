using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Models;
using SplatDev.Umbraco.Plugins.Schema2Yaml.Models;

namespace SplatDev.Umbraco.Plugins.Schema2Yaml.Services;

/// <summary>
/// Exports Umbraco MediaTypes to YAML format.
/// </summary>
public class MediaTypeExporter
{
    private readonly IMediaTypeService _mediaTypeService;
    private readonly IDataTypeService _dataTypeService;
    private readonly ILogger<MediaTypeExporter> _logger;

    public MediaTypeExporter(
        IMediaTypeService mediaTypeService,
        IDataTypeService dataTypeService,
        ILogger<MediaTypeExporter> logger)
    {
        _mediaTypeService = mediaTypeService ?? throw new ArgumentNullException(nameof(mediaTypeService));
        _dataTypeService = dataTypeService ?? throw new ArgumentNullException(nameof(dataTypeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Exports all MediaTypes from Umbraco.
    /// </summary>
    public async Task<List<ExportMediaType>> ExportAsync()
    {
        _logger.LogInformation("Starting MediaType export");

        var mediaTypes = _mediaTypeService.GetAll();
        var exported = new List<ExportMediaType>();

        foreach (var mediaType in mediaTypes)
        {
            try
            {
                var export = new ExportMediaType
                {
                    Alias = mediaType.Alias,
                    Name = mediaType.Name,
                    Icon = mediaType.Icon,
                    AllowedAtRoot = mediaType.AllowedAsRoot,
                    Tabs = ExportTabs(mediaType)
                };

                exported.Add(export);
                _logger.LogDebug("Exported MediaType: {Name} ({Alias})", export.Name, export.Alias);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export MediaType: {Name}", mediaType.Name);
            }
        }

        _logger.LogInformation("Exported {Count} MediaTypes", exported.Count);
        return await Task.FromResult(exported);
    }

    /// <summary>
    /// Exports property tabs from a media type.
    /// </summary>
    private List<ExportTab> ExportTabs(IMediaType mediaType)
    {
        var tabs = new List<ExportTab>();
        var propertyGroups = mediaType.PropertyGroups;

        foreach (var group in propertyGroups.OrderBy(g => g.SortOrder))
        {
            var tab = new ExportTab
            {
                Name = group.Name,
                SortOrder = group.SortOrder,
                Properties = ExportProperties(group.PropertyTypes)
            };

            tabs.Add(tab);
        }

        // Handle properties without a tab
        var genericProperties = mediaType.PropertyTypes
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
