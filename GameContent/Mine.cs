using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Systems.AI;
using TanksRebirth.GameContent.Systems.TankSystem;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent;

public sealed class Mine : IAITankDanger {

    public delegate void ExplodeDelegate(Mine mine);
    public static event ExplodeDelegate? OnExplode;
    public delegate void PostUpdateDelegate(Mine mine);
    public static event PostUpdateDelegate? OnPostUpdate;
    public delegate void PostRenderDelegate(Mine mine);
    public static event PostRenderDelegate? OnPostRender;

    // this used to be 500. why?
    public const int MAX_MINES = 50;
    public static Mine[] AllMines { get; } = new Mine[MAX_MINES];

    public Tank? Owner;

    Vector2 _oldPosition;
    public Vector2 Position { get; set; }

    public Matrix View;
    public Matrix Projection;
    public Matrix World;

    public Color InactiveColor = new(219, 228, 64);
    public Color ActiveColor = new(231, 62, 99);

    public Vector3 Position3D => Position.ExpandZ();

    public Model Model;

    static Texture2D? _mineTexture;
    static Texture2D? _envTexture;

    public int Id { get; private set; }
    public int Team => Owner?.Team ?? TeamID.NoTeam;

    readonly ModelMesh? _mineMesh;
    readonly ModelMesh? _envMesh;

    public OggAudio? TickingNoise;

    public Rectangle Hitbox;

    float _oldDetonateTime;
    /// <summary>The time left (in ticks) until detonation.</summary>
    public float DetonateTime;
    /// <summary>The time until detonation (in ticks) from when this <see cref="Mine"/> was/is created.</summary>
    public readonly float DetonateTimeMax;

    bool _tickRed;
    /// <summary>Whether or not this <see cref="Mine"/> is near destructible <see cref="Block"/>s.</summary>
    public bool IsNearDestructibles { get; private set; }

    public float MineScale = 1f;
    /// <summary>The radius of this <see cref="Mine"/>'s explosion.</summary>
    public float ExplosionRadius;
    public float ExplosionRadiusInUnits => ExplosionRadius * 65f;

    /// <summary>Whether or not this <see cref="Mine"/> has detonated.</summary>
    public bool Detonated { get; set; }

    /// <summary>The amount of time until detonation this <see cref="Mine"/> is set to when an enemy is within <see cref="MineReactRadius"/>.</summary>
    public int MineReactTime = 30;
    /// <summary>The radius of which this mine will shorten its detonation time to <see cref="MineReactTime"/>.<para></para>
    /// Is automatically set to <see cref="ExplosionRadius"/><c> * 0.8f</c></summary>
    public float MineReactRadius;

    public const float TICKS_OF_FLASHING = 120;

    /// <summary>
    /// Creates a new <see cref="Mine"/>.
    /// </summary>
    /// <param name="owner">The <see cref="Tank"/> which owns this <see cref="Mine"/>.</param>
    /// <param name="pos">The position of this <see cref="Mine"/> in the game world.</param>
    /// <param name="detonateTime">The time it takes for this <see cref="Mine"/> to detonate.</param>
    /// <param name="radius">The radius of this <see cref="Mine"/>'s explosion.</param>
    public Mine(Tank? owner, Vector2 pos, float detonateTime, float radius = 1f) { // radius, old = 65
        Owner = owner;
        ExplosionRadius = radius;

        AITank.Dangers.Add(this);

        Model = ModelGlobals.Mine.Asset;

        DetonateTime = detonateTime;
        DetonateTimeMax = detonateTime;

        Position = pos;

        if (owner != null) {
            var placeSound = SoundPlayer.PlaySoundInstance("Assets/sounds/mine_place.ogg", SoundContext.Effect, 0.5f);

            //if (CameraGlobals.IsUsingFirstPresonCamera)
            //    SoundUtils.CreateSpatialSound(placeSound, Position3D, CameraGlobals.RebirthFreecam.Position);
        }

        _mineMesh = Model.Meshes["polygon1"];
        _envMesh = Model.Meshes["polygon0"];

        MineReactRadius = ExplosionRadius * ExplosionRadiusInUnits;

        int index = Array.IndexOf(AllMines, AllMines.First(mine => mine is null));

        Id = index;

        AllMines[index] = this;
    }
    public static void InitTextures() {
        _mineTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/mine/mine_env");
        _envTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/mine/mine_shadow");
    }
    /// <summary>Detonates this <see cref="Mine"/>.</summary>
    public void Detonate() {
        Detonated = true;
        var scale = ExplosionRadiusInUnits * 0.101f * (Difficulties.Types["UltraMines"] ? 2 : 1);
        var expl = new Explosion(Position, scale, Owner);

        if (Owner != null)
            Owner.OwnedMineCount--;
        TickingNoise?.Stop();

        OnExplode?.Invoke(this);

        Client.SyncMineDetonate(this);

        Remove();
    }

    public void Remove() {
        TickingNoise?.Stop();
        AITank.Dangers.Remove(this);
        AllMines[Id] = null;
    }

