# Changelog

All notable changes to `SplatDev.Umbraco.Plugins.Yaml2Schema` are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
This project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

## [1.0.5] - 2026-03-31

### Added

#### Template Assignment on Document Types (`allowedTemplates` / `defaultTemplate` fields)
- `YamlDocumentType` now supports two new optional fields: `allowedTemplates` (list of template aliases) and `defaultTemplate` (single alias string).
- `DocumentTypeCreator` gains `ITemplateService` injection and a new `LinkTemplatesToDocumentTypes` method that resolves template aliases and assigns `AllowedTemplates` / `SetDefaultTemplate` on the content type.
- Template linking runs as a dedicated step in `YamlInitializationHandler` immediately **after** `CreateTemplates`, ensuring templates exist in the database before the link is attempted.
- Unresolved template aliases are logged as warnings and skipped rather than throwing.

## [1.0.3] - 2026-03-30

### Added

#### Languages (`languages` section)
- New top-level `languages` section for declaring Umbraco languages.
- Each entry supports `isoCode`, `cultureName`, `isDefault`, `isMandatory`, `remove`, and `update` flags.
- `LanguageCreator` service uses `ILanguageService` (async). Languages are created before all other entities so that dictionary items and content can reference culture codes.
- `[REMOVE]` deletes the language by ISO code; `[UPDATE]` modifies `isDefault` and `isMandatory` on the existing entry; create-if-not-found falls through from UPDATE.

#### Dictionary Items (`dictionaryItems` section)
- New top-level `dictionaryItems` section for bootstrapping Umbraco dictionary keys.
- Each entry supports `key`, `translations` (a map of ISO culture code → string value), `remove`, and `update` flags.
- `DictionaryCreator` service uses `ILocalizationService` and `ILanguageService`. Missing language references are logged as warnings rather than thrown.
- `[REMOVE]` deletes the dictionary item; `[UPDATE]` upserts translation values on an existing key; create-if-not-found falls through from UPDATE.

#### Media Types (`mediaTypes` section)
- New top-level `mediaTypes` section mirroring the `documentTypes` structure.
- Each entry supports `alias`, `name`, `icon`, `allowedAtRoot`, `tabs` (with properties), `remove`, and `update` flags.
- `MediaTypeCreator` service uses `IMediaTypeService` with the same tab/property building logic as `DocumentTypeCreator`.
- Additive update strategy: `[UPDATE]` replaces top-level fields only; tab/property merge follows the same additive-only rule as DocumentType updates.

#### Media Items (`media` section)
- New top-level `media` section for creating Umbraco Media nodes.
- Each entry supports `alias`, `name`, `mediaType`, `url` (downloads the file from a web URL), `properties`, `children` (recursive), `remove`, and `update` flags.
- `MediaCreator` service uses `IMediaService`. File downloads use `IHttpClientFactory`; failures are logged as warnings and do not abort the startup sequence.
- `IHttpClientFactory` is registered automatically by the composer (`AddHttpClient()`).

#### Members (`members` section)
- New top-level `members` section for creating Umbraco member accounts.
- Each entry supports `alias`, `name`, `email`, `username`, `password`, `memberType`, `isApproved`, `properties`, `remove`, and `update` flags.
- `MemberCreator` service uses `IMemberService`.
- `[REMOVE]` deletes by email; `[UPDATE]` updates name, approval status, and properties; create-if-not-found falls through from UPDATE.

#### Users (`users` section)
- New top-level `users` section for creating Umbraco backoffice users.
- Each entry supports `alias`, `name`, `email`, `username`, `userGroups` (list of group aliases), `remove`, and `update` flags.
- `UserCreator` service uses `IUserService`. Groups are resolved by alias and assigned via `AddGroup`.
- `[REMOVE]` deletes by email; `[UPDATE]` updates name, username, and group assignments.

#### Razor Content in Templates (`content` field)
- Templates now support an optional `content:` field containing explicit Razor markup.
- When provided, the content is used verbatim instead of the auto-generated `@inherits UmbracoViewPage` scaffold.
- Works for both CREATE and UPDATE operations; multi-line Razor views are supported via YAML block scalars (`|`).

#### DataType Config Application
- The `config:` dictionary declared in `dataTypes` entries is now applied to the created `DataType.Configuration` property.
- Supports any editor that accepts a `Dictionary<string, object>` config, including Block List, Image Cropper, Grid Layout, etc.

---

## [1.0.2] - 2025-xx-xx

### Added

#### Static Assets (JavaScript & CSS)
- New top-level `scripts` and `stylesheets` YAML sections for declaring static files to be written to `wwwroot`.
- Each entry supports `alias`, `name`, `path` (relative to `wwwroot`), and `content` fields.
- `StaticAssetCreator` service handles writing, updating, and deleting files under `IWebHostEnvironment.WebRootPath`.
- Subdirectories are created automatically when the target path includes nested folders.
- Duplicate aliases within a single YAML run are skipped with a warning.
- Leading slashes in paths are normalised automatically.
- Templates can reference static assets via `scripts` and `stylesheets` lists; the generated Razor template injects `<link rel="stylesheet">` tags into `<head>` and `<script src="...">` tags before `</body>`.

