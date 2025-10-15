using System.IO;
using TanksRebirth.Internals;

namespace TanksRebirth.GameContent.Globals.Assets;

#pragma warning disable
public class Resource<T> where T : class {
    // add ContentManager property... maybe.

    /// <summary>The name of the resource.</summary>
    public string ResourceName { get; set; }
    /// <summary>The local path to the resource.</summary>
    public string ResourcePath { get; set; }
    /// <summary>Whether or not the resource has been loaded into memory.</summary>
    public bool IsLoaded { get; private set; }

    T _asset;
    /// <summary>The asset, if loaded.</summary>
    public T Asset {
        get {
            if (!IsLoaded)
                Load();
            return _asset;
        }
        private set => _asset = value;
    }
    public Resource(string path, string name, bool autoLoad = true) {
        ResourcePath = path;
        ResourceName = name;

        if (autoLoad)
            Load();
    }
    /// <summary>Duplicates the data of the given resource, and returns said duplicate.</summary>
    public T Duplicate() => GameResources.GetRawGameAsset<T>(Path.Combine(ResourcePath, ResourceName));
    /// <summary>Sets 'Asset' to its proper value.</summary>
    public void Load() {
        Asset = GameResources.GetGameResource<T>(Path.Combine(ResourcePath, ResourceName));
        IsLoaded = true;
    }
}