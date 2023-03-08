using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.IO
{

    public struct JsonHandler<T>
    {
        public readonly string JsonPath;
        public readonly string JsonDir;

        public T Object;
        
        public JsonHandler(T type, string path)
        {
            Object = type;
            JsonPath = path;

            JsonDir = JsonPath.Remove(JsonPath.Length - Path.GetFileName(JsonPath).Length);
        }

        public string Serialize(JsonSerializerOptions options, bool writeToFile = false) {
            var serialized = JsonSerializer.Serialize(Object, options);

            if (!writeToFile) return serialized;
    
            if (!string.IsNullOrEmpty(JsonDir))
                Directory.CreateDirectory(JsonDir);

            File.WriteAllText(JsonPath, serialized);
            return serialized;
        }

        public T Deserialize() {
            // The file doesn't exist, create it and write the default value for the time in it to avoid crashes in the future.
            if (!File.Exists(JsonPath)) {
                File.Create(JsonPath);
                File.WriteAllText(JsonPath, JsonSerializer.Serialize<T>(default));
                return default;
            }

            var asDeserialized = JsonSerializer.Deserialize<T>(File.ReadAllText(JsonPath));
            Object = asDeserialized;
            return asDeserialized;
        }

        public string ReadDeserialized() {
            StringBuilder builder = new("\nDeserialized: {\n");
            var instanceProperties = Object.GetType().GetProperties();
            
            for (var i = 0; i < instanceProperties.Length; i++) {
                var propInfo = instanceProperties[i];

                builder.Append("  \"").Append(propInfo.Name).Append("\": ").Append(propInfo.GetValue(Object));
                
                if (i < instanceProperties.Length - 1)
                    builder.Append(",\n");
                else
                    builder.Append('}');
                    
            }

            return builder.ToString();
        }
    }
}
