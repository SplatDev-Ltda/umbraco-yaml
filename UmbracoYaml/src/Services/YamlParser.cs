using System;
using System.IO;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using UmbracoYaml.Models;

namespace UmbracoYaml.Services
{
    public class YamlParser
    {
        public YamlRoot ParseYaml(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"YAML file not found: {filePath}");
            }

            try
            {
                var fileContents = File.ReadAllText(filePath);

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                var result = deserializer.Deserialize<YamlRoot>(fileContents);

                return result ?? new YamlRoot { Umbraco = new UmbracoConfig() };
            }
            catch (YamlException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to parse YAML file '{filePath}': {ex.Message}",
                    ex
                );
            }
        }
    }
}
