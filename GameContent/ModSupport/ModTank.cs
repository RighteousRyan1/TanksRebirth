using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Systems.AI;
using TanksRebirth.GameContent.Systems.TankSystem;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Framework.Interfaces;
using TanksRebirth.Localization;

namespace TanksRebirth.GameContent.ModSupport;
// events will be tied to the virtual methods like OnTargetsSpotted when a modded tank is spawned.

// what can probably be done:
// 1) do checks from AITank to see what tanks exist are modded tanks.
// 2) see if those tanks fit the bill for a certain overridden method (i.e: OnTargetsSpotted), then subscribe said methods to an event.
// 3) invoke per-ModTank type, like a modded X tank will only invoke Y method within the X ModTank class.
#pragma warning disable CS8618
/// <summary>
/// Create your own tank for this game!
/// </summary>
public class ModTank : ILoadable, IModContent {
    private List<OggMusic> _music;
    private Texture2D? _texture;

    /// <summary>The <see cref="TanksMod"/> that this <see cref="ModTank"/> is a part of.</summary>
    public TanksMod Mod { get; set; }
    /// <summary>The AI Tier ID for the modded tank.</summary>
    public int Type { get; set; }
    /// <summary>The path to the texture of this <see cref="ModTank"/>. Can be either the PNG or JPG image format.</summary>
    public virtual string Texture { get; internal set; }

    public AITank AITank { get; internal set; }
    /// <summary>Should include the name of the tank type without 'Tank' after it. 
    /// <para>
    /// i.e: a "Brown Tank" would just be called "Brown"
    /// </para>
    /// The English localized name will be the name that gets added to the game internally.
    /// </summary>
    public virtual LocalizedString Name { get; internal set; }
    /// <summary>The color to associate with this <see cref="ModTank"/>. This color will be used for the "Hit!" graphics, the post-game
    /// results screen, and the color of the tank chunks when the tank is destroyed. Defaults to <see cref="Color.Black"/>.</summary>
    public virtual Color AssociatedColor => Color.Black;
    /// <summary>Whether or not this <see cref="ModTank"/> has a song associated with it. Defaults to false.</summary>
    public virtual bool HasSong => false;
    /// <summary>The amount of songs given for this tank. For example, the Green tank has 4 songs, which depending on the number of tanks in-game.<para>
    /// If this value is greater than 1, your song files must be appended with "1", "2", "3", and so on, depending on the amount of songs.</para>
    /// i.e: The Green tank has "green1", "green2", "green3" and "green4", but since the brown tank has one song, it simply is named "brown".</summary>
    public virtual int Songs => 0;
    /// <summary>Initialize what you want alongside the loading of your modded tank.</summary>
    public virtual void OnLoad() { }
    /// <summary>Manually unload things that may not be automatically unloaded by the game.</summary>
    public virtual void OnUnload() { }
    //public virtual void TargetsSpotted(List<Tank> tanksSpotted) { }
    /// <summary>Change things about the tank when it is spawned in the world. Be absolutely sure to call <c>base.PostApplyDefaults(AITank)</c>
    /// to ensure that your modded tank's texture is placed onto any spawning tanks that are this <see cref="ModTank"/>.</summary>
    public virtual void PostApplyDefaults() {
        if (_texture is null || Texture is null)
            return;
        AITank.SwapTankTexture(_texture);
    }
    /// <summary>Called when your tank takes damage.</summary>
    /// <param name="destroy">Whether or not the tank will be destroyed upon taking this damage.</param>
    /// <param name="context">The context under which this tank took damage.</param>
    public virtual void TakeDamage(bool destroy, ITankHurtContext context) { }
    /// <summary>Called when this tank detects danger.
    /// <br></br>Use <see cref="AITank.NearbyDangers"/> to access dangerous objects near the tank.</summary>
    public virtual void DangerDetected() { }
    /// <summary>Called when this tank shoots.</summary>
    /// <param name="shell">The shell which was shot.</param>
    public virtual void Shoot(Shell shell) { }
    /// <summary>Called when this tank lays a mine.</summary>
    /// <param name="mine">The mine that was laid</param>
    public virtual void LayMine(Mine mine) { }
    /// <summary>Called before the update cycle of a tank.</summary>
    public virtual void PreUpdate() { }
    /// <summary>Called after the update cycle of a tank.</summary>
    public virtual void PostUpdate() { }
    /// <summary>Custom code you wish to execute for AI. This method can be used to write your own tank behaviors.
    /// <br></br>Metadata will STILL be updated if returned false. (i.e: <see cref="AITank.NearbyDangers"/>, <see cref="AITank.TanksNearMineAwareness"/>, etc)
    /// <br></br>Return true if you wish to keep standard tank behavior, return false if you wish to not use it.</summary>
    public virtual bool CustomAI() => true;
    // Pre/Post render soon...

    internal static int unloadOffset = 0;
    internal void Unload() {
        var name = Name.GetLocalizedString(LangCode.English)!;
        // if more than one mod has a modded tank, the game unloads that, and indexes are not adjusted... unloadOffset fixes that
        TankID.Collection.TryRemove(Type - unloadOffset);
        AITank.TankDestructionColors.Remove(Type);
        if (!HasSong)
            TankMusicSystem.TierExclusionRule_DoesntHaveSong.Remove(Type); 
        for (int i = 0; i < _music.Count; i++) {
            _music[i].Stop();
            _music[i].BackingAudio.Dispose();
            TankMusicSystem.Audio.Remove($"{name.ToLower()}{i + 1}");
        }
        Tank.Assets.Remove("tank_" + name.ToLower());
    }

    internal void Load() {
        _music = [];

        var name = Name.GetLocalizedString(LangCode.English)!;

        Type = TankID.Collection.ForcefullyInsert(name);
        TankMusicSystem.MaxSongNumPerTank[Type] = Songs;
        AITank.TankDestructionColors[Type] = AssociatedColor;
        if (Songs > 1) {
            for (int i = 0; i < Songs; i++) {
                var fileName = $"{name.ToLower()}{i + 1}";
                var oggMusic = new OggMusic(name.ToLower(), $"{Mod.ModPath}/{Mod.MusicFolder}/{fileName}.ogg", 1f);
                _music.Add(oggMusic);
                TankMusicSystem.Audio.Add(fileName, oggMusic);
            }
        } else if (Songs == 1) {
            var fileName = $"{Mod.ModPath}/{Mod.MusicFolder}/{name.ToLower()}.ogg";
            var oggMusic = new OggMusic(name.ToLower(), fileName, 1f);
            _music.Add(oggMusic);
            // why was the first parameter "fileName" before instead of name.ToLower()?
            TankMusicSystem.Audio.Add(name.ToLower(), oggMusic);
        } else if (!HasSong) {
            TankMusicSystem.TierExclusionRule_DoesntHaveSong.Add(Type);
        }

        _texture = Mod.ImportAsset<Texture2D>(Texture);
        Tank.Assets["tank_" + name.ToLower()] = _texture;
    }

    internal virtual ModTank Clone() => (ModTank)MemberwiseClone();
}