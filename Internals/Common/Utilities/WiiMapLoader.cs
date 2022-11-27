using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using TanksRebirth.GameContent.ID;

namespace TanksRebirth.Internals.Common.Utilities;
// todo: implement
public readonly struct WiiMap
{
    public const int TNK_PLR_ID = -1;
    public const int TNK_E_ID = -2;

    public readonly byte[] RawData;

    public readonly int Width;
    public readonly int Height;

    public readonly int QValue;
    public readonly int PValue;

    public readonly List<KeyValuePair<Point, KeyValuePair<int, int>>> MapItems; // Point (xy pos), Kvp<int, int> (type, stack)

    public WiiMap(string file)
    {
        RawData = File.ReadAllBytes(file);
        if (RawData.Length < 0x10)
            throw new Exception("The file is too short to be a valid Wii Tanks map file.");

        Width = BitUtils.GetInt(RawData, 0x0);
        Height = BitUtils.GetInt(RawData, 0x4);
        PValue = RawData[0xB];
        QValue = RawData[0xF];
        MapItems = new();
        for (int i = 0; i < Width; i++) {
            for (int j = 0; j < Height; j++) {
                var blockTypeOrig = BitUtils.GetInt(RawData, (((j * Width) + i) << 2) + 0x10);

                var blockType = ConvertToEditorSpace(blockTypeOrig);
                MapItems.Add(new(new(i, j), blockType));
            }
        }
    }
    public static KeyValuePair<int, int> ConvertToEditorSpace(int input)
    {
        if (input == 0)
            return new(-1, 0); // this is an empty space.
        else if (input >= 101 && input <= 107)
            return new(BlockID.Cork, input - 100); // this would be a cork block in the binary file. (base 10 101-107)
        else if (input == 200)
            return new(BlockID.Hole, 0); // this is a hole in the binary file.
        else if (input >= 201 && input <= 207)
            return new(BlockID.Wood, input - 100); // this would be a wood block in the binary file. (base 10 201-207)
        else if (input >= 44 && input <= 45)
            return new(input - 44, TNK_PLR_ID); // respective blue and red player tank. -1 stack because we want to identify it.
        else if (input >= 144 && input <= 151)
            return new(input - 142, TNK_E_ID); // -2 stack to identify as well
        else
            throw new Exception("Invalid conversion process to a " + nameof(WiiMap) + ".");
        // unfortunately we cannot do much more than this, since enemy spawns are handled in the parameter file.
        // ...but we can find where they would spawn. go ahead and put a brown tank on the blue team there.
    }
}
