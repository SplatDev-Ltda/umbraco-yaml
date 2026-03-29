using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Strings;
using Microsoft.Extensions.Logging;
using Umbraco.Plugins.Yaml2Schema.Models;

namespace Umbraco.Plugins.Yaml2Schema.Services
{
    public class DocumentTypeCreator
    {
        private readonly IContentTypeService _contentTypeService;
        private readonly IDataTypeService _dataTypeService;
        private readonly IShortStringHelper _shortStringHelper;
        private readonly ILogger<DocumentTypeCreator>? _logger;

        public DocumentTypeCreator(
            IContentTypeService contentTypeService,
            IDataTypeService dataTypeService,
            IShortStringHelper shortStringHelper,
            ILogger<DocumentTypeCreator>? logger = null)
        {
            _contentTypeService = contentTypeService ?? throw new ArgumentNullException(nameof(contentTypeService));
            _dataTypeService = dataTypeService ?? throw new ArgumentNullException(nameof(dataTypeService));
            _shortStringHelper = shortStringHelper ?? throw new ArgumentNullException(nameof(shortStringHelper));
            _logger = logger;
        }

        public void CreateDocumentTypes(List<YamlDocumentType> documentTypes)
        {
            if (documentTypes == null)
            {
                throw new ArgumentNullException(nameof(documentTypes));
            }

            var processedAliases = new HashSet<string>();

            foreach (var yamlDocType in documentTypes)
            {
                try
                {
                    // Skip if alias has already been processed in this batch
                    if (processedAliases.Contains(yamlDocType.Alias))
                    {
                        _logger?.LogWarning(
                            "DocumentType with alias '{Alias}' is a duplicate and will be skipped.",
                            yamlDocType.Alias
                        );
                        continue;
                    }

                    // Check if DocumentType already exists in the system
                    var existing = _contentTypeService.Get(yamlDocType.Alias);
                    if (existing != null)
                    {
                        _logger?.LogInformation(
                            "DocumentType with alias '{Alias}' already exists. Skipping.",
                            yamlDocType.Alias
                        );
                        processedAliases.Add(yamlDocType.Alias);
                        continue;
                    }

                    // Create new ContentType
                    var contentType = new ContentType(_shortStringHelper, -1)
                    {
                        Name = yamlDocType.Name,
                        Alias = yamlDocType.Alias,
                        Icon = yamlDocType.Icon ?? "icon-document",
                        AllowedAsRoot = yamlDocType.AllowAsRoot
                    };

                    // Add tabs and properties
                    foreach (var tab in yamlDocType.Tabs)
                    {
                        var tabAlias = _shortStringHelper.CleanStringForSafeAlias(tab.Name);
                        var contentTab = new PropertyGroup(false) { Name = tab.Name, Alias = tabAlias };

                        foreach (var property in tab.Properties)
                        {
                            var dataType = _dataTypeService.GetDataType(property.DataType);
                            if (dataType == null)
                            {
                                _logger?.LogWarning(
                                    "DataType '{DataType}' not found. Skipping property '{PropertyAlias}' in DocumentType '{DocTypeAlias}'.",
                                    property.DataType,
                                    property.Alias,
                                    yamlDocType.Alias
                                );
                                continue;
                            }

                            var contentProp = new PropertyType(_shortStringHelper, dataType)
                            {
                                Alias = property.Alias,
                                Name = property.Name,
                                Mandatory = property.Required,
                                Description = property.Description
                            };

                            contentTab.PropertyTypes!.Add(contentProp);
                        }

                        contentType.PropertyGroups.Add(contentTab);
                    }

                    // Set allowed child types
                    if (yamlDocType.AllowedChildTypes.Any())
                    {
                        var childTypes = yamlDocType.AllowedChildTypes
                            .Select(alias => _contentTypeService.Get(alias))
                            .Where(ct => ct != null)
                            .ToList();

                        contentType.AllowedContentTypes = childTypes
                            .Select(ct => new ContentTypeSort(ct!.Key, 0, ct.Alias))
                            .ToList();
                    }

                    // Save the ContentType
                    _contentTypeService.Save(contentType);
                    processedAliases.Add(yamlDocType.Alias);

                    _logger?.LogInformation(
                        "DocumentType '{Name}' with alias '{Alias}' created successfully.",
                        yamlDocType.Name,
                        yamlDocType.Alias
                    );
                }
                catch (Exception ex)
                {
                    _logger?.LogError(
                        ex,
                        "Error creating DocumentType '{Name}' with alias '{Alias}'.",
                        yamlDocType.Name,
                        yamlDocType.Alias
                    );
                    throw;
                }
            }
        }
    }
}
