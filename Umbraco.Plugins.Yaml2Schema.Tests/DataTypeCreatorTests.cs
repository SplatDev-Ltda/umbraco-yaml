using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Microsoft.Extensions.Logging;
using Umbraco.Plugins.Yaml2Schema.Services;
using Umbraco.Plugins.Yaml2Schema.Models;
using Umbraco.Cms.Core.Configuration.Models;

namespace Umbraco.Plugins.Yaml2Schema.Tests
{
    public class DataTypeCreatorTests
    {
        private readonly Mock<IDataTypeService> _mockDataTypeService;
        private readonly Mock<PropertyEditorCollection> _mockPropertyEditors;
        private readonly Mock<IConfigurationEditorJsonSerializer> _mockConfigSerializer;
        private readonly Mock<ILogger<DataTypeCreator>> _mockLogger;
        private readonly DataTypeCreator _dataTypeCreator;
        private readonly Mock<IDataEditor> _mockEditor;

        public DataTypeCreatorTests()
        {
            _mockDataTypeService = new Mock<IDataTypeService>();
            var dataEditorCollection = new DataEditorCollection(() => Enumerable.Empty<IDataEditor>());
            _mockPropertyEditors = new Mock<PropertyEditorCollection>(dataEditorCollection);
            _mockConfigSerializer = new Mock<IConfigurationEditorJsonSerializer>();
            _mockLogger = new Mock<ILogger<DataTypeCreator>>();

            _mockEditor = new Mock<IDataEditor>();
            _mockEditor.Setup(x => x.Alias).Returns("Umbraco.TextBox");
            var mockConfigEditor = new Mock<IConfigurationEditor>();
            mockConfigEditor.Setup(x => x.DefaultConfiguration).Returns(new Dictionary<string, object>());
            _mockEditor.Setup(x => x.GetConfigurationEditor()).Returns(mockConfigEditor.Object);

            // Setup TryGet to return the mock editor
            IDataEditor outEditor = _mockEditor.Object;
            _mockPropertyEditors
                .Setup(x => x.TryGet(It.IsAny<string>(), out outEditor))
                .Returns(true);

            _dataTypeCreator = new DataTypeCreator(
                _mockDataTypeService.Object,
                _mockPropertyEditors.Object,
                _mockConfigSerializer.Object,
                _mockLogger.Object);
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

            // Mock: GetByEditorAlias returns empty (not found)
            _mockDataTypeService
                .Setup(x => x.GetByEditorAlias(It.IsAny<string>()))
                .Returns(Enumerable.Empty<IDataType>());

            // Act
            _dataTypeCreator.CreateDataTypes(dataTypes);

            // Assert
            // Verify that Save was called twice (once for each DataType)
            _mockDataTypeService.Verify(
                x => x.Save(It.IsAny<IDataType>(), It.IsAny<int>()),
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

            // Mock: returns empty (not found in system)
            _mockDataTypeService
                .Setup(x => x.GetByEditorAlias(It.IsAny<string>()))
                .Returns(Enumerable.Empty<IDataType>());

            // Act
            _dataTypeCreator.CreateDataTypes(dataTypes);

            // Assert
            // Verify that Save was called only once (first duplicate is created, second is skipped)
            _mockDataTypeService.Verify(
                x => x.Save(It.IsAny<IDataType>(), It.IsAny<int>()),
                Times.Once,
                "Save should have been called only once - second duplicate should be skipped"
            );
        }
    }
}
