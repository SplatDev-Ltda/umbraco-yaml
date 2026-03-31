using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Plugins.Yaml2Schema.Models;

namespace Umbraco.Plugins.Yaml2Schema.Services
{
    public class DataTypeCreator
    {
        private readonly IDataTypeService _dataTypeService;
        private readonly PropertyEditorCollection _propertyEditors;
        private readonly IConfigurationEditorJsonSerializer _configSerializer;
        private readonly ILogger<DataTypeCreator>? _logger;

        public DataTypeCreator(
            IDataTypeService dataTypeService,
            PropertyEditorCollection propertyEditors,
            IConfigurationEditorJsonSerializer configSerializer,
            ILogger<DataTypeCreator>? logger = null)
        {
            _dataTypeService = dataTypeService ?? throw new ArgumentNullException(nameof(dataTypeService));
            _propertyEditors = propertyEditors ?? throw new ArgumentNullException(nameof(propertyEditors));
            _configSerializer = configSerializer ?? throw new ArgumentNullException(nameof(configSerializer));
            _logger = logger;
        }

        public void CreateDataTypes(List<YamlDataType> dataTypes)
        {
            if (dataTypes == null)
            {
                throw new ArgumentNullException(nameof(dataTypes));
            }

            var processedAliases = new HashSet<string>();

            foreach (var yamlDataType in dataTypes)
            {
                try
                {
                    // Skip if alias has already been processed in this batch
                    if (processedAliases.Contains(yamlDataType.Alias))
                    {
                        _logger?.LogWarning(
                            "DataType with alias '{Alias}' is a duplicate and will be skipped.",
                            yamlDataType.Alias
                        );
                        continue;
                    }

                    // [UPDATE] — upsert by name: create if not found, skip if already present
                    if (yamlDataType.Update)
                    {
                        var existing = _dataTypeService.GetDataType(yamlDataType.Name);
                        if (existing != null)
                        {
                            _logger?.LogInformation(
                                "DataType '{Name}' already exists. No structural update required.",
                                yamlDataType.Name);
                            processedAliases.Add(yamlDataType.Alias);
                            continue;
                        }
                        // Not found — fall through to create, bypassing the broad editor-alias check below
                    }

                    // [REMOVE] — delete the DataType if flagged
                    if (yamlDataType.Remove)
                    {
                        var toDelete = _dataTypeService.GetDataType(yamlDataType.Name);
                        if (toDelete != null)
                        {
                            _dataTypeService.Delete(toDelete, Constants.Security.SuperUserId);
                            _logger?.LogInformation(
                                "DataType '{Name}' with alias '{Alias}' removed.",
                                yamlDataType.Name, yamlDataType.Alias);
                        }
                        else
                        {
                            _logger?.LogWarning(
                                "DataType '{Name}' not found for removal. Skipping.",
                                yamlDataType.Name);
                        }
                        processedAliases.Add(yamlDataType.Alias);
                        continue;
                    }

                    // Check if DataType already exists in the system (by editor alias)
                    // Skipped when update:true — already resolved above by name lookup
                    if (!yamlDataType.Update)
                    {
                        var existingDataTypes = _dataTypeService.GetByEditorAlias(yamlDataType.Editor);
                        if (existingDataTypes != null && existingDataTypes.Any())
                        {
                            _logger?.LogInformation(
                                "DataType with editor alias '{EditorAlias}' already exists. Skipping.",
                                yamlDataType.Editor
                            );
                            processedAliases.Add(yamlDataType.Alias);
                            continue;
                        }
                    }

                    // Look up the property editor by alias
                    if (!_propertyEditors.TryGet(yamlDataType.Editor, out var editor) || editor == null)
                    {
                        _logger?.LogWarning(
                            "Property editor '{EditorAlias}' not found. Skipping DataType '{Alias}'.",
                            yamlDataType.Editor,
                            yamlDataType.Alias
                        );
                        continue;
                    }

                    // Create new DataType
                    var dataType = new DataType(editor, _configSerializer, -1)
                    {
                        Name = yamlDataType.Name,
                        DatabaseType = ValueStorageType.Nvarchar
                    };

                    // Apply config from YAML (supports Block List, Image Cropper, etc.)
                    if (yamlDataType.Config != null && yamlDataType.Config.Count > 0)
                    {
                        dataType.Configuration = yamlDataType.Config;
                    }

                    // Save the DataType
                    _dataTypeService.Save(dataType, Constants.Security.SuperUserId);
                    _logger?.LogInformation(
                        "DataType '{Name}' with alias '{Alias}' created successfully.",
                        yamlDataType.Name,
                        yamlDataType.Alias
                    );

                    processedAliases.Add(yamlDataType.Alias);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(
                        ex,
                        "Error creating DataType '{Name}' with alias '{Alias}'.",
                        yamlDataType.Name,
                        yamlDataType.Alias
                    );
                    throw;
                }
            }
        }
    }
}
