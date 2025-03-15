using System.IO;
using TanksRebirth.Internals;

namespace TanksRebirth.GameContent.Globals.Assets;

#pragma warning disable
public class Resource<T> where T : class {
    // add ContentManager property... maybe.
    public string ResourceName { get; set; }
    public string ResourcePath { get; set; }
    public bool IsLoaded { get; private set; }
    public T Asset { get; private set; }
    public Resource(string path, string name, bool autoLoad = true) {
        ResourcePath = path;
        ResourceName = name;
        Load();
    }

    public T Duplicate() => GameResources.GetRawGameAsset<T>(Path.Combine(ResourcePath, ResourceName));
    public void Load() {
        Asset = GameResources.GetGameResource<T>(Path.Combine(ResourcePath, ResourceName));
        IsLoaded = true;
    }
}