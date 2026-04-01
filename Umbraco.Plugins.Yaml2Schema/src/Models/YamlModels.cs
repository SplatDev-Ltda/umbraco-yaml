using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Umbraco.Plugins.Yaml2Schema.Models
{
    public class YamlRoot
    {
        [YamlMember(Alias = "umbraco")]
        public UmbracoConfig Umbraco { get; set; }
    }

    public class UmbracoConfig
    {
        [YamlMember(Alias = "dataTypes")]
        public List<YamlDataType> DataTypes { get; set; } = new();

        [YamlMember(Alias = "documentTypes")]
        public List<YamlDocumentType> DocumentTypes { get; set; } = new();

        [YamlMember(Alias = "mediaTypes")]
        public List<YamlMediaType> MediaTypes { get; set; } = new();

        [YamlMember(Alias = "templates")]
        public List<YamlTemplate> Templates { get; set; } = new();

        [YamlMember(Alias = "content")]
        public List<YamlContent> Content { get; set; } = new();

        [YamlMember(Alias = "media")]
        public List<YamlMedia> Media { get; set; } = new();

        [YamlMember(Alias = "scripts")]
        public List<YamlScript> Scripts { get; set; } = new();

        [YamlMember(Alias = "stylesheets")]
        public List<YamlStylesheet> Stylesheets { get; set; } = new();

        [YamlMember(Alias = "languages")]
        public List<YamlLanguage> Languages { get; set; } = new();

        [YamlMember(Alias = "dictionaryItems")]
        public List<YamlDictionaryItem> DictionaryItems { get; set; } = new();

        [YamlMember(Alias = "members")]
        public List<YamlMember> Members { get; set; } = new();

        [YamlMember(Alias = "users")]
        public List<YamlUser> Users { get; set; } = new();

        [YamlMember(Alias = "packages")]
        public List<YamlPackage> Packages { get; set; } = new();

        [YamlMember(Alias = "propertyEditors")]
        public List<YamlPropertyEditor> PropertyEditors { get; set; } = new();
    }

    public class YamlDataType
    {
        [YamlMember(Alias = "alias")]
        public string Alias { get; set; }

        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [YamlMember(Alias = "editorUiAlias")]
        public string Editor { get; set; }

        [YamlMember(Alias = "config")]
        public Dictionary<string, object> Config { get; set; } = new();

        /// <summary>
        /// Optional storage type override for custom (frontend-only) property editors that have
        /// no server-side <c>IDataEditor</c> registration. Accepted values: NVARCHAR (default),
        /// NTEXT, TEXT, INT, INTEGER, BIGINT, DECIMAL, DATE.
        /// </summary>
        [YamlMember(Alias = "valueType")]
        public string? ValueType { get; set; }

        [YamlMember(Alias = "remove")]
        public bool Remove { get; set; } = false;

        [YamlMember(Alias = "update")]
        public bool Update { get; set; } = false;
    }

    public class YamlDocumentType
    {
        [YamlMember(Alias = "alias")]
        public string Alias { get; set; }

        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [YamlMember(Alias = "icon")]
        public string Icon { get; set; }

        [YamlMember(Alias = "allowAsRoot")]
        public bool AllowAsRoot { get; set; } = true;

        [YamlMember(Alias = "allowedChildTypes")]
        public List<string> AllowedChildTypes { get; set; } = new();

        [YamlMember(Alias = "tabs")]
        public List<YamlTab> Tabs { get; set; } = new();

        [YamlMember(Alias = "allowedTemplates")]
        public List<string> AllowedTemplates { get; set; } = new();

        [YamlMember(Alias = "defaultTemplate")]
        public string DefaultTemplate { get; set; }

        [YamlMember(Alias = "remove")]
        public bool Remove { get; set; } = false;

        [YamlMember(Alias = "update")]
        public bool Update { get; set; } = false;
    }

    public class YamlTab
    {
        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [YamlMember(Alias = "properties")]
        public List<YamlProperty> Properties { get; set; } = new();
    }

    public class YamlProperty
    {
        [YamlMember(Alias = "alias")]
        public string Alias { get; set; }

        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [YamlMember(Alias = "dataType")]
        public string DataType { get; set; }

        [YamlMember(Alias = "required")]
        public bool Required { get; set; } = false;

        [YamlMember(Alias = "description")]
        public string Description { get; set; }
    }

    public class YamlTemplate
    {
        [YamlMember(Alias = "alias")]
        public string Alias { get; set; }

        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [YamlMember(Alias = "path")]
        public string Path { get; set; }

        [YamlMember(Alias = "masterTemplate")]
        public string MasterTemplate { get; set; }

        /// <summary>
        /// Explicit Razor content for this template. When provided, overrides the auto-generated
        /// scaffold. Use a YAML block scalar (|) for multi-line Razor views.
        /// </summary>
        [YamlMember(Alias = "content")]
        public string? RazorContent { get; set; }

        /// <summary>Paths to JavaScript files (relative to wwwroot) to inject into the generated template.</summary>
        [YamlMember(Alias = "scripts")]
        public List<string> Scripts { get; set; } = new();

        /// <summary>Paths to CSS stylesheets (relative to wwwroot) to inject into the generated template.</summary>
        [YamlMember(Alias = "stylesheets")]
        public List<string> Stylesheets { get; set; } = new();

        [YamlMember(Alias = "remove")]
        public bool Remove { get; set; } = false;

        [YamlMember(Alias = "update")]
        public bool Update { get; set; } = false;
    }

    public class YamlScript
    {
        [YamlMember(Alias = "alias")]
        public string Alias { get; set; }

        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        /// <summary>Output path relative to wwwroot (e.g. "js/site.js").</summary>
        [YamlMember(Alias = "path")]
        public string Path { get; set; }

        [YamlMember(Alias = "content")]
        public string Content { get; set; }

        [YamlMember(Alias = "remove")]
        public bool Remove { get; set; } = false;

        [YamlMember(Alias = "update")]
        public bool Update { get; set; } = false;
    }

    public class YamlStylesheet
    {
        [YamlMember(Alias = "alias")]
        public string Alias { get; set; }

        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        /// <summary>Output path relative to wwwroot (e.g. "css/site.css").</summary>
        [YamlMember(Alias = "path")]
        public string Path { get; set; }

        [YamlMember(Alias = "content")]
        public string Content { get; set; }

        [YamlMember(Alias = "remove")]
        public bool Remove { get; set; } = false;

        [YamlMember(Alias = "update")]
        public bool Update { get; set; } = false;
    }

    public class YamlContent
    {
        [YamlMember(Alias = "alias")]
        public string Alias { get; set; }

        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [YamlMember(Alias = "documentType")]
        public string Type { get; set; }

        [YamlMember(Alias = "isPublished")]
        public bool Published { get; set; } = false;

        [YamlMember(Alias = "sortOrder")]
        public int SortOrder { get; set; } = 0;

        [YamlMember(Alias = "properties")]
        public Dictionary<string, object> Values { get; set; } = new();

        [YamlMember(Alias = "children")]
        public List<YamlContent> Children { get; set; } = new();

        [YamlMember(Alias = "remove")]
        public bool Remove { get; set; } = false;

        [YamlMember(Alias = "update")]
        public bool Update { get; set; } = false;
    }

    // ── Media Type ────────────────────────────────────────────────────────────

    public class YamlMediaType
    {
        [YamlMember(Alias = "alias")]
        public string Alias { get; set; }

        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [YamlMember(Alias = "icon")]
        public string? Icon { get; set; }

        [YamlMember(Alias = "allowedAtRoot")]
        public bool AllowedAtRoot { get; set; } = false;

        [YamlMember(Alias = "tabs")]
        public List<YamlTab> Tabs { get; set; } = new();

        [YamlMember(Alias = "remove")]
        public bool Remove { get; set; } = false;

        [YamlMember(Alias = "update")]
        public bool Update { get; set; } = false;
    }

    // ── Media ─────────────────────────────────────────────────────────────────

    public class YamlMedia
    {
        [YamlMember(Alias = "alias")]
        public string Alias { get; set; }

        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        /// <summary>Alias of the Media Type (e.g. "Image", "File").</summary>
        [YamlMember(Alias = "mediaType")]
        public string MediaType { get; set; }

        /// <summary>
        /// URL to download file content from. When provided, the file is downloaded
        /// and attached as the umbracoFile property value.
        /// </summary>
        [YamlMember(Alias = "url")]
        public string? Url { get; set; }

        /// <summary>
        /// Optional folder path where this media item will be placed (e.g. "Images" or "Images/Partners").
        /// Folders are created automatically if they do not exist.
        /// </summary>
        [YamlMember(Alias = "folder")]
        public string? Folder { get; set; }

        [YamlMember(Alias = "properties")]
        public Dictionary<string, object> Properties { get; set; } = new();

        [YamlMember(Alias = "children")]
        public List<YamlMedia> Children { get; set; } = new();

        [YamlMember(Alias = "remove")]
        public bool Remove { get; set; } = false;

        [YamlMember(Alias = "update")]
        public bool Update { get; set; } = false;
    }

    // ── Language ──────────────────────────────────────────────────────────────

    public class YamlLanguage
    {
        /// <summary>ISO culture code, e.g. "en-US", "fr-FR".</summary>
        [YamlMember(Alias = "isoCode")]
        public string IsoCode { get; set; }

        [YamlMember(Alias = "cultureName")]
        public string? CultureName { get; set; }

        [YamlMember(Alias = "isDefault")]
        public bool IsDefault { get; set; } = false;

        [YamlMember(Alias = "isMandatory")]
        public bool IsMandatory { get; set; } = false;

        [YamlMember(Alias = "remove")]
        public bool Remove { get; set; } = false;

        [YamlMember(Alias = "update")]
        public bool Update { get; set; } = false;
    }

    // ── Dictionary Item ───────────────────────────────────────────────────────

    public class YamlDictionaryItem
    {
        [YamlMember(Alias = "key")]
        public string Key { get; set; }

        /// <summary>Translation values keyed by ISO culture code.</summary>
        [YamlMember(Alias = "translations")]
        public Dictionary<string, string> Translations { get; set; } = new();

        [YamlMember(Alias = "remove")]
        public bool Remove { get; set; } = false;

        [YamlMember(Alias = "update")]
        public bool Update { get; set; } = false;
    }

    // ── Member ────────────────────────────────────────────────────────────────

    public class YamlMember
    {
        [YamlMember(Alias = "alias")]
        public string Alias { get; set; }

        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [YamlMember(Alias = "email")]
        public string Email { get; set; }

        [YamlMember(Alias = "username")]
        public string Username { get; set; }

        [YamlMember(Alias = "password")]
        public string Password { get; set; }

        /// <summary>Alias of the Member Type (defaults to "Member").</summary>
        [YamlMember(Alias = "memberType")]
        public string MemberType { get; set; } = "Member";

        [YamlMember(Alias = "isApproved")]
        public bool IsApproved { get; set; } = true;

        [YamlMember(Alias = "properties")]
        public Dictionary<string, object> Properties { get; set; } = new();

        [YamlMember(Alias = "remove")]
        public bool Remove { get; set; } = false;

        [YamlMember(Alias = "update")]
        public bool Update { get; set; } = false;
    }

    // ── User ──────────────────────────────────────────────────────────────────

    public class YamlUser
    {
        [YamlMember(Alias = "alias")]
        public string Alias { get; set; }

        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [YamlMember(Alias = "email")]
        public string Email { get; set; }

        [YamlMember(Alias = "username")]
        public string Username { get; set; }

        /// <summary>User group aliases to assign (e.g. "admin", "editor").</summary>
        [YamlMember(Alias = "userGroups")]
        public List<string> UserGroups { get; set; } = new();

        [YamlMember(Alias = "remove")]
        public bool Remove { get; set; } = false;

        [YamlMember(Alias = "update")]
        public bool Update { get; set; } = false;
    }

    // ── Package ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Declares a NuGet package that the site depends on. At startup the plugin checks whether
    /// the package assembly is loaded in the current AppDomain and logs a warning (or error if
    /// <c>required: true</c>) if it is missing.
    /// </summary>
    public class YamlPackage
    {
        /// <summary>NuGet package ID (e.g. "Our.Umbraco.Community.SomePlugin").</summary>
        [YamlMember(Alias = "id")]
        public string Id { get; set; }

        /// <summary>Expected version string (informational, e.g. "2.0.0"). Logged if mismatch found.</summary>
        [YamlMember(Alias = "version")]
        public string? Version { get; set; }

        /// <summary>When true, a missing assembly is logged as an error rather than a warning.</summary>
        [YamlMember(Alias = "required")]
        public bool Required { get; set; } = false;

        /// <summary>
        /// Override the assembly name to check when it differs from the NuGet package ID
        /// (e.g. "Our.Umbraco.Community.SomePlugin.Core").
        /// </summary>
        [YamlMember(Alias = "assemblyName")]
        public string? AssemblyName { get; set; }
    }

    // ── PropertyEditor ────────────────────────────────────────────────────────

    /// <summary>
    /// Defines a custom (frontend-only) Umbraco property editor. The plugin writes an
    /// <c>App_Plugins/[folderName]/umbraco-package.json</c> manifest and, when
    /// <c>jsContent</c> is provided, the corresponding JavaScript file.
    /// </summary>
    public class YamlPropertyEditor
    {
        /// <summary>
        /// Schema alias used both as the <c>propertyEditorSchema</c> alias in the manifest
        /// and as the editor alias in DataType definitions (e.g. "My.CustomTextEditor").
        /// </summary>
        [YamlMember(Alias = "alias")]
        public string Alias { get; set; }

        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        /// <summary>Backoffice icon alias (e.g. "icon-code"). Defaults to "icon-code".</summary>
        [YamlMember(Alias = "icon")]
        public string? Icon { get; set; }

        /// <summary>Backoffice group (e.g. "common", "lists"). Defaults to "common".</summary>
        [YamlMember(Alias = "group")]
        public string? Group { get; set; }

        /// <summary>
        /// UI component alias (e.g. "My.PropertyEditorUi.CustomEditor").
        /// Auto-derived as <c>{Alias}.Ui</c> when not specified.
        /// </summary>
        [YamlMember(Alias = "uiAlias")]
        public string? UiAlias { get; set; }

        /// <summary>
        /// App_Plugins sub-folder name. Auto-derived from <c>alias</c> (dots replaced with dashes,
        /// lower-cased) when not specified.
        /// </summary>
        [YamlMember(Alias = "folderName")]
        public string? FolderName { get; set; }

        /// <summary>
        /// URL path to the JavaScript file served from wwwroot
        /// (e.g. "/App_Plugins/my-editor/index.js"). Auto-derived when not specified.
        /// </summary>
        [YamlMember(Alias = "jsPath")]
        public string? JsPath { get; set; }

        /// <summary>Inline JavaScript content written to the JS file (use YAML block scalar |).</summary>
        [YamlMember(Alias = "jsContent")]
        public string? JsContent { get; set; }

        [YamlMember(Alias = "remove")]
        public bool Remove { get; set; } = false;

        [YamlMember(Alias = "update")]
        public bool Update { get; set; } = false;
    }
}
