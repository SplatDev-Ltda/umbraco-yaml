using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Models;
using Microsoft.Extensions.Logging;
using Umbraco.Plugins.Yaml2Schema.Models;

namespace Umbraco.Plugins.Yaml2Schema.Services
{
    public class ContentCreator
    {
        private readonly IContentService _contentService;
        private readonly IContentTypeService _contentTypeService;
        private readonly ILogger<ContentCreator>? _logger;

        public ContentCreator(
            IContentService contentService,
            IContentTypeService contentTypeService,
            ILogger<ContentCreator>? logger = null)
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
                    // [UPDATE] — update matching content node if it exists, create if not
                    if (yamlContent.Update)
                    {
                        var candidates = parentId.HasValue
                            ? _contentService.GetPagedChildren(parentId.Value, 0, int.MaxValue, out _).ToList()
                            : _contentService.GetRootContent().ToList();

                        var toUpdate = candidates.FirstOrDefault(c => c.Name == yamlContent.Name);
                        if (toUpdate != null)
                        {
                            foreach (var kvp in yamlContent.Values)
                            {
                                if (toUpdate.Properties.Any(p => p.Alias == kvp.Key))
                                    toUpdate.SetValue(kvp.Key, kvp.Value);
                            }

                            toUpdate.SortOrder = yamlContent.SortOrder;
                            _contentService.Save(toUpdate, null, null);

                            if (yamlContent.Published)
                                _contentService.Publish(toUpdate, Array.Empty<string>(), Constants.Security.SuperUserId);

                            _logger?.LogInformation("Content '{Name}' updated.", yamlContent.Name);

                            if (yamlContent.Children.Any())
                                CreateContent(yamlContent.Children, toUpdate.Id);

                            continue;
                        }
                        // Not found — fall through to create
                    }

                    // [REMOVE] — delete matching content node if flagged
                    if (yamlContent.Remove)
                    {
                        var candidates = parentId.HasValue
                            ? _contentService.GetPagedChildren(parentId.Value, 0, int.MaxValue, out _).ToList()
                            : _contentService.GetRootContent().ToList();

                        var toDelete = candidates.FirstOrDefault(c => c.Name == yamlContent.Name);
                        if (toDelete != null)
                        {
                            _contentService.Delete(toDelete, Constants.Security.SuperUserId);
                            _logger?.LogInformation("Content '{Name}' removed.", yamlContent.Name);
                        }
                        else
                        {
                            _logger?.LogWarning("Content '{Name}' not found for removal. Skipping.", yamlContent.Name);
                        }
                        // Deletion cascades in Umbraco — no need to recurse into children
                        continue;
                    }

                    var contentType = _contentTypeService.Get(yamlContent.Type);
                    if (contentType == null)
                    {
                        _logger?.LogError("DocumentType not found: {Type}", yamlContent.Type);
                        continue;
                    }

                    var content = _contentService.Create(yamlContent.Name, parentId ?? -1, contentType.Alias, Constants.Security.SuperUserId);

                    // Set property values
                    foreach (var kvp in yamlContent.Values)
                    {
                        if (content.Properties.Any(p => p.Alias == kvp.Key))
                        {
                            content.SetValue(kvp.Key, kvp.Value);
                        }
                    }

                    content.SortOrder = yamlContent.SortOrder;

                    _contentService.Save(content, null, null);

                    if (yamlContent.Published)
                    {
                        _contentService.Publish(content, Array.Empty<string>(), Constants.Security.SuperUserId);
                    }

                    _logger?.LogInformation("Created Content: {Alias}", yamlContent.Alias);

                    // Recursively create children
                    if (yamlContent.Children.Any())
                    {
                        CreateContent(yamlContent.Children, content.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error creating content {Alias}", yamlContent.Alias);
                    throw;
                }
            }
        }
    }
}