    internal void Update() {
        if (!GameScene.ShouldRenderAll || (!CampaignGlobals.InMission && !MainMenuUI.IsActive))
            return;

        World = Matrix.CreateScale(MineScale * 0.6f) * Matrix.CreateTranslation(Position3D);

        Hitbox = new((int)Position.X - 10, (int)Position.Y - 10, 20, 20);

        if (Server.NetManager != null || !Client.IsConnected()) {
            DetonateTime -= RuntimeData.DeltaTime;

            if (DetonateTime < TICKS_OF_FLASHING) {
                if (DetonateTime % 3.5f <= RuntimeData.DeltaTime) {
                    _tickRed = !_tickRed;
                }
                if (_oldDetonateTime > TICKS_OF_FLASHING && Owner is not null && Owner is PlayerTank) {
                    SoundPlayer.PlaySoundInstance("Assets/sounds/mine_trip.ogg", SoundContext.Effect, 1f);
                }
            }
            if (Owner is not null && Owner is PlayerTank) {
                if (DetonateTime < TICKS_OF_FLASHING - 5 && _oldDetonateTime > TICKS_OF_FLASHING - 5) {
                    TickingNoise = SoundPlayer.PlaySoundInstance("Assets/sounds/mine_tick.ogg", SoundContext.Effect, 0.7f);
                    TickingNoise.Instance.IsLooped = true;
                }
            }

            if (DetonateTime <= 0)
                Detonate();

            foreach (var shell in Shell.AllShells) {
                if (shell is not null && shell.Hitbox.Intersects(Hitbox)) {
                    shell.Destroy(Shell.DestructionContext.WithMine);
                    Detonate();
                }
            }

            if (Position != _oldPosition) // magicqe number
                IsNearDestructibles = Block.AllBlocks.Any(b => b != null && Position.Distance(b.Position) <= ExplosionRadius - 6f && b.Properties.IsDestructible);

            // NOTE: this scope may be inconsistent over a server? check soon.
            if (DetonateTime > MineReactTime) {
                bool tryDetonate = false;
                foreach (var tank in GameHandler.AllTanks) {
                    if (tank is null) continue;
                    if (tank.IsDestroyed) continue;
                    if (Owner is null) continue;

                    var dist = GameUtils.Distance_WiiTanksUnits(tank.Position, Position);
                    if (dist > MineReactRadius) continue;

                    // don't try any further, we don't want to explode if these conditions are true
                    if (tank.WorldId == Owner!.WorldId || tank.IsOnSameTeamAs(Team)) {
                        tryDetonate = false;
                        break; // good check in case the tank is on NoTeam;
                    }

                    tryDetonate = true;
                }
                if (tryDetonate)
                    DetonateTime = MineReactTime;
            }
        }

        _oldPosition = Position;
        _oldDetonateTime = DetonateTime;

        OnPostUpdate?.Invoke(this);
    }

    internal void Render() {
        if (!GameScene.ShouldRenderAll)
            return;

        View = CameraGlobals.GameView;
        Projection = CameraGlobals.GameProjection;
        DebugManager.DrawDebugString(TankGame.SpriteRenderer, $"DetonationTime: {DetonateTime}/{DetonateTimeMax}\nNearDestructibles: {IsNearDestructibles}\nId: {Id}", MatrixUtils.ConvertWorldToScreen(Vector3.Zero, World, View, Projection) - new Vector2(0, 20), 1, centered: true);

        // this is horrendous but it looks better
        TankGame.Instance.GraphicsDevice.BlendState = BlendState.Additive;
        foreach (ModelMesh mesh in Model.Meshes) {
            foreach (BasicEffect effect in mesh.Effects) {
                effect.World = World;
                effect.View = View;
                effect.Projection = Projection;

                effect.TextureEnabled = true;

                if (mesh == _mineMesh) {
                    effect.EmissiveColor = (_tickRed ?
                        ActiveColor.ToVector3() : InactiveColor.ToVector3())
                        * SceneManager.GameLight.Brightness;
                    effect.DiffuseColor *= 0.5f;
                    effect.Texture = _mineTexture;
                    effect.Alpha = 1f;

                    mesh.Draw();
                }
                effect.SetDefaultGameLighting_IngameEntities();
            }
        }
        TankGame.Instance.GraphicsDevice.BlendState = BlendState.AlphaBlend;
        foreach (ModelMesh mesh in Model.Meshes) {
            foreach (BasicEffect effect in mesh.Effects) {
                effect.World = World;
                effect.View = View;
                effect.Projection = Projection;

                effect.TextureEnabled = true;

                if (mesh == _envMesh) {
                    effect.Texture = _envTexture;
                    effect.Alpha = 0.6f;
                    mesh.Draw();
                }
                effect.SetDefaultGameLighting_IngameEntities();
            }
        }
        for (int i = 0; i < (Lighting.AccurateShadows ? 2 : 1); i++) {
            foreach (ModelMesh mesh in Model.Meshes) {
                foreach (BasicEffect effect in mesh.Effects) {
                    effect.World = i == 0 ? World : World * Matrix.CreateShadow(Lighting.AccurateLightingDirection, new(Vector3.UnitY, 0)) * Matrix.CreateTranslation(0, 0.2f, 0);
                    effect.View = View;
                    effect.Projection = Projection;

                    effect.TextureEnabled = false;

                    ActiveColor = new Color(231, 62, 99);
                    InactiveColor = new Color(219, 228, 64);
                    if (mesh == _mineMesh) {
                        effect.EmissiveColor = (_tickRed ? 
                            ActiveColor.ToVector3() : InactiveColor.ToVector3())
                            * SceneManager.GameLight.Brightness;
                        effect.DiffuseColor *= 0.5f;
                        //effect.Texture = _mineTexture;
                        effect.Alpha = 1f;

                        mesh.Draw();
                    }
                    else {
                        if (!Lighting.AccurateShadows) {
                            //effect.Texture = _envTexture;
                            effect.Alpha = 0.6f;
                            //mesh.Draw();
                        }
                    }
                    effect.SetDefaultGameLighting_IngameEntities();
                }
            }
        }
        OnPostRender?.Invoke(this);
    }
}