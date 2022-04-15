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

        public readonly FileInfo JsonInfo;

        public T Object;
        
        public JsonHandler(T type, string path)
        {
            Object = type;
            JsonPath = path;
            JsonInfo = new(path);

            JsonDir = JsonPath.Remove(JsonPath.Length - JsonInfo.Name.Length);
        }

        public string Serialize(JsonSerializerOptions options, bool writeToFile = false)
        {
            var serialized = JsonSerializer.Serialize(Object, options);
            if (writeToFile)
            {
                Directory.CreateDirectory(JsonDir);
                File.WriteAllText(JsonPath, serialized);
            }
            return serialized;
        }

        public T DeserializeAndSet()
        {
            var asDeserialized = JsonSerializer.Deserialize<T>(File.ReadAllText(JsonPath));
            Object = asDeserialized;
            return asDeserialized;
        }

        public string ReadDeserialized<T>()
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
