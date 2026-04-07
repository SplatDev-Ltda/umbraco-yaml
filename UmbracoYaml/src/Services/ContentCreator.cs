using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Models;
using Microsoft.Extensions.Logging;
using UmbracoYaml.Models;

namespace UmbracoYaml.Services
{
    public class ContentCreator
    {
        private readonly IContentService _contentService;
        private readonly IContentTypeService _contentTypeService;
        private readonly ILogger<ContentCreator> _logger;

        public ContentCreator(
            IContentService contentService,
            IContentTypeService contentTypeService,
            ILogger<ContentCreator> logger = null)
        {
            _contentService = contentService ?? throw new ArgumentNullException(nameof(contentService));
            _contentTypeService = contentTypeService ?? throw new ArgumentNullException(nameof(contentTypeService));
            _logger = logger;
        }

        public void CreateContent(List<YamlContent> contentList, int? parentId = null)
        {
            foreach (var yamlContent in contentList)
            {
                try
                {
                    var contentType = _contentTypeService.Get(yamlContent.Type);
                    if (contentType == null)
                    {
                        _logger?.LogError($"DocumentType not found: {yamlContent.Type}");
                        continue;
                    }

                    var existing = _contentService.GetById(yamlContent.Alias);
                    if (existing != null)
                    {
                        _logger?.LogInformation($"Content already exists: {yamlContent.Alias}");
                        continue;
                    }

                    var content = _contentService.Create(yamlContent.Name, parentId ?? -1, contentType.Alias);

                    // Set property values
                    foreach (var kvp in yamlContent.Values)
                    {
                        if (content.Properties.Any(p => p.Alias == kvp.Key))
                        {
                            content.SetValue(kvp.Key, kvp.Value);
                        }
                    }

                    content.SortOrder = yamlContent.SortOrder;

                    _contentService.Save(content);

                    if (yamlContent.Published)
                    {
                        _contentService.Publish(content);
                    }

                    _logger?.LogInformation($"Created Content: {yamlContent.Alias}");

                    // Recursively create children
                    if (yamlContent.Children.Any())
                    {
                        CreateContent(yamlContent.Children, content.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Error creating content {yamlContent.Alias}: {ex.Message}");
                    throw;
                }
            }
        }
    }
}
