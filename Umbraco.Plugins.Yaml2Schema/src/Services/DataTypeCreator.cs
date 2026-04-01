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

                    // [UPDATE] — re-apply config + DatabaseType if exists; create if not found
                    if (yamlDataType.Update)
                    {
                        var existingIface = _dataTypeService.GetDataType(yamlDataType.Name);
                        if (existingIface is DataType existing)
                        {
                            // Re-derive storage type from the editor so stale entries are corrected
                            if (_propertyEditors.TryGet(yamlDataType.Editor, out var updEditor) && updEditor != null)
                            {
                                existing.DatabaseType = updEditor.GetValueEditor().ValueType switch
                                {
                                    "TEXT"    => ValueStorageType.Ntext,
                                    "INT"     => ValueStorageType.Integer,
                                    "INTEGER" => ValueStorageType.Integer,
                                    "BIGINT"  => ValueStorageType.Integer,
                                    "DECIMAL" => ValueStorageType.Decimal,
                                    "DATE"    => ValueStorageType.Date,
                                    _         => ValueStorageType.Nvarchar
                                };
                            }

                            // Re-apply config so stale or incorrectly-formatted config is fixed
                            if (yamlDataType.Config != null && yamlDataType.Config.Count > 0)
                            {
                                ApplyConfig(yamlDataType.Config);
                                existing.SetConfigurationData(yamlDataType.Config);
                            }

                            _dataTypeService.Save(existing, Constants.Security.SuperUserId);
                            _logger?.LogInformation(
                                "DataType '{Name}' updated (config + storage type re-applied).",
                                yamlDataType.Name);
                            processedAliases.Add(yamlDataType.Alias);
                            continue;
                        }
                        if (existingIface != null)
                        {
                            // Unexpected concrete type — skip update, treat as existing
                            _logger?.LogInformation(
                                "DataType '{Name}' exists but is not a concrete DataType; skipping update.",
                                yamlDataType.Name);
                            processedAliases.Add(yamlDataType.Alias);
                            continue;
                        }
                        // Not found — fall through to creation below
                        _logger?.LogInformation(
                            "DataType '{Name}' not found during UPDATE; will create it.",
                            yamlDataType.Name);
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
                            _logger?.LogDebug(
                                "DataType '{Name}' not found for removal. Skipping.",
                                yamlDataType.Name);
                        }
                        processedAliases.Add(yamlDataType.Alias);
                        continue;
                    }

                    // Check if a DataType with the same name already exists
                    var existingByName = _dataTypeService.GetDataType(yamlDataType.Name);
                    if (existingByName != null)
                    {
                        _logger?.LogInformation(
                            "DataType '{Name}' already exists. Skipping.",
                            yamlDataType.Name
                        );
                        processedAliases.Add(yamlDataType.Alias);
                        continue;
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
                    var dbType = editor.GetValueEditor().ValueType switch
                    {
                        "TEXT"    => ValueStorageType.Ntext,
                        "INT"     => ValueStorageType.Integer,
                        "INTEGER" => ValueStorageType.Integer,
                        "BIGINT"  => ValueStorageType.Integer,
                        "DECIMAL" => ValueStorageType.Decimal,
                        "DATE"    => ValueStorageType.Date,
                        _         => ValueStorageType.Nvarchar
                    };

                    var dataType = new DataType(editor, _configSerializer, -1)
                    {
                        Name = yamlDataType.Name,
                        DatabaseType = dbType
                    };

                    // Apply config from YAML (supports Block List, Image Cropper, etc.)
                    if (yamlDataType.Config != null && yamlDataType.Config.Count > 0)
                    {
                        ApplyConfig(yamlDataType.Config);
                        dataType.SetConfigurationData(yamlDataType.Config);
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

        /// <summary>
        /// Normalises the config dict in-place before passing to SetConfigurationData.
        ///
        /// ValueListConfiguration.Items (Umbraco.DropDown.Flexible, CheckBoxList, etc.) is
        /// List&lt;string&gt; in Umbraco 17. YamlDotNet deserialises YAML string sequences as
        /// List&lt;object&gt;. Umbraco's ValueListConfigurationEditor validator performs a strict
        /// <c>is List&lt;string&gt;</c> check — List&lt;object&gt; is rejected even when every element
        /// is a string. We must convert in-place so the correct concrete type is stored.
        ///
        /// For other editors that use an "items" list in {id, value} format the items are
        /// already dicts (not plain strings), so the conversion is skipped and their structure
        /// is left unchanged.
        /// </summary>
        private static void ApplyConfig(Dictionary<string, object> config)
        {
            if (!config.TryGetValue("items", out var raw)) return;
            if (raw is not List<object> items) return;
            if (items.Count == 0 || items[0] is not string) return;

            // Convert List<object> of plain strings → List<string> so Umbraco's
            // ValueListConfigurationEditor validator accepts it.
            config["items"] = items
                .Select(item => item?.ToString() ?? string.Empty)
                .ToList();
        }
    }
}
