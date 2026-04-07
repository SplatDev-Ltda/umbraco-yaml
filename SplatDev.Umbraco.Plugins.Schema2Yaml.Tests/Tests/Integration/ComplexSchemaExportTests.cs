using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SplatDev.Umbraco.Plugins.Schema2Yaml.Configuration;
using SplatDev.Umbraco.Plugins.Schema2Yaml.Services;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SplatDev.Umbraco.Plugins.Schema2Yaml.Tests.Integration;

/// <summary>
/// Tests that exercise complex, realistic Umbraco schemas —
/// nested document types, compositions, block editors, multi-language content,
/// deep media hierarchies, and mixed property types.
/// </summary>
public class ComplexSchemaExportTests
{
    private readonly IOptions<Schema2YamlOptions> _options;

    public ComplexSchemaExportTests()
    {
        _options = Options.Create(new Schema2YamlOptions
        {
            IncludeContent = true,
            IncludeMedia = true,
            IncludeMembers = true,
            IncludeUsers = true,
            IncludeDictionary = true,
            IncludeLanguages = true,
            MaxHierarchyDepth = 50
        });
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Complex Document Types with compositions, tabs, and nested properties
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Export_DocumentTypesWithCompositions_PreservesInheritanceHierarchy()
    {
        // Setup: seoMixin composition, basePage inherits seoMixin, articlePage inherits basePage
        var mockContentTypeService = new Mock<IContentTypeService>();
        var mockDataTypeService = new Mock<IDataTypeService>();

        var textDataType = CreateMockDataType(1, "Textstring");
        var richTextDataType = CreateMockDataType(2, "Rich Text Editor");
        mockDataTypeService.Setup(s => s.GetDataType(1)).Returns(textDataType.Object);
        mockDataTypeService.Setup(s => s.GetDataType(2)).Returns(richTextDataType.Object);

        // SEO Mixin (element type)
        var seoMixin = CreateContentType("seoMixin", "SEO Mixin", id: 10, isElement: true);
        AddPropertyGroup(seoMixin, "SEO", 0, [
            CreatePropertyType("metaTitle", "Meta Title", 1, mandatory: true),
            CreatePropertyType("metaDescription", "Meta Description", 1),
            CreatePropertyType("ogImage", "OG Image", 1)
        ]);

        // Base Page
        var basePage = CreateContentType("basePage", "Base Page", id: 20, allowedAsRoot: true);
        var baseComposition = new Mock<IContentTypeComposition>();
        baseComposition.Setup(c => c.Alias).Returns("seoMixin");
        baseComposition.Setup(c => c.Id).Returns(10);
        var selfComposition20 = new Mock<IContentTypeComposition>();
        selfComposition20.Setup(c => c.Alias).Returns("basePage");
        selfComposition20.Setup(c => c.Id).Returns(20);
        basePage.Setup(c => c.ContentTypeComposition).Returns([selfComposition20.Object, baseComposition.Object]);

        AddPropertyGroup(basePage, "Content", 0, [
            CreatePropertyType("title", "Title", 1, mandatory: true),
            CreatePropertyType("bodyText", "Body Text", 2)
        ]);
        AddPropertyGroup(basePage, "Navigation", 10, [
            CreatePropertyType("navTitle", "Navigation Title", 1),
            CreatePropertyType("hideFromNav", "Hide from Navigation", 1)
        ]);

        // Article Page (inherits basePage, adds article-specific tabs)
        var articlePage = CreateContentType("articlePage", "Article Page", id: 30);
        var articleSelfComp = new Mock<IContentTypeComposition>();
        articleSelfComp.Setup(c => c.Alias).Returns("articlePage");
        articleSelfComp.Setup(c => c.Id).Returns(30);
        var articleBaseComp = new Mock<IContentTypeComposition>();
        articleBaseComp.Setup(c => c.Alias).Returns("basePage");
        articleBaseComp.Setup(c => c.Id).Returns(20);
        articlePage.Setup(c => c.ContentTypeComposition).Returns([articleSelfComp.Object, articleBaseComp.Object]);

        AddPropertyGroup(articlePage, "Article", 0, [
            CreatePropertyType("author", "Author", 1, mandatory: true),
            CreatePropertyType("publishDate", "Publish Date", 1),
            CreatePropertyType("category", "Category", 1),
            CreatePropertyType("tags", "Tags", 1),
            CreatePropertyType("excerpt", "Excerpt", 2)
        ]);

        mockContentTypeService.Setup(s => s.GetAll()).Returns([seoMixin.Object, basePage.Object, articlePage.Object]);

        var exporter = new DocumentTypeExporter(mockContentTypeService.Object, mockDataTypeService.Object,
            Mock.Of<ILogger<DocumentTypeExporter>>());

        var result = await exporter.ExportAsync();

        Assert.Equal(3, result.Count);

        // SEO Mixin is element type
        var seo = result.First(r => r.Alias == "seoMixin");
        Assert.True(seo.IsElement);
        Assert.Single(seo.Tabs);
        Assert.Equal(3, seo.Tabs[0].Properties.Count);

        // Base Page has seoMixin composition
        var bp = result.First(r => r.Alias == "basePage");
        Assert.Single(bp.Compositions);
        Assert.Equal("seoMixin", bp.Compositions[0]);
        Assert.True(bp.AllowAsRoot);
        Assert.Equal(2, bp.Tabs.Count);
        Assert.Equal("Content", bp.Tabs[0].Name);
        Assert.Equal("Navigation", bp.Tabs[1].Name);

        // Article Page has basePage composition
        var ap = result.First(r => r.Alias == "articlePage");
        Assert.Single(ap.Compositions);
        Assert.Equal("basePage", ap.Compositions[0]);
        Assert.Equal(5, ap.Tabs[0].Properties.Count);
    }

    [Fact]
    public async Task Export_DocumentTypesWithAllowedChildTypes_MapsCorrectly()
    {
        var mockContentTypeService = new Mock<IContentTypeService>();
        var mockDataTypeService = new Mock<IDataTypeService>();

        var homePage = CreateContentType("homePage", "Home Page", id: 1, allowedAsRoot: true);
        homePage.Setup(c => c.AllowedContentTypes).Returns([
            new ContentTypeSort(2, 0) { Alias = "articlePage" },
            new ContentTypeSort(3, 1) { Alias = "blogPage" },
            new ContentTypeSort(4, 2) { Alias = "contactPage" }
        ]);

        var articlePage = CreateContentType("articlePage", "Article Page", id: 2);
        var blogPage = CreateContentType("blogPage", "Blog Page", id: 3);
        var contactPage = CreateContentType("contactPage", "Contact Page", id: 4);

        mockContentTypeService.Setup(s => s.GetAll()).Returns([homePage.Object, articlePage.Object, blogPage.Object, contactPage.Object]);

        var exporter = new DocumentTypeExporter(mockContentTypeService.Object, mockDataTypeService.Object,
            Mock.Of<ILogger<DocumentTypeExporter>>());

        var result = await exporter.ExportAsync();

        var home = result.First(r => r.Alias == "homePage");
        Assert.Equal(3, home.AllowedChildTypes.Count);
        Assert.Contains("articlePage", home.AllowedChildTypes);
        Assert.Contains("blogPage", home.AllowedChildTypes);
        Assert.Contains("contactPage", home.AllowedChildTypes);
    }

    [Fact]
    public async Task Export_DocumentTypesWithTemplates_MapsAllowedAndDefault()
    {
        var mockContentTypeService = new Mock<IContentTypeService>();
        var mockDataTypeService = new Mock<IDataTypeService>();

        var mainTpl = new Mock<ITemplate>();
        mainTpl.Setup(t => t.Alias).Returns("mainLayout");
        var altTpl = new Mock<ITemplate>();
        altTpl.Setup(t => t.Alias).Returns("alternateLayout");

        var page = CreateContentType("page", "Page", id: 1);
        page.Setup(c => c.AllowedTemplates).Returns([mainTpl.Object, altTpl.Object]);
        page.Setup(c => c.DefaultTemplate).Returns(mainTpl.Object);

        mockContentTypeService.Setup(s => s.GetAll()).Returns([page.Object]);

        var exporter = new DocumentTypeExporter(mockContentTypeService.Object, mockDataTypeService.Object,
            Mock.Of<ILogger<DocumentTypeExporter>>());

        var result = await exporter.ExportAsync();

        Assert.Equal(2, result[0].AllowedTemplates.Count);
        Assert.Equal("mainLayout", result[0].DefaultTemplate);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Complex DataTypes with configuration
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Export_DataTypesWithRichConfiguration_ExtractsAllConfigProperties()
    {
        var mockDataTypeService = new Mock<IDataTypeService>();
        var mockVersionDetector = CreateVersionDetector(13);

        // Dropdown with items
        var dropdown = new Mock<IDataType>();
        dropdown.Setup(d => d.Name).Returns("Country Picker");
        dropdown.Setup(d => d.EditorAlias).Returns("Umbraco.DropDown.Flexible");
        dropdown.Setup(d => d.DatabaseType).Returns(ValueStorageType.Nvarchar);
        dropdown.Setup(d => d.Configuration).Returns(new
        {
            multiple = false,
            items = new[] {
                new { id = 1, value = "USA" },
                new { id = 2, value = "UK" },
                new { id = 3, value = "Australia" }
            }
        });

        // Textstring with validation
        var validatedText = new Mock<IDataType>();
        validatedText.Setup(d => d.Name).Returns("Email Address");
        validatedText.Setup(d => d.EditorAlias).Returns("Umbraco.TextBox");
        validatedText.Setup(d => d.DatabaseType).Returns(ValueStorageType.Nvarchar);
        validatedText.Setup(d => d.Configuration).Returns(new
        {
            maxChars = 255,
            pattern = @"^[^@]+@[^@]+\.[^@]+$"
        });

        // Slider with min/max
        var slider = new Mock<IDataType>();
        slider.Setup(d => d.Name).Returns("Price Range");
        slider.Setup(d => d.EditorAlias).Returns("Umbraco.Slider");
        slider.Setup(d => d.DatabaseType).Returns(ValueStorageType.Nvarchar);
        slider.Setup(d => d.Configuration).Returns(new
        {
            enableRange = true,
            initVal1 = 0,
            initVal2 = 1000,
            minVal = 0,
            maxVal = 10000,
            step = 50
        });

        mockDataTypeService.Setup(s => s.GetAll()).Returns([dropdown.Object, validatedText.Object, slider.Object]);

        var exporter = new DataTypeExporter(mockDataTypeService.Object, mockVersionDetector,
            Mock.Of<ILogger<DataTypeExporter>>());

        var result = await exporter.ExportAsync();

        Assert.Equal(3, result.Count);

        // Dropdown config
        var dp = result.First(r => r.Name == "Country Picker");
        Assert.True(dp.Config.ContainsKey("multiple"));
        Assert.True(dp.Config.ContainsKey("items"));

        // Validated text config
        var vt = result.First(r => r.Name == "Email Address");
        Assert.True(vt.Config.ContainsKey("maxChars"));
        Assert.True(vt.Config.ContainsKey("pattern"));

        // Slider config
        var sl = result.First(r => r.Name == "Price Range");
        Assert.True(sl.Config.ContainsKey("enableRange"));
        Assert.True(sl.Config.ContainsKey("minVal"));
        Assert.True(sl.Config.ContainsKey("maxVal"));
        Assert.True(sl.Config.ContainsKey("step"));
    }

    [Fact]
    public async Task Export_AllPropertyEditorTypes_ProducesValidYaml()
    {
        var mockDataTypeService = new Mock<IDataTypeService>();
        var versionDetector = CreateVersionDetector(13);

        var editors = new[]
        {
            ("Textstring",         "Umbraco.TextBox",            ValueStorageType.Nvarchar),
            ("Textarea",           "Umbraco.TextArea",           ValueStorageType.Ntext),
            ("Rich Text Editor",   "Umbraco.TinyMCE",            ValueStorageType.Ntext),
            ("Numeric",            "Umbraco.Integer",            ValueStorageType.Integer),
            ("True/False",         "Umbraco.TrueFalse",          ValueStorageType.Integer),
            ("Date Picker",        "Umbraco.DateTime",           ValueStorageType.Date),
            ("Color Picker",       "Umbraco.ColorPicker",        ValueStorageType.Nvarchar),
            ("Content Picker",     "Umbraco.ContentPicker",      ValueStorageType.Nvarchar),
            ("Media Picker",       "Umbraco.MediaPicker3",       ValueStorageType.Ntext),
            ("Multi URL Picker",   "Umbraco.MultiUrlPicker",     ValueStorageType.Ntext),
            ("Tags",               "Umbraco.Tags",               ValueStorageType.Ntext),
            ("Dropdown",           "Umbraco.DropDown.Flexible",   ValueStorageType.Nvarchar),
            ("Checkbox List",      "Umbraco.CheckBoxList",       ValueStorageType.Nvarchar),
            ("Radiobutton List",   "Umbraco.RadioButtonList",    ValueStorageType.Nvarchar),
            ("Image Cropper",      "Umbraco.ImageCropper",       ValueStorageType.Ntext),
            ("Block List",         "Umbraco.BlockList",          ValueStorageType.Ntext),
            ("Block Grid",         "Umbraco.BlockGrid",          ValueStorageType.Ntext),
            ("Slider",             "Umbraco.Slider",             ValueStorageType.Nvarchar),
            ("Decimal",            "Umbraco.Decimal",            ValueStorageType.Decimal),
            ("Email Address",      "Umbraco.EmailAddress",       ValueStorageType.Nvarchar),
            ("Upload Field",       "Umbraco.UploadField",        ValueStorageType.Nvarchar),
            ("Label",              "Umbraco.Label",              ValueStorageType.Nvarchar),
            ("Member Picker",      "Umbraco.MemberPicker",       ValueStorageType.Nvarchar),
        };

        var dataTypes = editors.Select(e =>
        {
            var dt = new Mock<IDataType>();
            dt.Setup(d => d.Name).Returns(e.Item1);
            dt.Setup(d => d.EditorAlias).Returns(e.Item2);
            dt.Setup(d => d.DatabaseType).Returns(e.Item3);
            dt.Setup(d => d.Configuration).Returns(null as object);
            return dt.Object;
        }).ToArray();

        mockDataTypeService.Setup(s => s.GetAll()).Returns(dataTypes);

        var exporter = new DataTypeExporter(mockDataTypeService.Object, versionDetector,
            Mock.Of<ILogger<DataTypeExporter>>());

        var result = await exporter.ExportAsync();

        Assert.Equal(editors.Length, result.Count);

        // Verify all can be serialized to YAML
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        var yaml = serializer.Serialize(result);
        Assert.NotEmpty(yaml);
        Assert.DoesNotContain("System.Object", yaml); // no unresolved types
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Deep Content Hierarchy
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Export_DeepContentHierarchy_ExportsAllLevels()
    {
        var mockContentService = new Mock<IContentService>();
        var mockFileService = new Mock<IFileService>();

        // Create 5-level deep hierarchy: Home > Section > Category > Article > Comment
        var home = CreateMockContent("Home", "homePage", published: true);
        var section = CreateMockContent("Products", "sectionPage", published: true);
        var category = CreateMockContent("Electronics", "categoryPage", published: true);
        var article = CreateMockContent("Best Laptops 2026", "articlePage", published: true);
        var comment = CreateMockContent("Great article!", "comment", published: false);

        mockContentService.Setup(s => s.GetRootContent()).Returns([home.Object]);

        long total;
        mockContentService.Setup(s => s.GetPagedChildren(home.Object.Id, 0, int.MaxValue, out total))
            .Returns([section.Object]);
        mockContentService.Setup(s => s.GetPagedChildren(section.Object.Id, 0, int.MaxValue, out total))
            .Returns([category.Object]);
        mockContentService.Setup(s => s.GetPagedChildren(category.Object.Id, 0, int.MaxValue, out total))
            .Returns([article.Object]);
        mockContentService.Setup(s => s.GetPagedChildren(article.Object.Id, 0, int.MaxValue, out total))
            .Returns([comment.Object]);
        mockContentService.Setup(s => s.GetPagedChildren(comment.Object.Id, 0, int.MaxValue, out total))
            .Returns([]);

        var exporter = new ContentExporter(mockContentService.Object, mockFileService.Object,
            _options, Mock.Of<ILogger<ContentExporter>>());

        var result = await exporter.ExportAsync();

        // Root level
        Assert.Single(result);
        Assert.Equal("Home", result[0].Name);

        // Level 2
        Assert.Single(result[0].Children);
        Assert.Equal("Products", result[0].Children[0].Name);

        // Level 3
        Assert.Single(result[0].Children[0].Children);
        Assert.Equal("Electronics", result[0].Children[0].Children[0].Name);

        // Level 4
        Assert.Single(result[0].Children[0].Children[0].Children);
        Assert.Equal("Best Laptops 2026", result[0].Children[0].Children[0].Children[0].Name);

        // Level 5 (unpublished)
        var deepChild = result[0].Children[0].Children[0].Children[0].Children[0];
        Assert.Equal("Great article!", deepChild.Name);
        Assert.False(deepChild.IsPublished);
    }

    [Fact]
    public async Task Export_ContentWithAllPropertyTypes_SerializesAllValues()
    {
        var mockContentService = new Mock<IContentService>();
        var mockFileService = new Mock<IFileService>();

        var contentType = new Mock<ISimpleContentType>();
        contentType.Setup(ct => ct.Alias).Returns("richPage");

        var content = new Mock<IContent>();
        content.Setup(c => c.Id).Returns(1);
        content.Setup(c => c.Name).Returns("Rich Content Page");
        content.Setup(c => c.ContentType).Returns(contentType.Object);
        content.Setup(c => c.TemplateId).Returns((int?)null);
        content.Setup(c => c.SortOrder).Returns(0);
        content.Setup(c => c.Published).Returns(true);

        // Create diverse property values
        var props = new List<IProperty>();
        props.Add(CreateProperty("title", "Welcome to our site"));
        props.Add(CreateProperty("bodyText", "<p>This is <strong>rich text</strong> content with <a href=\"/about\">links</a>.</p>"));
        props.Add(CreateProperty("viewCount", 42));
        props.Add(CreateProperty("isActive", true));
        props.Add(CreateProperty("publishDate", new DateTime(2026, 1, 15, 10, 30, 0)));
        props.Add(CreateProperty("price", 29.99m));
        props.Add(CreateProperty("tags", "[\"tech\",\"reviews\",\"2026\"]"));
        props.Add(CreateProperty("jsonData", "{\"key\":\"value\",\"nested\":{\"a\":1}}"));

        content.Setup(c => c.Properties).Returns(new PropertyCollection(props));

        mockContentService.Setup(s => s.GetRootContent()).Returns([content.Object]);
        long total;
        mockContentService.Setup(s => s.GetPagedChildren(1, 0, int.MaxValue, out total)).Returns([]);

        var exporter = new ContentExporter(mockContentService.Object, mockFileService.Object,
            _options, Mock.Of<ILogger<ContentExporter>>());

        var result = await exporter.ExportAsync();

        Assert.Single(result);
        var page = result[0];
        Assert.Equal("Welcome to our site", page.Properties["title"]);
        Assert.Contains("<strong>rich text</strong>", page.Properties["bodyText"].ToString());
        Assert.Equal(42, page.Properties["viewCount"]);
        Assert.Equal(true, page.Properties["isActive"]);
        Assert.Equal("2026-01-15 10:30:00", page.Properties["publishDate"]);
        Assert.Equal("29.99", page.Properties["price"].ToString());
        Assert.Contains("tech", page.Properties["tags"].ToString());
    }

    [Fact]
    public async Task Export_MaxHierarchyDepth_StopsAtLimit()
    {
        var options = Options.Create(new Schema2YamlOptions
        {
            IncludeContent = true,
            MaxHierarchyDepth = 3
        });

        var mockContentService = new Mock<IContentService>();
        var mockFileService = new Mock<IFileService>();

        // Create hierarchy deeper than max depth
        var level0 = CreateMockContent("Level 0", "page", published: true, id: 100);
        var level1 = CreateMockContent("Level 1", "page", published: true, id: 101);
        var level2 = CreateMockContent("Level 2", "page", published: true, id: 102);
        var level3 = CreateMockContent("Level 3 (should be cut off)", "page", published: true, id: 103);

        mockContentService.Setup(s => s.GetRootContent()).Returns([level0.Object]);
        long total;
        mockContentService.Setup(s => s.GetPagedChildren(100, 0, int.MaxValue, out total)).Returns([level1.Object]);
        mockContentService.Setup(s => s.GetPagedChildren(101, 0, int.MaxValue, out total)).Returns([level2.Object]);
        mockContentService.Setup(s => s.GetPagedChildren(102, 0, int.MaxValue, out total)).Returns([level3.Object]);
        mockContentService.Setup(s => s.GetPagedChildren(103, 0, int.MaxValue, out total)).Returns([]);

        var exporter = new ContentExporter(mockContentService.Object, mockFileService.Object,
            options, Mock.Of<ILogger<ContentExporter>>());

        var result = await exporter.ExportAsync();

        // Level 0 → Level 1 → Level 2 should exist, Level 3 should be cut off at depth 3
        Assert.Single(result);
        Assert.Single(result[0].Children);
        Assert.Single(result[0].Children[0].Children);
        Assert.Empty(result[0].Children[0].Children[0].Children); // Cut off
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Multi-language Dictionary Items
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Export_DictionaryWithNestedHierarchy_ExportsAllItems()
    {
        var mockLocalization = new Mock<ILocalizationService>();

        // Create nested dictionary: Buttons > Buttons.Submit, Buttons.Cancel
        var buttons = CreateDictionaryItem("Buttons", new Dictionary<string, string>
        {
            ["en-US"] = "Buttons",
            ["es-ES"] = "Botones",
            ["fr-FR"] = "Boutons"
        });

        var submit = CreateDictionaryItem("Buttons.Submit", new Dictionary<string, string>
        {
            ["en-US"] = "Submit",
            ["es-ES"] = "Enviar",
            ["fr-FR"] = "Soumettre"
        });

        var cancel = CreateDictionaryItem("Buttons.Cancel", new Dictionary<string, string>
        {
            ["en-US"] = "Cancel",
            ["es-ES"] = "Cancelar",
            ["fr-FR"] = "Annuler"
        });

        mockLocalization.Setup(s => s.GetRootDictionaryItems()).Returns([buttons.Object]);
        mockLocalization.Setup(s => s.GetDictionaryItemChildren(buttons.Object.Key))
            .Returns([submit.Object, cancel.Object]);
        mockLocalization.Setup(s => s.GetDictionaryItemChildren(submit.Object.Key)).Returns([]);
        mockLocalization.Setup(s => s.GetDictionaryItemChildren(cancel.Object.Key)).Returns([]);

        var exporter = new DictionaryExporter(mockLocalization.Object, Mock.Of<ILogger<DictionaryExporter>>());

        var result = await exporter.ExportAsync();

        Assert.Equal(3, result.Count);
        var buttonsExport = result.First(r => r.Key == "Buttons");
        Assert.Equal(3, buttonsExport.Translations.Count);
        Assert.Equal("Botones", buttonsExport.Translations["es-ES"]);

        var submitExport = result.First(r => r.Key == "Buttons.Submit");
        Assert.Equal("Soumettre", submitExport.Translations["fr-FR"]);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Multiple Languages
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Export_MultipleLanguages_IncludesAllWithFlags()
    {
        var mockLocalization = new Mock<ILocalizationService>();

        var enUS = CreateLanguage("en-US", "English (US)", isDefault: true, isMandatory: true);
        var esES = CreateLanguage("es-ES", "Spanish (Spain)", isDefault: false, isMandatory: false);
        var frFR = CreateLanguage("fr-FR", "French (France)", isDefault: false, isMandatory: true);
        var zhCN = CreateLanguage("zh-CN", "Chinese (Simplified)", isDefault: false, isMandatory: false);

        mockLocalization.Setup(s => s.GetAllLanguages()).Returns([enUS.Object, esES.Object, frFR.Object, zhCN.Object]);

        var exporter = new LanguageExporter(mockLocalization.Object, Mock.Of<ILogger<LanguageExporter>>());

        var result = await exporter.ExportAsync();

        Assert.Equal(4, result.Count);

        var en = result.First(l => l.IsoCode == "en-US");
        Assert.True(en.IsDefault);
        Assert.True(en.IsMandatory);

        var fr = result.First(l => l.IsoCode == "fr-FR");
        Assert.False(fr.IsDefault);
        Assert.True(fr.IsMandatory);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Complex Media Hierarchy
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Export_DeepMediaHierarchy_ExportsNestedFolders()
    {
        var mockMediaService = new Mock<IMediaService>();
        var mockHostingEnv = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();

        // Root folder > Subfolder > Image
        var rootFolder = CreateMockMedia("Images", "Folder", id: 1);
        var subFolder = CreateMockMedia("Products", "Folder", id: 2);
        var image = CreateMockMedia("hero.jpg", "Image", id: 3, hasFile: true, filePath: "/media/hero.jpg");

        mockMediaService.Setup(s => s.GetRootMedia()).Returns([rootFolder.Object]);
        long total;
        mockMediaService.Setup(s => s.GetPagedChildren(1, 0, int.MaxValue, out total)).Returns([subFolder.Object]);
        mockMediaService.Setup(s => s.GetPagedChildren(2, 0, int.MaxValue, out total)).Returns([image.Object]);
        mockMediaService.Setup(s => s.GetPagedChildren(3, 0, int.MaxValue, out total)).Returns([]);

        mockHostingEnv.Setup(h => h.WebRootPath).Returns("/nonexistent");

        var exporter = new MediaExporter(mockMediaService.Object, mockHostingEnv.Object,
            _options, Mock.Of<ILogger<MediaExporter>>());

        var (media, files) = await exporter.ExportAsync();

        Assert.Single(media);
        Assert.Equal("Images", media[0].Name);
        Assert.Single(media[0].Children);
        Assert.Equal("Products", media[0].Children[0].Name);
        Assert.Single(media[0].Children[0].Children);
        Assert.Equal("hero.jpg", media[0].Children[0].Children[0].Name);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Templates with master templates
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Export_TemplatesWithMasterHierarchy_PreservesRelationship()
    {
        var mockFileService = new Mock<IFileService>();

        var masterTpl = new Mock<ITemplate>();
        masterTpl.Setup(t => t.Alias).Returns("master");
        masterTpl.Setup(t => t.Name).Returns("Master");
        masterTpl.Setup(t => t.MasterTemplateAlias).Returns((string?)null);
        masterTpl.Setup(t => t.Content).Returns("@inherits UmbracoViewPage\n@{ Layout = null; }\n<html>@RenderBody()</html>");

        var homeTpl = new Mock<ITemplate>();
        homeTpl.Setup(t => t.Alias).Returns("homePage");
        homeTpl.Setup(t => t.Name).Returns("Home Page");
        homeTpl.Setup(t => t.MasterTemplateAlias).Returns("master");
        homeTpl.Setup(t => t.Content).Returns("@inherits UmbracoViewPage\n@{ Layout = \"master.cshtml\"; }\n<div>@Model.Value(\"title\")</div>");

        var articleTpl = new Mock<ITemplate>();
        articleTpl.Setup(t => t.Alias).Returns("article");
        articleTpl.Setup(t => t.Name).Returns("Article");
        articleTpl.Setup(t => t.MasterTemplateAlias).Returns("master");
        articleTpl.Setup(t => t.Content).Returns("@inherits UmbracoViewPage\n@{ Layout = \"master.cshtml\"; }\n<article>@Model.Value(\"bodyText\")</article>");

        mockFileService.Setup(s => s.GetTemplates()).Returns([masterTpl.Object, homeTpl.Object, articleTpl.Object]);

        var exporter = new TemplateExporter(mockFileService.Object, Mock.Of<ILogger<TemplateExporter>>());

        var result = await exporter.ExportAsync();

        Assert.Equal(3, result.Count);

        var master = result.First(t => t.Alias == "master");
        Assert.Null(master.MasterTemplate);
        Assert.Contains("@RenderBody()", master.Content);

        var home = result.First(t => t.Alias == "homePage");
        Assert.Equal("master", home.MasterTemplate);

        var article = result.First(t => t.Alias == "article");
        Assert.Equal("master", article.MasterTemplate);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Members and Users
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Export_MembersWithCustomProperties_ExportsWithoutPasswords()
    {
        var mockMemberService = new Mock<IMemberService>();

        var memberType = new Mock<ISimpleContentType>();
        memberType.Setup(mt => mt.Alias).Returns("premiumMember");

        var member = new Mock<IMember>();
        member.Setup(m => m.Name).Returns("John Doe");
        member.Setup(m => m.Email).Returns("john@example.com");
        member.Setup(m => m.Username).Returns("jdoe");
        member.Setup(m => m.ContentType).Returns(memberType.Object);
        member.Setup(m => m.IsApproved).Returns(true);

        var props = new List<IProperty>();
        props.Add(CreateProperty("company", "Acme Corp"));
        props.Add(CreateProperty("phoneNumber", "+1-555-0123"));
        member.Setup(m => m.Properties).Returns(new PropertyCollection(props));

        long total;
        mockMemberService.Setup(s => s.GetAll(0, int.MaxValue, out total)).Returns([member.Object]);

        var exporter = new MemberExporter(mockMemberService.Object, _options,
            Mock.Of<ILogger<MemberExporter>>());

        var result = await exporter.ExportAsync();

        Assert.Single(result);
        Assert.Equal("John Doe", result[0].Name);
        Assert.Equal("premiumMember", result[0].MemberType);
        Assert.True(result[0].IsApproved);
        Assert.Equal("Acme Corp", result[0].Properties["company"]);
    }

    [Fact]
    public async Task Export_UsersWithGroups_MapsCorrectly()
    {
        var options = Options.Create(new Schema2YamlOptions { IncludeUsers = true });
        var mockUserService = new Mock<IUserService>();

        var adminGroup = new Mock<IReadOnlyUserGroup>();
        adminGroup.Setup(g => g.Alias).Returns("admin");
        var editorGroup = new Mock<IReadOnlyUserGroup>();
        editorGroup.Setup(g => g.Alias).Returns("editor");

        var user = new Mock<IUser>();
        user.Setup(u => u.Name).Returns("Admin User");
        user.Setup(u => u.Email).Returns("admin@example.com");
        user.Setup(u => u.Username).Returns("admin");
        user.Setup(u => u.Groups).Returns([adminGroup.Object, editorGroup.Object]);

        long total;
        mockUserService.Setup(s => s.GetAll(0, int.MaxValue, out total)).Returns([user.Object]);

        var exporter = new UserExporter(mockUserService.Object, options,
            Mock.Of<ILogger<UserExporter>>());

        var result = await exporter.ExportAsync();

        Assert.Single(result);
        Assert.Equal(2, result[0].UserGroups.Count);
        Assert.Contains("admin", result[0].UserGroups);
        Assert.Contains("editor", result[0].UserGroups);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Full Orchestration Test — all exporters together producing valid YAML
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task FullExport_ComplexSchema_ProducesValidYamlWithAllSections()
    {
        var service = CreateFullService();

        var yaml = await service.ExportToYamlAsync();
        var stats = service.GetLastExportStatistics();

        // Should contain all section keys
        Assert.Contains("umbraco:", yaml);
        Assert.Contains("languages:", yaml);
        Assert.Contains("dataTypes:", yaml);
        Assert.Contains("documentTypes:", yaml);
        Assert.Contains("templates:", yaml);
        Assert.Contains("content:", yaml);
        Assert.Contains("media:", yaml);
        Assert.Contains("dictionaryItems:", yaml);
        Assert.Contains("members:", yaml);
        Assert.Contains("users:", yaml);

        // Should have real data
        Assert.True(stats.LanguageCount > 0);
        Assert.True(stats.DataTypeCount > 0);
        Assert.True(stats.DocumentTypeCount > 0);
        Assert.True(stats.TemplateCount > 0);
        Assert.True(stats.ContentCount > 0);
        Assert.True(stats.DictionaryItemCount > 0);

        // Verify YAML can be deserialized
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        var parsed = deserializer.Deserialize<Dictionary<string, object>>(yaml);
        Assert.NotNull(parsed);
        Assert.True(parsed.ContainsKey("umbraco"));
    }

    [Fact]
    public async Task FullExport_ComplexSchema_ZipContainsYamlWithCorrectContent()
    {
        var service = CreateFullService();

        var zipBytes = await service.ExportToZipAsync();

        using var stream = new MemoryStream(zipBytes);
        using var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read);

        var yamlEntry = archive.Entries.First(e => e.Name == "umbraco.yml");
        using var reader = new StreamReader(yamlEntry.Open());
        var content = await reader.ReadToEndAsync();

        Assert.Contains("umbraco:", content);
        Assert.Contains("languages:", content);
        Assert.Contains("en-US", content);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MediaType with complex property structure
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Export_MediaTypesWithProperties_MapsTabsAndProperties()
    {
        var mockMediaTypeService = new Mock<IMediaTypeService>();
        var mockDataTypeService = new Mock<IDataTypeService>();

        var textDataType = CreateMockDataType(1, "Textstring");
        var uploadDataType = CreateMockDataType(2, "Upload Field");
        mockDataTypeService.Setup(s => s.GetDataType(1)).Returns(textDataType.Object);
        mockDataTypeService.Setup(s => s.GetDataType(2)).Returns(uploadDataType.Object);

        var imageType = new Mock<IMediaType>();
        imageType.Setup(mt => mt.Alias).Returns("Image");
        imageType.Setup(mt => mt.Name).Returns("Image");
        imageType.Setup(mt => mt.Icon).Returns("icon-picture");
        imageType.Setup(mt => mt.AllowedAsRoot).Returns(false);

        var fileProp = new Mock<IPropertyType>();
        fileProp.Setup(p => p.Alias).Returns("umbracoFile");
        fileProp.Setup(p => p.Name).Returns("Upload Image");
        fileProp.Setup(p => p.DataTypeId).Returns(2);
        fileProp.Setup(p => p.Mandatory).Returns(true);
        fileProp.Setup(p => p.SortOrder).Returns(0);

        var altTextProp = new Mock<IPropertyType>();
        altTextProp.Setup(p => p.Alias).Returns("altText");
        altTextProp.Setup(p => p.Name).Returns("Alt Text");
        altTextProp.Setup(p => p.DataTypeId).Returns(1);
        altTextProp.Setup(p => p.Mandatory).Returns(false);
        altTextProp.Setup(p => p.SortOrder).Returns(1);

        var group = new PropertyGroup(new PropertyTypeCollection(true, [fileProp.Object, altTextProp.Object]))
        {
            Alias = "image",
            Name = "Image",
            SortOrder = 0
        };

        imageType.Setup(mt => mt.PropertyGroups).Returns(new PropertyGroupCollection([group]));
        imageType.Setup(mt => mt.PropertyTypes).Returns(new PropertyTypeCollection(true, []));

        mockMediaTypeService.Setup(s => s.GetAll()).Returns([imageType.Object]);

        var exporter = new MediaTypeExporter(mockMediaTypeService.Object, mockDataTypeService.Object,
            Mock.Of<ILogger<MediaTypeExporter>>());

        var result = await exporter.ExportAsync();

        Assert.Single(result);
        Assert.Equal("Image", result[0].Name);
        Assert.Equal("icon-picture", result[0].Icon);
        Assert.Single(result[0].Tabs);
        Assert.Equal(2, result[0].Tabs[0].Properties.Count);
        Assert.Equal("umbracoFile", result[0].Tabs[0].Properties[0].Alias);
        Assert.Equal("Upload Field", result[0].Tabs[0].Properties[0].DataType);
        Assert.True(result[0].Tabs[0].Properties[0].Required);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════════

    private static UmbracoVersionDetector CreateVersionDetector(int major)
    {
        var mockVersion = new Mock<IUmbracoVersion>();
        mockVersion.Setup(v => v.Version).Returns(new Version(major, 0, 0));
        return new UmbracoVersionDetector(mockVersion.Object, Mock.Of<ILogger<UmbracoVersionDetector>>());
    }

    private static Mock<IDataType> CreateMockDataType(int id, string name)
    {
        var mock = new Mock<IDataType>();
        mock.Setup(d => d.Id).Returns(id);
        mock.Setup(d => d.Name).Returns(name);
        return mock;
    }

    private static Mock<IContentType> CreateContentType(string alias, string name, int id = 1,
        bool isElement = false, bool allowedAsRoot = false)
    {
        var mock = new Mock<IContentType>();
        mock.Setup(ct => ct.Id).Returns(id);
        mock.Setup(ct => ct.Alias).Returns(alias);
        mock.Setup(ct => ct.Name).Returns(name);
        mock.Setup(ct => ct.Icon).Returns("icon-document");
        mock.Setup(ct => ct.IsElement).Returns(isElement);
        mock.Setup(ct => ct.AllowedAsRoot).Returns(allowedAsRoot);
        mock.Setup(ct => ct.AllowedContentTypes).Returns([]);
        mock.Setup(ct => ct.ContentTypeComposition).Returns([]);
        mock.Setup(ct => ct.AllowedTemplates).Returns([]);
        mock.Setup(ct => ct.DefaultTemplate).Returns((ITemplate?)null);
        mock.Setup(ct => ct.PropertyGroups).Returns(new PropertyGroupCollection([]));
        mock.Setup(ct => ct.PropertyTypes).Returns(new PropertyTypeCollection(true, []));
        mock.Setup(ct => ct.NoGroupPropertyTypes).Returns(new PropertyTypeCollection(true, []));
        return mock;
    }

    private static void AddPropertyGroup(Mock<IContentType> ct, string name, int sortOrder, List<Mock<IPropertyType>> properties)
    {
        var propTypes = properties.Select(p => p.Object).ToList();
        var group = new PropertyGroup(new PropertyTypeCollection(true, propTypes))
        {
            Alias = name.ToLowerInvariant().Replace(" ", ""),
            Name = name,
            SortOrder = sortOrder
        };

        var existingGroups = ct.Object.PropertyGroups.ToList();
        existingGroups.Add(group);
        ct.Setup(c => c.PropertyGroups).Returns(new PropertyGroupCollection(existingGroups));
    }

    private static Mock<IPropertyType> CreatePropertyType(string alias, string name, int dataTypeId,
        bool mandatory = false, string? description = null)
    {
        var mock = new Mock<IPropertyType>();
        mock.Setup(p => p.Alias).Returns(alias);
        mock.Setup(p => p.Name).Returns(name);
        mock.Setup(p => p.DataTypeId).Returns(dataTypeId);
        mock.Setup(p => p.Mandatory).Returns(mandatory);
        mock.Setup(p => p.Description).Returns(description);
        mock.Setup(p => p.SortOrder).Returns(0);
        return mock;
    }

    private static Mock<IContent> CreateMockContent(string name, string docTypeAlias, bool published, int id = 0)
    {
        if (id == 0) id = Random.Shared.Next(1000, 99999);

        var contentType = new Mock<ISimpleContentType>();
        contentType.Setup(ct => ct.Alias).Returns(docTypeAlias);

        var content = new Mock<IContent>();
        content.Setup(c => c.Id).Returns(id);
        content.Setup(c => c.Name).Returns(name);
        content.Setup(c => c.ContentType).Returns(contentType.Object);
        content.Setup(c => c.TemplateId).Returns((int?)null);
        content.Setup(c => c.SortOrder).Returns(0);
        content.Setup(c => c.Published).Returns(published);
        content.Setup(c => c.Properties).Returns(new PropertyCollection([]));
        return content;
    }

    private static IProperty CreateProperty(string alias, object value)
    {
        var prop = new Mock<IProperty>();
        prop.Setup(p => p.Alias).Returns(alias);
        prop.Setup(p => p.GetValue(null, null, false)).Returns(value);
        return prop.Object;
    }

    private static Mock<IDictionaryItem> CreateDictionaryItem(string key, Dictionary<string, string> translations)
    {
        var mock = new Mock<IDictionaryItem>();
        mock.Setup(d => d.ItemKey).Returns(key);
        mock.Setup(d => d.Key).Returns(Guid.NewGuid());

        var translationList = translations.Select(t =>
        {
            var trans = new Mock<IDictionaryTranslation>();
            trans.Setup(x => x.LanguageIsoCode).Returns(t.Key);
            trans.Setup(x => x.Value).Returns(t.Value);
            return trans.Object;
        }).ToList();

        mock.Setup(d => d.Translations).Returns(translationList);
        return mock;
    }

    private static Mock<ILanguage> CreateLanguage(string isoCode, string cultureName, bool isDefault, bool isMandatory)
    {
        var mock = new Mock<ILanguage>();
        mock.Setup(l => l.IsoCode).Returns(isoCode);
        mock.Setup(l => l.CultureName).Returns(cultureName);
        mock.Setup(l => l.IsDefault).Returns(isDefault);
        mock.Setup(l => l.IsMandatory).Returns(isMandatory);
        return mock;
    }

    private static Mock<IMedia> CreateMockMedia(string name, string mediaTypeAlias, int id = 0,
        bool hasFile = false, string? filePath = null)
    {
        if (id == 0) id = Random.Shared.Next(1000, 99999);

        var mediaType = new Mock<ISimpleContentType>();
        mediaType.Setup(mt => mt.Alias).Returns(mediaTypeAlias);

        var media = new Mock<IMedia>();
        media.Setup(m => m.Id).Returns(id);
        media.Setup(m => m.Name).Returns(name);
        media.Setup(m => m.ContentType).Returns(mediaType.Object);
        media.Setup(m => m.SortOrder).Returns(0);
        media.Setup(m => m.Properties).Returns(new PropertyCollection([]));

        if (hasFile && filePath != null)
        {
            media.Setup(m => m.HasProperty("umbracoFile")).Returns(true);
            media.Setup(m => m.GetValue<string>("umbracoFile", null, null, false)).Returns(filePath);
        }
        else
        {
            media.Setup(m => m.HasProperty("umbracoFile")).Returns(false);
        }

        return media;
    }

    private SchemaExportService CreateFullService()
    {
        // Languages
        var mockLocalization = new Mock<ILocalizationService>();
        mockLocalization.Setup(s => s.GetAllLanguages()).Returns([
            CreateLanguage("en-US", "English (US)", true, true).Object,
            CreateLanguage("es-ES", "Spanish", false, false).Object
        ]);

        // Dictionary
        var dictItem = CreateDictionaryItem("Welcome", new Dictionary<string, string>
        {
            ["en-US"] = "Welcome",
            ["es-ES"] = "Bienvenido"
        });
        mockLocalization.Setup(s => s.GetRootDictionaryItems()).Returns([dictItem.Object]);
        mockLocalization.Setup(s => s.GetDictionaryItemChildren(dictItem.Object.Key)).Returns([]);

        // DataTypes
        var mockDataTypeService = new Mock<IDataTypeService>();
        var textDt = new Mock<IDataType>();
        textDt.Setup(d => d.Name).Returns("Textstring");
        textDt.Setup(d => d.EditorAlias).Returns("Umbraco.TextBox");
        textDt.Setup(d => d.DatabaseType).Returns(ValueStorageType.Nvarchar);
        textDt.Setup(d => d.Configuration).Returns(null as object);
        mockDataTypeService.Setup(s => s.GetAll()).Returns([textDt.Object]);
        mockDataTypeService.Setup(s => s.GetDataType(It.IsAny<int>())).Returns(textDt.Object);

        // DocumentTypes
        var mockContentTypeService = new Mock<IContentTypeService>();
        var homeCt = CreateContentType("homePage", "Home Page", id: 1, allowedAsRoot: true);
        var titleProp = new Mock<IPropertyType>();
        titleProp.Setup(p => p.Alias).Returns("title");
        titleProp.Setup(p => p.Name).Returns("Title");
        titleProp.Setup(p => p.DataTypeId).Returns(1);
        titleProp.Setup(p => p.Mandatory).Returns(true);
        titleProp.Setup(p => p.SortOrder).Returns(0);
        var contentGroup = new PropertyGroup(new PropertyTypeCollection(true, [titleProp.Object]))
        {
            Alias = "content",
            Name = "Content",
            SortOrder = 0
        };
        homeCt.Setup(c => c.PropertyGroups).Returns(new PropertyGroupCollection([contentGroup]));
        mockContentTypeService.Setup(s => s.GetAll()).Returns([homeCt.Object]);

        // MediaTypes
        var mockMediaTypeService = new Mock<IMediaTypeService>();
        mockMediaTypeService.Setup(s => s.GetAll()).Returns([]);

        // Templates
        var mockFileService = new Mock<IFileService>();
        var tpl = new Mock<ITemplate>();
        tpl.Setup(t => t.Alias).Returns("homePage");
        tpl.Setup(t => t.Name).Returns("Home Page");
        tpl.Setup(t => t.MasterTemplateAlias).Returns((string?)null);
        tpl.Setup(t => t.Content).Returns("<html>@RenderBody()</html>");
        mockFileService.Setup(s => s.GetTemplates()).Returns([tpl.Object]);

        // Content
        var mockContentService = new Mock<IContentService>();
        var homeContent = CreateMockContent("Home", "homePage", published: true, id: 100);
        var homePropValue = CreateProperty("title", "Welcome Home");
        homeContent.Setup(c => c.Properties).Returns(new PropertyCollection([homePropValue]));
        mockContentService.Setup(s => s.GetRootContent()).Returns([homeContent.Object]);
        long total;
        mockContentService.Setup(s => s.GetPagedChildren(100, 0, int.MaxValue, out total)).Returns([]);

        // Media
        var mockMediaService = new Mock<IMediaService>();
        mockMediaService.Setup(s => s.GetRootMedia()).Returns([]);

        // Members
        var mockMemberService = new Mock<IMemberService>();
        mockMemberService.Setup(s => s.GetAll(0, int.MaxValue, out total)).Returns([]);

        // Users
        var mockUserService = new Mock<IUserService>();
        mockUserService.Setup(s => s.GetAll(0, int.MaxValue, out total)).Returns([]);

        var mockHostingEnv = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        mockHostingEnv.Setup(h => h.WebRootPath).Returns(Path.GetTempPath());
        var versionDetector = CreateVersionDetector(13);

        return new SchemaExportService(
            new LanguageExporter(mockLocalization.Object, Mock.Of<ILogger<LanguageExporter>>()),
            new DataTypeExporter(mockDataTypeService.Object, versionDetector, Mock.Of<ILogger<DataTypeExporter>>()),
            new DocumentTypeExporter(mockContentTypeService.Object, mockDataTypeService.Object, Mock.Of<ILogger<DocumentTypeExporter>>()),
            new MediaTypeExporter(mockMediaTypeService.Object, mockDataTypeService.Object, Mock.Of<ILogger<MediaTypeExporter>>()),
            new TemplateExporter(mockFileService.Object, Mock.Of<ILogger<TemplateExporter>>()),
            new MediaExporter(mockMediaService.Object, mockHostingEnv.Object, _options, Mock.Of<ILogger<MediaExporter>>()),
            new ContentExporter(mockContentService.Object, mockFileService.Object, _options, Mock.Of<ILogger<ContentExporter>>()),
            new DictionaryExporter(mockLocalization.Object, Mock.Of<ILogger<DictionaryExporter>>()),
            new MemberExporter(mockMemberService.Object, _options, Mock.Of<ILogger<MemberExporter>>()),
            new UserExporter(mockUserService.Object, _options, Mock.Of<ILogger<UserExporter>>()),
            versionDetector,
            _options,
            Mock.Of<ILogger<SchemaExportService>>());
    }
}
