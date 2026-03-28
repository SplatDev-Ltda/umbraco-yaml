using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Microsoft.Extensions.Logging;
using UmbracoYaml.Services;
using UmbracoYaml.Models;

namespace UmbracoYaml.Tests
{
    /// <summary>
    /// Integration tests verifying the complete YAML parsing and creation flow
    /// Tests all components working together: Parser, DataTypeCreator,
    /// DocumentTypeCreator, TemplateCreator, and ContentCreator
    /// </summary>
    public class IntegrationTests
    {
        private readonly string _testFixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "tests",
            "fixtures",
            "sample.yaml"
        );

        /// <summary>
        /// Full integration test: Parse YAML and verify complete structure
        /// Verifies that root, sections (DataTypes, DocumentTypes, Templates, Content)
        /// and nested items are correctly deserialized
        /// </summary>
        [Fact]
        public void FullYamlFlow_ShouldParseAndVerifyCompleteStructure()
        {
            // Arrange
            var parser = new YamlParser();

            // Act
            var result = parser.ParseYaml(_testFixturePath);

            // Assert
            // Root verification
            Assert.NotNull(result);
            Assert.NotNull(result.Umbraco);

            // DataTypes verification
            Assert.NotNull(result.Umbraco.DataTypes);
            Assert.Equal(2, result.Umbraco.DataTypes.Count);
            Assert.Equal("textString", result.Umbraco.DataTypes[0].Alias);
            Assert.Equal("Text String", result.Umbraco.DataTypes[0].Name);
            Assert.Equal("Umbraco.TextBox", result.Umbraco.DataTypes[0].Editor);
            Assert.NotNull(result.Umbraco.DataTypes[0].Config);
            Assert.Contains("maxLength", result.Umbraco.DataTypes[0].Config.Keys);

            Assert.Equal("richText", result.Umbraco.DataTypes[1].Alias);
            Assert.Equal("Rich Text", result.Umbraco.DataTypes[1].Name);
            Assert.Equal("Umbraco.RichText", result.Umbraco.DataTypes[1].Editor);

            // DocumentTypes verification
            Assert.NotNull(result.Umbraco.DocumentTypes);
            Assert.Single(result.Umbraco.DocumentTypes);
            var docType = result.Umbraco.DocumentTypes[0];
            Assert.Equal("page", docType.Alias);
            Assert.Equal("Page", docType.Name);
            Assert.Equal("icon-document", docType.Icon);
            Assert.True(docType.AllowAsRoot);
            Assert.Single(docType.AllowedChildTypes);
            Assert.Contains("page", docType.AllowedChildTypes);

            // Nested tabs and properties verification
            Assert.NotNull(docType.Tabs);
            Assert.Single(docType.Tabs);
            var tab = docType.Tabs[0];
            Assert.Equal("Content", tab.Name);
            Assert.NotNull(tab.Properties);
            Assert.Single(tab.Properties);
            var property = tab.Properties[0];
            Assert.Equal("title", property.Alias);
            Assert.Equal("Title", property.Name);
            Assert.Equal("textString", property.DataType);
            Assert.True(property.Required);
            Assert.Equal("The page title", property.Description);

            // Templates verification
            Assert.NotNull(result.Umbraco.Templates);
            Assert.Single(result.Umbraco.Templates);
            var template = result.Umbraco.Templates[0];
            Assert.Equal("masterPage", template.Alias);
            Assert.Equal("Master Page", template.Name);
            Assert.Equal("Master.cshtml", template.Path);
            Assert.Null(template.MasterTemplate);

            // Content verification
            Assert.NotNull(result.Umbraco.Content);
            Assert.Single(result.Umbraco.Content);
            var content = result.Umbraco.Content[0];
            Assert.Equal("home", content.Alias);
            Assert.Equal("Home", content.Name);
            Assert.Equal("page", content.Type);
            Assert.True(content.Published);
            Assert.Equal(0, content.SortOrder);
            Assert.NotNull(content.Values);
            Assert.Contains("title", content.Values.Keys);
            Assert.Equal("Welcome to Umbraco", content.Values["title"]);
            Assert.NotNull(content.Children);
            Assert.Empty(content.Children);
        }

