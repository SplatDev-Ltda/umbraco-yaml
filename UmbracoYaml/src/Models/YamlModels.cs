using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace UmbracoYaml.Models
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

        [YamlMember(Alias = "templates")]
        public List<YamlTemplate> Templates { get; set; } = new();

        [YamlMember(Alias = "content")]
        public List<YamlContent> Content { get; set; } = new();
    }

    public class YamlDataType
    {
        [YamlMember(Alias = "alias")]
        public string Alias { get; set; }

        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [YamlMember(Alias = "editor")]
        public string Editor { get; set; }

        [YamlMember(Alias = "config")]
        public Dictionary<string, object> Config { get; set; } = new();
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
    }

    public class YamlContent
    {
        [YamlMember(Alias = "alias")]
        public string Alias { get; set; }

        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [YamlMember(Alias = "type")]
        public string Type { get; set; }

        [YamlMember(Alias = "published")]
        public bool Published { get; set; } = false;

        [YamlMember(Alias = "sortOrder")]
        public int SortOrder { get; set; } = 0;

        [YamlMember(Alias = "values")]
        public Dictionary<string, object> Values { get; set; } = new();

        [YamlMember(Alias = "children")]
        public List<YamlContent> Children { get; set; } = new();
    }
}
