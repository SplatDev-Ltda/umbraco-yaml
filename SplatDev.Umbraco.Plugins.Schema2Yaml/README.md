# SplatDev.Umbraco.Plugins.Schema2Yaml

[![NuGet](https://img.shields.io/nuget/v/SplatDev.Umbraco.Plugins.Schema2Yaml.svg)](https://www.nuget.org/packages/SplatDev.Umbraco.Plugins.Schema2Yaml)
[![NuGet Downloads](https://img.shields.io/nuget/dt/SplatDev.Umbraco.Plugins.Schema2Yaml.svg)](https://www.nuget.org/packages/SplatDev.Umbraco.Plugins.Schema2Yaml)
[![CI](https://github.com/SplatDev-Ltda/umbraco-yaml/actions/workflows/ci.yml/badge.svg)](https://github.com/SplatDev-Ltda/umbraco-yaml/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A migration and Infrastructure-as-Code tool for **Umbraco 13–17** that exports your entire Umbraco site structure to YAML format. Export once, version-control it, and import anywhere using the companion [SplatDev.Umbraco.Plugins.Yaml2Schema](https://www.nuget.org/packages/SplatDev.Umbraco.Plugins.Yaml2Schema) plugin.

Perfect for:
- **Migrating** between Umbraco versions (13 → 14 → 15 → 16 → 17)
- **Site cloning** and environment bootstrapping
- **Version control** of your Umbraco structure
- **Backup** and documentation
- **Team collaboration** with declarative site definitions

---

## Umbraco Version Compatibility

| Umbraco | .NET | Dashboard | NuGet TFM |
|---------|------|-----------|-----------|
| 13.x | net8.0 | AngularJS (backoffice v13) | `net8.0` |
| 14.x | net8.0 | Lit web component | `net8.0` |
| 15.x | net9.0 | Lit web component | `net9.0` |
| 16.x | net9.0 | Lit web component | `net9.0` |
| 17.x | net10.0 | Lit web component | `net10.0` |

The NuGet package ships **all three TFMs** in a single package — NuGet automatically selects the right one at install time.

---

## Features

### Complete Schema Export
- ✅ **DataTypes** — Property editors with full configuration
- ✅ **DocumentTypes** — Content types with all properties, tabs, compositions, and templates
- ✅ **Media Types** — Media types with properties and folder structure
- ✅ **Templates** — Razor templates with full content
- ✅ **Languages** — Multi-language configuration
- ✅ **Dictionary Items** — Translations for all languages with parent-child hierarchy

### Content & Media Export
- ✅ **Content** — All content nodes with properties, sort order, and published state
- ✅ **Media** — Media items with automatic file download to local folder
- ✅ **Media Files** — Physical files downloaded and organized in Umbraco folder structure

### User Management Export
- ✅ **Members** — Member accounts with properties and groups (passwords excluded)
- ✅ **Users** — Back-office users with roles (opt-in via config, passwords excluded)

### Export Features
- 📦 **Single ZIP Download** — All YAML + media bundled in one archive
- 📊 **Export Statistics** — Live counts per entity type with duration
- 🖥️ **Dashboard UI** — Settings section dashboard for Umbraco 13 (AngularJS) and 14–17 (Lit)
- 🔄 **Version Detection** — Automatic API compatibility for Umbraco 13–17
- 📁 **Folder Structure** — Media files maintain Umbraco's original folder hierarchy

---

## Installation

```bash
dotnet add package SplatDev.Umbraco.Plugins.Schema2Yaml
```

Or via NuGet Package Manager:

```
Install-Package SplatDev.Umbraco.Plugins.Schema2Yaml
```

No further registration is required — the plugin self-registers via `Schema2YamlComposer` on startup and the dashboard appears automatically in the **Settings** section.

---

## Usage

### Via Dashboard (Recommended)

1. Navigate to the **Settings** section in the Umbraco back-office
2. Click the **Schema Export** dashboard tab
3. Click **Export to YAML** to generate YAML from your current site
4. Review the export statistics and YAML preview
5. Click **Download YAML** to save as a single file, or
6. Click **Download ZIP** to get YAML + all media files bundled together

The ZIP archive structure:
```
umbraco-export.zip
├── umbraco.yaml           # Complete schema + content file
└── media/                 # Media files in Umbraco folder structure
    ├── images/
    │   └── logo.png
    └── documents/
        └── brochure.pdf
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

        // Export to file on disk
        await _exportService.ExportToFileAsync("exports/umbraco.yaml");

        // Export to ZIP with media files
        var zipBytes = await _exportService.ExportToZipAsync();
        return File(zipBytes, "application/zip", "umbraco-export.zip");
    }
}
```

### REST API Endpoints

The plugin exposes authenticated endpoints under `/umbraco/api/SchemaExport/`:

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/Export` | Export and return `{ yaml, statistics }` as JSON |
| GET | `/DownloadYaml` | Stream YAML as a file download |
| GET | `/DownloadZip` | Stream ZIP (YAML + media) as a file download |
| GET | `/Statistics` | Return statistics from the last export |

All endpoints require the **Settings** section access policy.

### Configuration

Override defaults in `appsettings.json`:

```json
{
  "UmbracoSchema2Yaml": {
    "ExportPath": "exports/umbraco.yaml",
    "IncludeMedia": true,
    "MediaPath": "exports/media",
    "IncludeContent": true,
    "IncludeMembers": true,
    "IncludeUsers": false,
    "CompressYaml": false,
    "MaxHierarchyDepth": 50
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| `ExportPath` | `exports/umbraco.yaml` | Default YAML export file path |
| `IncludeMedia` | `true` | Include media items and file downloads |
| `MediaPath` | `exports/media` | Media files download location |
| `IncludeContent` | `true` | Include content nodes |
| `IncludeMembers` | `true` | Include member accounts |
| `IncludeUsers` | `false` | Include back-office users (security consideration) |
| `CompressYaml` | `false` | Minimise YAML output |
| `MaxHierarchyDepth` | `50` | Max depth for content/media tree traversal |

---

## Generated YAML Structure

```yaml
umbraco:
  languages:
    - isoCode: en-US
      cultureName: English (United States)
      isDefault: true
      isMandatory: true

  dataTypes:
    - alias: pageTitle
      name: Page Title
      editorUiAlias: Umbraco.TextBox
      config:
        maxLength: 100

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

  templates:
    - alias: homePage
      name: Home Page
      content: |
        @inherits Umbraco.Cms.Web.Common.Views.UmbracoViewPage
        @{ Layout = "_Layout.cshtml"; }
        <h1>@Model.Value("pageTitle")</h1>

  content:
    - name: Home
      documentType: homePage
      template: homePage
      published: true
      properties:
        pageTitle: Welcome to My Site

  dictionaryItems:
    - key: Welcome
      translations:
        en-US: Welcome
        es-ES: Bienvenido

  members:
    - name: John Doe
      email: john@example.com
      username: johndoe
      memberType: Member
      isApproved: true
```

---

## Migration Workflow

```
Source Site (Umbraco 13–17)
       │
       ▼  Schema2Yaml (this plugin)
  umbraco.yaml + media files
       │
       ▼  Git / file transfer
Target Site (Umbraco 13–17)
       │
       ▼  Yaml2Schema companion plugin
  Replicated Site Structure
```

### What transfers
✅ All structure (types, templates, languages)  
✅ All content and media  
✅ Property values and data type configurations  
✅ Parent-child relationships and sort order  
✅ Multi-language content  
✅ Dictionary translations  

### What doesn't transfer
❌ User/member passwords (require reset on import)  
❌ Package-specific data (packages must be reinstalled)  
❌ Database IDs (new GUIDs generated on import)  
❌ Content version history (only latest published version)  

---

## Best Practices

### Security
- ⚠️ **Passwords are never exported** — members and users require a password reset after import
- ⚠️ **Review before committing** — check for sensitive property values before adding YAML to version control
- ✅ **Add `exports/` to `.gitignore`** — prevent accidental commits of local export files
- ✅ **`IncludeUsers` is off by default** — opt in explicitly if you need back-office user export

### Performance
- For **large sites** (1 000+ content nodes): export may take several minutes
- Disable media export for **schema-only** exports: `"IncludeMedia": false`
- Disable content export for **structure-only** exports: `"IncludeContent": false`

### Team Collaboration
- Commit `umbraco.yaml` to version control for shared environments
- Don't commit media files unless the library is small — use a CDN or shared storage instead
- Use `Yaml2Schema` on local/staging environments to bootstrap from the shared YAML

---

## Troubleshooting

### Export is slow
Disable media and/or content export in `appsettings.json` for schema-only exports.

### Media files not downloading
Check file permissions on the `wwwroot/media` folder and verify there is enough disk space for ZIP creation.

### YAML import fails on target site
- Ensure the Yaml2Schema version supports the target Umbraco version
- Verify all required packages/property editors are installed on the target site

---

## Roadmap

- [x] Core export — all 10 entity types
- [x] Dashboard UI — AngularJS (Umbraco 13) and Lit (Umbraco 14–17)
- [x] ZIP download with media
- [x] Multi-version support (Umbraco 13–17, net8/9/10)
- [x] CI/CD — GitHub Actions for build, test, and NuGet publish
- [ ] Selective export (choose specific types or subtrees)
- [ ] Incremental exports (only changes since last export)
- [ ] Export scheduling and automation
- [ ] CLI tool for CI/CD pipeline integration
- [ ] Cloud storage export (Azure Blob, S3)

---

## Contributing

1. Fork the repository at [github.com/SplatDev-Ltda/umbraco-yaml](https://github.com/SplatDev-Ltda/umbraco-yaml)
2. Create a feature branch
3. Follow the existing code style (see companion `Yaml2Schema` plugin as baseline)
4. Add unit tests for new functionality
5. Submit a pull request

---

## Support

- **Issues**: [GitHub Issues](https://github.com/SplatDev-Ltda/umbraco-yaml/issues)
- **Repository**: [github.com/SplatDev-Ltda/umbraco-yaml](https://github.com/SplatDev-Ltda/umbraco-yaml)
- **Companion plugin**: [SplatDev.Umbraco.Plugins.Yaml2Schema](https://www.nuget.org/packages/SplatDev.Umbraco.Plugins.Yaml2Schema)

---

## License

MIT — see [LICENSE](https://github.com/SplatDev-Ltda/umbraco-yaml/blob/master/LICENSE) for details.

---

Developed by **[SplatDev Ltda](https://github.com/SplatDev-Ltda)**

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
