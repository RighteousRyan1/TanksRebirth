using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using TanksRebirth.GameContent.Tanks;
using TanksRebirth.Graphics;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.Systems.Coordinates;

// the plan is to support bigger (or smaller) maps in the future

/// <summary>
/// The board as a grid of <see cref="MapCell"/>s with predefined width and height.
/// <para></para>
/// The origin is the top left, going into the bottom right.
/// </summary>
public sealed class MapGrid : IEnumerable<MapCell> {
    /// <summary>Columns in the 16:9 map.</summary>
    public const int STD_WIDTH = 22;
    /// <summary>Rows in the 16:9 map.</summary>
    public const int STD_HEIGHT = 17;
    /// <summary>Columns in the non-standard 4:3 map. Currently only used for loading the original version's 4:3 maps.</summary>
    public const int WIDTH_43 = 16;

    /// <summary>World X of the grid's left edge (the outside of the first column).</summary>
    public float MinX => Origin.X - CellSize / 2f;
    /// <summary>World X of the grid's right edge.</summary>
    public float MaxX => Origin.X + (Width - 0.5f) * CellSize;
    /// <summary>World Z of the grid's near edge (the outside of the first row).</summary>
    public float MinZ => Origin.Y - CellSize / 2f;
    /// <summary>World Z of the grid's far edge.</summary>
    public float MaxZ => Origin.Y + (Height - 0.5f) * CellSize;
    /// <summary>World position of the grid's center.</summary>
    public Vector2 Center => Origin + new Vector2(Width - 1, Height - 1) * CellSize / 2f;
    /// <summary>World size of the whole grid.</summary>
    public Vector2 Size => new(Width * CellSize, Height * CellSize);

    /// <summary>The world origin of the center cell.</summary>
    public static readonly Vector2 StandardOrigin = new(GameScene.CUBE_MIN_X + 1f, GameScene.MIN_Z + 8f);

    /// <summary>The grid the game is using.</summary>
    public static MapGrid Current { get; private set; } = CreateStandard();

    /// <summary>Raised after <see cref="Current"/> changes (old grid, new grid).</summary>
    public static event Action<MapGrid, MapGrid>? OnCurrentChanged;

    /// <summary>Columns.</summary>
    public int Width { get; }
    /// <summary>Rows.</summary>
    public int Height { get; }
    public float CellSize { get; }
    /// <summary>World position of the center of cell (0, 0).</summary>
    public Vector2 Origin { get; }
    public int Count => Width * Height;

    readonly MapCell[] _cells;

