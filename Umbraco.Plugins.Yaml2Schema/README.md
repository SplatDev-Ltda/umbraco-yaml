# SplatDev Umbraco YAML Plugin

[![NuGet](https://img.shields.io/nuget/v/SplatDev.Umbraco.Plugins.Yaml2Schema.svg)](https://www.nuget.org/packages/SplatDev.Umbraco.Plugins.Yaml2Schema)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A declarative, **Infrastructure-as-Code** style plugin for **Umbraco 17** that bootstraps your entire Umbraco site structure from a single YAML file on application startup.

Define DataTypes, DocumentTypes, Media Types, Templates, Content, Media, Languages, Dictionary Items, Members, and Users as code. Version-control it. Reproduce it anywhere.

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

All structures defined in the YAML file are created automatically on startup. Existing items (matched by alias/name/email) are skipped — restarts are safe.

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

All top-level sections are optional. Processing order:

`languages` → `dataTypes` → `documentTypes` → `mediaTypes` → `scripts` → `stylesheets` → `templates` → `content` → `media` → `dictionaryItems` → `members` → `users`

### Flags (`remove` / `update`)

Every item in every section supports two optional control flags:

| Flag | Behaviour |
|------|-----------|
| `remove: true` | Delete the entity on startup. Logs a warning if not found. |
| `update: true` | **Upsert**: update if found, create if not found. |

Neither flag is set by default; omitting both means **create if not exists, skip otherwise**.

**Per-entity UPDATE semantics:**

| Entity | What gets updated |
|--------|------------------|
| `dataTypes` | `DatabaseType` re-derived from the editor; `config` re-applied. Use this to correct stale entries after upgrading the plugin. |
| `documentTypes` / `mediaTypes` | Additive merge — top-level fields replaced; new tabs/properties added; existing properties never removed. |
| `templates` | `content` field regenerated (or replaced with explicit Razor). |
| `content` | Property values, sort order, and published state updated. |
| `scripts` / `stylesheets` | File overwritten in `wwwroot`. |
| `languages` | `isDefault` and `isMandatory` updated. |
| `dictionaryItems` | Translation values upserted per language. |
| `members` | Name, approval status, and properties updated. |
| `users` | Name, username, and group assignments updated. |

---

### `dataTypes`

Define property editors. The `config` map is applied directly to the DataType — supports Block List, Image Cropper, and any editor accepting a key/value configuration.

```yaml
dataTypes:
  - alias: pageTitle
    name: Page Title
    editorUiAlias: Umbraco.TextBox
    config:
      maxLength: 100

  - alias: status
    name: Status
    editorUiAlias: Umbraco.DropDown.Flexible
    config:
      items:
        - Draft
        - Published
        - Archived

  - alias: blockList
    name: Block List
    editorUiAlias: Umbraco.BlockList
    config:
      blocks:
        - contentElementTypeKey: "00000000-0000-0000-0000-000000000000"

  # [UPDATE] — re-applies config and DatabaseType on every startup
  - alias: richText
    name: Rich Text
    editorUiAlias: Umbraco.RichText
    update: true

  # [REMOVE] — deletes this DataType
  - alias: legacyEditor
    name: Legacy Editor
    editorUiAlias: Umbraco.TextBox
    remove: true
```

| Field | Required | Description |
|-------|----------|-------------|
| `alias` | Yes | Unique identifier |
| `name` | Yes | Display name in the back-office |
| `editorUiAlias` | Yes | Registered property editor alias |
| `config` | No | Editor-specific configuration (key-value map) |

> **`Umbraco.DropDown.Flexible` / `Umbraco.CheckBoxList`**: `config.items` must be a plain YAML string list — the plugin converts it to the `List<string>` format that `ValueListConfiguration` expects in Umbraco 17.
>
> **UPDATE behaviour**: `update: true` re-derives the `DatabaseType` from the editor and re-applies the `config`. Add it to any DataType whose database storage type or config may be stale (e.g. after a plugin upgrade).

---

### `documentTypes`

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
            dataType: richText
```

| Field | Required | Default | Description |
|-------|----------|---------|-------------|
| `alias` | Yes | — | Unique identifier |
| `name` | Yes | — | Display name |
| `icon` | No | `icon-document` | Umbraco icon CSS class |
| `allowAsRoot` | No | `true` | Allow creation at the content tree root |
| `allowedChildTypes` | No | `[]` | DocumentType aliases permitted as children |
| `tabs` | No | `[]` | Property tabs, each with `name` and `properties` |

**Property fields:**

| Field | Required | Default | Description |
|-------|----------|---------|-------------|
| `alias` | Yes | — | Unique within the DocumentType |
| `name` | Yes | — | Editor-facing label |
| `dataType` | Yes | — | Alias of a DataType defined in `dataTypes` |
| `required` | No | `false` | Mandatory field for editors |
| `description` | No | — | Help text shown below the field |

> **UPDATE behaviour**: Additive merge — existing properties and tabs are never removed. New tabs and new properties are added. Top-level fields (`name`, `icon`, `allowAsRoot`) are replaced.

---

### `mediaTypes`

Define media blueprints (mirrors `documentTypes` structure).

```yaml
mediaTypes:
  - alias: customImage
    name: Custom Image
    icon: icon-picture
    allowedAtRoot: false
    tabs:
      - name: Media
        properties:
          - alias: umbracoFile
            name: File
            dataType: Upload File
```

---

### `templates`

Register Razor view templates. A default `@inherits UmbracoViewPage` scaffold is generated automatically. Provide an explicit `content:` field to use your own Razor markup instead.

```yaml
templates:
  - alias: master
    name: Master
    masterTemplate: null
    stylesheets:
      - css/site.css
    scripts:
      - js/app.js

  - alias: page
    name: Page
    masterTemplate: master
    content: |
      @inherits Umbraco.Cms.Web.Common.Views.UmbracoViewPage
      @{
          Layout = "master";
      }
      <h1>@Model.Value("title")</h1>
      @Html.Raw(Model.Value("body"))
```

| Field | Required | Description |
|-------|----------|-------------|
| `alias` | Yes | Unique identifier |
| `name` | Yes | Display name |
| `masterTemplate` | No | Alias of the parent layout template, or `null` |
| `content` | No | Explicit Razor markup; overrides the auto-generated scaffold |
| `stylesheets` | No | List of wwwroot-relative CSS paths to inject into `<head>` |
| `scripts` | No | List of wwwroot-relative JS paths to inject before `</body>` |

---

### `scripts` / `stylesheets`

Write JavaScript and CSS files to `wwwroot` on startup.

```yaml
scripts:
  - alias: siteJs
    name: Site JavaScript
    path: js/site.js
    content: |
      console.log('loaded');
  - alias: siteJs
    path: js/site.js
    update: true          # overwrite on every startup
    content: |
      console.log('updated');
  - alias: legacyJs
    path: js/legacy.js
    remove: true          # delete from wwwroot

stylesheets:
  - alias: siteStyles
    name: Site Styles
    path: css/site.css
    content: |
      body { margin: 0; }
```

| Field | Required | Description |
|-------|----------|-------------|
| `alias` | Yes | Unique identifier (deduplication key) |
| `path` | Yes | Output path relative to `wwwroot` |
| `content` | No | File content to write |

---

### `content`

Seed content nodes, nested to any depth.

```yaml
content:
  - alias: home
    name: Home
    documentType: page
    isPublished: true
    sortOrder: 0
    properties:
      title: "Welcome"
      body: "<p>Hello world.</p>"
    children:
      - alias: about
        name: About Us
        documentType: page
        isPublished: true
        properties:
          title: "About Us"
```

| Field | Required | Default | Description |
|-------|----------|---------|-------------|
| `alias` | Yes | — | Unique node identifier |
| `name` | Yes | — | Name shown in content tree |
| `documentType` | Yes | — | DocumentType alias |
| `isPublished` | No | `false` | Publish on creation |
| `sortOrder` | No | `0` | Position among siblings |
| `properties` | No | `{}` | Property alias → value pairs |
| `children` | No | `[]` | Nested child content nodes |

---

### `media`

Create media nodes. Optionally download a file from a URL and attach it.

```yaml
media:
  - alias: siteBanner
    name: Site Banner
    mediaType: Image
    url: https://example.com/banner.jpg
  - alias: docs
    name: Documents
    mediaType: Folder
    children:
      - alias: brochure
        name: Brochure
        mediaType: File
        url: https://example.com/brochure.pdf
```

| Field | Required | Description |
|-------|----------|-------------|
| `alias` | Yes | Unique identifier |
| `name` | Yes | Name in the media tree |
| `mediaType` | Yes | Media Type alias (e.g. `Image`, `File`, `Folder`) |
| `url` | No | URL to download and attach as the file property |
| `properties` | No | Additional property alias → value pairs |
| `children` | No | Nested child media nodes |

---

### `languages`

Register Umbraco languages. Languages are created before all other entities.

```yaml
languages:
  - isoCode: en-US
    cultureName: English (United States)
    isDefault: true
    isMandatory: true
  - isoCode: fr-FR
    cultureName: French (France)
```

| Field | Required | Default | Description |
|-------|----------|---------|-------------|
| `isoCode` | Yes | — | BCP-47 culture code (e.g. `en-US`) |
| `cultureName` | No | Auto from .NET | Display name; defaults to the .NET culture display name |
| `isDefault` | No | `false` | Set as the default language |
| `isMandatory` | No | `false` | Require translation before publishing |

---

### `dictionaryItems`

Seed Umbraco dictionary keys with per-language translations.

```yaml
dictionaryItems:
  - key: general.hello
    translations:
      en-US: Hello
      fr-FR: Bonjour
  - key: nav.home
    translations:
      en-US: Home
      fr-FR: Accueil
    update: true
```

| Field | Required | Description |
|-------|----------|-------------|
| `key` | Yes | Dictionary key (dot-notation recommended) |
| `translations` | No | ISO code → translated string map |

---

### `members`

Create Umbraco member accounts.

```yaml
members:
  - alias: testMember
    name: Test Member
    email: test@example.com
    username: testmember
    password: "S3cure!Pass"
    memberType: Member
    isApproved: true
    properties:
      comments: Welcome note
```

| Field | Required | Default | Description |
|-------|----------|---------|-------------|
| `email` | Yes | — | Unique identifier for lookup/dedup |
| `name` | Yes | — | Display name |
| `username` | No | email | Login username |
| `password` | No | — | Initial password |
| `memberType` | No | `Member` | Member Type alias |
| `isApproved` | No | `true` | Whether the member can log in |
| `properties` | No | `{}` | Member property values |

---

### `users`

Create Umbraco backoffice users.

```yaml
users:
  - alias: editorUser
    name: Editor User
    email: editor@example.com
    username: editoruser
    userGroups:
      - editor
```

| Field | Required | Description |
|-------|----------|-------------|
| `email` | Yes | Unique identifier for lookup/dedup |
| `name` | Yes | Display name |
| `username` | No | Login username (defaults to email) |
| `userGroups` | No | List of user group aliases to assign |

---

## Common Editor Aliases

Use the server-side schema alias in `editorUiAlias`. The plugin automatically resolves the correct Umbraco 17 backoffice UI component alias (`Umb.PropertyEditorUi.*`) so the property editor renders correctly.

| `editorUiAlias` | Backoffice UI alias resolved | Type |
|-----------------|------------------------------|------|
| `Umbraco.TextBox` | `Umb.PropertyEditorUi.TextBox` | Single-line text |
| `Umbraco.TextArea` | `Umb.PropertyEditorUi.TextArea` | Multi-line text |
| `Umbraco.RichText` | `Umb.PropertyEditorUi.Tiptap` | Rich HTML editor (Tiptap) |
| `Umbraco.MarkdownEditor` | `Umb.PropertyEditorUi.MarkdownEditor` | Markdown |
| `Umbraco.Integer` | `Umb.PropertyEditorUi.Integer` | Whole number |
| `Umbraco.Decimal` | `Umb.PropertyEditorUi.Decimal` | Decimal number |
| `Umbraco.TrueFalse` | `Umb.PropertyEditorUi.Toggle` | Boolean toggle |
| `Umbraco.DateTime` | `Umb.PropertyEditorUi.DatePicker` | Date and time |
| `Umbraco.MediaPicker3` | `Umb.PropertyEditorUi.MediaPicker` | Media picker |
| `Umbraco.ContentPicker` | `Umb.PropertyEditorUi.DocumentPicker` | Content picker |
| `Umbraco.MultiNodeTreePicker` | `Umb.PropertyEditorUi.ContentPicker` | Multi-node tree picker |
| `Umbraco.MultiUrlPicker` | `Umb.PropertyEditorUi.MultiUrlPicker` | Multi-URL picker |
| `Umbraco.Tags` | `Umb.PropertyEditorUi.Tags` | Tag input |
| `Umbraco.DropDown.Flexible` | `Umb.PropertyEditorUi.Dropdown` | Dropdown / multi-select (use `config.items` string list) |
| `Umbraco.CheckBoxList` | `Umb.PropertyEditorUi.CheckBoxList` | Checkbox list (use `config.items` string list) |
| `Umbraco.RadioButtonList` | `Umb.PropertyEditorUi.RadioButtonList` | Radio button list |
| `Umbraco.BlockList` | `Umb.PropertyEditorUi.BlockList` | Block List editor |
| `Umbraco.BlockGrid` | `Umb.PropertyEditorUi.BlockGrid` | Block Grid editor |
| `Umbraco.ImageCropper` | `Umb.PropertyEditorUi.ImageCropper` | Image with crop config |
| `Umbraco.EmailAddress` | `Umb.PropertyEditorUi.EmailAddress` | Email address |
| `Umbraco.Label` | `Umb.PropertyEditorUi.Label` | Read-only label |
| `Umbraco.UploadField` | `Umb.PropertyEditorUi.UploadField` | File upload |
| `Umbraco.ColorPicker` | `Umb.PropertyEditorUi.ColorPicker` | Color picker |

---

## Architecture

| Component | Responsibility |
|-----------|---------------|
| `YamlStartupComposer` | Registers all services and wires the startup handler |
| `YamlInitializationHandler` | Fires on `UmbracoApplicationStarted`; orchestrates all creators |
| `YamlParser` | Deserializes the YAML file using YamlDotNet |
| `DataTypeCreator` | Creates/updates/removes DataTypes |
| `DocumentTypeCreator` | Creates/updates/removes DocumentTypes and their tabbed properties |
| `MediaTypeCreator` | Creates/updates/removes Media Types |
| `TemplateCreator` | Creates/updates/removes Razor templates |
| `ContentCreator` | Recursively creates/updates/removes content nodes |
| `MediaCreator` | Creates/updates/removes media nodes; downloads files from URLs |
| `StaticAssetCreator` | Writes/deletes JS and CSS files under `wwwroot` |
| `LanguageCreator` | Creates/updates/removes Umbraco languages |
| `DictionaryCreator` | Creates/updates/removes dictionary items with translations |
| `MemberCreator` | Creates/updates/removes member accounts |
| `UserCreator` | Creates/updates/removes backoffice users |

---

## Behaviour

- **Idempotent**: Items already present are skipped — safe across restarts
- **Ordered**: Languages first, then DataTypes, DocumentTypes, MediaTypes, Scripts/Stylesheets, Templates, Content, Media, DictionaryItems, Members, Users
- **Logged**: All activity and warnings go to the standard Umbraco log
- **Forgiving**: Missing references log a warning and continue without throwing
- **Additive updates**: DocumentType/MediaType `[UPDATE]` never removes existing properties — only adds new ones

---

## Running Tests

```bash
dotnet test
```

---

## License

MIT © 2026 [SplatDev](https://github.com/SplatDev-Ltda)
