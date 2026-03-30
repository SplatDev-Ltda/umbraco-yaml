# SplatDev Umbraco YAML Plugin

[![NuGet](https://img.shields.io/nuget/v/SplatDev.Umbraco.Plugins.Yaml2Schema.svg)](https://www.nuget.org/packages/SplatDev.Umbraco.Plugins.Yaml2Schema)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A declarative, **Infrastructure-as-Code** style plugin for **Umbraco 17** that creates DataTypes, DocumentTypes, Templates, and Content automatically from a single YAML configuration file on application startup.

Define your entire Umbraco structure as code. Version-control it. Reproduce it anywhere.

---

## Installation

```bash
dotnet add package SplatDev.Umbraco.Plugins.Yaml2Schema
```

Or via the NuGet Package Manager:

```
Install-Package SplatDev.Umbraco.Plugins.Yaml2Schema
```

No further registration is required — the plugin self-registers via an Umbraco composer on startup.

---

## Quick Start

1. Install the package
2. Create `config/umbraco.yaml` in your project root
3. Run your Umbraco application

All structures defined in the YAML file are created automatically. Existing items (matched by alias) are skipped, so restarts are safe.

---

## Configuration

Override the default config file path in `appsettings.json`:

```json
{
  "UmbracoYaml": {
    "ConfigPath": "config/umbraco.yaml"
  }
}
```

The path is relative to the application content root. Absolute paths are also accepted.

---

## YAML Schema

The config file has four top-level sections. All are optional and processed in order: `dataTypes` → `documentTypes` → `templates` → `content`.

### DataTypes

Define the property editors available to your DocumentTypes.

```yaml
dataTypes:
  - alias: pageTitle
    name: Page Title
    editorUiAlias: Umbraco.TextBox
    config:
      maxLength: 100

  - alias: bodyContent
    name: Body Content
    editorUiAlias: Umbraco.TinyMCE
```

| Field | Required | Description |
|-------|----------|-------------|
| `alias` | Yes | Unique identifier — referenced by DocumentType properties |
| `name` | Yes | Display name in the back-office |
| `editorUiAlias` | Yes | Registered property editor alias |
| `config` | No | Editor-specific configuration (key-value map) |

### DocumentTypes

Define content blueprints with tabbed property groups.

```yaml
documentTypes:
  - alias: page
    name: Page
    icon: icon-document
    allowAsRoot: true
    allowedChildTypes:
      - page
      - article
    tabs:
      - name: Content
        properties:
          - alias: title
            name: Title
            dataType: pageTitle
            required: true
            description: The main heading shown on the page

          - alias: body
            name: Body
            dataType: bodyContent
```

| Field | Required | Default | Description |
|-------|----------|---------|-------------|
| `alias` | Yes | — | Unique identifier |
| `name` | Yes | — | Display name |
| `icon` | No | — | Umbraco icon CSS class (e.g. `icon-document`) |
| `allowAsRoot` | No | `true` | Allow creation at the content tree root |
| `allowedChildTypes` | No | `[]` | DocumentType aliases permitted as children |
| `tabs` | No | `[]` | Property tabs, each with `name` and `properties` |

**Property fields:**

| Field | Required | Default | Description |
|-------|----------|---------|-------------|
| `alias` | Yes | — | Unique within the DocumentType; used in templates |
| `name` | Yes | — | Editor-facing label |
| `dataType` | Yes | — | Alias of a DataType defined above |
| `required` | No | `false` | Mandatory field for editors |
| `description` | No | — | Help text shown below the field |

### Templates

Register Razor view templates. The `.cshtml` file must exist on disk.

```yaml
templates:
  - alias: master
    name: Master
    path: Master.cshtml
    masterTemplate: null

  - alias: page
    name: Page
    path: Page.cshtml
    masterTemplate: master
```

| Field | Required | Description |
|-------|----------|-------------|
| `alias` | Yes | Unique identifier |
| `name` | Yes | Display name |
| `path` | Yes | Path to `.cshtml` relative to `Views/` |
| `masterTemplate` | No | Alias of the parent layout template, or `null` |

### Content

Seed content nodes, nested to any depth.

```yaml
content:
  - alias: home
    name: Home
    documentType: page
    isPublished: true
    sortOrder: 0
    properties:
      title: "Welcome to Our Website"
      body: "<p>Hello world.</p>"
    children:
      - alias: about
        name: About Us
        documentType: page
        isPublished: true
        properties:
          title: "About Us"
          body: "<p>Who we are.</p>"
```

| Field | Required | Default | Description |
|-------|----------|---------|-------------|
| `alias` | Yes | — | Unique node identifier |
| `name` | Yes | — | Name shown in content tree |
| `documentType` | Yes | — | DocumentType alias |
| `isPublished` | No | `false` | Publish on creation; otherwise saved as draft |
| `sortOrder` | No | `0` | Position among siblings (zero-based) |
| `properties` | No | `{}` | Property alias → value pairs |
| `children` | No | `[]` | Nested child content nodes |

---

## Common Editor Aliases

| `editorUiAlias` | Type |
|-----------------|------|
| `Umbraco.TextBox` | Single-line text |
| `Umbraco.TextArea` | Multi-line text |
| `Umbraco.TinyMCE` | Rich HTML editor |
| `Umbraco.MarkdownEditor` | Markdown |
| `Umbraco.Integer` | Whole number |
| `Umbraco.TrueFalse` | Boolean toggle |
| `Umbraco.DateTime` | Date and time |
| `Umbraco.MediaPicker3` | Media picker |
| `Umbraco.ContentPicker` | Content picker |
| `Umbraco.Tags` | Tag input |

---

## Architecture

| Component | Responsibility |
|-----------|---------------|
| `YamlStartupComposer` | Registers services and wires the startup handler |
| `YamlInitializationHandler` | Fires on `UmbracoApplicationStarted` and orchestrates creation |
| `YamlParser` | Deserializes the YAML file using YamlDotNet |
| `DataTypeCreator` | Creates DataTypes via Umbraco's `IDataTypeService` |
| `DocumentTypeCreator` | Creates DocumentTypes and their tabbed properties |
| `TemplateCreator` | Creates template records in Umbraco |
| `ContentCreator` | Recursively creates and publishes content nodes |

---

## Behaviour

- **Idempotent**: Items already present (by alias) are skipped — safe across restarts
- **Ordered**: DataTypes → DocumentTypes → Templates → Content
- **Logged**: All activity and warnings go to the standard Umbraco log
- **Forgiving**: Missing references (e.g. unknown DataType) log a warning and continue

---

## Running Tests

```bash
dotnet test
```

---

## License

MIT © 2026 [SplatDev](https://github.com/SplatDev-Ltda)
