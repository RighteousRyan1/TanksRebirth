using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using tainicom.Aether.Physics2D.Dynamics;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.ModSupport;
using TanksRebirth.GameContent.Systems.Coordinates;
using TanksRebirth.GameContent.Systems.ParticleSystem;
using TanksRebirth.GameContent.Tanks;
using TanksRebirth.Graphics;
using TanksRebirth.Graphics.Drawing;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework.Interfaces;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.Core.Interfaces;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent;

public struct BlockTemplate {
    public byte Stack;
    public int Type;
    public Vector2 Position;
    public byte TpLink;

    public readonly Block GetBlock() {
        Block c = new(Type, Stack, Position) {
            //c.Position = Position;
            //if (c.Body != null)
            //c.Body.Position = Position;
            TpLink = TpLink
        };

        for (int i = 0; i < PlacementSquare.Placements.Count; i++) {
            if (c.Position3D == PlacementSquare.Placements[i].Position)
                PlacementSquare.Placements[i].BlockId = c.Id;
        }

        return c;
    }
}

/// <summary>A class that is used for obstacles for <see cref="Tank"/>s.</summary>
public class Block : IGameObject, IHasModContent<ModBlock> {
    // TODO: ModBlock instance for the modblock used on this block instance...? to save performance in the future, obviously... same with other modded types
    public delegate void DestroyDelegate(Block block);
    /// <summary>Called after this <see cref="Block"/> is destroyed.</summary>
    public static event DestroyDelegate? OnDestroy;
    public delegate void UpdateDelegate(Block block);
    /// <summary>Called after this <see cref="Block"/> is updated on the CPU.</summary>
    public static event UpdateDelegate? OnPostUpdate;
    public delegate void PostRenderDelegate(Block block);
    /// <summary>Called after this <see cref="Block"/> is rendered on the GPU.</summary>
    public static event PostRenderDelegate? OnPostRender;
    public delegate void InitializeDelegate(Block block);
    /// <summary>Called after this <see cref="Block"/> is initialized.</summary>
    public static event InitializeDelegate? OnInitialize;

    public bool IgnoreRegister;

    public const float BLOCK_DEF_SCALING = 0.646f;

    Vector3 _offset;
    Vector3 _scaling = new(0.646f);
    public Texture2D Texture;
    Particle _shadow;

    public ModelTextureMap TextureMap;

    /// <summary>The teleportation index for this <see cref="Block"/>. Make sure that no more than 2 teleporters share this same number.</summary>
    public byte TpLink = 0;
    readonly int[] _tankCooldowns = new int[GameHandler.AllTanks.Length];

    public ModBlock ModdedData { get; internal set; }

    /// <summary>The type of this <see cref="Block"/>. (i.e: Wood, Cork, Hole)</summary>
    public int Type { get; set; }
    /// <summary>All <see cref="Block"/>s stored in the same array.</summary>
    public static Block[] AllBlocks = new Block[BlockMapPosition.MAP_WIDTH_169 * BlockMapPosition.MAP_HEIGHT];

    Vector2 _nonPhysPos;
    public Vector2 Position {
        get => Physics != null ? Physics.Position * Tank.UNITS_PER_METER : _nonPhysPos;
        set {
            if (Physics != null)
                Physics.Position = value / Tank.UNITS_PER_METER;
            else
                _nonPhysPos = value;
        }
    }
    public Vector3 Position3D => Position.ExpandZ();

    public Model Model;

    public Matrix World;
    public Matrix View;
    public Matrix Projection;
    /// <summary>Represents how tall (in arbitrary units) the top of this block is from the ground.</summary>
    public float HeightFromGround { get; private set; }
    /// <summary>The physics body for this <see cref="Block"/>.</summary>
    public Body Physics;
    /// <summary>The hitbox for this <see cref="Block"/>.</summary>
    public Rectangle Hitbox;

