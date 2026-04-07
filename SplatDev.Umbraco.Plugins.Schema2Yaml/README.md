# SplatDev.Umbraco.Plugins.Schema2Yaml

[![NuGet](https://img.shields.io/nuget/v/SplatDev.Umbraco.Plugins.Schema2Yaml.svg)](https://www.nuget.org/packages/SplatDev.Umbraco.Plugins.Schema2Yaml)
[![NuGet Downloads](https://img.shields.io/nuget/dt/SplatDev.Umbraco.Plugins.Schema2Yaml.svg)](https://www.nuget.org/packages/SplatDev.Umbraco.Plugins.Schema2Yaml)
[![CI](https://github.com/SplatDev-Ltda/umbraco-yaml/actions/workflows/ci.yml/badge.svg)](https://github.com/SplatDev-Ltda/umbraco-yaml/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

An Infrastructure-as-Code tool for **Umbraco 13** that exports your entire Umbraco site structure to YAML format. Export once, version-control it, and import anywhere using the companion [Yaml2Schema](https://www.nuget.org/packages/SplatDev.Umbraco.Plugins.Yaml2Schema) plugin.

> **v1.0.x** supports Umbraco 13 only (net8.0). For Umbraco 14-17, use **v2.x** from the `master` branch.

---

## Features

### Complete Schema Export
- **DataTypes** — Property editors with full configuration (JSON serialized)
- **DocumentTypes** — Content types with properties, tabs, compositions, allowed children, and templates
- **Media Types** — Media types with properties and folder structure
- **Templates** — Razor templates with full content and master template hierarchy
- **Languages** — Multi-language configuration with default/mandatory flags
- **Dictionary Items** — Translations for all languages with parent-child hierarchy

### Content & Media Export
- **Content** — All content nodes with properties, sort order, and published state (recursive)
- **Media** — Media items with automatic file download to local folder
- **Media Files** — Physical files downloaded and organized in Umbraco folder structure

### User Management Export
- **Members** — Member accounts with custom properties and groups (passwords excluded)
- **Users** — Back-office users with roles (opt-in, passwords excluded)

### Export Features
- **Single ZIP Download** — YAML + media bundled in one archive
- **Export Statistics** — Live counts per entity type with duration
- **Dashboard UI** — AngularJS dashboard in the Settings section
- **Folder Structure** — Media files maintain Umbraco's original folder hierarchy

---

## Installation

```bash
dotnet add package SplatDev.Umbraco.Plugins.Schema2Yaml --version "1.0.*"
```

No further registration is required — the plugin self-registers via `Schema2YamlComposer` on startup and the dashboard appears automatically in the **Settings** section.

---

## Usage

### Via Dashboard (Recommended)

1. Navigate to **Settings** in the Umbraco back-office
2. Click the **Schema Export** dashboard tab
3. Click **Export to YAML** to generate YAML from your current site
4. Review the export statistics and YAML preview
5. Click **Download YAML** for a single file, or **Download ZIP** for YAML + media

The ZIP archive structure:
```
umbraco-export.zip
├── umbraco.yml           # Complete schema + content
└── media/                # Media files in Umbraco folder structure
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
        await _exportService.ExportToFileAsync("exports/umbraco.yml");

        // Export to ZIP with media files
        var zipBytes = await _exportService.ExportToZipAsync();
        return File(zipBytes, "application/zip", "umbraco-export.zip");
    }
}
```

### REST API Endpoints

The plugin exposes authenticated endpoints under `/umbraco/backoffice/api/SchemaExport/`:

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/Export` | Export and return `{ yaml, statistics }` as JSON |
| GET | `/DownloadYaml` | Stream YAML as a file download |
| GET | `/DownloadZip` | Stream ZIP (YAML + media) as a file download |
| GET | `/Statistics` | Return statistics from the last export |

All endpoints require the **Settings** section access policy.

---

## Configuration

Override defaults in `appsettings.json`:

```json
{
  "UmbracoSchema2Yaml": {
    "ExportPath": "exports/umbraco.yml",
    "IncludeMedia": true,
    "MediaPath": "exports/media",
    "IncludeContent": true,
    "IncludeMembers": true,
    "IncludeUsers": false,
    "IncludeDictionary": true,
    "IncludeLanguages": true,
    "CompressYaml": false,
    "MaxHierarchyDepth": 50
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| `ExportPath` | `exports/umbraco.yml` | Default YAML export file path |
| `IncludeMedia` | `true` | Include media items and file downloads |
| `MediaPath` | `exports/media` | Media files download location |
| `IncludeContent` | `true` | Include content nodes |
| `IncludeMembers` | `true` | Include member accounts |
| `IncludeUsers` | `false` | Include back-office users (security consideration) |
| `IncludeDictionary` | `true` | Include dictionary items |
| `IncludeLanguages` | `true` | Include languages |
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
              required: true

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
      isPublished: true
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

## Security

- Passwords are never exported — members and users require a password reset after import
- Review exported YAML for sensitive property values before committing to version control
- Add `exports/` to `.gitignore` to prevent accidental commits
- `IncludeUsers` is off by default — opt in explicitly

## Performance

- For large sites (1000+ content nodes), export may take several minutes
- Set `"IncludeMedia": false` for schema-only exports
- Set `"IncludeContent": false` for structure-only exports

---

## Support

- **Issues**: [GitHub Issues](https://github.com/SplatDev-Ltda/umbraco-yaml/issues)
- **Repository**: [github.com/SplatDev-Ltda/umbraco-yaml](https://github.com/SplatDev-Ltda/umbraco-yaml)

## License

MIT — see [LICENSE](https://github.com/SplatDev-Ltda/umbraco-yaml/blob/master/LICENSE) for details.

---

Developed by **[SplatDev Ltda](https://github.com/SplatDev-Ltda)**
