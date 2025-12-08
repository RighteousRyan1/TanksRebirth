using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Cosmetics;

#pragma warning disable
public static class VanillaCosmetics {
    public static Prop2D Anger = new("Anger!", GameResources.GetGameResource<Texture2D>("Assets/models/cosmetics/anger_symbol"), new(8, 20, 8), PropLockOptions.None) {
        UniqueBehavior = (cos, tnk) => {
            var sin = MathF.Sin((float)TankGame.LastGameTime.TotalGameTime.TotalMilliseconds / 500) / 8;
            cos.Scale = new Vector3(MathF.Abs(sin) + 0.15f) * 1.25f;
            // apparently this stuff aint updating or sum.
        }
    };
    public static Prop3D BlenderCube = new("Default Blender Cube", ModelGlobals.BlenderDefaultCube.Asset,
        TextureGlobals.Pixels[Color.White], new(0, 100, 0), PropLockOptions.None) {
        Rotation = new(-MathHelper.PiOver2, 0, 0),
        UniqueBehavior = (cos, tnk) => {
            cos.LockOptions = PropLockOptions.ToTurret;
            cos.Scale = new(5f);
            cos.RelativePosition = new Vector3(0, 20, 0); // + new Vector3(20, 0, 0).RotateXZ(RuntimeData.RunTime / 10);
            cos.Rotation = Vector3.Zero; //new Vector3(0.0102f, 0.034f, 0.075f) * RuntimeData.DeltaTime;
        },
        Scale = new(10f)
    };
    // cosmetics like this block the first person camera. fix it by either changing camera position or changing cosmetic location/rotation anchor
    public static Prop3D KingsCrown = new("King's Crown", ModelGlobals.Crown.Asset, GameResources.GetGameResource<Texture2D>("Assets/models/cosmetics/crown_tex"), new(0, 21, 0), PropLockOptions.ToTurret) {
        UniqueBehavior = (cos, tnk) => {
            // corner follow
            /*cos.LockOptions = CosmeticLockOptions.None;
            cos.Rotation = new(-MathHelper.PiOver2 - 0.3f, 0, -MathHelper.PiOver4 / 2);
            cos.RelativePosition = new Vector3(5, 19, -3);
            cos.Scale = new(2.7f);

            var v2 = new Vector2(cos.RelativePosition.X, cos.RelativePosition.Z);
            var rot = v2.RotatedByRadians(tnk.TurretRotation);
            cos.RelativePosition += new Vector3(rot.X, 0, rot.Y);
            cos.Rotation += new */

            //cos.LockOptions = PropLockOptions.ToTurretCentered;
            //cos.RelativePosition = new(0, 19.9f, -5f);
            //cos.Rotation = new Vector3(MathHelper.PiOver2 + MathHelper.PiOver4 * 3 + MathHelper.PiOver4 / 2, tnk.TurretRotation, 0);
        },
        Rotation = new(-MathHelper.PiOver2, 0, 0),
        Scale = new(3.5f)
    };
    public static Prop3D DevilsHorns = new("Devil Horns", ModelGlobals.Horns.Asset, TextureGlobals.Pixels[Color.White], new(0, 11, 0), PropLockOptions.ToTurret) {
        Rotation = new(MathHelper.Pi, 0, 0),
        Scale = new(100f)
    };
    public static Prop3D AngelHalo = new("Angel Halo", ModelGlobals.Halo.Asset, GameResources.GetGameResource<Texture2D>("Assets/models/cosmetics/halo_tex"), new(0, 20, 0), PropLockOptions.ToTurret) {
        Rotation = new(MathHelper.PiOver2, 0, 0),
        Scale = new(5f, 2f, 5f),

        UniqueBehavior = (cos, tnk) => {
            var sinx = MathF.Sin((float)TankGame.LastGameTime.TotalGameTime.TotalMilliseconds / 250);
            var siny = MathF.Sin((float)TankGame.LastGameTime.TotalGameTime.TotalMilliseconds / 150);

            cos.RelativePosition = new Vector3(sinx, 20 + siny / 5, 0f);

            if (!tnk.Properties.Invisible) {
                // float y = 20f;
                if (RuntimeData.UpdateCount % 10 == 0) {
                    GameHandler.Particles.MakeShineSpot(tnk.Position3D +
                        new Vector3(cos.RelativePosition.X, cos.RelativePosition.Y, siny / 5 + (Vector2.UnitY * 5).RotatedBy(Client.ClientRandom.NextFloat(0, MathHelper.TwoPi)).Y),
                        Color.Yellow, Client.ClientRandom.NextFloat(0.3f, 0.5f));
                }
            }
        }
    };
    public static Prop3D ArmyHat = new("Army Hat", ModelGlobals.ArmyHat.Asset, GameResources.GetGameResource<Texture2D>("Assets/models/cosmetics/army_hat_tex"), new(0, 13.5f, 0), PropLockOptions.ToTurret) {
        Rotation = new(-MathHelper.PiOver2, 0, 0),
        Scale = new(10f)
    };
    public static Prop3D SantaHat = new("Santa Hat", ModelGlobals.SantaHat.Asset, GameResources.GetGameResource<Texture2D>("Assets/models/cosmetics/santa_hat_tex"), new(0, 12.5f, 0), PropLockOptions.ToTurret) {
        Rotation = new(-MathHelper.PiOver2, MathHelper.Pi, 0),
        Scale = new(100f)
    };
    public static Prop3D WitchHat = new("Witch Hat", ModelGlobals.WitchHat.Asset,
        GameResources.GetGameResource<Texture2D>("Assets/models/rebirth_tanks/tank_necro_extras"), new(0, 15f, 0), PropLockOptions.None) {
        Rotation = new(-MathHelper.PiOver2, MathHelper.Pi, 0),
        Scale = new(100f),
        UniqueBehavior = (hat, tnk) => {
            var sinx = MathF.Sin((float)TankGame.LastGameTime.TotalGameTime.TotalMilliseconds / 250) * 0.1f;
            var siny = MathF.Sin((float)TankGame.LastGameTime.TotalGameTime.TotalMilliseconds / 150) * 0.1f;

            hat.Rotation = new Vector3(sinx - MathHelper.PiOver2, 0, siny);
            hat.RelativePosition = new Vector3(0, 15f + sinx * 10, 0);

            /*var matrix = Matrix.CreateFromYawPitchRoll(hat.Rotation.Z, hat.Rotation.Y, hat.Rotation.X)
                * Matrix.CreateTranslation(tnk.Position3D.X + 1.958f, tnk.Position3D.Y, tnk.Position3D.Z);
            var newPos = Vector3.Transform(hat.RelativePosition, matrix);

            GameHandler.Particles.MakeShineSpot(newPos, Color.Red, 0.5f);*/
        }
    };