    // this probably shouldn't be exposed to modders yet, but i doubt modders will have a use for it anytime soon
    /// <param name="width">Columns (at least 1).</param>
    /// <param name="height">Rows (at least 1).</param>
    /// <param name="cellSize">World units per cell.</param>
    /// <param name="origin">World position of the of the cell at the origin.</param>
    public MapGrid(int width, int height, float cellSize, Vector2 origin) {
        Width = width;
        Height = height;
        CellSize = cellSize;
        Origin = origin;

        _cells = new MapCell[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                _cells[y * width + x] = new MapCell(this, x, y);
    }

    /// <summary>The standard 22x17 board.</summary>
    public static MapGrid CreateStandard() => new(STD_WIDTH, STD_HEIGHT, Block.SIDE_LENGTH, StandardOrigin);

    /// <summary>A grid of any size whose center sits on <paramref name="center"/> (the world origin by default).</summary>
    public static MapGrid CreateCentered(int width, int height, float cellSize, Vector2 center = default)
        => new(width, height, cellSize, center - new Vector2(width - 1, height - 1) * cellSize / 2f);

    /// <summary>Makes <paramref name="grid"/> the grid the game uses and rebuilds the level editor's squares for it.</summary>
    public static void SetCurrent(MapGrid grid) {
        if (grid == Current)
            return;
        var old = Current;
        Current = grid;
        EditorTile.RebuildSquares();
        OnCurrentChanged?.Invoke(old, grid);
    }

    // everything below is just ways to access cells

    /// <summary>The cell at column <paramref name="x"/>, row <paramref name="y"/>. Throws if it's outside the grid.</summary>
    public MapCell this[int x, int y] {
        get {
            // no pretections or exceptions cuz it will throw if an idiot accesses outside of the bounds anyway lol
            return _cells[y * Width + x];
        }
    }

    /// <summary>The cell at <paramref name="position"/>.</summary>
    public MapCell this[Point position] => this[position.X, position.Y];

    /// <summary>The cell with the given <see cref="MapCell.Index"/>.</summary>
    public MapCell GetByIndex(int index) => _cells[index];

    public bool InBounds(int x, int y) => x < Width && y < Height;

    /// <summary>The cell at (<paramref name="x"/>, <paramref name="y"/>), or false if that's outside the grid.</summary>
    public bool TryGetCell(int x, int y, out MapCell cell) {
        if (InBounds(x, y)) {
            cell = _cells[y * Width + x];
            return true;
        }
        cell = null!;
        return false;
    }

    /// <summary>The cell <paramref name="world"/> is inside of (X and Z of a 3D position), or false if it's off the grid.</summary>
    public bool TryGetCellAt(Vector2 world, out MapCell cell) {
        var coords = WorldToCell(world);
        return TryGetCell(coords.X, coords.Y, out cell);
    }

    // W inheritdoc
    /// <inheritdoc cref="TryGetCellAt(Vector2, out MapCell)"/>
    public bool TryGetCellAt(Vector3 world, out MapCell cell) => TryGetCellAt(world.FlattenZ(), out cell);

    /// <summary>The cell <paramref name="world"/> is inside of, or null if it's off the grid.</summary>
    public MapCell? CellAt(Vector2 world) => TryGetCellAt(world, out var cell) ? cell : null;

    /// <inheritdoc cref="CellAt(Vector2)"/>
    public MapCell? CellAt(Vector3 world) => TryGetCellAt(world, out var cell) ? cell : null;

    /// <summary>Forgets every cell's block and tank, but doesn't remove them from the game.</summary>
    public void Forget() {
        foreach (var cell in _cells)
            cell.Forget();
    }

    // coordinate conversions

    /// <summary>World position (X, Z) of the center of cell (<paramref name="x"/>, <paramref name="y"/>). Works outside the grid too.</summary>
    public Vector2 CellToWorld(int x, int y) => new(Origin.X + x * CellSize, Origin.Y + y * CellSize);

    /// <summary>Allows the conversion of partial cell cordinates to world space.</summary>
    public Vector2 CellToWorld(Vector2 cellCoordinates) => Origin + cellCoordinates * CellSize;

    /// <summary>3D world position of the center of cell (<paramref name="x"/>, <paramref name="y"/>) on the floor (Y = 0).</summary>
    public Vector3 CellToWorld3D(int x, int y) {
        var world = CellToWorld(x, y);
        return new Vector3(world.X, 0f, world.Y);
    }

    /// <summary>The fractional cell position of <paramref name="world"/>.</summary>
    public Vector2 WorldToCellCoordinates(Vector2 world) => (world - Origin) / CellSize;

    /// <summary>The cell <paramref name="world"/> is inside of, may throw if out of bounds.</summary>
    public Point WorldToCell(Vector2 world) {
        var coordinates = WorldToCellCoordinates(world);
        return new Point((int)MathF.Floor(coordinates.X + 0.5f), (int)MathF.Floor(coordinates.Y + 0.5f));
    }

    /// <inheritdoc cref="WorldToCell(Vector2)"/>
    public Point WorldToCell(Vector3 world) => WorldToCell(world.FlattenZ());

    /// <summary>Moves <paramref name="world"/> to the center of the cell it's in.</summary>
    public Vector2 Snap(Vector2 world) {
        var cell = WorldToCell(world);
        return CellToWorld(cell.X, cell.Y);
    }

    /// <summary>Enumerates from cell (0, 0) to cell (<see cref="Width"/> - 1, <see cref="Height"/> - 1).</summary>
    public IEnumerator<MapCell> GetEnumerator() => ((IEnumerable<MapCell>)_cells).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _cells.GetEnumerator();

    public override string ToString() => $"MapGrid {Width}x{Height} (cell size {CellSize}, origin {Origin})";
}

/// <summary>One square of a <see cref="MapGrid"/>: where it is, and the block and / or tank placed on it.</summary>
public sealed class MapCell {
    public MapGrid Grid { get; }
    public int X { get; }
    public int Y { get; }
    public Point Coordinates => new(X, Y);

    /// <summary>Position in the grid. (<c>Y * Width + X</c>).</summary>
    public int Index => Y * Grid.Width + X;

    /// <summary>World XZ position of this cell's center.</summary>
    public Vector2 Position { get; }
    /// <summary>3D world position of this cell's center, on the floor.</summary>
    public Vector3 Position3D => new(Position.X, 0f, Position.Y);

    /// <summary>The <see cref="Block.Id"/> of the block on this cell, or -1.</summary>
    public int BlockId { get; set; } = -1;
    /// <summary>The <see cref="Tank.WorldId"/> of the tank on this cell, or -1.</summary>
    public int TankId { get; set; } = -1;

    /// <summary>The block on this cell, if any.</summary>
    public Block? PlacedBlock => BlockId > -1 ? Block.AllBlocks[BlockId] : null;
    /// <summary>The tank on this cell, if any.</summary>
    public Tank? PlacedTank => TankId > -1 ? GameHandler.AllTanks[TankId] : null;

    public bool HasBlock => BlockId > -1;
    public bool HasTank => TankId > -1;
    public bool IsEmpty => BlockId < 0 && TankId < 0;

    internal MapCell(MapGrid grid, int x, int y) {
        Grid = grid;
        X = x;
        Y = y;
        Position = grid.CellToWorld(x, y);
    }

    /// <summary>Forgets this cell's block and tank, but doesn't remove them from the game. Probably has few use cases individually.</summary>
    public void Forget() {
        BlockId = -1;
        TankId = -1;
    }

    public override string ToString() => $"Cell ({X}, {Y})";
}
