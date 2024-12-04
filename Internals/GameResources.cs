using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TanksRebirth.GameContent.ModSupport;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Internals;

public static class GameResources {
    private static Dictionary<string, object> ResourceCache { get; set; } = new();

    private static Dictionary<string, object> QueuedResources { get; set; } = new();

    private static T GetResource<T>(this ContentManager? manager, string name) where T : class {
        if (manager != null) {
            if (ResourceCache.TryGetValue(Path.Combine(manager.RootDirectory, name), out var val) && val is T content)
                return content;
        }
        else if (ResourceCache.TryGetValue(name, out var val) && val is T content)
            return content;

        return LoadResource<T>(manager, name);
    }

    private static T LoadResource<T>(ContentManager? manager, string name) where T : class {
        if (ResourceCache.TryGetValue(name, out var value))
            return (T)value;

        if (typeof(T) == typeof(Texture2D)) {
            // we call this BOXING HELL. anyway.
            var texture = Texture2D.FromFile(TankGame.Instance.GraphicsDevice, name);
            texture.Name = name;

            object result = texture;
            ResourceCache[name] = result;

            return (T)result;
        }

        var loaded = manager.Load<T>(name);

        ResourceCache[name] = loaded;
        return loaded;
    }

    public static T GetGameResource<T>(string name, bool addDotPng = true, bool addContentPrefix = true, bool premultiply = false) where T : class {
        var realResourceName = name + (addDotPng ? ".png" : string.Empty);

        if (ResourceCache.TryGetValue(realResourceName, out var value))
            return (T)value;
        else if (typeof(T) == typeof(Texture2D)) {
            // Bustin' all the bells out the box
            var texture = Texture2D.FromFile(TankGame.Instance.GraphicsDevice, Path.Combine(addContentPrefix ? TankGame.Instance.Content.RootDirectory : string.Empty, realResourceName));
            texture.Name = name;

            object result = texture;
            ResourceCache[realResourceName] = result;

            if (!premultiply)
                return (T)result;

            var refUse = (Texture2D)result;
            ColorUtils.FromPremultiplied(ref refUse);
            result = refUse;
            return (T)result;
        }

        return GetResource<T>(TankGame.Instance.Content, name);
    }

    public static void QueueAsset<T>(string name) {
        if (!QueuedResources.TryGetValue(name, out var val) || val is not T)
            QueuedResources[name] = typeof(T);
    }

    public static void LoadQueuedAssets() {
        Task.Run(() => { }); // rndunfsdauif fd saoidf s
        foreach (var resource in QueuedResources) { }
    }

    public static T GetRawAsset<T>(this ContentManager manager, string assetName) where T : class {
        var t = typeof(ContentManager).GetMethod("ReadAsset", BindingFlags.Instance | BindingFlags.NonPublic);

        var generic = t.MakeGenericMethod(typeof(T)).Invoke(manager, new object[] { assetName, null }) as T;

        return generic;
    }

    public static T GetRawGameAsset<T>(string assetName) where T : class {
        var t = typeof(ContentManager).GetMethod("ReadAsset", BindingFlags.Instance | BindingFlags.NonPublic);

        var generic = t.MakeGenericMethod(typeof(T)).Invoke(TankGame.Instance.Content, new object[] { assetName, null }) as T;

        return generic;

    }
}