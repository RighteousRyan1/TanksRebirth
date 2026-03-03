using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Systems.CommandsSystem;
using TanksRebirth.GameContent.Tanks;
using TanksRebirth.GameContent.Tanks.AI;

using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.Graphics;
using TanksRebirth.Graphics.Drawing;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent;

public sealed class Mine : IAITankDanger {
    public struct MineDrawParams {
        public Texture2D? MineTexture;
        public Texture2D? ShadowTexture;

        public Model Model;
    }

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

    public BasicDrawParams DrawParams = new();
    public MineDrawParams DrawParamsMine;

    public Color InactiveColor = new(219, 228, 64);
    public Color ActiveColor = new(231, 62, 99);

    public Vector3 Position3D => Position.ExpandZ();

    public int Id { get; private set; }
    public int Team => Owner?.Team ?? TeamID.NoTeam;

    readonly ModelMesh? _mineMesh;
    readonly ModelMesh? _shadowMesh;

    public OggAudio? TickingNoise;

    public float HitBoxSize = 18.0f;
    public BoundingBox HitBox;

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
    Mine(Tank? owner, Vector2 pos, float detonateTime, float radius = 1f) { // radius, old = 65
        Owner = owner;
        ExplosionRadius = radius;
        DetonateTime = detonateTime;
        DetonateTimeMax = detonateTime;
        Position = pos;

        DrawParamsMine.Model = ModelGlobals.Mine.Asset;

        if (owner != null) {
            var placeSound = SoundPlayer.PlaySoundInstance("Assets/sounds/mine_place.ogg", SoundContext.Effect, 0.5f, pitchOverride: GameUtils.NaturalPitchShift);

            //if (CameraGlobals.IsUsingFirstPresonCamera)
            //    SoundUtils.CreateSpatialSound(placeSound, Position3D, CameraGlobals.RebirthFreecam.Position);
        }

        DrawParamsMine.MineTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/mine/mine_env");
        DrawParamsMine.ShadowTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/mine/mine_shadow").Duplicate(TankGame.Instance.GraphicsDevice);

        _mineMesh = DrawParamsMine.Model.Meshes["mine"];
        _shadowMesh = DrawParamsMine.Model.Meshes["shadow"];

        MineReactRadius = ExplosionRadius * ExplosionRadiusInUnits;

        int index = Array.IndexOf(AllMines, AllMines.First(mine => mine is null));
        Id = index;
        AllMines[index] = this;

        AITank.Dangers.Add(this);
    }

    /// <summary>
    /// Creates a new <see cref="Mine"/>. This method is thread-agnostic.
    /// </summary>
    /// <param name="owner">The <see cref="Tank"/> which owns this <see cref="Mine"/>.</param>
    /// <param name="pos">The position of this <see cref="Mine"/> in the game world.</param>
    /// <param name="detonateTime">The time it takes for this <see cref="Mine"/> to detonate.</param>
    /// <param name="explosionRadius">The radius of this <see cref="Mine"/>'s explosion.</param>
    public static Mine Create(Tank? owner, Vector2 position, float detonateTime, float explosionRadius = 1f) {
        var mine = TankGame.ThreadAgnostic(() => new Mine(owner, position, detonateTime, explosionRadius));
        return mine;
    }
    /// <summary>Detonates this <see cref="Mine"/>.</summary>
    public void Detonate() {
        Detonated = true;
        var scale = ExplosionRadiusInUnits * 0.101f * (Modifiers.Map[Modifiers.BIG_MINES] ? 2 : 1);
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
        if (!CampaignGlobals.InMission && !MainMenuUI.IsActive) return;

        DrawParams.World = Matrix.CreateScale(MineScale * 0.6f) * Matrix.CreateTranslation(Position3D);

        // this might need offsetting due to the nature of the orthographic camera
        HitBox = new(Position3D - new Vector3(HitBoxSize / 2, HitBoxSize / 2, HitBoxSize / 2),
                Position3D + new Vector3(HitBoxSize / 2, HitBoxSize / 2, HitBoxSize / 2));

        // only decrements mine timing if the host
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
                if (shell is not null && shell.Hitbox.Intersects(HitBox)) {
                    shell.Destroy(Shell.DestructionContext.WithMine);
                    Detonate();
                }
            }

