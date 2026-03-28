using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using UmbracoYaml.Models;

namespace UmbracoYaml.Services
{
    public class DataTypeCreator
    {
        private readonly IDataTypeService _dataTypeService;
        private readonly ILogger<DataTypeCreator>? _logger;

        public DataTypeCreator(IDataTypeService dataTypeService, ILogger<DataTypeCreator>? logger = null)
        {
            _dataTypeService = dataTypeService ?? throw new ArgumentNullException(nameof(dataTypeService));
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

                    // Check if DataType already exists in the system
                    var existingDataType = _dataTypeService.GetDataTypeByEditorAlias(yamlDataType.Editor);
                    if (existingDataType != null)
                    {
                        _logger?.LogInformation(
                            "DataType with editor alias '{EditorAlias}' already exists. Skipping.",
                            yamlDataType.Editor
                        );
                        processedAliases.Add(yamlDataType.Alias);
                        continue;
                    }

                    // Create new DataType
                    var dataType = new DataType(
                        new DataTypeDefinition(yamlDataType.Editor)
                        {
                            Name = yamlDataType.Name,
                            DatabaseType = ValueStorageType.Nvarchar
                        }
                    );

                    // Save the DataType
                    _dataTypeService.Save(dataType);
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
