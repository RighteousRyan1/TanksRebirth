namespace TanksRebirth.GameContent.Systems.TankSystem; 

/// <summary>
/// Extra fun stuff that <see cref="Tank"/>s use that change organically (via typical in-game actions) during gameplay.
/// </summary>
public class TankExtras {
    /// <summary>The armor properties this <see cref="Tank"/> has.</summary>
    public TankArmor? Armor { get; set; }
    /// <summary>Safely gets the hit points remaining of this <see cref="Tank"/>'s armor- if it has no armor, it returns 0.</summary>
    public int SafeGetArmorHitPoints() => Armor == null ? 0 : Armor.HitPoints;
}
