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

                    // [UPDATE] — update (no-op) if already present; create if not found
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
                        // ValueListConfiguration.Items is List<string> in Umbraco 17.
                        // Do NOT convert string items to {id,value} dicts — pass them as-is.
                        // NormalizeConfig is only applied for editors whose item format differs.
                        if (!IsValueListConfig(yamlDataType.Config))
                            NormalizeConfig(yamlDataType.Config);

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
        /// Returns true when config contains a plain string list under "items",
        /// i.e. it is already in the format expected by ValueListConfiguration.Items (List&lt;string&gt;).
        /// In that case NormalizeConfig must be skipped — converting strings to {id,value}
        /// dicts would produce the wrong shape and cause validation errors.
        /// </summary>
        private static bool IsValueListConfig(Dictionary<string, object> config)
        {
            if (!config.TryGetValue("items", out var raw)) return false;
            if (raw is not List<object> items) return false;
            return items.Count == 0 || items[0] is string;
        }

        /// <summary>
        /// Normalisation for non-ValueList editors that store an "items" list
        /// (e.g. custom editors). Converts "- Foo\n- Bar" to [{ id, value }] format
        /// so SetConfigurationData receives a serialisable structure.
        /// </summary>
        private static void NormalizeConfig(Dictionary<string, object> config)
        {
            if (!config.TryGetValue("items", out var raw)) return;

            if (raw is List<object> items && items.Count > 0 && items[0] is string)
            {
                config["items"] = items
                    .Select((item, idx) => (object)new Dictionary<string, object>
                    {
                        ["id"]    = idx + 1,
                        ["value"] = item?.ToString() ?? string.Empty
                    })
                    .ToList();
            }
        }
    }
}
