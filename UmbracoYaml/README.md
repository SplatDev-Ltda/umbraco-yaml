# Umbraco YAML Plugin

A declarative, Infrastructure-as-Code style plugin for Umbraco 17 that enables programmatic creation of DataTypes, DocumentTypes, Templates, and Content from a YAML configuration file.

## Installation

1. Add this plugin to your Umbraco 17 project
2. Create a `config/umbraco.yaml` file in your project root
3. Restart your Umbraco application
4. The plugin automatically creates all structures defined in the YAML file on startup

## Configuration

Place your YAML configuration file at `config/umbraco.yaml` (configurable in `appsettings.json`):

```json
{
  "UmbracoYaml": {
    "ConfigPath": "config/umbraco.yaml"
  }
}
```

## YAML Schema

See `config/umbraco.yaml` for a complete example.

### DataTypes
```yaml
dataTypes:
  - alias: textString
    name: Text String
    editor: Umbraco.TextBox
    config:
      maxLength: 255
```

### DocumentTypes
```yaml
documentTypes:
  - alias: page
    name: Page
    icon: icon-document
    allowAsRoot: true
    tabs:
      - name: Content
        properties:
          - alias: title
            name: Title
            dataType: textString
            required: true
```

### Templates
```yaml
templates:
  - alias: page
    name: Page
    path: Views/Page.cshtml
    masterTemplate: null
```

### Content
```yaml
content:
  - alias: home
    name: Home
    type: page
    published: true
    values:
      title: Welcome
    children: []
```

## Testing

Run all tests:
```bash
dotnet test
```

## Architecture

- **YamlParser:** Deserializes YAML file
- **DataTypeCreator:** Creates DataTypes
- **DocumentTypeCreator:** Creates DocumentTypes with properties
- **TemplateCreator:** Creates Templates
- **ContentCreator:** Creates and publishes Content
- **YamlStartupComposer:** Orchestrates initialization on Umbraco startup

## License

MIT
