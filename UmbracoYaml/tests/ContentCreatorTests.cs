using Xunit;
using Moq;
using UmbracoYaml.Services;
using UmbracoYaml.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Models;
using System.Collections.Generic;

namespace UmbracoYaml.Tests
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
            mockContentTypeService.Setup(x => x.Get(It.IsAny<string>())).Returns(contentType.Object);

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
                x.Save(It.IsAny<IContent>()), Times.Once);
        }
    }
}
