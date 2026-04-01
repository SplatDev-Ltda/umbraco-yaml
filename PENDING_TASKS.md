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
