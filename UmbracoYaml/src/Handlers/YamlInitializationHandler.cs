using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using UmbracoYaml.Services;

namespace UmbracoYaml.Handlers
{
    public class YamlInitializationHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
    {
        private readonly YamlParser _yamlParser;
        private readonly DataTypeCreator _dataTypeCreator;
        private readonly DocumentTypeCreator _documentTypeCreator;
        private readonly TemplateCreator _templateCreator;
        private readonly ContentCreator _contentCreator;
        private readonly ILogger<YamlInitializationHandler> _logger;
        private readonly IConfiguration _configuration;

        public YamlInitializationHandler(
            YamlParser yamlParser,
            DataTypeCreator dataTypeCreator,
            DocumentTypeCreator documentTypeCreator,
            TemplateCreator templateCreator,
            ContentCreator contentCreator,
            ILogger<YamlInitializationHandler> logger,
            IConfiguration configuration)
        {
            _yamlParser = yamlParser ?? throw new ArgumentNullException(nameof(yamlParser));
            _dataTypeCreator = dataTypeCreator ?? throw new ArgumentNullException(nameof(dataTypeCreator));
            _documentTypeCreator = documentTypeCreator ?? throw new ArgumentNullException(nameof(documentTypeCreator));
            _templateCreator = templateCreator ?? throw new ArgumentNullException(nameof(templateCreator));
            _contentCreator = contentCreator ?? throw new ArgumentNullException(nameof(contentCreator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task Handle(UmbracoApplicationStartedNotification notification)
        {
            _logger.LogInformation("YamlInitializationHandler: Umbraco application started, initializing YAML configuration.");

            try
            {
                // Get config path from IConfiguration or use default
                var configPath = _configuration["UmbracoYaml:ConfigPath"] ?? "config/umbraco.yaml";

                _logger.LogInformation("YamlInitializationHandler: Attempting to parse YAML configuration from '{ConfigPath}'.", configPath);

                // Parse YAML
                var yamlRoot = _yamlParser.ParseYaml(configPath);

                if (yamlRoot?.Umbraco == null)
                {
                    _logger.LogWarning("YamlInitializationHandler: YAML configuration is empty or invalid. No items to create.");
                    return;
                }

                // Create DataTypes
                if (yamlRoot.Umbraco.DataTypes?.Count > 0)
                {
                    _logger.LogInformation("YamlInitializationHandler: Creating {Count} DataTypes.", yamlRoot.Umbraco.DataTypes.Count);
                    _dataTypeCreator.CreateDataTypes(yamlRoot.Umbraco.DataTypes);
                    _logger.LogInformation("YamlInitializationHandler: Successfully created {Count} DataTypes.", yamlRoot.Umbraco.DataTypes.Count);
                }
                else
                {
                    _logger.LogInformation("YamlInitializationHandler: No DataTypes to create.");
                }

                // Create DocumentTypes
                if (yamlRoot.Umbraco.DocumentTypes?.Count > 0)
                {
                    _logger.LogInformation("YamlInitializationHandler: Creating {Count} DocumentTypes.", yamlRoot.Umbraco.DocumentTypes.Count);
                    _documentTypeCreator.CreateDocumentTypes(yamlRoot.Umbraco.DocumentTypes);
                    _logger.LogInformation("YamlInitializationHandler: Successfully created {Count} DocumentTypes.", yamlRoot.Umbraco.DocumentTypes.Count);
                }
                else
                {
                    _logger.LogInformation("YamlInitializationHandler: No DocumentTypes to create.");
                }

                // Create Templates
                if (yamlRoot.Umbraco.Templates?.Count > 0)
                {
                    _logger.LogInformation("YamlInitializationHandler: Creating {Count} Templates.", yamlRoot.Umbraco.Templates.Count);
                    _templateCreator.CreateTemplates(yamlRoot.Umbraco.Templates);
                    _logger.LogInformation("YamlInitializationHandler: Successfully created {Count} Templates.", yamlRoot.Umbraco.Templates.Count);
                }
                else
                {
                    _logger.LogInformation("YamlInitializationHandler: No Templates to create.");
                }

                // Create Content
                if (yamlRoot.Umbraco.Content?.Count > 0)
                {
                    _logger.LogInformation("YamlInitializationHandler: Creating {Count} Content items.", yamlRoot.Umbraco.Content.Count);
                    _contentCreator.CreateContent(yamlRoot.Umbraco.Content);
                    _logger.LogInformation("YamlInitializationHandler: Successfully created {Count} Content items.", yamlRoot.Umbraco.Content.Count);
                }
                else
                {
                    _logger.LogInformation("YamlInitializationHandler: No Content items to create.");
                }

                _logger.LogInformation("YamlInitializationHandler: YAML initialization completed successfully.");
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning(ex, "YamlInitializationHandler: YAML configuration file not found. Skipping initialization.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "YamlInitializationHandler: An error occurred during YAML initialization.");
                throw;
            }

            await Task.CompletedTask;
        }
    }
}
