namespace TanksRebirth.GameContent;

public class BlockProperties
{
    /// <summary>Whether or not this <see cref="Block"/> is destructible from explosions.</summary>
    public bool IsDestructible { get; set; }
    /// <summary>Whether or not this <see cref="Block"/> is solid. This only affects <see cref="Shell"/>s and their ability to pass through.</summary>
    public bool IsSolid { get; set; } = true;
    /// <summary>Whether or not this <see cref="Block"/> is collidable. This only affects things with physics bodies (i.e: <see cref="Tank"/>s).</summary>
    public bool IsCollidable { get; set; } = true;
    /// <summary>Whether or not an <see cref="AITank"/> should calculate a bounce off of this <see cref="Block"/>.</summary>
    public bool AllowShotPathBounce { get; set; } = true;
    /// <summary> How many bounces a <see cref="Shell"/> should regain from hitting this <see cref="Block"/>.
    /// <para></para>Set to negative to increase the amount. Set to 0 to make nothing happen.</summary>
    public int PathBounceCount { get; set; } = 1;
    /// <summary>Whether or not this <see cref="Block"/> can stack. This will make the level editor UI not show a stack count.</summary>
    public bool CanStack { get; set; } = true;
    /// <summary>Whether or not this <see cref="Block"/> has a visible shadow under it.</summary>
    public bool HasShadow { get; set; } = true;
}
