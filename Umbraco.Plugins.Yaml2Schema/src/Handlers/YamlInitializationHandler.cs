using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Plugins.Yaml2Schema.Services;

namespace Umbraco.Plugins.Yaml2Schema.Handlers
{
    public class YamlInitializationHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
    {
        private readonly YamlParser _yamlParser;
        private readonly DataTypeCreator _dataTypeCreator;
        private readonly DocumentTypeCreator _documentTypeCreator;
        private readonly MediaTypeCreator _mediaTypeCreator;
        private readonly TemplateCreator _templateCreator;
        private readonly ContentCreator _contentCreator;
        private readonly MediaCreator _mediaCreator;
        private readonly StaticAssetCreator _staticAssetCreator;
        private readonly LanguageCreator _languageCreator;
        private readonly DictionaryCreator _dictionaryCreator;
        private readonly MemberCreator _memberCreator;
        private readonly UserCreator _userCreator;
        private readonly IRuntimeState _runtimeState;
        private readonly ILogger<YamlInitializationHandler> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _hostEnvironment;

        public YamlInitializationHandler(
            YamlParser yamlParser,
            DataTypeCreator dataTypeCreator,
            DocumentTypeCreator documentTypeCreator,
            MediaTypeCreator mediaTypeCreator,
            TemplateCreator templateCreator,
            ContentCreator contentCreator,
            MediaCreator mediaCreator,
            StaticAssetCreator staticAssetCreator,
            LanguageCreator languageCreator,
            DictionaryCreator dictionaryCreator,
            MemberCreator memberCreator,
            UserCreator userCreator,
            IRuntimeState runtimeState,
            ILogger<YamlInitializationHandler> logger,
            IConfiguration configuration,
            IHostEnvironment hostEnvironment)
        {
            _yamlParser = yamlParser ?? throw new ArgumentNullException(nameof(yamlParser));
            _dataTypeCreator = dataTypeCreator ?? throw new ArgumentNullException(nameof(dataTypeCreator));
            _documentTypeCreator = documentTypeCreator ?? throw new ArgumentNullException(nameof(documentTypeCreator));
            _mediaTypeCreator = mediaTypeCreator ?? throw new ArgumentNullException(nameof(mediaTypeCreator));
            _templateCreator = templateCreator ?? throw new ArgumentNullException(nameof(templateCreator));
            _contentCreator = contentCreator ?? throw new ArgumentNullException(nameof(contentCreator));
            _mediaCreator = mediaCreator ?? throw new ArgumentNullException(nameof(mediaCreator));
            _staticAssetCreator = staticAssetCreator ?? throw new ArgumentNullException(nameof(staticAssetCreator));
            _languageCreator = languageCreator ?? throw new ArgumentNullException(nameof(languageCreator));
            _dictionaryCreator = dictionaryCreator ?? throw new ArgumentNullException(nameof(dictionaryCreator));
            _memberCreator = memberCreator ?? throw new ArgumentNullException(nameof(memberCreator));
            _userCreator = userCreator ?? throw new ArgumentNullException(nameof(userCreator));
            _runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
        }