    byte _stack;
    /// <summary>How tall this <see cref="Block"/> is.</summary>
    public byte Stack {
        get => _stack;
        set {
            value = (byte)MathHelper.Clamp(value, 0, 7);
            // integer division on purpose, every 3 height values after stack '1' creates a new full block
            HeightFromGround = GetHeightFromGround(value);
            _stack = value;

            // ViewBox = new BoundingBox()
        }
    }
    public static float GetHeightFromGround(int stack) {
        var fullBlockCount = 1 + ((stack - 1) / 3);
        var fullBlockHeight = fullBlockCount * SIDE_LENGTH;
        // 1, 2, 1, 2, 3, 2
        var slabCount = (stack / 4) + (stack - 1) % 3;
        if (stack > 6) slabCount++;
        var fullSlabHeight = slabCount * SLAB_SIZE;
        return fullBlockHeight + fullSlabHeight;
    }
    // public BoundingBox ViewBox;
    /// <summary>The maximum height of any <see cref="Block"/>.</summary>
    public const byte MAX_BLOCK_HEIGHT = 7;

    /// <summary>The floating point square-rooted dimensions of any <see cref="Block"/>.</summary>
    public const float SIDE_LENGTH = 21.7f; // 24.5 = 0.7 | 21 = 0.6
    /// <summary>The floating-point height of any <see cref="Block"/> that has a slab in its height.</summary>
    public const float SLAB_SIZE = 11.5142857114f; // 13 = 0.7 | 11.14285714 = 0.6

    // 36, 18 respectively for normal size
    /// <summary>The total height of a <see cref="Block"/> when its stack is <see cref="MAX_BLOCK_HEIGHT"/>.</summary>
    public const float FULL_SIZE = 89.28f; // 100.8 = 0.7 | 86.4 = 0.6

    // 141 for normal

    /// <summary>The identifier for this <see cref="Block"/>.</summary>
    public int Id { get; }

    public BlockProperties Properties { get; set; } = new();
    /// <summary>Whether or not this <see cref="Block"/> is using its alternate model.</summary>
    public bool IsAlternateModel => Stack == 3 || Stack == 6;

    public static bool WouldUseAlternateModel(int stack) => stack == 3 || stack == 6;

    public static BlockProperties GetProperties(int type) {
        BlockProperties properties = new();
        switch (type) {
            case BlockID.Wood:
                properties.IsSolid = true;
                break;
            case BlockID.Cork:
                properties.IsSolid = true;
                properties.IsDestructible = true;
                break;
            case BlockID.Hole:
                properties.IsSolid = false;
                properties.CanStack = false;
                properties.HasShadow = false;
                break;
            case BlockID.Teleporter:
                properties.IsSolid = false;
                properties.IsCollidable = false;
                properties.CanStack = false;
                properties.HasShadow = false;
                break;
        }
        return properties;
    }
    public void Swap(int type) {
        Type = type;

        var model = GameScene.Theme switch {
            MapTheme.Vanilla => IsAlternateModel ? ModelGlobals.BlockStackAlt : ModelGlobals.BlockStack,
            MapTheme.Christmas => IsAlternateModel ? ModelGlobals.BlockStackAltSnowy : ModelGlobals.BlockStackSnowy,
            _ => throw new Exception()
        };

        switch (type) {
            case BlockID.Wood:
                Model = model.Asset;
                Texture = GameScene.Assets["block.1"];
                break;
            case BlockID.Cork:
                Model = model.Asset;
                Texture = GameScene.Assets["block.2"];
                break;
            case BlockID.Hole:
                Model = ModelGlobals.FlatFace.Asset;
                Texture = GameScene.Assets["block_harf.1"];
                break;
            case BlockID.Teleporter:
                Model = ModelGlobals.Teleporter.Asset;
                Texture = GameScene.Assets["teleporter"];

                TextureMap = new() {
                    ["Button"] = Texture,
                    ["Ring"] = TextureGlobals.Pixels[Color.Red]
                };
                break;
        }
        Properties = GetProperties(type);

        TextureMap ??= new() {
            ["base"] = Texture,
            ["snow"] = GameScene.Assets["snow"]
        };

        if (Properties.HasShadow) {
            // fix this, but dont worry about it for now
            _shadow = GameHandler.Particles.MakeParticle(Position3D, GameResources.GetGameResource<Texture2D>($"Assets/textures/tank_shadow"));
            _shadow.TextureCrop = null;
            //bool moveL = true;
            //bool moveD = true;
            _shadow.Pitch = MathHelper.PiOver2;
            _shadow.Scale = new(1f);
            _shadow.Alpha = 1f;
            _shadow.HasAdditiveBlending = false;
            _shadow.UniqueBehavior = (a) => {
                // TODO: save for when i make shadows look... proper.
                //p.TextureCrop = new(0, 0, 32, 32);

                //moveL = p.TextureCrop.Value.X < 32;
                //moveD = p.TextureCrop.Value.Y < 32;

                //float coordOff = 16 * p.Scale.X;

                _shadow.Position = new Vector3(Position3D.X, 0.15f, Position3D.Z);
            };
            // TODO: Finish collisions
        }

        ModdedData?.PostInitialize();
    }

