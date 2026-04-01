# SplatDev Umbraco YAML Plugin

[![NuGet](https://img.shields.io/nuget/v/SplatDev.Umbraco.Plugins.Yaml2Schema.svg)](https://www.nuget.org/packages/SplatDev.Umbraco.Plugins.Yaml2Schema)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A declarative, **Infrastructure-as-Code** style plugin for **Umbraco 17** that bootstraps your entire Umbraco site structure from a single YAML file on application startup.

Define DataTypes, DocumentTypes, Media Types, Templates, Content, Media, Languages, Dictionary Items, Members, Users, custom Property Editors, and NuGet package requirements as code. Version-control it. Reproduce it anywhere.

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

`packages` → `propertyEditors` → `languages` → `dataTypes` → `documentTypes` → `mediaTypes` → `scripts` → `stylesheets` → `templates` → `content` → `media` → `dictionaryItems` → `members` → `users`

> After `documentTypes`, two linking passes run automatically:
> 1. **Link templates** — assigns `allowedTemplates` / `defaultTemplate` to document types
> 2. **Link Block List element types** — resolves `contentElementTypeAlias` → GUID in Block List/Grid DataType configs

### Flags (`remove` / `update`)

Every item in every section supports two optional control flags:

| Flag | Behaviour |
|------|-----------|
| `remove: true` | Delete the entity on startup. Logs a warning if not found. |
| `update: true` | Update if found, create if not found (upsert). |

Neither flag is set by default; omitting both means **create if not exists, skip otherwise**.

---

### `packages`

Declare NuGet packages your site depends on. At startup the plugin checks whether each package's assembly is loaded in the current AppDomain and logs a warning (optional packages) or error (required packages) if it is missing. No installation is performed — add missing packages to your `.csproj` manually.

```yaml
packages:
  - id: Our.Umbraco.Community.SomePlugin
    version: "2.0.0"
    required: true
  - id: Another.Plugin
    assemblyName: Another.Plugin.Core   # override when assembly name differs from package ID
```

| Field | Required | Default | Description |
|-------|----------|---------|-------------|
| `id` | Yes | — | NuGet package ID |
| `version` | No | — | Expected version (informational; logged if mismatch) |
| `required` | No | `false` | `true` logs an error; `false` logs a warning |
| `assemblyName` | No | same as `id` | Override when the assembly name differs from the package ID |

---

### `propertyEditors`

Define custom (frontend-only) Umbraco property editors. The plugin writes an `App_Plugins/[folderName]/umbraco-package.json` manifest (Umbraco 14+ format) and, when `jsContent` is provided, the corresponding JavaScript file.

```yaml
propertyEditors:
  - alias: My.ColourSwatchPicker
    name: "Colour Swatch Picker"
    icon: icon-color
    group: common
    jsContent: |
      customElements.define('my-colour-swatch', class extends HTMLElement {
        connectedCallback() {
          this.innerHTML = '<input type="color">';
        }
      });
```

| Field | Required | Default | Description |
|-------|----------|---------|-------------|
| `alias` | Yes | — | Schema alias (e.g. `My.ColourSwatchPicker`) |
| `name` | Yes | — | Display name |
| `icon` | No | `icon-code` | Backoffice icon alias |
| `group` | No | `common` | Backoffice group |
| `uiAlias` | No | `{alias}.Ui` | UI component alias; auto-derived when omitted |
| `folderName` | No | derived from alias | `App_Plugins` sub-folder name |
| `jsPath` | No | `/App_Plugins/{folderName}/index.js` | URL path to the JS file |
| `jsContent` | No | — | Inline JavaScript; written to the JS file on startup |

To create a DataType using a custom property editor, add `valueType` (see [DataTypes](#datatypes)):

```yaml
dataTypes:
  - alias: colourSwatch
    name: "Colour Swatch"
    editorUiAlias: My.ColourSwatchPicker
    valueType: NVARCHAR
```

---

### `dataTypes`

Define property editors. The `config` map is applied directly to the DataType — supports Block List, Block Grid, Image Cropper, Dropdown, and any editor accepting a key/value configuration.

```yaml
dataTypes:
  - alias: pageTitle
    name: Page Title
    editorUiAlias: Umbraco.TextBox
    config:
      maxLength: 100

  # Block List with element type alias resolution (no GUIDs required)
  - alias: pilaresBlockList
    name: Pilares Block List
    editorUiAlias: Umbraco.BlockList
    config:
      blocks:
        - contentElementTypeAlias: pilarElement   # resolved to GUID after DocumentTypes are created
          label: Pilar
      validationLimit:
        min: 0
        max: null

  # Custom frontend-only editor (requires valueType for server-side storage fallback)
  - alias: colourSwatch
    name: Colour Swatch
    editorUiAlias: My.ColourSwatchPicker
    valueType: NVARCHAR

  # [UPDATE] — upsert
  - alias: richText
    name: Rich Text
    editorUiAlias: Umbraco.RichText
    update: true

  # [REMOVE] — delete
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
| `valueType` | No | Storage type override: `NVARCHAR` (default), `NTEXT`, `TEXT`, `INT`, `INTEGER`, `BIGINT`, `DECIMAL`, `DATE`. Required for custom frontend-only editors. |

#### Block List config: `contentElementTypeAlias`

Instead of hard-coding GUIDs, use `contentElementTypeAlias` in the `blocks` array. After all DocumentTypes are created, the plugin automatically resolves each alias to its actual content element type GUID and re-saves the DataType.

```yaml
config:
  blocks:
    - contentElementTypeAlias: myElementType   # ← resolved automatically
      label: "My Block"
```

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
    allowedTemplates:
      - page
    defaultTemplate: page
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
| `allowedTemplates` | No | `[]` | Template aliases to assign |
| `defaultTemplate` | No | — | Default template alias |
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
      <h1>@(Model.Value<string>("title"))</h1>
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

> **Razor tip**: Always wrap generic method calls in `@(...)` — e.g. `@(Model.Value<string>("prop"))`. Without the parentheses, Razor parses `<string>` as an HTML tag.

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
  - alias: siteJsUpdate
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

#### Block List content seeding

Use a list of mappings with a `$type` key to seed Block List properties without writing raw JSON. The `$type` value is the element document-type alias; all other keys become property values on the block.

```yaml
properties:
  pilares:
    - $type: pilarElement
      title: "Pilar 1"
      text:  "Texto do pilar 1"
    - $type: pilarElement
      title: "Pilar 2"
      text:  "Texto do pilar 2"
```

#### Generic complex values

Any property stored as `Ntext` whose YAML value is a list or mapping (without `$type`) is serialised to a JSON string automatically. This covers Tags, Multi-URL Pickers, and other editors that store JSON.

```yaml
properties:
  tags:
    - umbraco
    - yaml
```

---

### `media`

Create media nodes. Optionally download a file from a URL and attach it. Use `folder` to place the item inside an auto-created folder hierarchy.

```yaml
media:
  - alias: siteBanner
    name: Site Banner
    mediaType: Image
    url: https://example.com/banner.jpg

  # Place inside a folder path (created automatically if missing)
  - alias: logoPartnerA
    name: Logo Partner A
    mediaType: Image
    folder: "Images/Partners"
    url: https://example.com/logo-a.png

  # Nest using children (alternative to folder)
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
| `folder` | No | Folder path to place this item in (e.g. `"Images"` or `"Images/Partners"`). Folders are created automatically if missing. |
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

| `editorUiAlias` | Type |
|-----------------|------|
| `Umbraco.TextBox` | Single-line text |
| `Umbraco.TextArea` | Multi-line text |
| `Umbraco.RichText` | Rich HTML editor (Tiptap) |
| `Umbraco.MarkdownEditor` | Markdown |
| `Umbraco.Integer` | Whole number |
| `Umbraco.Decimal` | Decimal number |
| `Umbraco.TrueFalse` | Boolean toggle |
| `Umbraco.DateTime` | Date and time |
| `Umbraco.DropDown.Flexible` | Dropdown (single/multi) |
| `Umbraco.CheckBoxList` | Checkbox list |
| `Umbraco.RadioButtonList` | Radio button list |
| `Umbraco.Tags` | Tag input |
| `Umbraco.MediaPicker3` | Media picker |
| `Umbraco.ContentPicker` | Content picker |
| `Umbraco.MultiNodeTreePicker` | Multi-node tree picker |
| `Umbraco.MultiUrlPicker` | Multi-URL picker |
| `Umbraco.BlockList` | Block List editor |
| `Umbraco.BlockGrid` | Block Grid editor |
| `Umbraco.ImageCropper` | Image with crop config |
| `Umbraco.UploadField` | File upload |

---

## Architecture

| Component | Responsibility |
|-----------|---------------|
| `YamlStartupComposer` | Registers all services and wires the startup handler |
| `YamlInitializationHandler` | Fires on `UmbracoApplicationStarted`; orchestrates all creators in dependency order |
| `YamlParser` | Deserializes the YAML file using YamlDotNet |
| `PackageValidator` | Checks declared NuGet package assemblies are loaded; logs errors/warnings |
| `PropertyEditorCreator` | Writes App_Plugins manifests and JS files for custom property editors |
| `DataTypeCreator` | Creates/updates/removes DataTypes; resolves Block List element type aliases post-creation |
| `DocumentTypeCreator` | Creates/updates/removes DocumentTypes; links templates in a post-creation pass |
| `MediaTypeCreator` | Creates/updates/removes Media Types |
| `TemplateCreator` | Creates/updates/removes Razor templates |
| `ContentCreator` | Recursively creates/updates/removes content nodes; serialises Block List and complex values to JSON |
| `MediaCreator` | Creates/updates/removes media nodes; creates folder hierarchy; downloads files from URLs |
| `StaticAssetCreator` | Writes/deletes JS and CSS files under `wwwroot` |
| `LanguageCreator` | Creates/updates/removes Umbraco languages |
| `DictionaryCreator` | Creates/updates/removes dictionary items with translations |
| `MemberCreator` | Creates/updates/removes member accounts |
| `UserCreator` | Creates/updates/removes backoffice users |

---

## Behaviour

- **Idempotent**: Items already present are skipped — safe across restarts
- **Ordered**: Packages → PropertyEditors → Languages → DataTypes → DocumentTypes → MediaTypes → Scripts/Stylesheets → Templates → LinkTemplates → LinkBlockListElementTypes → Content → Media → DictionaryItems → Members → Users
- **Logged**: All activity and warnings go to the standard Umbraco log
- **Forgiving**: Missing references log a warning and continue without throwing
- **Additive updates**: DocumentType/MediaType `update: true` never removes existing properties — only adds new ones
- **Block List linking**: `contentElementTypeAlias` in DataType configs is resolved to GUIDs automatically after DocumentTypes are created — no hard-coded GUIDs needed

---

## Repository Structure

```
Umbraco.Plugins.Yaml2Schema/       NuGet package (source)
  src/
    Composers/                     DI registration
    Handlers/                      Startup notification handler
    Models/                        YAML model classes
    Services/                      Creator services
Umbraco.Plugins.Yaml2Schema.Tests/ xUnit test project
  fixtures/                        sample.yaml, web-config.yaml
UmbracoYaml.Web/                   Demo Umbraco 17 website
  config/umbraco.yaml              Live configuration example
Themes/                            Example YAML configurations
  Corporate__RISIN/umbraco.yaml    RISIN Corporate site schema
```

---

## Running Tests

```bash
dotnet test
```

---

## License

MIT © 2026 [SplatDev](https://github.com/SplatDev-Ltda)
