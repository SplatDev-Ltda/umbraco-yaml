using Xunit;
using Moq;
using Umbraco.Plugins.Yaml2Schema.Services;
using Umbraco.Plugins.Yaml2Schema.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Strings;
using System.Collections.Generic;

namespace Umbraco.Plugins.Yaml2Schema.Tests
{
    public class DocumentTypeCreatorTests
    {
        private Mock<IShortStringHelper> CreateShortStringHelper()
        {
            var mock = new Mock<IShortStringHelper>();
            mock.Setup(x => x.CleanStringForSafeAlias(It.IsAny<string>()))
                .Returns((string s) => s.ToLower().Replace(" ", ""));
            return mock;
        }

        [Fact]
        public void CreateDocumentTypes_ShouldCreateFromYaml()
        {
            var mockDocTypeService = new Mock<IContentTypeService>();
            var mockDataTypeService = new Mock<IDataTypeService>();
            var mockShortStringHelper = CreateShortStringHelper();
            var creator = new DocumentTypeCreator(mockDocTypeService.Object, mockDataTypeService.Object, mockShortStringHelper.Object);

            var documentTypes = new List<YamlDocumentType>
            {
                new YamlDocumentType
                {
                    Alias = "page",
                    Name = "Page",
                    Icon = "icon-document",
                    AllowAsRoot = true,
                    Tabs = new List<YamlTab>
                    {
                        new YamlTab
                        {
                            Name = "Content",
                            Properties = new List<YamlProperty>
                            {
                                new YamlProperty
                                {
                                    Alias = "title",
                                    Name = "Title",
                                    DataType = "textString",
                                    Required = true
                                }
                            }
                        }
                    }
                }
            };

            // Mock: Get returns null for the first call (documentType doesn't exist)
            mockDocTypeService
                .Setup(x => x.Get(It.IsAny<string>()))
                .Returns((IContentType)null);

            // Mock: DataType lookup returns a valid datatype
            var mockDataType = new Mock<IDataType>();
            mockDataTypeService
                .Setup(x => x.GetDataType(It.IsAny<string>()))
                .Returns(mockDataType.Object);

            // Act
            creator.CreateDocumentTypes(documentTypes);

            // Assert
            mockDocTypeService.Verify(x =>
                x.Save(It.IsAny<IContentType>()), Times.Once);
        }

        [Fact]
        public void CreateDocumentTypes_ShouldSkipDuplicateAliases()
        {
            var mockDocTypeService = new Mock<IContentTypeService>();
            var mockDataTypeService = new Mock<IDataTypeService>();
            var mockShortStringHelper = CreateShortStringHelper();
            var creator = new DocumentTypeCreator(mockDocTypeService.Object, mockDataTypeService.Object, mockShortStringHelper.Object);

            var documentTypes = new List<YamlDocumentType>
            {
                new YamlDocumentType
                {
                    Alias = "page",
                    Name = "Page",
                    Icon = "icon-document",
                    AllowAsRoot = true,
                    Tabs = new List<YamlTab>()
                },
                new YamlDocumentType
                {
                    Alias = "page",
                    Name = "Page Duplicate",
                    Icon = "icon-document",
                    AllowAsRoot = true,
                    Tabs = new List<YamlTab>()
                }
            };

            // Mock: Get returns null (documentType doesn't exist)
            mockDocTypeService
                .Setup(x => x.Get(It.IsAny<string>()))
                .Returns((IContentType)null);

            // Act
            creator.CreateDocumentTypes(documentTypes);

            // Assert
            // Verify that Save was called only once (first is created, second is skipped)
            mockDocTypeService.Verify(x =>
                x.Save(It.IsAny<IContentType>()), Times.Once);
        }
    }
}