    /// <summary>Construct a <see cref="Block"/>.</summary>
    public Block(int type, int height, Vector2 position, bool ignoreRegister = false) {
        IgnoreRegister = ignoreRegister;
        Stack = (byte)MathHelper.Clamp(height, 0, 7);
        Type = type;

        var modelname = GameScene.Theme switch {
            MapTheme.Vanilla => IsAlternateModel ? "Assets/models/scene/block_stack_alt" : "Assets/models/scene/block_stack",
            MapTheme.Christmas => IsAlternateModel ? "Assets/christmas/block_stack_alt_snowy" : "Assets/christmas/block_stack_snowy",
            _ => ""
        };

        this.AttachModdedContent();

        Position = position;

        if (!ignoreRegister) {

            Id = Array.FindIndex(AllBlocks, block => block is null);
            AllBlocks[Id] = this;

            OnInitialize?.Invoke(this);
        }

        Swap(type);

        if (Properties.IsCollidable) {
            Physics = Tank.CollisionsWorld.CreateRectangle(SIDE_LENGTH / Tank.UNITS_PER_METER, SIDE_LENGTH / Tank.UNITS_PER_METER, 1f, position / Tank.UNITS_PER_METER, 0f, BodyType.Static);
            Physics.Tag = this;
        }

        _offset.Y = GetOffsetY(Stack, Properties.CanStack);
    }

    /// <summary>Remove this <see cref="Block"/> from the game scene and memory.</summary>
    public void Remove() {
        _shadow?.Destroy();

        if (Physics != null && Tank.CollisionsWorld.BodyList.Contains(Physics))
            Tank.CollisionsWorld.Remove(Physics);

        if (!IgnoreRegister) AllBlocks[Id] = null;
    }

    /// <summary>Make destruction particle effects and later, <see cref="Remove"/> this <see cref="Block"/>.</summary>
    public void Destroy() {
        if (!Properties.IsDestructible) return;

        DestructParticles();

        ModdedData?.OnDestroy();
        OnDestroy?.Invoke(this);
        Remove();
    }
    public void DestructParticles() {
        const int PARTICLE_COUNT = 12;

        for (int i = 0; i < PARTICLE_COUNT; i++) {
            var tex = GameResources.GetGameResource<Texture2D>(Client.ClientRandom.Next(0, 2) == 0 ? "Assets/textures/misc/tank_rock" : "Assets/textures/misc/tank_rock_2");

            var part = GameHandler.Particles.MakeParticle(Position3D, tex);
            part.HasAdditiveBlending = false;

            var vel = new Vector3(Client.ClientRandom.NextFloat(-3, 3), Client.ClientRandom.NextFloat(4, 6), Client.ClientRandom.NextFloat(-3, 3));
            part.Roll = -CameraGlobals.DEFAULT_ORTHOGRAPHIC_ANGLE;
            part.Scale = new(0.7f);
            part.Color = Color.Coral;

            part.UniqueBehavior = (p) => {
                vel.Y -= 0.2f;
                part.Position += vel;
                part.Alpha -= 0.025f;

                if (part.Alpha <= 0f)
                    part.Destroy();
            };
        }
    }

