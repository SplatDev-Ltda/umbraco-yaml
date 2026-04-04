using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Cms.Web.Common.Authorization;
using SplatDev.Umbraco.Plugins.Schema2Yaml.Services;
using System.Text;

namespace SplatDev.Umbraco.Plugins.Schema2Yaml.Controllers;

/// <summary>
/// API controller for Schema2Yaml dashboard operations.
/// </summary>
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class SchemaExportController : UmbracoApiController
{
    private readonly ISchemaExportService _exportService;
    private readonly ILogger<SchemaExportController> _logger;

    public SchemaExportController(
        ISchemaExportService exportService,
        ILogger<SchemaExportController> logger)
    {
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Exports Umbraco schema to YAML and returns it with statistics.
    /// GET: /umbraco/api/schemaexport/export
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Export()
    {
        try
        {
            _logger.LogInformation("Dashboard: Export YAML requested");

            var yaml = await _exportService.ExportToYamlAsync();
            var stats = _exportService.GetLastExportStatistics();

            return Ok(new
            {
                yaml,
                statistics = new
                {
                    exportDate = stats.ExportDate,
                    umbracoVersion = stats.UmbracoVersion,
                    languages = stats.LanguageCount,
                    dataTypes = stats.DataTypeCount,
                    documentTypes = stats.DocumentTypeCount,
                    mediaTypes = stats.MediaTypeCount,
                    templates = stats.TemplateCount,
                    media = stats.MediaCount,
                    content = stats.ContentCount,
                    dictionaryItems = stats.DictionaryItemCount,
                    members = stats.MemberCount,
                    users = stats.UserCount,
                    durationSeconds = stats.Duration.TotalSeconds
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export YAML");
            return StatusCode(500, new { error = "Export failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Downloads the exported YAML as a file.
    /// GET: /umbraco/api/schemaexport/downloadyaml
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> DownloadYaml()
    {
        try
        {
            _logger.LogInformation("Dashboard: Download YAML requested");

            var yaml = await _exportService.ExportToYamlAsync();
            var bytes = Encoding.UTF8.GetBytes(yaml);

            return File(bytes, "application/x-yaml", $"umbraco-{DateTime.UtcNow:yyyyMMdd-HHmmss}.yaml");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download YAML");
            return StatusCode(500, new { error = "Download failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Downloads the exported schema with media files as a ZIP archive.
    /// GET: /umbraco/api/schemaexport/downloadzip
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> DownloadZip()
    {
        try
        {
            _logger.LogInformation("Dashboard: Download ZIP requested");

            var zipBytes = await _exportService.ExportToZipAsync();

            return File(zipBytes, "application/zip", $"umbraco-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create ZIP");
            return StatusCode(500, new { error = "ZIP creation failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Gets export statistics without performing the export.
    /// GET: /umbraco/api/schemaexport/statistics
    /// </summary>
    [HttpGet]
    public IActionResult Statistics()
    {
        try
        {
            var stats = _exportService.GetLastExportStatistics();

            if (stats.DataTypeCount == 0)
            {
                return Ok(new { message = "No export performed yet" });
            }

            return Ok(new
            {
                exportDate = stats.ExportDate,
                umbracoVersion = stats.UmbracoVersion,
                languages = stats.LanguageCount,
                dataTypes = stats.DataTypeCount,
                documentTypes = stats.DocumentTypeCount,
                mediaTypes = stats.MediaTypeCount,
                templates = stats.TemplateCount,
                media = stats.MediaCount,
                content = stats.ContentCount,
                dictionaryItems = stats.DictionaryItemCount,
                members = stats.MemberCount,
                users = stats.UserCount,
                durationSeconds = stats.Duration.TotalSeconds
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get statistics");
            return StatusCode(500, new { error = "Failed to get statistics", message = ex.Message });
        }
    }
}
