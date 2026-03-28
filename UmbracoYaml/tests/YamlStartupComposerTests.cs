using Xunit;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using UmbracoYaml.Composers;
using UmbracoYaml.Services;
using UmbracoYaml.Handlers;

namespace UmbracoYaml.Tests
{
    public class YamlStartupComposerTests
    {
        [Fact]
        public void YamlStartupComposer_ShouldRegisterServices()
        {
            // Arrange
            var mockBuilder = new Mock<IUmbracoBuilder>();
            var serviceCollection = new ServiceCollection();
            mockBuilder.Setup(x => x.Services).Returns(serviceCollection);

            var composer = new YamlStartupComposer();

            // Act
            composer.Compose(mockBuilder.Object);

            // Assert
            var provider = serviceCollection.BuildServiceProvider();

            // Verify YamlParser is registered
            var yamlParser = provider.GetService<YamlParser>();
            Assert.NotNull(yamlParser);

            // Verify all Creators are registered
            var dataTypeCreator = provider.GetService<DataTypeCreator>();
            Assert.NotNull(dataTypeCreator);

            var documentTypeCreator = provider.GetService<DocumentTypeCreator>();
            Assert.NotNull(documentTypeCreator);

            var templateCreator = provider.GetService<TemplateCreator>();
            Assert.NotNull(templateCreator);

            var contentCreator = provider.GetService<ContentCreator>();
            Assert.NotNull(contentCreator);

            // Verify notification handler registration was called
            mockBuilder.Verify(
                x => x.AddNotificationHandler(typeof(UmbracoYaml.Handlers.YamlInitializationHandler)),
                Times.Once);
        }
    }
}