    public static float GetOffsetY(int stack, bool canStack) {
        // offset *= scale / BLOCK_DEF_SCALING;
        float offset = 0f;
        if (!canStack) {
            offset -= 0.1f;
            return offset;
        }

        switch (stack) {
            case 1:
                offset = FULL_SIZE - SIDE_LENGTH;
                break;
            case 2:
                offset = FULL_SIZE - (SIDE_LENGTH + SLAB_SIZE);
                break;
            case 3:
                offset = FULL_SIZE - (SIDE_LENGTH + SLAB_SIZE * 3);
                break;
            case 4:
                offset = FULL_SIZE - (SIDE_LENGTH * 2 + SLAB_SIZE);
                break;
            case 5:
                offset = FULL_SIZE - (SIDE_LENGTH * 2 + SLAB_SIZE * 2);
                break;
            case 6:
                offset = FULL_SIZE - (SIDE_LENGTH * 2 + SLAB_SIZE * 4);
                break;
            case 7:
                offset = FULL_SIZE - (SIDE_LENGTH * 3 + SLAB_SIZE * 2);
                break;
        }

        return offset;
    }

    void IGameObject.OnDestroy() {
        OnDestroy?.Invoke(this);
    }

    void IGameObject.OnInitialize() {
        OnInitialize?.Invoke(this);
    }

    public void OnPreRender() { }

    // somethind buhhstid with snowy block models
    public void OnRender() {
        Projection = CameraGlobals.GameProjection;
        View = CameraGlobals.GameView;
        World = Matrix.CreateScale(_scaling) * Matrix.CreateTranslation(Position3D - _offset);
        // TODO: seeing this, don't make this poor CPU have overhead (use derived types!)
        foreach (var mesh in Model.Meshes) {
            foreach (BasicEffect effect in mesh.Effects) {
                effect.World = World;
                effect.View = View;
                effect.Projection = Projection;

                effect.TextureEnabled = true;
                effect.Texture = TextureMap[mesh.Name];

                effect.SetDefaultGameLighting_IngameEntities(10f);

                effect.DirectionalLight0.Direction *= 0.1f;
                effect.Alpha = 1f;
            }
            mesh.Draw();
        }

        ModdedData?.PostRender();
        OnPostRender?.Invoke(this);
    }

    void IGameObject.OnPostRender() {
        OnPostRender?.Invoke(this);
    }

    public void OnUpdate() {
        Hitbox = new((int)(Position.X - SIDE_LENGTH / 2 + 1), (int)(Position.Y - SIDE_LENGTH / 2), (int)SIDE_LENGTH - 1, (int)SIDE_LENGTH);
        _offset = new();

        if (Type == BlockID.Teleporter) {
            foreach (var tnk in GameHandler.AllTanks) {
                if (tnk is null) continue;
                if (--_tankCooldowns[tnk.WorldId] > 0) continue;
                if (Vector2.Distance(tnk.Position, Position) >= SIDE_LENGTH) continue;

                var otherTp = AllBlocks.FirstOrDefault(bl => bl != null && bl != this && bl.TpLink == TpLink);

                if (Array.IndexOf(AllBlocks, otherTp) <= -1) continue;

                otherTp!._tankCooldowns[tnk.WorldId] = 120;
                tnk.Position = otherTp.Position;
            }
        }

        _offset.Y = GetOffsetY(Stack, Properties.CanStack);
        ModdedData?.PostUpdate();
        OnPostUpdate?.Invoke(this);
    }
}