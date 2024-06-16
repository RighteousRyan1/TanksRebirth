using System.IO;
using System.Reflection;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework.Interfaces;

namespace TanksRebirth.GameContent.ModSupport;

/// <summary>A class that contains an entrypoint into the game.</summary>
public abstract class TanksMod : ILoadable {
    /// <summary>The display name of this mod. Generally named the internal name, but split by PascalCase.</summary>
    public virtual string Name { get; internal set; } = "";
    /// <summary>The internal name of this mod. This is the name of the project the mod was built in.</summary>
    public string InternalName { get; internal set; } = "";
    public string ModPath => Path.Combine(TankGame.SaveDirectory, "Mods", InternalName);
    /// <summary>The place where music for tanks is loaded from in your mod.</summary>
    public string MusicFolder { get; } = "Music";
    /// <summary>
    /// This method is called when your mod is loaded.<para></para>
    /// Use this method to initialize hooking, add things to your mod, load content, etc.
    /// </summary>
    public abstract void OnLoad();
    /// <summary>
    /// This method is called when your mod is unloaded.<para></para>
    /// BE SURE to remove hooking, unsubscribe events, unload assets, and everything else that may remain in memory otherwise when your mod unloads, unused memory may remain.
    /// </summary>
    public abstract void OnUnload();
    /// <summary>
    /// Import an asset from your mod's location. <para></para>
    /// Things like shaders (FX) and models (FBX or OBJ) MUST be compiled before being loaded, unfortunately. Still searching for a better way to handle that stuff.
    /// Textures can be loaded via mulitple formats, including PNG and JPG.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    public T ImportAsset<T>(string path) where T : class {
        var defaultContentPath = Path.Combine(TankGame.GameDir, TankGame.Instance.Content.RootDirectory);
        var modContentPath = Path.Combine(TankGame.SaveDirectory, "Mods", InternalName);

        TankGame.Instance.Content.RootDirectory = modContentPath;

        var asset = GameResources.GetGameResource<T>(path);

        TankGame.Instance.Content.RootDirectory = defaultContentPath;

        return asset;
    }
}