        public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
        {
            // Only run when Umbraco is fully installed and running — skip during installer/upgrade
            if (_runtimeState.Level != Umbraco.Cms.Core.RuntimeLevel.Run)
            {
                _logger.LogInformation(
                    "YamlInitializationHandler: Skipping YAML initialization — runtime level is {Level} (requires Run).",
                    _runtimeState.Level);
                return;
            }

            _logger.LogInformation("YamlInitializationHandler: Umbraco application started, initializing YAML configuration.");

            try
            {
                // Get config path from IConfiguration or use default; resolve relative paths against the content root
                var configPath = _configuration["UmbracoYaml:ConfigPath"] ?? "config/umbraco.yaml";
                if (!Path.IsPathRooted(configPath))
                {
                    configPath = Path.Combine(_hostEnvironment.ContentRootPath, configPath);
                }

                _logger.LogInformation("YamlInitializationHandler: Attempting to parse YAML configuration from '{ConfigPath}'.", configPath);

                // Parse YAML
                var yamlRoot = _yamlParser.ParseYaml(configPath);

                if (yamlRoot?.Umbraco == null)
                {
                    _logger.LogWarning("YamlInitializationHandler: YAML configuration is empty or invalid. No items to create.");
                    return;
                }

                // Create Languages first (other creators may reference culture codes)
                if (yamlRoot.Umbraco.Languages?.Count > 0)
                {
                    _logger.LogInformation("YamlInitializationHandler: Creating {Count} Languages.", yamlRoot.Umbraco.Languages.Count);
                    _languageCreator.CreateLanguages(yamlRoot.Umbraco.Languages);
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
                    _documentTypeCreator.CreateDocumentTypes(yamlRoot.Umbraco.DocumentTypes, yamlRoot.Umbraco.DataTypes);
                    _logger.LogInformation("YamlInitializationHandler: Successfully created {Count} DocumentTypes.", yamlRoot.Umbraco.DocumentTypes.Count);
                }
                else
                {
                    _logger.LogInformation("YamlInitializationHandler: No DocumentTypes to create.");
                }

                // Create MediaTypes
                if (yamlRoot.Umbraco.MediaTypes?.Count > 0)
                {
                    _logger.LogInformation("YamlInitializationHandler: Creating {Count} MediaTypes.", yamlRoot.Umbraco.MediaTypes.Count);
                    _mediaTypeCreator.CreateMediaTypes(yamlRoot.Umbraco.MediaTypes);
                }

                // Create static JavaScript files
                if (yamlRoot.Umbraco.Scripts?.Count > 0)
                {
                    _logger.LogInformation("YamlInitializationHandler: Creating {Count} Scripts.", yamlRoot.Umbraco.Scripts.Count);
                    _staticAssetCreator.CreateScripts(yamlRoot.Umbraco.Scripts);
                    _logger.LogInformation("YamlInitializationHandler: Successfully created {Count} Scripts.", yamlRoot.Umbraco.Scripts.Count);
                }
                else
                {
                    _logger.LogInformation("YamlInitializationHandler: No Scripts to create.");
                }

                // Create static CSS stylesheets
                if (yamlRoot.Umbraco.Stylesheets?.Count > 0)
                {
                    _logger.LogInformation("YamlInitializationHandler: Creating {Count} Stylesheets.", yamlRoot.Umbraco.Stylesheets.Count);
                    _staticAssetCreator.CreateStylesheets(yamlRoot.Umbraco.Stylesheets);
                    _logger.LogInformation("YamlInitializationHandler: Successfully created {Count} Stylesheets.", yamlRoot.Umbraco.Stylesheets.Count);
                }
                else
                {
                    _logger.LogInformation("YamlInitializationHandler: No Stylesheets to create.");
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

                // Link templates to document types (runs after both are created)
                if (yamlRoot.Umbraco.DocumentTypes?.Count > 0)
                {
                    _logger.LogInformation("YamlInitializationHandler: Linking templates to DocumentTypes.");
                    _documentTypeCreator.LinkTemplatesToDocumentTypes(yamlRoot.Umbraco.DocumentTypes);
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

                // Create Media
                if (yamlRoot.Umbraco.Media?.Count > 0)
                {
                    _logger.LogInformation("YamlInitializationHandler: Creating {Count} Media items.", yamlRoot.Umbraco.Media.Count);
                    _mediaCreator.CreateMedia(yamlRoot.Umbraco.Media);
                }

                // Create Dictionary Items
                if (yamlRoot.Umbraco.DictionaryItems?.Count > 0)
                {
                    _logger.LogInformation("YamlInitializationHandler: Creating {Count} Dictionary items.", yamlRoot.Umbraco.DictionaryItems.Count);
                    _dictionaryCreator.CreateDictionaryItems(yamlRoot.Umbraco.DictionaryItems);
                }

                // Create Members
                if (yamlRoot.Umbraco.Members?.Count > 0)
                {
                    _logger.LogInformation("YamlInitializationHandler: Creating {Count} Members.", yamlRoot.Umbraco.Members.Count);
                    _memberCreator.CreateMembers(yamlRoot.Umbraco.Members);
                }

                // Create Users
                if (yamlRoot.Umbraco.Users?.Count > 0)
                {
                    _logger.LogInformation("YamlInitializationHandler: Creating {Count} Users.", yamlRoot.Umbraco.Users.Count);
                    _userCreator.CreateUsers(yamlRoot.Umbraco.Users);
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
