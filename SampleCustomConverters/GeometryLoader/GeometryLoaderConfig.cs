using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using DB = Autodesk.Revit.DB;

namespace Elements.Conversion.Revit
{
    public class GeometryLoaderConfig
    {
        private const string configRelativePath = ".hypar\\dsConfig.json";
        private GeometryLoaderConfig() { }
        [JsonProperty("ignored_element_types")]
        public List<string> IgnoredElementTypes { get; set; } = new List<string>();

        [JsonProperty("discriminator_to_category")]
        public Dictionary<string, string> DiscriminatorToCategory { get; set; } = new Dictionary<string, string>();

        private static GeometryLoaderConfig _settings = null;
        private static GeometryLoaderConfig settings
        {
            get
            {
                if (_settings == null)
                {
                    SetSettingsSingleton();
                }
                return _settings;
            }
        }

        public static DB.BuiltInCategory GetBuiltInCategory(Element hyparElement)
        {
            if (hyparElement is ElementInstance instance)
            {
                if (instance.BaseDefinition.AdditionalProperties.TryGetValue("discriminator", out var discriminator))
                {
                    return settings.CategoryFromDiscriminator(discriminator as string);
                }
                else
                {
                    return DB.BuiltInCategory.OST_GenericModel;
                }
            }
            else
            {
                if (hyparElement.AdditionalProperties.TryGetValue("discriminator", out var discriminator))
                {
                    return settings.CategoryFromDiscriminator(discriminator as string);
                }
                else
                {
                    return DB.BuiltInCategory.OST_GenericModel;
                }

            }
        }

        private Dictionary<string, DB.BuiltInCategory> categoryCache = new Dictionary<string, DB.BuiltInCategory>();
        private DB.BuiltInCategory CategoryFromDiscriminator(string discriminatorString)
        {

            if (discriminatorString == null)
            {
                return DB.BuiltInCategory.OST_GenericModel;
            }

            if (this.categoryCache.TryGetValue(discriminatorString, out var builtInCategory))
            {
                return builtInCategory;
            }
            else
            {
                if (!this.DiscriminatorToCategory.TryGetValue(discriminatorString, out var categoryString))
                {
                    categoryString = "";
                }
                if (Enum.TryParse<DB.BuiltInCategory>(categoryString, out var parsedCategory))
                {
                    this.categoryCache[discriminatorString] = parsedCategory;
                }
                else if (Enum.TryParse<DB.BuiltInCategory>("OST_" + categoryString, out var parsedCategory2))
                {
                    this.categoryCache[discriminatorString] = parsedCategory2;
                }
                else
                {
                    this.categoryCache[discriminatorString] = DB.BuiltInCategory.OST_GenericModel;
                }
                return this.categoryCache[discriminatorString];
            }
        }

        public static bool ShouldDirectShapeObject(Element hyparElement)
        {
            if (hyparElement.AdditionalProperties.TryGetValue("discriminator", out var discriminator))
            {
                if (discriminator is string discriminatorName)
                {
                    return !settings.IgnoredElementTypes.Contains(discriminatorName);
                }
            }
            return true;
        }

        private static void SetSettingsSingleton()
        {
            if (!File.Exists(LoadContext.ModelPath))
            {
                _settings = new GeometryLoaderConfig();
                return;
            }
            var configPath = Path.Combine(Path.GetDirectoryName(LoadContext.ModelPath), configRelativePath);
            if (!File.Exists(configPath))
            {
                _settings = new GeometryLoaderConfig();
            }
            else
            {
                var file = File.ReadAllText(configPath);
                try
                {
                    var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<GeometryLoaderConfig>(file);
                    if (settings != null)
                    {
                        _settings = settings;
                    }
                }
                catch
                {
                    _settings = new GeometryLoaderConfig();
                }

            }
        }
    }
}