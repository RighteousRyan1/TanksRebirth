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
        public static Prop2D Anger = new("Anger!", GameResources.GetGameResource<Texture2D>("Assets/cosmetics/anger_symbol"), new(8, 20, 8), PropLockOptions.None)
        {
            UniqueBehavior = (cos, tnk) =>
            {
                var sin = MathF.Sin((float)TankGame.LastGameTime.TotalGameTime.TotalMilliseconds / 500) / 8;
                cos.Scale = new Vector3(MathF.Abs(sin) + 0.15f) * 1.25f;
                // apparently this stuff aint updating or sum.
            }
        };
        public static Prop3D DefaultBlenderCube = new("Default Blender Cube", GameResources.GetGameResource<Model>("Assets/cosmetics/blender_default_cube")
            , GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel"), new(0, 100, 0), PropLockOptions.None)
        {
            Rotation = new(-MathHelper.PiOver2, 0, 0),
            UniqueBehavior = (cos, tnk) =>
            {
                cos.Rotation += new Vector3(0.0102f, 0.034f, 0.075f) * TankGame.DeltaTime;
            },
            Scale = new(10f)
        };
        // cosmetics like this block the first person camera. fix it by either changing camera position or changing cosmetic location/rotation anchor
        public static Prop3D KingsCrown = new("King's Crown", GameResources.GetGameResource<Model>("Assets/cosmetics/crown"), GameResources.GetGameResource<Texture2D>("Assets/cosmetics/crown_tex"), new(0, 21, 0), PropLockOptions.ToTurret) {
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
        public static Prop3D DevilsHorns = new Prop3D("Devil Horns", GameResources.GetGameResource<Model>("Assets/cosmetics/horns"), GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel"), new(0, 11, 0), PropLockOptions.ToTurret)
        {
            Rotation = new(MathHelper.Pi, 0, 0),
            Scale = new(100f)
        };
        public static Prop3D AngelHalo = new Prop3D("Angel Halo", GameResources.GetGameResource<Model>("Assets/cosmetics/halo"), GameResources.GetGameResource<Texture2D>("Assets/cosmetics/halo_tex"), new(0, 20, 0), PropLockOptions.ToTurret)
        {
            Rotation = new(MathHelper.PiOver2, 0, 0),
            Scale = new(5f, 2f, 5f),

            UniqueBehavior = (cos, tnk) =>
            {
                var sinx = MathF.Sin((float)TankGame.LastGameTime.TotalGameTime.TotalMilliseconds / 250);
                var siny = MathF.Sin((float)TankGame.LastGameTime.TotalGameTime.TotalMilliseconds / 150);

                cos.RelativePosition = new Vector3(sinx, 20 + siny / 5, 0f);

                if (tnk.IsIngame && !tnk.Properties.Invisible) {
                    float y = 20f;
                    if (TankGame.UpdateCount % 10 == 0) {
                        GameHandler.Particles.MakeShineSpot(tnk.Position3D +
                            new Vector3(cos.RelativePosition.X, cos.RelativePosition.Y, siny / 5 + (Vector2.UnitY * 5).Rotate(GameHandler.GameRand.NextFloat(0, MathHelper.TwoPi)).Y), 
                            Color.Yellow, GameHandler.GameRand.NextFloat(0.3f, 0.5f));
                    }
                }
            }
        };
        public static Prop3D ArmyHat = new Prop3D("Army Hat", GameResources.GetGameResource<Model>("Assets/cosmetics/army_hat"), GameResources.GetGameResource<Texture2D>("Assets/cosmetics/army_hat_tex"), new(0, 13.5f, 0), PropLockOptions.ToTurret)
        {
            Rotation = new(-MathHelper.PiOver2, 0, 0),
            Scale = new(10f)
        };
        public static Prop3D SantaHat = new Prop3D("Santa Hat", GameResources.GetGameResource<Model>("Assets/cosmetics/santa_hat"), GameResources.GetGameResource<Texture2D>("Assets/textures/tank/tank_santa"), new(0, 12.5f, 0), PropLockOptions.ToTurret)
        {
            Rotation = new(-MathHelper.PiOver2, MathHelper.Pi, 0),
            Scale = new(100f)
        };
        public static CosmeticChest Basic = new(new List<IProp>()
        {
            Anger,
            DefaultBlenderCube,
            KingsCrown,
            DevilsHorns,
            AngelHalo,
            ArmyHat,
            SantaHat,
        });

        public static CosmeticChest Template = new(new List<IProp>()
        {

        });

        /// <summary>All of the contents should add up to 100f.</summary>
        public Dictionary<IProp, float> WeightedContents;

        public CosmeticChest(Dictionary<IProp, float> weightedContents)
        {
            WeightedContents = weightedContents;
        }
        public CosmeticChest(List<IProp> contents) {
            var evenProportion = 1f / contents.Count;
            WeightedContents = contents.ToDictionary(x => x, y => evenProportion);
        }

        public readonly IProp Open()
        {
            static bool between(float min, float max, float value) => value > min && value < max;

            var rolledRand = GameHandler.GameRand.NextFloat(0, 1);

            // get the lowest float value in WeightedContents
            var ordered = WeightedContents.Values.OrderBy(x => x).ToArray();
            var orderedDict = WeightedContents.OrderBy(x => x.Value);

            Dictionary<(float, float), float> minMaxes = [];

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

                if (between(entry.Key.Item1, entry.Key.Item2, rolledRand))
                    pickedIdx = i;
            }
            //var pickedIdx = (int)Math.Floor(rolledRand / )

            var pickedDictEntry = orderedDict.ElementAt(pickedIdx);// WeightedContents.First(pair => pair.Value == pickedIdx);

            return pickedDictEntry.Key;
        }
    }
}
