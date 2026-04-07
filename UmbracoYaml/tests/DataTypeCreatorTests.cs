using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Microsoft.Extensions.Logging;
using UmbracoYaml.Services;
using UmbracoYaml.Models;

namespace UmbracoYaml.Tests
{
    public class DataTypeCreatorTests
    {
        private readonly Mock<IDataTypeService> _mockDataTypeService;
        private readonly Mock<ILogger<DataTypeCreator>> _mockLogger;
        private readonly DataTypeCreator _dataTypeCreator;

        public DataTypeCreatorTests()
        {
            _mockDataTypeService = new Mock<IDataTypeService>();
            _mockLogger = new Mock<ILogger<DataTypeCreator>>();
            _dataTypeCreator = new DataTypeCreator(_mockDataTypeService.Object, _mockLogger.Object);
        }

        [Fact]
        public void CreateDataTypes_ShouldCreateDataTypesFromYaml()
        {
            // Arrange
            var dataTypes = new List<YamlDataType>
            {
                new YamlDataType
                {
                    Alias = "customTextString",
                    Name = "Custom Text String",
                    Editor = "Umbraco.TextBox",
                    Config = new Dictionary<string, object>()
                },
                new YamlDataType
                {
                    Alias = "customRichText",
                    Name = "Custom Rich Text",
                    Editor = "Umbraco.RichText",
                    Config = new Dictionary<string, object>()
                }
            };

            // Mock: first call returns null (not found), second call returns null (not found)
            _mockDataTypeService
                .Setup(x => x.GetDataTypeByEditorAlias(It.IsAny<string>()))
                .Returns((IDataType)null);

            // Act
            _dataTypeCreator.CreateDataTypes(dataTypes);

            // Assert
            // Verify that Save was called twice (once for each DataType)
            _mockDataTypeService.Verify(
                x => x.Save(It.IsAny<IDataType>()),
                Times.Exactly(2),
                "Save should have been called twice for two new DataTypes"
            );
        }

        [Fact]
        public void CreateDataTypes_ShouldSkipDuplicateAliases()
        {
            // Arrange
            var dataTypes = new List<YamlDataType>
            {
                new YamlDataType
                {
                    Alias = "duplicateText",
                    Name = "Duplicate Text",
                    Editor = "Umbraco.TextBox",
                    Config = new Dictionary<string, object>()
                },
                new YamlDataType
                {
                    Alias = "duplicateText",
                    Name = "Duplicate Text Again",
                    Editor = "Umbraco.TextBox",
                    Config = new Dictionary<string, object>()
                }
            };

            // Mock: returns null (not found in system)
            _mockDataTypeService
                .Setup(x => x.GetDataTypeByEditorAlias(It.IsAny<string>()))
                .Returns((IDataType)null);

            // Act
            _dataTypeCreator.CreateDataTypes(dataTypes);

            // Assert
            // Verify that Save was called only once (first duplicate is created, second is skipped)
            _mockDataTypeService.Verify(
                x => x.Save(It.IsAny<IDataType>()),
                Times.Once,
                "Save should have been called only once - second duplicate should be skipped"
            );
        }
    }
}
