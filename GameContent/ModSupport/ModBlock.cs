using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.GameContent.ID;
using TanksRebirth.Internals.Common.Framework.Interfaces;
using TanksRebirth.Localization;

namespace TanksRebirth.GameContent.ModSupport;
// maybe allow for changing the model.
public class ModBlock : ILoadable, IModContent
{
    /// <summary>The <see cref="TanksMod"/> that this <see cref="ModBlock"/> is a part of.</summary>
    public TanksMod Mod { get; set; }
    public int Type { get; set; }
    private Texture2D? _texture;
    public virtual string Texture => string.Empty;
    public virtual LocalizedString Name => new(new());

    /// <summary>This propery defaults to <see cref="Block.MAX_BLOCK_HEIGHT"/>, the default.</summary>
    public virtual byte MaxHeight => Block.MAX_BLOCK_HEIGHT;

    /// <summary>This propery defaults to <see cref="Block.FULL_SIZE"/>, the default.</summary>
    public virtual float FullSize => Block.FULL_SIZE;

    /// <summary>This propery defaults to <see cref="Block.SLAB_SIZE"/>, the default.</summary>
    public virtual float SlabSize => Block.SLAB_SIZE;
    /// <summary>Initialize what you want alongside the loading of your modded block.</summary>
    public virtual void OnLoad() { }
    /// <summary>Manually unload things that may not be automatically unloaded by the game.</summary>
    public virtual void OnUnload() { }
    /// <summary>Do things when a your modded block is created in game space. Be sure to call <c>base.PostInitialize(block)</c></summary>
    public virtual void PostInitialize(Block block) {
        if (_texture is null || Texture is null)
            return;
        block.SwapTexture(_texture);
    }
    /// <summary>Called each update of the block.</summary>
    public virtual void PostUpdate(Block block) { }
    /// <summary>Called each draw of the block.</summary>
    public virtual void PostRender(Block block) { }
    /// <summary>Called when this block is destroyed, if destructible.</summary>
    public virtual void OnDestroy(Block block) { }
    /// <summary>Called when any shell ricochets off of this block.</summary>
    public virtual void OnRicochet(Block block, Shell shell) { }
    internal static int unloadOffset = 0;
    internal void Register() {
        var name = Name.GetLocalizedString(LangCode.English);
        Type = BlockID.Collection.ForcefullyInsert(name);

        _texture = Mod.ImportAsset<Texture2D>(Texture);
    }
    internal void Unload() {
        BlockID.Collection.TryRemove(Type - unloadOffset);
    }
}
