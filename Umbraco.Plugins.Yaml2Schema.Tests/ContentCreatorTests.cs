using Xunit;
using Moq;
using Umbraco.Plugins.Yaml2Schema.Services;
using Umbraco.Plugins.Yaml2Schema.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Models;
using System.Collections.Generic;

namespace Umbraco.Plugins.Yaml2Schema.Tests
{
    public class ContentCreatorTests
    {
        [Fact]
        public void CreateContent_ShouldCreateFromYaml()
        {
            var mockContentService = new Mock<IContentService>();
            var mockContentTypeService = new Mock<IContentTypeService>();

            var contentType = new Mock<IContentType>();
            contentType.Setup(x => x.Id).Returns(1);
            contentType.Setup(x => x.Alias).Returns("page");
            mockContentTypeService.Setup(x => x.Get(It.IsAny<string>())).Returns(contentType.Object);

            var mockContent = new Mock<IContent>();
            mockContent.Setup(x => x.Properties).Returns(new PropertyCollection());
            mockContentService
                .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(mockContent.Object);

            var creator = new ContentCreator(mockContentService.Object, mockContentTypeService.Object);

            var content = new List<YamlContent>
            {
                new YamlContent
                {
                    Alias = "home",
                    Name = "Home",
                    Type = "page",
                    Published = true,
                    Values = new() { { "title", "Welcome" } }
                }
            };

            creator.CreateContent(content);

            mockContentService.Verify(x =>
                x.Save(It.IsAny<IContent>(), It.IsAny<int?>(), It.IsAny<ContentScheduleCollection>()), Times.Once);
        }
    }
}
