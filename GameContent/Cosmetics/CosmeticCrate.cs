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
    public struct CosmeticCrate
    {
        public static CosmeticCrate Basic = new(new()
        {
            { 
                new Cosmetic2D("A Literal X", GameResources.GetGameResource<Texture2D>("Assets/textures/check/check_white"), new(0, 50, 0), new(0, 0, 0), false)
                {
                    UniqueBehavior = (cos) =>
                    {
                        cos.Rotation += new Vector3(0.05f, 0, 0);
                    }
                }, 50f 
            },
            {
                new Cosmetic2D("A Literal Hole", GameResources.GetGameResource<Texture2D>("Assets/textures/ingame/block_harf.1"), new(0, 50, 0), new(0, 0, 0), false)
                {
                    UniqueBehavior = (cos) =>
                    {
                        cos.Rotation += new Vector3(0f, 0.05f, 0);
                    }
                },
                50f
            }
        });

        public static CosmeticCrate Template = new(new()
        {

        });

        /// <summary>All of the contents should add up to 100f.</summary>
        public Dictionary<object, float> WeightedContents;

        public CosmeticCrate(Dictionary<object, float> weightedContents)
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
