using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Internals;

public class TextureQueueEntry {
    public string TexturePath { get; set; }
    public MemoryStream? ContentBuffer { get; set; }
    public bool IsLoadedInMemory { get; set; }
    public bool IsLoadedInGraphicsDevice { get; set; }
    public Task? LoadTask { get; set; }
    public TexturePreloadSettings PreloadSettings { get; set; }
    public Texture2D? Texture { get; set; }
}

public struct TexturePreloadSettings : IPreloadSettings {
    public bool Premultiply { get; set; }

    public TexturePreloadSettings(bool premultiply) {
        this.Premultiply = premultiply;
    }
}

public class TextureQueue {
    private List<Task> _taskList = new();
    private Dictionary<string, TextureQueueEntry> _textureQueue = new();

    public Texture2D? GetTextureFromPath(string path) {
        if (!_textureQueue.TryGetValue(path, out var entry))
            return null;

        if (entry.IsLoadedInGraphicsDevice)
            return entry.Texture;

        /*
         *  We must assure we are called from the main thread; as this function will implicitly load the texture into the GraphicsDevice should it not be loaded already.
         *  It can be called out of sync once the texture is preloaded, although that makes little sense.
         */

        if (!RuntimeData.IsMainThread)
            throw new InvalidOperationException("TextureQueue.GetTextureFromPath() can only be called from the main thread.");

        if (!entry.IsLoadedInMemory)
            entry.LoadTask?.GetAwaiter().GetResult();

        _taskList.Remove(entry.LoadTask!);

        entry.Texture = Texture2D.FromStream(TankGame.Instance.GraphicsDevice, entry.ContentBuffer);

        entry.ContentBuffer?.Dispose();

        entry.IsLoadedInGraphicsDevice = true;

        if (!entry.PreloadSettings.Premultiply)
            return entry.Texture;

        var texture = entry.Texture;
        ColorUtils.FromPremultiplied(ref texture);
        entry.Texture = texture;

        return entry.Texture;
    }

    public void PreLoadTexture(string path, IPreloadSettings preloadSettings) {
        if (!typeof(TexturePreloadSettings).TypeHandle.Equals(preloadSettings.GetType().TypeHandle))
            throw new Exception($"Cannot preload texture: {nameof(preloadSettings)} must be of type {nameof(TexturePreloadSettings)}.");
        /*
         *  We can preload textures into memory, which will skip the IO part.
         *  however when uploading graphics to the GPU we must be synchronized with the Rendering Thread.
         *  this is why this class will manage pre-loading them into memory, skipping the hefty locking IO
         *  and then dispatch with the main thread to load them into the GPU.
         */

        var entry = new TextureQueueEntry {
            TexturePath = path,
            IsLoadedInMemory = false,
            IsLoadedInGraphicsDevice = false,
            Texture = null,
            PreloadSettings = (TexturePreloadSettings)preloadSettings
        };

        entry.LoadTask = Task.Run(async () => {
            var hFile = File.OpenRead(path);
            entry.ContentBuffer = new MemoryStream((int)hFile.Length);
            await hFile.CopyToAsync(entry.ContentBuffer);
            await hFile.DisposeAsync();
            entry.IsLoadedInMemory = true;
        });

        _taskList.Add(entry.LoadTask);

        _textureQueue[path] = entry;
    }

    public Task AssureEverythingIsPreloaded() {
        return Task.WhenAll(this._taskList);
    }
}