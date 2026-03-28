using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Models;
using Microsoft.Extensions.Logging;
using UmbracoYaml.Models;

namespace UmbracoYaml.Services
{
    public class DocumentTypeCreator
    {
        private readonly IContentTypeService _contentTypeService;
        private readonly IDataTypeService _dataTypeService;
        private readonly ILogger<DocumentTypeCreator>? _logger;

        public DocumentTypeCreator(
            IContentTypeService contentTypeService,
            IDataTypeService dataTypeService,
            ILogger<DocumentTypeCreator>? logger = null)
        {
            _contentTypeService = contentTypeService ?? throw new ArgumentNullException(nameof(contentTypeService));
            _dataTypeService = dataTypeService ?? throw new ArgumentNullException(nameof(dataTypeService));
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
                    var contentType = new ContentType(null)
                    {
                        Name = yamlDocType.Name,
                        Alias = yamlDocType.Alias,
                        Icon = yamlDocType.Icon ?? "icon-document",
                        AllowAsRoot = yamlDocType.AllowAsRoot
                    };

                    // Add tabs and properties
                    foreach (var tab in yamlDocType.Tabs)
                    {
                        var contentTab = new ContentPropertyGroup { Name = tab.Name };

                        foreach (var property in tab.Properties)
                        {
                            var dataType = _dataTypeService.Get(property.DataType);
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

                            var contentProp = new ContentPropertyType(dataType)
                            {
                                Alias = property.Alias,
                                Name = property.Name,
                                Mandatory = property.Required,
                                Description = property.Description
                            };

                            contentTab.PropertyTypes.Add(contentProp);
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
                            .Select(ct => new ContentTypeSort(ct.Id, 0))
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
