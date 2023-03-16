using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        public readonly BlockMapPosition Position;
        public readonly int Type;
        /// <summary>-1 for player tank, -2 for enemy tank.</summary>
        public readonly int Stack;

        public WiiMapTileData(BlockMapPosition pos, int type, int stack) {
            Position = pos;
            Type = type;
            Stack = stack;
        }
    }

    public const int LargeMapBytepool = 1512;

    public const int PLAYER_TANK_ID = -1;
    public const int ENEMY_TANK_ID = -2;

    public readonly byte[] RawData;

    public readonly int Width;
    public readonly int Height;

    public readonly int QValue;
    public readonly int PValue;
    public readonly List<WiiMapTileData> MapItems; // Point (xy pos), Kvp<int, int> (type, stack)
    public WiiMap(string file) {
        RawData = File.ReadAllBytes(file);
        if (RawData.Length < 0x10)
            throw new Exception("The file is too short to be a valid Wii Tanks map file.");

        Width = RawData.GetInt(0x0);
        Height = RawData.GetInt(0x4);

        if (RawData.Length != 0x10 + ((Height * Width) << 2))
            throw new Exception("The file header is invalid.");

        PValue = RawData[0xB];
        QValue = RawData[0xF];
        MapItems = new();
        for (var i = 0; i < Width * Height; i++) {
            var blockTypeOrig = RawData[i * 0x4 + 0x13];

            var tileMetaData = ConvertToEditorSpace(blockTypeOrig);
            // 731 == player tank byte position for the vanilla map
            var x = i % Width;
            MapItems.Add(new WiiMapTileData(new BlockMapPosition(x, i / Width), tileMetaData.Key, tileMetaData.Value));
        }
    }
    public static void SaveToTanksBinFile(string fileLocation, bool largeMap = true) {

        // Validate map before allocating memory and wasting resources in saving something we just can not save correctly.
        var validation = ValidateWiiMap();
        if (validation == WiiMapValidationResult.FailureTooManyPlayers) {
            LevelEditor.Alert("Too many players for Tanks! file format.", 240f);
            LevelEditor.GUICategory = LevelEditor.UICategory.LevelEditor;
            return;
        }
        else if (validation == WiiMapValidationResult.FailureTooManyAI) {
            LevelEditor.Alert("Too many AI for Tanks! file format.", 240f);
            LevelEditor.GUICategory = LevelEditor.UICategory.LevelEditor;
            return;
        }
        else if (validation == (WiiMapValidationResult.FailureTooManyPlayers | WiiMapValidationResult.FailureTooManyAI)) {
            LevelEditor.Alert("Too many AI AND players for Tanks! file format.", 240f);
            LevelEditor.GUICategory = LevelEditor.UICategory.LevelEditor;
            return;
        }
        
        var rawData = new byte[LargeMapBytepool];
        var byteOffset = 0x3; // 3 bytes

        void SetBit(byte[] dataCollection, int data) {
            dataCollection[byteOffset] = (byte)data;
            byteOffset += 0x4; // 4 bytes per tile (int == 32 bits (32 / 8 = 4))
        }

        SetBit(rawData, (byte)(largeMap ? BlockMapPosition.MAP_WIDTH_169 : BlockMapPosition.MAP_WIDTH_43));

        SetBit(rawData, BlockMapPosition.MAP_HEIGHT);

        SetBit(rawData, 0);
        SetBit(rawData, 0);

        foreach (var pl in PlacementSquare.Placements) {
            if (pl.BlockId > -1 && pl.HasBlock) {
                var block = Block.AllBlocks[pl.BlockId];
                switch (block.Type) {
                    case BlockID.Wood:
                        SetBit(rawData, block.Stack + 200);
                        break;
                    case BlockID.Cork:
                        SetBit(rawData, block.Stack + 100);
                        break;
                    case BlockID.Hole:
                        SetBit(rawData, 200);
                        break;
                }
            }
            if (pl.TankId > -1) {
                var tank = GameHandler.AllTanks[pl.TankId];

                switch (tank) {
                    case PlayerTank player: {
                        if (player.PlayerType < 2) {
                            rawData[byteOffset - 0x1] = 1;
                            SetBit(rawData, player.PlayerType + 44);
                        }
                        break;
                    }
                    case AITank ai:
                        rawData[byteOffset - 0x1] = 1;
                        SetBit(rawData, ai.AITankId + 144);
                        break;
                }
            }
            if (pl is { BlockId: -1, TankId: -1 }) {
                SetBit(rawData, 0);
            }
        }

        File.WriteAllBytes(fileLocation, rawData);
    }

    private static WiiMapValidationResult ValidateWiiMap() {
        var result = WiiMapValidationResult.Success;
        var tankAiCount = 0;
        var playerCount = 0;
        for (var i = 0; i < PlacementSquare.Placements.Count; i++) {
            var pl = PlacementSquare.Placements[i];
            if (pl.TankId > -1) {
                var tnk = GameHandler.AllTanks[pl.TankId];
                if (tnk is PlayerTank)
                    playerCount++;
                else if (tnk is AITank)
                    tankAiCount++;
            }
        }
        if (tankAiCount > 8) result = WiiMapValidationResult.FailureTooManyAI;
        if (playerCount > 2) {
            if (result == WiiMapValidationResult.Success)
                result = WiiMapValidationResult.FailureTooManyPlayers;
            else
                result |= WiiMapValidationResult.FailureTooManyPlayers;
        }

        return result;
    }

    public static void ApplyToGameWorld(WiiMap map) {
        PlacementSquare.ResetSquares();
        GameHandler.CleanupEntities();

        foreach (var mapTile in map.MapItems) {
            ProcessWiiMapTile(mapTile);
        }
    }

    private static void ProcessWiiMapTile(WiiMapTileData mapTile) {
        //var tile = PlacementSquare.Placements[map.Width * item.Key.Y + item.Key.X]; // access from the list like it's a 2D array

        // The coordinates for the placements are actually just Row and Column. Annoying...
        var tile = PlacementSquare.Placements.First(sq =>
            sq.RelativePosition.X == mapTile.Position.X && sq.RelativePosition.Y == mapTile.Position.Y);

        /* Code used to debug Map processing, somewhat:
         *  if (tile.RelativePosition == new Point(5, 1))
         *  {
         *      ChatSystem.SendMessage($"Type: {item.Type}", Color.White);
         *      ChatSystem.SendMessage($"Stack: {item.Stack}", Color.White);
         *  }
         */

        if (mapTile.Type <= -1) return;

        if (mapTile.Stack is not (-1 or -2)) { // Normal tile, not a player.
            var bl = new Block(mapTile.Type, mapTile.Stack, tile.Position.FlattenZ());
            tile.BlockId = bl.Id;
            tile.HasBlock = true;
            return;
        }

        // This tile is basically a tank.

        var tnkRot = 0f;

        const int HALF_MAP_WIDTH = BlockMapPosition.MAP_WIDTH_169 / 2;
        const int HALF_MAP_HEIGHT = BlockMapPosition.MAP_HEIGHT / 2;


        // The tank will face rightward.
        if (tile.RelativePosition.X <= tile.RelativePosition.Y && tile.RelativePosition.X < HALF_MAP_WIDTH)
            tnkRot = -MathHelper.PiOver2 * 3;
        // The tank will face downward.
        else if (tile.RelativePosition.X >= tile.RelativePosition.Y && tile.RelativePosition.Y < HALF_MAP_HEIGHT)
            tnkRot = 0;

        // The tank will face leftward.
        if ((tile.RelativePosition.X - HALF_MAP_WIDTH) <= tile.RelativePosition.Y &&
            tile.RelativePosition.X > HALF_MAP_WIDTH)
            tnkRot = -MathHelper.PiOver2;

        // The tank will face upward.
        else if (tile.RelativePosition.X > (tile.RelativePosition.Y - HALF_MAP_HEIGHT) &&
                 tile.RelativePosition.Y > HALF_MAP_HEIGHT)
            tnkRot = -MathHelper.Pi;

        switch (mapTile.Stack) {
            case PLAYER_TANK_ID: { // Player Tank, That's us!
                var pl = GameHandler.SpawnMe(mapTile.Type, TeamID.Red, tile.Position);
                pl.TankRotation = tnkRot;
                pl.TargetTankRotation = tnkRot;
                pl.TurretRotation = tnkRot;
                tile.TankId = pl.WorldId;
                break;
            }
            case ENEMY_TANK_ID: { // Enemy Tank.
                var ai = GameHandler.SpawnTankAt(tile.Position, AITank.PickRandomTier(), TeamID.Blue);
                ai.TankRotation = tnkRot;
                ai.TargetTankRotation = tnkRot;
                ai.TurretRotation = tnkRot;
                tile.TankId = ai.WorldId;
                ai.ReassignId(mapTile.Type);
                break;
            }
        }

        tile.HasBlock = false;
        return;
    }

    public static KeyValuePair<int, int> ConvertToEditorSpace(int input) {
        return input switch {
            0 => new(-1, 0), // this is an empty space.
            >= 101 and <= 107 => new(BlockID.Cork, input - 100),                    // this would be a cork block in the binary file. (base 10 101-107)
            200 => new(BlockID.Hole, 0),                                            // this is a hole in the binary file.
            >= 201 and <= 207 => new(BlockID.Wood, input - 200),                    // this would be a wood block in the binary file. (base 10 201-207)
            >= 44 and <= 45 => new(input - 44, PLAYER_TANK_ID),                     // respective blue and red player tank. -1 stack because we want to identify it.
            >= 144 and <= 151 => new(input - 144, ENEMY_TANK_ID),                   // -2 stack to identify as well, and since we can't identify the tank type, too bad.
            _ => throw new Exception($"Invalid conversion process to a {nameof(WiiMap)}.")
        };

        // unfortunately we cannot do much more than this, since enemy spawns are handled in the parameter file.
        // ...but we can find where they would spawn. go ahead and put a brown tank on the blue team there.
    }
}
[Flags]
public enum WiiMapValidationResult {
    Success, 
    FailureTooManyAI, 
    FailureTooManyPlayers
}