#### `[REMOVE]` flag
- Any item in any YAML section (`dataTypes`, `documentTypes`, `templates`, `content`, `scripts`, `stylesheets`) can be flagged with `remove: true` to delete the corresponding Umbraco entity or static file on startup.
- If the target does not exist a warning is logged and execution continues without throwing.
- Umbraco content removal cascades to child nodes automatically (no YAML child enumeration required).

#### `[UPDATE]` flag
- Any item can be flagged with `update: true` to upsert: update if already present, create if not.
- **DataType UPDATE**: looks up by name (`GetDataType`); if found, logs and skips the broad editor-alias existence check, then continues to creation. Effectively a "create-if-missing" guard bypass.
- **DocumentType UPDATE**: applies an additive merge — name, icon, and `allowedAsRoot` are replaced; new tabs are added wholesale; new properties are merged into existing tabs; no existing property is ever removed to prevent data loss.
- **Template UPDATE**: regenerates the template file content (including any injected script/stylesheet tags) using `ITemplateService.UpdateAsync`.
- **Content UPDATE**: updates matching content node found by name under the same parent; sets property values, sort order, and published state; recurses into children.
- **Script/Stylesheet UPDATE**: overwrites the existing file in `wwwroot` when the `update` flag is set; skips the file otherwise.

#### Tests
- `StaticAssetCreatorTests` — 18 tests covering: file creation, subdirectory creation, skip-existing, overwrite-on-update, delete-on-remove, no-throw when remove target is missing, duplicate alias skipping, null alias, empty path, null list guard, leading slash normalisation — for both `Scripts` and `Stylesheets`.
- `YamlModelsTests` — extended with deserialization tests for `YamlScript`, `YamlStylesheet`, `remove`/`update` flag round-trips on all model types.
- `TemplateCreatorTests` — added REMOVE, UPDATE, create-when-update-target-missing, and HTML tag injection tests.
- `DataTypeCreatorTests` — added REMOVE, UPDATE (skip-when-exists), create-when-update-target-missing, and null list guard tests.
- `DocumentTypeCreatorTests` — refactored to class-level fixtures; added REMOVE, UPDATE (additive merge), create-when-update-target-missing, and null list guard tests.
- `ContentCreatorTests` — refactored to shared `Build()` helper; added REMOVE, no-throw when remove target missing, UPDATE, and create-when-update-target-missing tests.
- `YamlStartupComposerTests` — added assertion that `StaticAssetCreator` is registered in the DI container.
- `WebProjectConfigTests` — new smoke-test class that parses the web project's live `config/umbraco.yaml` (linked as `fixtures/web-config.yaml`) and verifies structure, counts, aliases, nesting, and absence of accidental REMOVE/UPDATE flags.

### Fixed
- `fixtures/sample.yaml` had an `umbraco:` root wrapper that caused all parser-based integration tests to silently produce empty collections (`YamlParser.ParseYaml` deserialises directly to `UmbracoConfig`, not `YamlRoot`). The wrapper and extra indentation have been removed.
- `fixtures/sample.yaml` used incorrect YAML keys: `editor:` → `editorUiAlias:`, `type:` → `documentType:`, `published:` → `isPublished:`, `values:` → `properties:`. All keys now match the `[YamlMember]` annotations in the model.
- `IntegrationTests` count assertions updated from 2 to 4 DataTypes to reflect the addition of `update:true` and `remove:true` fixture entries; config assertion offset corrected from `DataTypes[0]` to `DataTypes[2]`.

---

## [1.0.1] - 2025-04-xx

### Added
- Initial public release on NuGet (`SplatDev.Umbraco.Plugins.Yaml2Schema`).
- YAML-driven bootstrapping of Umbraco entities on application startup via `INotificationAsyncHandler<UmbracoApplicationStartedNotification>`.
- `YamlParser` — deserialises a YAML file into `UmbracoConfig` using YamlDotNet with camelCase naming and unmatched-property tolerance.
- `DataTypeCreator` — creates Data Types from the `dataTypes` YAML section; skips if a Data Type with the same editor alias already exists.
- `DocumentTypeCreator` — creates Document Types (Content Types) including tabs and properties; resolves Data Type references by name.
- `TemplateCreator` — creates Razor templates under `Views/`; generates a default `.cshtml` scaffold with `@inherits UmbracoViewPage`.
- `ContentCreator` — creates content nodes recursively; supports property value assignment, sort order, and immediate publishing.
- `YamlStartupComposer` — `IComposer` that registers all services as scoped and hooks the startup handler.
- Duplicate alias detection within a YAML run for all entity types.
- Full unit test suite (`DataTypeCreatorTests`, `DocumentTypeCreatorTests`, `TemplateCreatorTests`, `ContentCreatorTests`, `YamlParserTests`, `YamlModelsTests`, `YamlStartupComposerTests`, `IntegrationTests`).

---

## [1.0.0] - 2025-04-xx

### Added
- Initial prototype / internal release.
