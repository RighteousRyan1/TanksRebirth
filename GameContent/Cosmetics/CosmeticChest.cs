using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.Cosmetics
{
    // Not inherently locked to cosmetics only, but exist to distinguish from tank crates.
    public struct CosmeticChest
    {
        public static Cosmetic2D Anger = new("Anger!", GameResources.GetGameResource<Texture2D>("Assets/cosmetics/anger_symbol"), new(8, 20, 8), CosmeticLockOptions.None)
        {
            UniqueBehavior = (cos, tnk) =>
            {
                var sin = MathF.Sin((float)TankGame.LastGameTime.TotalGameTime.TotalMilliseconds / 350) / 6;
                cos.Scale = new Vector3(MathF.Abs(sin) + 0.15f) * 0.1f;
                // apparently this stuff aint updating or sum.
            }
        };
        public static Cosmetic3D DefaultBlenderCube = new("Default Blender Cube", GameResources.GetGameResource<Model>("Assets/cosmetics/blender_default_cube")
            , GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel"), new(0, 100, 0), CosmeticLockOptions.None)
        {
            Rotation = new(-MathHelper.PiOver2, 0, 0),
            UniqueBehavior = (cos, tnk) =>
            {
                cos.Rotation += new Vector3(0, 0, 0.075f);
            },
            Scale = new(10f)
        };
        public static Cosmetic3D KingsCrown = new("King's Crown", GameResources.GetGameResource<Model>("Assets/cosmetics/crown"), GameResources.GetGameResource<Texture2D>("Assets/cosmetics/crown_tex"), new(0, 21, 0), CosmeticLockOptions.ToTurret) {
            UniqueBehavior = (cos, tnk) => {
                /*cos.LockOptions = CosmeticLockOptions.None;
                cos.Rotation = new(-MathHelper.PiOver2 - 0.3f, 0, -MathHelper.PiOver4 / 2);
                cos.RelativePosition = new Vector3(5, 19, -3);
                cos.Scale = new(2.7f);

                var v2 = new Vector2(cos.RelativePosition.X, cos.RelativePosition.Z);
                var rot = v2.RotatedByRadians(tnk.TurretRotation);
                cos.RelativePosition += new Vector3(rot.X, 0, rot.Y);
                cos.Rotation += new */
            },
            Rotation = new(-MathHelper.PiOver2, 0, 0),
            Scale = new(3.5f)
        };
        public static Cosmetic3D BigOlStump = new Cosmetic3D("Big Ol' Stump", GameResources.GetGameResource<Model>("Assets/forest/tree_stump"), GameResources.GetGameResource<Texture2D>("Assets/forest/tree_log_tex"), new(0, 60, 0), CosmeticLockOptions.None)
        {
            Rotation = new(-MathHelper.PiOver2, 0, 0),
            UniqueBehavior = (cos, tnk) =>
            {
                cos.Rotation += new Vector3(0.075f, 0.05f, -0.025f);
            },
            Scale = new(10f)
        };
        public static Cosmetic3D Chair = new Cosmetic3D("A Literal Chair", GameResources.GetGameResource<Model>("Assets/toy/wooden_chair"), GameResources.GetGameResource<Texture2D>("Assets/toy/chair_tex"), new(0, 60, 0), CosmeticLockOptions.None)
        {
            Rotation = new(-MathHelper.PiOver2, 0, -MathHelper.PiOver2),
            UniqueBehavior = (cos, tnk) =>
            {
                var sinx = MathF.Sin((float)TankGame.LastGameTime.TotalGameTime.TotalMilliseconds / 100) * 30;
                var siny = MathF.Sin((float)TankGame.LastGameTime.TotalGameTime.TotalMilliseconds / 150);
                // cos.Rotation = new Vector3(0, 0, 0);
                cos.RelativePosition = new Vector3(10 + sinx, 30 + MathF.Abs(siny) * 20, 0f);
            },
            Scale = new(1f)
        };
        public static Cosmetic3D DevilsHorns = new Cosmetic3D("Devil Horns", GameResources.GetGameResource<Model>("Assets/cosmetics/horns"), GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel"), new(0, 11, 0), CosmeticLockOptions.ToTurret)
        {
            Rotation = new(MathHelper.Pi, 0, 0),
            Scale = new(100f)
        };
        public static Cosmetic3D AngelHalo = new Cosmetic3D("Angel Halo", GameResources.GetGameResource<Model>("Assets/cosmetics/halo"), GameResources.GetGameResource<Texture2D>("Assets/cosmetics/halo_tex"), new(0, 20, 0), CosmeticLockOptions.ToTurret)
        {
            Rotation = new(MathHelper.PiOver2, 0, 0),
            Scale = new(5f, 2f, 5f),

            UniqueBehavior = (cos, tnk) =>
            {
                var sinx = MathF.Sin((float)TankGame.LastGameTime.TotalGameTime.TotalMilliseconds / 250);
                var siny = MathF.Sin((float)TankGame.LastGameTime.TotalGameTime.TotalMilliseconds / 150);

                cos.RelativePosition = new Vector3(sinx, 20 + siny / 5, 0f);

                if (tnk.IsIngame && !tnk.Properties.Invisible)
                    if (TankGame.UpdateCount % 10 == 0)
                        GameHandler.Particles.MakeShineSpot(tnk.Position3D + new Vector3(0, 20 + siny / 5 + (Vector2.UnitY * 5).RotatedByRadians(GameHandler.GameRand.NextFloat(0, MathHelper.TwoPi)).Y, 0), Color.Yellow, GameHandler.GameRand.NextFloat(0.3f, 0.5f));
            }
        };
        public static Cosmetic3D ArmyHat = new Cosmetic3D("Army Hat", GameResources.GetGameResource<Model>("Assets/cosmetics/army_hat"), GameResources.GetGameResource<Texture2D>("Assets/cosmetics/army_hat_tex"), new(0, 13.5f, 0), CosmeticLockOptions.ToTurret)
        {
            Rotation = new(-MathHelper.PiOver2, 0, 0),
            Scale = new(10f)
        };
        public static Cosmetic3D SantaHat = new Cosmetic3D("Santa Hat", GameResources.GetGameResource<Model>("Assets/cosmetics/santa_hat"), GameResources.GetGameResource<Texture2D>("Assets/textures/tank/tank_santa"), new(0, 12.5f, 0), CosmeticLockOptions.ToTurret)
        {
            Rotation = new(-MathHelper.PiOver2, MathHelper.Pi, 0),
            Scale = new(100f)
        };
        public static CosmeticChest Basic = new(new()
        {
            { Anger, 11.1111111111f },
            { DefaultBlenderCube, 11.1111111111f },
            { KingsCrown, 11.1111111111f },
            { BigOlStump, 11.1111111111f },
            { Chair, 11.1111111111f },
            { DevilsHorns, 11.1111111111f },
            { AngelHalo, 11.1111111111f },
            { ArmyHat, 11.1111111111f },
            { SantaHat, 11.1111111111f }
        });

        public static CosmeticChest Template = new(new()
        {

        });

        /// <summary>All of the contents should add up to 100f.</summary>
        public Dictionary<object, float> WeightedContents;

        public CosmeticChest(Dictionary<object, float> weightedContents)
        {
            WeightedContents = weightedContents;
        }

        public object Open()
        {
            static bool between(float min, float max, float value) => value > min && value < max;

            var rolledRand = GameHandler.GameRand.NextFloat(0, 100);

            // get the lowest float value in WeightedContents
            var ordered = WeightedContents.Values.OrderBy(x => x).ToArray();
            var orderedDict = WeightedContents.OrderBy(x => x.Value);

            Dictionary<(float, float), float> minMaxes = new();

            float cur = 0f;
            int pickedIdx = 0;
            foreach (var pair in ordered)
            {
                minMaxes.Add((cur, cur + pair), pair);
                cur += pair;
            }
            for (int i = 0; i < minMaxes.Count; i++)
            {
                var entry = minMaxes.ElementAt(i);

                Console.WriteLine(entry);

                if (between(entry.Key.Item1, entry.Key.Item2, rolledRand))
                    pickedIdx = i;
            }

            var pickedDictEntry = orderedDict.ElementAt(pickedIdx);// WeightedContents.First(pair => pair.Value == pickedIdx);

            return pickedDictEntry.Key;
        }
    }
}