            if (Position != _oldPosition) // magicqe number
                IsNearDestructibles = Block.AllBlocks.Any(b => b != null && Position.DistanceTo(b.Position) <= ExplosionRadius - 6f && b.Properties.IsDestructible);

            // NOTE: this scope may be inconsistent over a server? check soon.
            if (DetonateTime > MineReactTime) {
                bool tryDetonate = false;
                foreach (var tank in GameHandler.AllTanks) {
                    if (tank is null) continue;
                    if (tank.IsDestroyed) continue;
                    if (Owner is null) continue;

                    var dist = GameUtils.TanksDistance(tank.Position, Position);
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
        DrawParams.View = CameraGlobals.GameView;
        DrawParams.Projection = CameraGlobals.GameProjection;

        if (DebugManager.DebuggingEnabled) {
            DebugManager.DrawBoundingBox(HitBox, Color.White, CameraGlobals.GameView, CameraGlobals.GameProjection);
        }

        DebugManager.DrawDebugString(TankGame.SpriteRenderer, $"DetonationTime: {DetonateTime}/{DetonateTimeMax}\nNearDestructibles: {IsNearDestructibles}\nId: {Id}",
            MatrixUtils.ConvertWorldToScreen(Vector3.Zero, DrawParams.World, DrawParams.View, DrawParams.Projection) - new Vector2(0, 20), 1, centered: true);

        // this is horrendous but it looks better
        // this is fucking horrible - ryan, 11/24/25
        TankGame.Instance.GraphicsDevice.BlendState = BlendState.Additive;
        foreach (ModelMesh mesh in DrawParamsMine.Model.Meshes) {
            foreach (BasicEffect effect in mesh.Effects) {
                effect.World = DrawParams.World;
                effect.View = DrawParams.View;
                effect.Projection = DrawParams.Projection;

                effect.TextureEnabled = true;

                if (mesh == _mineMesh) {
                    effect.EmissiveColor = (_tickRed ?
                        ActiveColor.ToVector3() : InactiveColor.ToVector3())
                        * SceneManager.GameLight.Brightness;
                    effect.DiffuseColor *= 0.5f;
                    effect.Texture = DrawParamsMine.MineTexture;
                    effect.Alpha = 1f;

                    effect.SetDefaultGameLighting_IngameEntities(DrawParams.LightPower, DrawParams.AmbientPower, DrawParams.UsePhong, DrawParams.LightDirection);
                    mesh.Draw();
                }
            }
        }
        TankGame.Instance.GraphicsDevice.BlendState = BlendState.AlphaBlend;
        foreach (ModelMesh mesh in DrawParamsMine.Model.Meshes) {
            foreach (BasicEffect effect in mesh.Effects) {
                effect.World = DrawParams.World;
                effect.View = DrawParams.View;
                effect.Projection = DrawParams.Projection;

                effect.TextureEnabled = true;

                if (mesh == _shadowMesh) {
                    if (!CommandGlobals.DrawMeshShadows)
                        continue;
                    effect.Texture = DrawParamsMine.ShadowTexture;
                    effect.Alpha = 0.6f;
                    effect.SetDefaultGameLighting_IngameEntities(DrawParams.LightPower, DrawParams.AmbientPower, DrawParams.UsePhong, DrawParams.LightDirection);
                    mesh.Draw();
                }
            }
        }
        foreach (ModelMesh mesh in DrawParamsMine.Model.Meshes) {
            foreach (BasicEffect effect in mesh.Effects) {
                effect.World = DrawParams.World;
                effect.View = DrawParams.View;
                effect.Projection = DrawParams.Projection;

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
                    if (!CommandGlobals.DrawMeshShadows)
                        continue;
                    effect.Alpha = 0.6f;
                }
                effect.SetDefaultGameLighting_IngameEntities(DrawParams.LightPower, DrawParams.AmbientPower, DrawParams.UsePhong, DrawParams.LightDirection);
            }
        }
        OnPostRender?.Invoke(this);
    }
}