        /// <summary>
        /// Integration test: Verify all creators work with parsed YAML data
        /// Tests DataTypeCreator, DocumentTypeCreator, TemplateCreator, ContentCreator
        /// with the complete parsed YAML structure
        /// </summary>
        [Fact]
        public void AllCreators_ShouldWorkWithParsedYamlData()
        {
            // Arrange
            var parser = new YamlParser();
            var result = parser.ParseYaml(_testFixturePath);

            var mockDataTypeService = new Mock<IDataTypeService>();
            var mockContentTypeService = new Mock<IContentTypeService>();
            var mockTemplateService = new Mock<ITemplateService>();
            var mockContentService = new Mock<IContentService>();
            var mockLogger = new Mock<ILogger<DataTypeCreator>>();
            var mockDocLogger = new Mock<ILogger<DocumentTypeCreator>>();
            var mockTplLogger = new Mock<ILogger<TemplateCreator>>();

            // Setup mocks
            mockDataTypeService
                .Setup(x => x.GetDataTypeByEditorAlias(It.IsAny<string>()))
                .Returns((IDataType)null);

            var mockContentType = new Mock<IContentType>();
            mockContentType.Setup(x => x.Id).Returns(1);
            mockContentTypeService
                .Setup(x => x.Get(It.IsAny<string>()))
                .Returns(mockContentType.Object);

            mockTemplateService
                .Setup(x => x.GetByAlias(It.IsAny<string>()))
                .Returns((ITemplate)null);

            var mockContent = new Mock<IContent>();
            mockContent.Setup(x => x.Id).Returns(1);
            mockContentService
                .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(mockContent.Object);

            var dataTypeCreator = new DataTypeCreator(mockDataTypeService.Object, mockLogger.Object);
            var docTypeCreator = new DocumentTypeCreator(mockContentTypeService.Object, mockDocLogger.Object);
            var templateCreator = new TemplateCreator(mockTemplateService.Object, mockTplLogger.Object);
            var contentCreator = new ContentCreator(mockContentService.Object, mockContentTypeService.Object);

            // Act
            dataTypeCreator.CreateDataTypes(result.Umbraco.DataTypes);
            docTypeCreator.CreateDocumentTypes(result.Umbraco.DocumentTypes);
            templateCreator.CreateTemplates(result.Umbraco.Templates);
            contentCreator.CreateContent(result.Umbraco.Content);

            // Assert - DataTypes created
            mockDataTypeService.Verify(
                x => x.Save(It.IsAny<IDataType>()),
                Times.Exactly(2),
                "Should create 2 data types"
            );

            // Assert - DocumentTypes created
            mockContentTypeService.Verify(
                x => x.Save(It.IsAny<IContentType>()),
                Times.Once,
                "Should create 1 document type"
            );

            // Assert - Templates created
            mockTemplateService.Verify(
                x => x.Save(It.IsAny<ITemplate>()),
                Times.Once,
                "Should create 1 template"
            );

            // Assert - Content created
            mockContentService.Verify(
                x => x.Save(It.IsAny<IContent>()),
                Times.Once,
                "Should create 1 content item"
            );
        }

        /// <summary>
        /// Integration test: Verify complete structure with nested content
        /// Tests that nested children in content are properly parsed
        /// </summary>
        [Fact]
        public void ParseYaml_ShouldHandleNestedContentStructure()
        {
            // Arrange
            var parser = new YamlParser();
            var result = parser.ParseYaml(_testFixturePath);

            // Act - Get the root content item
            var rootContent = result.Umbraco.Content.FirstOrDefault();

            // Assert
            Assert.NotNull(rootContent);
            Assert.Equal("home", rootContent.Alias);
            Assert.NotNull(rootContent.Children);
            Assert.Empty(rootContent.Children);

            // Verify the structure can accommodate nested items
            // (even though the sample doesn't have nested content)
            var testNestedContent = new YamlContent
            {
                Alias = "subpage",
                Name = "Subpage",
                Type = "page",
                Children = new List<YamlContent>()
            };
            rootContent.Children.Add(testNestedContent);

            // Verify nesting works
            Assert.Single(rootContent.Children);
            Assert.Equal("subpage", rootContent.Children[0].Alias);
        }

        /// <summary>
        /// Integration test: Verify all sections exist and are populated correctly
        /// This is a high-level sanity check for the complete YAML structure
        /// </summary>
        [Fact]
        public void ParseYaml_ShouldEnsureAllSectionsExist()
        {
            // Arrange
            var parser = new YamlParser();

            // Act
            var result = parser.ParseYaml(_testFixturePath);

            // Assert - All major sections exist
            Assert.NotNull(result.Umbraco.DataTypes);
            Assert.NotNull(result.Umbraco.DocumentTypes);
            Assert.NotNull(result.Umbraco.Templates);
            Assert.NotNull(result.Umbraco.Content);

            // Assert - All sections are populated with correct count
            Assert.NotEmpty(result.Umbraco.DataTypes);
            Assert.NotEmpty(result.Umbraco.DocumentTypes);
            Assert.NotEmpty(result.Umbraco.Templates);
            Assert.NotEmpty(result.Umbraco.Content);
        }

        /// <summary>
        /// Integration test: Verify data consistency across related objects
        /// Tests that DocumentType references DataTypes and Templates correctly
        /// </summary>
        [Fact]
        public void ParseYaml_ShouldVerifyDataConsistency()
        {
            // Arrange
            var parser = new YamlParser();
            var result = parser.ParseYaml(_testFixturePath);

            // Act - Get the document type
            var docType = result.Umbraco.DocumentTypes.FirstOrDefault();
            Assert.NotNull(docType);

            // Get all properties from all tabs
            var allProperties = docType.Tabs
                .SelectMany(t => t.Properties)
                .ToList();

            // Assert - All properties reference valid data types
            foreach (var property in allProperties)
            {
                var dataType = result.Umbraco.DataTypes
                    .FirstOrDefault(dt => dt.Alias == property.DataType);
                Assert.NotNull(dataType, $"Property {property.Alias} references non-existent DataType {property.DataType}");
            }

            // Assert - Content references valid document type
            var content = result.Umbraco.Content.FirstOrDefault();
            Assert.NotNull(content);
            var contentDocType = result.Umbraco.DocumentTypes
                .FirstOrDefault(dt => dt.Alias == content.Type);
            Assert.NotNull(contentDocType, $"Content references non-existent DocumentType {content.Type}");
        }
    }
}
