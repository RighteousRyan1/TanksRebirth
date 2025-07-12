using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Cosmetics;

// TODO: pets lol???
// TODO: make cosmetics fade out if you're really close in freecam/POV
// Not inherently locked to cosmetics only, but exist to distinguish from tank crates.
public struct CosmeticChest
{
    public static Prop2D Anger = new("Anger!", GameResources.GetGameResource<Texture2D>("Assets/models/cosmetics/anger_symbol"), new(8, 20, 8), PropLockOptions.None)
    {
        UniqueBehavior = (cos, tnk) =>
        {
            var sin = MathF.Sin((float)TankGame.LastGameTime.TotalGameTime.TotalMilliseconds / 500) / 8;
            cos.Scale = new Vector3(MathF.Abs(sin) + 0.15f) * 1.25f;
            // apparently this stuff aint updating or sum.
        }
    };
    public static Prop3D DefaultBlenderCube = new("Default Blender Cube", ModelGlobals.BlenderDefaultCube.Asset, 
        TextureGlobals.Pixels[Color.White], new(0, 100, 0), PropLockOptions.None)
    {
        Rotation = new(-MathHelper.PiOver2, 0, 0),
        UniqueBehavior = (cos, tnk) =>
        {
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
    public static Prop3D DevilsHorns = new("Devil Horns", ModelGlobals.Horns.Asset, TextureGlobals.Pixels[Color.White], new(0, 11, 0), PropLockOptions.ToTurret)
    {
        Rotation = new(MathHelper.Pi, 0, 0),
        Scale = new(100f)
    };
    public static Prop3D AngelHalo = new("Angel Halo", ModelGlobals.Halo.Asset, GameResources.GetGameResource<Texture2D>("Assets/models/cosmetics/halo_tex"), new(0, 20, 0), PropLockOptions.ToTurret)
    {
        Rotation = new(MathHelper.PiOver2, 0, 0),
        Scale = new(5f, 2f, 5f),

        UniqueBehavior = (cos, tnk) =>
        {
            var sinx = MathF.Sin((float)TankGame.LastGameTime.TotalGameTime.TotalMilliseconds / 250);
            var siny = MathF.Sin((float)TankGame.LastGameTime.TotalGameTime.TotalMilliseconds / 150);

            cos.RelativePosition = new Vector3(sinx, 20 + siny / 5, 0f);

            if (tnk.IsIngame && !tnk.Properties.Invisible) {
                // float y = 20f;
                if (RuntimeData.UpdateCount % 10 == 0) {
                    GameHandler.Particles.MakeShineSpot(tnk.Position3D +
                        new Vector3(cos.RelativePosition.X, cos.RelativePosition.Y, siny / 5 + (Vector2.UnitY * 5).Rotate(Client.ClientRandom.NextFloat(0, MathHelper.TwoPi)).Y), 
                        Color.Yellow, Client.ClientRandom.NextFloat(0.3f, 0.5f));
                }
            }
        }
    };
    public static Prop3D ArmyHat = new("Army Hat", ModelGlobals.ArmyHat.Asset, GameResources.GetGameResource<Texture2D>("Assets/models/cosmetics/army_hat_tex"), new(0, 13.5f, 0), PropLockOptions.ToTurret)
    {
        Rotation = new(-MathHelper.PiOver2, 0, 0),
        Scale = new(10f)
    };
    public static Prop3D SantaHat = new("Santa Hat", ModelGlobals.SantaHat.Asset, GameResources.GetGameResource<Texture2D>("Assets/models/cosmetics/santa_hat_tex"), new(0, 12.5f, 0), PropLockOptions.ToTurret)
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

        var rolledRand = Client.ClientRandom.NextFloat(0, 1);

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
