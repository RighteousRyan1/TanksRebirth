using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Systems.Coordinates;
using TanksRebirth.GameContent.UI;

namespace TanksRebirth.Internals.Common.Utilities;
// todo: implement
public readonly struct WiiMap
{

    public readonly struct WiiMapTileData {
        public readonly CubeMapPosition Position;
        public readonly int Type;
        /// <summary>-1 for player tank, -2 for enemy tank.</summary>
        public readonly int Stack;

        public WiiMapTileData(CubeMapPosition pos, int type, int stack) {
            Position = pos;
            Type = type;
            Stack = stack;
        }
    }

    public const int TNK_PLR_ID = -1;
    public const int TNK_E_ID = -2;

    public readonly byte[] RawData;

    public readonly int Width;
    public readonly int Height;

    public readonly int QValue;
    public readonly int PValue;
    public readonly List<WiiMapTileData> MapItems; // Point (xy pos), Kvp<int, int> (type, stack)

    public WiiMap(string file)
    {
        RawData = File.ReadAllBytes(file);
        if (RawData.Length < 0x10)
            throw new Exception("The file is too short to be a valid Wii Tanks map file.");

        Width = BitUtils.GetInt(RawData, 0x0);
        Height = BitUtils.GetInt(RawData, 0x4);

        if (RawData.Length != 0x10 + ((Height * Width) << 2))
            throw new Exception("The file header is invalid.");

        PValue = RawData[0xB];
        QValue = RawData[0xF];
        MapItems = new();
        for (int i = 0; i < Width * Height; i++) {
            //for (int j = 0; j < Height; j++) {
            var blockTypeOrig = RawData[i * 4 + /*0xF*/ 19];//BitUtils.GetInt(RawData, (((j * Width) + i) << 2) + 0x10);

            var tileMetaData = ConvertToEditorSpace(blockTypeOrig);

            var x = i % Width;
            MapItems.Add(new WiiMapTileData(new CubeMapPosition(x, i / Width), tileMetaData.Key, tileMetaData.Value));
            //}
        }
    }

    public static void ApplyToGameWorld(WiiMap map)
    {
        PlacementSquare.ResetSquares();
        GameHandler.CleanupEntities();

        foreach (var item in map.MapItems)
        {
            //var tile = PlacementSquare.Placements[map.Width * item.Key.Y + item.Key.X]; // access from the list like it's a 2D array

            // this is because the coordinates for the placements are actually just (row, column) ughh
            var tile = PlacementSquare.Placements.First(
                sq => sq.RelativePosition.X == item.Position.X && sq.RelativePosition.Y == item.Position.Y);

            /*if (tile.RelativePosition == new Point(5, 1))
            {
                ChatSystem.SendMessage($"Type: {item.Type}", Color.White);
                ChatSystem.SendMessage($"Stack: {item.Stack}", Color.White);
            }*/
            if (item.Type > -1)
            {
                if (item.Stack == -1 || item.Stack == -2)
                {
                    float tnkRot = 0f;

                    var halfW = CubeMapPosition.MAP_WIDTH / 2;
                    var halfH = CubeMapPosition.MAP_HEIGHT / 2;

                    // face right.
                    if (tile.RelativePosition.X <= tile.RelativePosition.Y && tile.RelativePosition.X < halfW)
                        tnkRot = -MathHelper.PiOver2 * 3;
                    // face left
                    if ((tile.RelativePosition.X - halfW) <= tile.RelativePosition.Y && tile.RelativePosition.X > halfW)
                        tnkRot = -MathHelper.PiOver2;
                    // face up
                    if (tile.RelativePosition.X > (tile.RelativePosition.Y - halfH) && tile.RelativePosition.Y > halfH)
                        tnkRot = -MathHelper.Pi;
                    // face downwards.
                    else if (tile.RelativePosition.X >= tile.RelativePosition.Y && tile.RelativePosition.Y < halfH)
                        tnkRot = 0;

                    if (item.Stack == -1)
                    {
                        var pl = GameHandler.SpawnMe(item.Type, TeamID.Red, tile.Position);
                        pl.TankRotation = tnkRot;
                        pl.TargetTankRotation = tnkRot;
                        pl.TurretRotation = tnkRot;
                        tile.TankId = pl.WorldId;
                    }
                    if (item.Stack == -2)
                    {
                        var ai = GameHandler.SpawnTankAt(tile.Position, AITank.PickRandomTier(), TeamID.Blue);
                        ai.TankRotation = tnkRot;
                        ai.TargetTankRotation = tnkRot;
                        ai.TurretRotation = tnkRot;
                        tile.TankId = ai.WorldId;
                    }
                    tile.HasBlock = false;
                }
                else
                {
                    var bl = new Block(item.Type, item.Stack, tile.Position.FlattenZ());
                    tile.BlockId = bl.Id;
                    tile.HasBlock = true;
                }
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
            return new(BlockID.Wood, input - 200); // this would be a wood block in the binary file. (base 10 201-207)
        else if (input >= 44 && input <= 45)
            return new(input - 44, TNK_PLR_ID); // respective blue and red player tank. -1 stack because we want to identify it.
        else if (input >= 144 && input <= 151)
            return new(input - 144, TNK_E_ID); // -2 stack to identify as well, and since we can't identify the tank type, too bad.
        else
            throw new Exception("Invalid conversion process to a " + nameof(WiiMap) + ".");
        // unfortunately we cannot do much more than this, since enemy spawns are handled in the parameter file.
        // ...but we can find where they would spawn. go ahead and put a brown tank on the blue team there.
    }
}
