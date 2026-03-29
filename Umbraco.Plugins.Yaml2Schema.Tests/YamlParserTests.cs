using Xunit;
using System;
using System.IO;
using Umbraco.Plugins.Yaml2Schema.Services;
using Umbraco.Plugins.Yaml2Schema.Models;
using YamlDotNet.Core;

namespace Umbraco.Plugins.Yaml2Schema.Tests
{
    public class YamlParserTests
    {
        private readonly string _testFixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "fixtures",
            "sample.yaml"
        );

        [Fact]
        public void ParseYaml_ShouldReadAndDeserializeValidFile()
        {
            var parser = new YamlParser();

            var result = parser.ParseYaml(_testFixturePath);

            Assert.NotNull(result);
            Assert.NotNull(result.Umbraco);
            Assert.Equal(2, result.Umbraco.DataTypes.Count);
            Assert.Equal("textString", result.Umbraco.DataTypes[0].Alias);
            Assert.Equal("richText", result.Umbraco.DataTypes[1].Alias);

            Assert.Single(result.Umbraco.DocumentTypes);
            Assert.Equal("page", result.Umbraco.DocumentTypes[0].Alias);
            Assert.Single(result.Umbraco.DocumentTypes[0].Tabs);

            Assert.Single(result.Umbraco.Templates);
            Assert.Equal("masterPage", result.Umbraco.Templates[0].Alias);

            Assert.Single(result.Umbraco.Content);
            Assert.Equal("home", result.Umbraco.Content[0].Alias);
        }

        [Fact]
        public void ParseYaml_ShouldThrowOnMissingFile()
        {
            var parser = new YamlParser();
            var missingFilePath = "/nonexistent/path/to/missing.yaml";

            var exception = Assert.Throws<FileNotFoundException>(() =>
                parser.ParseYaml(missingFilePath)
            );

            Assert.Contains(missingFilePath, exception.Message);
        }

        [Fact]
        public void ParseYaml_ShouldThrowOnInvalidYaml()
        {
            var parser = new YamlParser();
            var invalidYamlPath = Path.Combine(
                AppContext.BaseDirectory,
                "fixtures",
                "invalid.yaml"
            );

            // Create invalid YAML file temporarily
            Directory.CreateDirectory(Path.GetDirectoryName(invalidYamlPath));
            File.WriteAllText(invalidYamlPath, "invalid: yaml: content: [");

            try
            {
                var exception = Assert.Throws<InvalidOperationException>(() =>
                    parser.ParseYaml(invalidYamlPath)
                );

                Assert.Contains("Failed to parse YAML", exception.Message);
            }
            finally
            {
                if (File.Exists(invalidYamlPath))
                {
                    File.Delete(invalidYamlPath);
                }
            }
        }
    }
}
