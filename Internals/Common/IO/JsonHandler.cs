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
            if (writeToFile) {
                if (!string.IsNullOrEmpty(JsonDir))
                    Directory.CreateDirectory(JsonDir);
                File.WriteAllText(JsonPath, serialized);
            }
            return serialized;
        }

        public T Deserialize()
        {
            if (!File.Exists(JsonPath))
                File.Create(JsonPath);
            var asDeserialized = JsonSerializer.Deserialize<T>(File.ReadAllText(JsonPath));
            Object = asDeserialized;
            return asDeserialized;
        }

        public string ReadDeserialized()
        {
            using StreamReader reader = File.OpenText(JsonPath);

            var deserialized = JsonSerializer.Deserialize<T>(File.ReadAllText(JsonPath));

            var def = "";
            var i = 0;
            def += "\nDeserialized: {\n";
            foreach (var fld in Object.GetType().GetProperties())
            {
                if (i < Object.GetType().GetProperties().Length - 1)
                    def += $"  \"{fld.Name}\": {fld.GetValue(Object)},\n";
                else
                    def += $"  \"{fld.Name}\": {fld.GetValue(Object)}\n";
                i++;
            }
            def += "}";
            return def;
        }
    }
}
