using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.GameContent.ID;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Framework.Interfaces;
using TanksRebirth.Localization;

namespace TanksRebirth.GameContent.ModSupport;

public class ModShell : ILoadable, IModContent {
    /// <summary>The <see cref="TanksMod"/> that this <see cref="ModShell"/> is a part of.</summary>
    public TanksMod Mod { get; set; }
    public Shell Shell { get; set; }
    public int Type { get; set; }

    private Texture2D? _texture;
    /// <summary>If not assigned manually, the texture will default to the regular, (mostly) white shell texture.</summary>
    public virtual string Texture => string.Empty;
    /// <summary>The sound for when this shell is shot. Utilize Path.Combine(ModPath, ...)</summary>
    public virtual string ShootSound => string.Empty;
    /// <summary>The sound for while this shell is flying. Utilize Path.Combine(ModPath, ...)</summary>
    public virtual string TrailSound => string.Empty;
    public virtual LocalizedString Name => new([]);

    /// <summary>Initialize what you want alongside the loading of your modded shell.</summary>
    public virtual void OnLoad() { }
    /// <summary>Manually unload things that may not be automatically unloaded by the game.</summary>
    public virtual void OnUnload() { }
    /// <summary>Called upon the creation of the shell. Be sure to call <c>base.OnCreate(shell)</c></summary>
    public virtual void OnCreate(Shell shell) {
        if (_texture is null || Texture is null)
            return;
        shell.SwapTexture(!string.IsNullOrEmpty(Texture) ? Mod.ImportAsset<Texture2D>(Texture) : GameResources.GetGameResource<Texture2D>("Assets/textures/bullet/bullet"));
        if (shell.Properties.LeavesTrail)
            shell.TrailSound = !string.IsNullOrEmpty(ShootSound) ? new OggAudio(Path.Combine(Mod.ModPath, TrailSound), 0.3f) : new OggAudio("Content/Assets/sounds/tnk_shoot_ricochet_rocket_loop.ogg", 0.3f);
        shell.ShootSound = !string.IsNullOrEmpty(ShootSound) ? new OggAudio(Path.Combine(Mod.ModPath, ShootSound)) : new OggAudio("Content/Assets/sounds/tnk_shoot_regular_1.ogg");
    }
    /// <summary>Called every update.</summary>
    public virtual void PostUpdate(Shell shell) { }
    /// <summary>Called every draw.</summary>
    public virtual void PostRender(Shell shell) { }
    /// <summary>Called every time this shell ricochets off of something. <paramref name="block"/> will be null if it bounces off of the side wall.</summary>
    public virtual void OnRicochet(Shell shell, Block block) { }
    /// <summary>Called when this shell is destroyed.</summary>
    /// <param name="shell">The shell.</param>
    /// <param name="context">The context of which this shell was destroyed.</param>
    /// <param name="playSound">Set to false to not play sounds.</param>
    public virtual void OnDestroy(Shell shell, Shell.DestructionContext context, ref bool playSound) { }

    internal static int unloadOffset = 0;
    internal void Register() {
        var name = Name.GetLocalizedString(LangCode.English);
        Type = ShellID.Collection.ForcefullyInsert(name);
        _texture = Mod.ImportAsset<Texture2D>(Texture);
    }
    internal void Unload() {
        ShellID.Collection.TryRemove(Type - unloadOffset);
    }
}
