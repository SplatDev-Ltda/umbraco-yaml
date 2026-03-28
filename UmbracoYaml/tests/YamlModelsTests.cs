using Xunit;
using UmbracoYaml.Models;

namespace UmbracoYaml.Tests
{
    public class YamlModelsTests
    {
        [Fact]
        public void YamlRoot_ShouldDeserializeFromValidYaml()
        {
            var yaml = @"
umbraco:
  dataTypes:
    - alias: textString
      name: Text String
      editor: Umbraco.TextBox
      config:
        maxLength: 255
";
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                .Build();

            var root = deserializer.Deserialize<YamlRoot>(yaml);

            Assert.NotNull(root);
            Assert.NotNull(root.Umbraco);
            Assert.Single(root.Umbraco.DataTypes);
            Assert.Equal("textString", root.Umbraco.DataTypes[0].Alias);
        }

        [Fact]
        public void DocumentType_ShouldAllowProperties()
        {
            var yaml = @"
umbraco:
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
";
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                .Build();

            var root = deserializer.Deserialize<YamlRoot>(yaml);

            Assert.Single(root.Umbraco.DocumentTypes);
            var docType = root.Umbraco.DocumentTypes[0];
            Assert.Equal("page", docType.Alias);
            Assert.Single(docType.Tabs);
            Assert.Single(docType.Tabs[0].Properties);
        }
    }
}
