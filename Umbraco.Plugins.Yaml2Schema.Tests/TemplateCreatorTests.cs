using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Microsoft.Extensions.Logging;
using Umbraco.Plugins.Yaml2Schema.Services;
using Umbraco.Plugins.Yaml2Schema.Models;

namespace Umbraco.Plugins.Yaml2Schema.Tests
{
    public class TemplateCreatorTests
    {
        private readonly Mock<ITemplateService> _mockTemplateService;
        private readonly Mock<ILogger<TemplateCreator>> _mockLogger;
        private readonly TemplateCreator _templateCreator;

        public TemplateCreatorTests()
        {
            _mockTemplateService = new Mock<ITemplateService>();
            _mockLogger = new Mock<ILogger<TemplateCreator>>();
            _templateCreator = new TemplateCreator(_mockTemplateService.Object, _mockLogger.Object);
        }

        [Fact]
        public void CreateTemplates_ShouldCreateFromYaml()
        {
            // Arrange
            var templates = new List<YamlTemplate>
            {
                new YamlTemplate
                {
                    Alias = "masterPage",
                    Name = "Master Page",
                    Path = "Master",
                    MasterTemplate = null
                },
                new YamlTemplate
                {
                    Alias = "contentPage",
                    Name = "Content Page",
                    Path = "Content",
                    MasterTemplate = "masterPage"
                }
            };

            // Mock: GetAsync returns null (templates don't exist)
            _mockTemplateService
                .Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync((ITemplate?)null);

            // Act
            _templateCreator.CreateTemplates(templates);

            // Assert
            // Verify that CreateAsync was called twice (once for each template)
            _mockTemplateService.Verify(
                x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>()),
                Times.Exactly(2),
                "CreateAsync should have been called twice for two new templates"
            );
        }

        [Fact]
        public void CreateTemplates_ShouldSkipDuplicateAliases()
        {
            // Arrange
            var templates = new List<YamlTemplate>
            {
                new YamlTemplate
                {
                    Alias = "duplicatePage",
                    Name = "Duplicate Page",
                    Path = "Duplicate",
                    MasterTemplate = null
                },
                new YamlTemplate
                {
                    Alias = "duplicatePage",
                    Name = "Duplicate Page Again",
                    Path = "Duplicate2",
                    MasterTemplate = null
                }
            };

            // Mock: GetAsync returns null (templates don't exist)
            _mockTemplateService
                .Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync((ITemplate?)null);

            // Act
            _templateCreator.CreateTemplates(templates);

            // Assert
            // Verify that CreateAsync was called only once (first duplicate is created, second is skipped)
            _mockTemplateService.Verify(
                x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid?>()),
                Times.Once,
                "CreateAsync should have been called only once - second duplicate should be skipped"
            );
        }
    }
}
