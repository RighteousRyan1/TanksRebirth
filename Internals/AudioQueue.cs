using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Framework.Audio.AudioSerializers;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Internals;

public class AudioQueueEntry {
    public string AudioPath { get; set; }
    
    public bool IsLoadedInMemory { get; set; }
    
    public Task? LoadTask { get; set; }
    
    public DeseralizationData? Data { get; set; }
    
    public SoundEffect? SoundEffect { get; set; }
}
public class AudioQueue {
    private OggDeserializer _deserializer = new();
    private List<Task> _taskList=new();
    private Dictionary<string, AudioQueueEntry> _audioQueue = new();

    public SoundEffect? GetSoundEffectFromPath(string path) {
        if (!_audioQueue.TryGetValue(path, out var entry))
            return null;

        if (entry.SoundEffect != null)
            return entry.SoundEffect;
        
        if (!RuntimeData.IsMainThread && entry.SoundEffect == null) 
            throw new InvalidOperationException("AudioQueue.GetOggAudioFromPath() can only be called from the main thread if the audio has not been brought into memory lazily.");

        if (!entry.IsLoadedInMemory) 
            entry.LoadTask?.GetAwaiter().GetResult();

        /*
         *  Decompress and load effect into memory.
         */

        _taskList.Remove(entry.LoadTask!);
        entry.SoundEffect = new SoundEffect(entry.Data!.Value.binaryData, entry.Data!.Value.sampleRate, (AudioChannels)entry.Data!.Value.channelCount);
        entry.Data = null; // clear data reference to free extra buffer.
        return entry.SoundEffect;
    }

    public void PreLoadAudio(string path) {
        var entry = new AudioQueueEntry {
            AudioPath = path,
            IsLoadedInMemory = false,
        };

        entry.LoadTask = Task.Run(async () => {
            entry.Data = await _deserializer.DeserializeAsync(path);
            entry.IsLoadedInMemory = true;
        });
        
        _taskList.Add(entry.LoadTask);

        _audioQueue[path.Replace("\\", "/")] = entry;
    }

    public Task AssureEverythingIsPreloaded() {
        return Task.WhenAll(this._taskList);
    }
}
