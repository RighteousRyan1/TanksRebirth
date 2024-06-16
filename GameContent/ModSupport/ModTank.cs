using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Systems.AI;
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
/// Create your own tank for this game!<para></para>
/// In order for everything to function properly,
/// </summary>
public class ModTank : ILoadable {
    private List<OggMusic> _music;

    /// <summary>The <see cref="TanksMod"/> that this <see cref="ModTank"/> is a part of.</summary>
    public TanksMod Mod { get; internal set; }
    private Texture2D? _texture;
    /// <summary>The path to the texture of this <see cref="ModTank"/>. Can be either the PNG or JPG image format.</summary>
    public virtual string Texture => string.Empty;

    /// <summary>Should include the name of the tank type without 'Tank' after it. 
    /// <para>
    /// i.e: a "Brown Tank" would just be called "Brown"
    /// </para>
    /// The English localized name will be the name that gets added to the game internally.
    /// </summary>
    public virtual LocalizedString Name => default!;
    /// <summary>The properties that should be given to your tank when it spawns in the world.</summary>
    public TankProperties Properties { get; set; }
    /// <summary>The parameters of the AI that your tank will have when it spawns.</summary>
    public AiParameters AiParameters { get; set; }
    public int Type { get; private set; }
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
    /// <param name="tank">The tank.</param>
    public virtual void PostApplyDefaults(AITank tank) {
        if (_texture is null || Texture is null)
            return;
        tank.SwapTankTexture(_texture);
    }
    /// <summary>Called when your tank takes damage.</summary>
    /// <param name="tank">The tank.</param>
    /// <param name="destroy">Whether or not the tank was destroyed upon taking this damage.</param>
    /// <param name="context">The context under which this tank took damage.</param>
    public virtual void TakeDamage(AITank tank, bool destroy, ITankHurtContext context) { }
    /// <summary>Called when this tank detects danger.</summary>
    /// <param name="tank">The tank.</param>
    /// <param name="danger">The dangerous object.</param>
    public virtual void DangerDetected(AITank tank, IAITankDanger danger) { }
    /// <summary>Called when this tank shoots.</summary>
    /// <param name="tank">The tank.</param>
    /// <param name="shell">The shell which was shot.</param>
    public virtual void Shoot(AITank tank, ref Shell shell) { }
    /// <summary>Called when this tank lays a mine.</summary>
    /// <param name="tank">The tank.</param>
    /// <param name="mine">The mine that was laid</param>
    public virtual void LayMine(AITank tank, ref Mine mine) { }
    /// <summary>Called before the update cycle of a tank.</summary>
    /// <param name="tank">The tank.</param>
    public virtual void PreUpdate(AITank tank) { }
    /// <summary>Called after the update cycle of a tank.</summary>
    /// <param name="tank">The tank.</param>
    public virtual void PostUpdate(AITank tank) { }

    // Pre/Post render soon...

    internal void Unload() {
        var name = Name.GetLocalizedString(LangCode.English);
        // think about what to do here lol.
        TankID.Collection.TryRemove(Type);
        AITank.TankDestructionColors.Remove(Type);
        if (!HasSong)
            TankMusicSystem.TierExclusionRule_DoesntHaveSong.Remove(Type);
        else {
            TankMusicSystem.Audio.Remove(name);
        }
        for (int i = 0; i < _music.Count; i++) {
            _music[i].Stop();
            _music[i].BackingAudio.Dispose();
        }
    }

    internal void Register() {
        var name = Name.GetLocalizedString(LangCode.English);
        _music = new();
        Type = TankID.Collection.ForcefullyInsert(name);
        TankMusicSystem.MaxSongNumPerTank[Type] = Songs;
        AITank.TankDestructionColors[Type] = AssociatedColor;
        if (Songs > 1) {
            for (int i = 0; i < Songs; i++) {
                _music.Add(new OggMusic(name.ToLower(), $"{Mod.ModPath}/{Mod.MusicFolder}/{name.ToLower()}.ogg", 1f));
            }
        } else if (Songs == 1) {
            _music.Add(new OggMusic(name.ToLower(), $"{Mod.ModPath}/{Mod.MusicFolder}/{name.ToLower()}.ogg", 1f));
        } else if (!HasSong) {
            TankMusicSystem.TierExclusionRule_DoesntHaveSong.Add(Type);
        }

        _texture = Mod.ImportAsset<Texture2D>(Texture);

        Properties = new();
        AiParameters = new();

        _music.ForEach(x => TankMusicSystem.Audio.Add(name.ToLower(), x));
    }
}
