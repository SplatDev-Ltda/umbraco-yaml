# SplatDev.Umbraco.Plugins.Schema2Yaml

[![NuGet](https://img.shields.io/nuget/v/SplatDev.Umbraco.Plugins.Schema2Yaml.svg)](https://www.nuget.org/packages/SplatDev.Umbraco.Plugins.Schema2Yaml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A migration and Infrastructure-as-Code tool for **Umbraco 13-17** that exports your entire Umbraco site structure to YAML format. Export once, version-control it, and import anywhere using the companion [SplatDev.Umbraco.Plugins.Yaml2Schema](https://www.nuget.org/packages/SplatDev.Umbraco.Plugins.Yaml2Schema) plugin.

Perfect for:
- **Migrating** between Umbraco versions (13 → 14 → 15 → 16 → 17)
- **Site cloning** and environment bootstrapping
- **Version control** of your Umbraco structure
- **Backup** and documentation
- **Team collaboration** with declarative site definitions

---

## Features

### Complete Schema Export
- ✅ **DataTypes** — Property editors with full configuration
- ✅ **DocumentTypes** — Content types with all properties, tabs, compositions, and templates
- ✅ **Media Types** — Media types with properties and folder structure
- ✅ **Templates** — Razor templates with content
- ✅ **Languages** — Multi-language configuration
- ✅ **Dictionary Items** — Translations for all languages

### Content & Media Export
- ✅ **Content** — All content nodes with properties, sort order, and published state
- ✅ **Media** — Media items with automatic file download to local folder
- ✅ **Media Files** — Physical files downloaded and organized in Umbraco folder structure

### User Management Export
- ✅ **Members** — Member accounts with properties and groups
- ✅ **Users** — Back-office users with roles and permissions

### Export Features
- 📦 **Single ZIP Download** — All YAML files and media bundled together
- 🎯 **Selective Export** — Choose what to export (coming soon)
- 📊 **Export Dashboard** — Lit-based UI in Settings section
- 🔄 **Version Detection** — Automatic compatibility for Umbraco 13-17
- 📁 **Folder Structure** — Media files maintain Umbraco's folder hierarchy

---

## Installation

```bash
dotnet add package SplatDev.Umbraco.Plugins.Schema2Yaml
```

Or via the NuGet Package Manager:

```
Install-Package SplatDev.Umbraco.Plugins.Schema2Yaml
```

No further registration is required — the plugin self-registers via an Umbraco composer on startup.

---

## Usage

### Via Dashboard (Recommended)

1. Navigate to **Settings** section in the Umbraco back-office
2. Find **Schema Export** dashboard
3. Click **Export to YAML** to generate YAML from your current site
4. Review the generated YAML in the preview panel
5. Click **Download YAML** to save as a single file, or
6. Click **Download ZIP** to get all YAML files + media files bundled together

The ZIP archive includes:
```
umbraco-export.zip
├── umbraco.yaml           # Main schema file
└── media/                 # Media files in Umbraco folder structure
    ├── folder1/
    │   └── image.jpg
    └── folder2/
        └── document.pdf
```

### Via API (Programmatic)

```csharp
using SplatDev.Umbraco.Plugins.Schema2Yaml.Services;

public class MyController : Controller
{
    private readonly ISchemaExportService _exportService;

    public MyController(ISchemaExportService exportService)
    {
        _exportService = exportService;
    }

    public async Task<IActionResult> ExportSchema()
    {
        // Export to YAML string
        var yaml = await _exportService.ExportToYamlAsync();

        // Export to file
        await _exportService.ExportToFileAsync("exports/umbraco.yaml");

        // Export to ZIP with media
        var zipBytes = await _exportService.ExportToZipAsync();
        return File(zipBytes, "application/zip", "umbraco-export.zip");
    }
}
```

### Configuration

Override export settings in `appsettings.json`:

```json
{
  "UmbracoSchema2Yaml": {
    "ExportPath": "exports/umbraco.yaml",
    "IncludeMedia": true,
    "MediaPath": "exports/media",
    "IncludeContent": true,
    "IncludeUsers": false,
    "CompressYaml": false
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| `ExportPath` | `exports/umbraco.yaml` | Default YAML export file path |
| `IncludeMedia` | `true` | Include media items in export |
| `MediaPath` | `exports/media` | Media files download location |
| `IncludeContent` | `true` | Include content nodes in export |
| `IncludeUsers` | `false` | Include back-office users (security consideration) |
| `CompressYaml` | `false` | Minimize YAML output (removes comments and whitespace) |

---

## Generated YAML Structure

The plugin generates a single `umbraco.yaml` file with all your site structure:

```yaml
umbraco:
  # Languages first (for dictionary items)
  languages:
    - isoCode: en-US
      cultureName: English (United States)
      isDefault: true
      isMandatory: true

  # Data types (property editors)
  dataTypes:
    - alias: pageTitle
      name: Page Title
      editorUiAlias: Umbraco.TextBox
      config:
        maxLength: 100

  # Document types
  documentTypes:
    - alias: homePage
      name: Home Page
      icon: icon-home
      allowedAsRoot: true
      allowedTemplates:
        - homePage
      defaultTemplate: homePage
      tabs:
        - name: Content
          properties:
            - alias: pageTitle
              name: Page Title
              dataType: Page Title
              mandatory: true

  # Media types
  mediaTypes:
    - alias: customImage
      name: Custom Image
      icon: icon-picture
      allowedAsRoot: false

  # Templates
  templates:
    - alias: homePage
      name: Home Page
      content: |
        @inherits Umbraco.Cms.Web.Common.Views.UmbracoViewPage
        @{
            Layout = "_Layout.cshtml";
        }
        <h1>@Model.Value("pageTitle")</h1>

  # Media items
  media:
    - name: Logo
      mediaType: Image
      folder: Images
      url: /media/logo.png
      properties:
        umbracoFile: logo.png

  # Content nodes
  content:
    - name: Home
      documentType: homePage
      template: homePage
      sortOrder: 0
      published: true
      properties:
        pageTitle: Welcome to My Site

  # Dictionary items
  dictionaryItems:
    - key: Welcome
      translations:
        - language: en-US
          value: Welcome
        - language: es-ES
          value: Bienvenido

  # Members
  members:
    - name: John Doe
      email: john@example.com
      username: johndoe
      memberType: Member
      isApproved: true

  # Users (if enabled)
  users:
    - name: Admin User
      email: admin@example.com
      username: admin
      userGroups:
        - Administrators
```

---

## Umbraco Version Compatibility

This plugin supports **Umbraco 13, 14, 15, 16, and 17**. The export process automatically detects your Umbraco version and adjusts the YAML structure accordingly.

### Version-Specific Features

| Feature | Umbraco 13 | Umbraco 14-17 |
|---------|------------|---------------|
| Data Types | ✅ Legacy format | ✅ New `editorUiAlias` format |
| Block List/Grid | ✅ v1 format | ✅ v2 format |
| Property Editors | ✅ All | ✅ All + new editors |
| Templates | ✅ Razor | ✅ Razor |
| Content Delivery API | ❌ | ✅ Exported if configured |

### Migration Workflow

1. **Source site** (any version 13-17): Install `SplatDev.Umbraco.Plugins.Schema2Yaml`
2. **Export**: Use dashboard to generate YAML + media ZIP
3. **Target site** (any version 13-17): Create new Umbraco installation
4. **Import**: Install `SplatDev.Umbraco.Plugins.Yaml2Schema`
5. **Deploy**: Place `umbraco.yaml` in `config/` folder and media files in appropriate location
6. **Bootstrap**: Start application — structure is created automatically

---

## Working with Yaml2Schema

This export plugin is designed to work seamlessly with [SplatDev.Umbraco.Plugins.Yaml2Schema](https://www.nuget.org/packages/SplatDev.Umbraco.Plugins.Yaml2Schema), the import companion plugin.

### Round-Trip Workflow

```
Source Site (Umbraco 13-17)
  ↓ Schema2Yaml Export
umbraco.yaml + media files
  ↓ Version Control (Git)
New Site (Umbraco 13-17)
  ↓ Yaml2Schema Import
Replicated Site Structure
```

### What Gets Preserved

✅ All structure (types, templates, languages)  
✅ All content and media  
✅ Property values and configurations  
✅ Parent-child relationships  
✅ Sort order and published state  
✅ Multi-language content  

### What Doesn't Transfer

❌ User passwords (members/users require password reset)  
❌ Media file permissions (reconfigure on target)  
❌ Package-specific data (packages must be reinstalled)  
❌ Database IDs (new GUIDs generated)  
❌ Content versions history (only latest published version)  

---

## Dashboard Preview

The export dashboard provides:

- **Real-time export preview** — See generated YAML before downloading
- **Export statistics** — Count of exported items by type
- **Download options** — YAML only or complete ZIP with media
- **Export history** — Track recent exports (coming soon)
- **Selective export** — Choose specific types to export (coming soon)

---

## Best Practices

### Security

- ⚠️ **Don't commit user passwords** — The YAML export excludes passwords; exported users require password reset
- ⚠️ **Review exported YAML** — Ensure no sensitive configuration is included before committing to version control
- ✅ **Use .gitignore** — Add `exports/` folder to prevent accidental commits of local exports

### Performance

- For **large sites** (1000+ content nodes): Export may take several minutes
- **Media files**: Large media libraries will increase ZIP file size significantly
- **Selective export**: Use configuration to exclude content/media for schema-only exports

### Team Collaboration

- **Commit `umbraco.yaml`** to version control for team sharing
- **Don't commit media files** unless necessary (use CDN or separate media storage)
- **Use Yaml2Schema** on local environments to bootstrap from the shared YAML

---

## Troubleshooting

### Export takes too long
- Disable media export: `"IncludeMedia": false`
- Disable content export: `"IncludeContent": false`
- Export schema only for faster development environment setup

### Missing properties in export
- Ensure all data types are created before document types
- Check Umbraco logs for export warnings
- Verify property editor packages are installed

### Media files not downloading
- Check file permissions in `media/` folder
- Verify URLs are accessible
- Check disk space for ZIP creation

### YAML import fails
- Ensure Umbraco versions are compatible (13-17)
- Verify YAML structure with Yaml2Schema documentation
- Check for breaking changes between Umbraco versions

---

## Roadmap

- [x] Core export functionality
- [x] Dashboard UI
- [x] ZIP download with media
- [x] Multi-version support (13-17)
- [ ] Selective export (choose specific types)
- [ ] Export scheduling and automation
- [ ] Incremental exports (only changes)
- [ ] Export templates/presets
- [ ] CLI tool for CI/CD integration
- [ ] Export diff viewer
- [ ] Cloud storage integration (Azure Blob, S3)

---

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Follow the existing code style (see baseline `Yaml2Schema` plugin)
4. Add unit tests for new functionality
5. Submit a pull request

---

## Support

- **Issues**: [GitHub Issues](https://github.com/SplatDev-Ltda/umbraco-yaml/issues)
- **Documentation**: [Full Documentation](https://github.com/SplatDev-Ltda/umbraco-yaml)
- **Umbraco Forum**: Tag with `schema2yaml`

---

## License

MIT License - see LICENSE file for details

---

## Credits

Developed by **SplatDev Ltda**  
Companion plugin: [SplatDev.Umbraco.Plugins.Yaml2Schema](https://www.nuget.org/packages/SplatDev.Umbraco.Plugins.Yaml2Schema)
