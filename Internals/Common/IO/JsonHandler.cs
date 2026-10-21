using System.IO;
using System.Text.Json;

namespace TanksRebirth.Internals.Common.IO;

public struct JsonHandler<T> {
    public readonly string JsonPath, JsonDir;
    public T Object;

    public JsonHandler(T type, string path) {
        Object = type;
        JsonPath = path;

        JsonDir = JsonPath[..^Path.GetFileName(JsonPath).Length];
    }

    public readonly string Serialize(JsonSerializerOptions options, bool writeToFile = false) {
        var serialized = JsonSerializer.Serialize(Object, options);
        if (writeToFile) {
            if (!string.IsNullOrEmpty(JsonDir))
                Directory.CreateDirectory(JsonDir);
            File.WriteAllText(JsonPath, serialized);
        }
        return serialized;
    }

    public T Deserialize() {
        // how did i never notice that File.Create just happens. with no recourse.... world's best progarmmer!!!
        if (!File.Exists(JsonPath)) File.WriteAllText(JsonPath, string.Empty);
        var asDeserialized = JsonSerializer.Deserialize<T>(File.ReadAllText(JsonPath))!;
        Object = asDeserialized;
        return asDeserialized;
    }

    public readonly string ReadDeserialized() {
        using StreamReader reader = File.OpenText(JsonPath);

        var deserialized = JsonSerializer.Deserialize<T>(File.ReadAllText(JsonPath));

        var def = "";
        var i = 0;
        def += "\nDeserialized: {\n";
        foreach (var fld in Object.GetType().GetProperties()) {
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