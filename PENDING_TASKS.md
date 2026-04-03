+ Commit and Push
+ Update nuget package and umbraco marketplace

# Phase 2
+ [x] Add support for including nuget packages (PackageValidator — validates assemblies are loaded, v1.0.19)
+ [x] Add support for custom DataTypes (DataTypeCreator — create/update/remove, v1.0.x)
+ [x] Add support for custom Block List items (contentElementTypeAlias resolution + $type content seeding, v1.0.18)
+ [x] Add support for custom Grid Block Item (contentElementTypeAlias resolution covers BlockGrid, v1.0.18)
+ [x] Add support for custom Media Types (MediaTypeCreator — create/update/remove, v1.0.x)
+ [x] Add support for custom Media Properties (MediaTypeCreator tabs/properties, v1.0.x)
+ [x] Add support for custom Document Types (DocumentTypeCreator — create/update/remove, v1.0.x)
+ [x] Add support for custom Property Editors (PropertyEditorCreator — App_Plugins manifest + JS, valueType fallback in DataTypeCreator, v1.0.19)

# Phase 3 ✅ (v1.0.18)
 + [x] Instead of flat Pilar N - Title / Text properties, use Block List ($type convention in content seeding + contentElementTypeAlias resolution in DataType config)
 + [x] Add media folder creation for organizing media (EnsureFolder helper)
 + [x] Add folder field to each media item
 + [x] Download medias from url and save in media folder into folder set in parameter

 # Phase 4 ✅
- [x] Add support for changing the ModelBuilder output path — `modelsBuilder.outputPath` in YAML writes `Umbraco:CMS:ModelsBuilder:ModelsDirectoryAbsolute` to `appsettings.json` (`ModelsBuilderConfigurator` service).
- [x] Add support for generating `publishedmodels` from the YAML-defined schema — `PublishedModelsGenerator` creates typed C# partial classes (`[PublishedModel]`, `PublishedContentModel`) with property accessors, written to the configured `outputPath`.
- [x] Organize media downloads into a folder structure — `mediaDefaultFolder:` on the `umbraco:` root applies as a section-level default for all `media:` items without their own `folder:`.
- [x] Add support for selecting icon for nested elements in Block List, Block Grid, and Single Block editor configs. *(Already implemented: `icon:` on `documentTypes` entries is applied to element types in DocumentTypeCreator.)*