    // definitions

    static VanillaCosmetics() {
        rarityMakeup = new() {
            [LootBoxRarity.Common] = .50f, // 50% makeup for common
            [LootBoxRarity.Uncommon] = .25f, // 25% makeup for uncommon
            [LootBoxRarity.Rare] = .15f, // 15% makeup for rare
            [LootBoxRarity.Epic] = .06f, // 6% makeup for epic
            [LootBoxRarity.Legendary] = .02f, // 2% makeup for legendary
            [LootBoxRarity.Mythical] = .015f, // 1.5% makeup for mythical
            [LootBoxRarity.Godly] = .005f // 0.5% makeup for godly
        };

        LootPool = new(new Dictionary<IProp, LootBoxRarity>() {
            [Anger]       = LootBoxRarity.Common,
            [BlenderCube] = LootBoxRarity.Common,

            [ArmyHat]     = LootBoxRarity.Uncommon,

            [DevilsHorns] = LootBoxRarity.Rare,

            [SantaHat]    = LootBoxRarity.Epic,

            [AngelHalo]   = LootBoxRarity.Legendary,

            [KingsCrown]  = LootBoxRarity.Mythical,

            [WitchHat]    = LootBoxRarity.Godly,
        });
    }

    // % composition
    internal static readonly Dictionary<LootBoxRarity, float> rarityMakeup;

    public static LootBox<IProp> LootPool;

    // may need changing with updating 
    public static LootBoxRarity GetRarityFromFloat(float flt) {
        if (flt > rarityMakeup[0])
            return LootBoxRarity.Common;
        else if (flt < rarityMakeup[(LootBoxRarity)(rarityMakeup.Count - 1)])
            return LootBoxRarity.Godly;

        // second rarity to second to last rarity
        for (int i = 1; i < rarityMakeup.Count; i++) {
            var thisRarity = (LootBoxRarity)i;
            var prevRarity = (LootBoxRarity)(i - 1);

            if (flt >= rarityMakeup[thisRarity] && flt < rarityMakeup[prevRarity]) {
                return thisRarity;
            }
        }

        throw new ArgumentOutOfRangeException($"'{nameof(flt)}' must be within 0 to 1.");
    }
}
