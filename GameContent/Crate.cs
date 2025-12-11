using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals;
using Microsoft.Xna.Framework.Audio;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Graphics;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.GameContent.Systems.AI;
using TanksRebirth.GameContent.Systems.TankSystem;

namespace TanksRebirth.GameContent;

// TODO: due for an overhaul
public class Crate {
    public delegate void OpenDelegate(Crate crate);
    public static event OpenDelegate? OnOpen;
    public delegate void PostUpdateDelegate(Crate crate);
    public static event PostUpdateDelegate? OnPostUpdate;
    public delegate void PostRenderDelegate(Crate crate);
    public static event PostRenderDelegate? OnPostRender;

    const float MAGICAL_BOUNCE_NUMBER = 9.6f;
    public const int MAX_CRATES = 50;

    public static Crate[] AllCrates = new Crate[MAX_CRATES];

    public Vector3 Position;
    public Vector3 Velocity;

    public float Gravity;

    /// <summary>How much this <see cref="Crate"/> accelerates while falling in the air.</summary>
    public float DropAcceleration = 0.05f;

    /// <summary>The uniform scale of this <see cref="Crate"/>.</summary>
    public float Scale = 1f;

    public Model Model;

    readonly Matrix[] _faceWorlds = new Matrix[6];

    /// <summary>Whether or not an animation sequence plays when the <see cref="Crate"/> lands.</summary>
    public bool IsOpening { get; private set; }

    public int id;

    /// <summary>What <see cref="Tank"/> to spawn on opening, if any.</summary>
    public TankTemplate TankToSpawn;
    public bool ContainsTank = true;

    /// <summary>How fast this <see cref="Crate"/> shrinks when it starts to open.</summary>
    public float FadeScale = 0.05f;

    int _bounceCount;
    readonly int _maxBounces = 2;

    Crate() {
        Model = ModelGlobals.BoxFace.Asset;

        int index = Array.IndexOf(AllCrates, AllCrates.First(c => c is null));

        id = index;

        AllCrates[index] = this;
    }

    /// <summary>
    /// Spawns a new <see cref="Crate"/>.
    /// </summary>
    /// <param name="pos">The position to spawn the <see cref="Crate"/> in the game world.</param>
    /// <param name="gravity">The gravity which affects the <see cref="Crate"/> while it falls.</param>
    /// <returns>The <see cref="Crate"/> spawned.</returns>
    public static Crate SpawnCrate(Vector3 pos, float gravity) {
        var spawnSfx = "Assets/sounds/crate/CrateSpawn.ogg";

        SoundPlayer.PlaySoundInstance(spawnSfx, SoundContext.Effect, 0.2f);

        return new() {
            Position = pos,
            Gravity = gravity,
        };
    }
    public void Remove() {
        AllCrates[id] = null;
    }
    public void Render() {
        // face order: right, left, front, back, top, bottom
        // 9.6 is waht?
        var blockOffset = MAGICAL_BOUNCE_NUMBER * Scale;

        var rotationMtxX = Matrix.CreateRotationX(MathHelper.PiOver2);
        var rotationMtxZ = Matrix.CreateRotationZ(MathHelper.PiOver4);

        // why is this a scale matrix bro
        var scaleMtx = Matrix.CreateScale(Scale, 11 * Scale, 0.1f);

        _faceWorlds[0] = scaleMtx * rotationMtxZ * rotationMtxX * Matrix.CreateRotationZ(MathHelper.PiOver2) * Matrix.CreateTranslation(Position.X + blockOffset, Position.Y, Position.Z);
        _faceWorlds[1] = scaleMtx * rotationMtxZ * rotationMtxX * Matrix.CreateRotationZ(MathHelper.PiOver2) * Matrix.CreateTranslation(Position.X - blockOffset, Position.Y, Position.Z);

        _faceWorlds[2] = scaleMtx * rotationMtxZ * Matrix.CreateTranslation(Position.X, Position.Y, Position.Z - blockOffset);
        _faceWorlds[3] = scaleMtx * rotationMtxZ * Matrix.CreateTranslation(Position.X, Position.Y, Position.Z + blockOffset);

        _faceWorlds[4] = scaleMtx * rotationMtxZ * Matrix.CreateRotationX(MathHelper.PiOver2) * Matrix.CreateTranslation(Position.X, Position.Y + blockOffset, Position.Z);
        _faceWorlds[5] = scaleMtx * rotationMtxZ * Matrix.CreateRotationX(MathHelper.PiOver2) * Matrix.CreateTranslation(Position.X, Position.Y - blockOffset, Position.Z);

        for (int i = 0; i < _faceWorlds.Length; i++) {
            foreach (ModelMesh mesh in Model.Meshes) {
                foreach (BasicEffect effect in mesh.Effects) {
                    effect.World = _faceWorlds[i];
                    effect.View = CameraGlobals.GameView;
                    effect.Projection = CameraGlobals.GameProjection;

                    effect.SetDefaultGameLighting_IngameEntities();

                    effect.TextureEnabled = true;

                    effect.Texture = GameResources.GetGameResource<Texture2D>("Assets/Textures/ingame/block_other_c");

                    //if (IsOpening)
                    //effect.Alpha -= fadeScale;
                }

                mesh.Draw();
            }
        }
        OnPostRender?.Invoke(this);
    }
    public void Update() {
        if (!IsOpening) {
            Velocity.Y -= Gravity * 0.05f * RuntimeData.DeltaTime;

            // dropSpeed += dropSpeedAccel;

            Position += Velocity * RuntimeData.DeltaTime;

            if (Position.Y <= (MAGICAL_BOUNCE_NUMBER * Scale)) {
                if (Velocity.Y <= -1f) {
                    var spawnSfx = "Assets/sounds/crate/CrateImpact.ogg";

                    SoundPlayer.PlaySoundInstance(spawnSfx, SoundContext.Effect, 0.2f);

                    Velocity.Y = -Velocity.Y * 0.3f;

                    _bounceCount++;
                }

                if (_bounceCount > _maxBounces)
                    Open();
            }
        }

        else {
            Scale -= FadeScale;

            if (Scale <= 0)
                AllCrates[id] = null;
        }
        if (Position.Y < 0)
            Position.Y = 0;

        OnPostUpdate?.Invoke(this);
    }

    /// <summary>Open this <see cref="Crate"/>.</summary>
    public void Open() {
        IsOpening = true;

        if (ContainsTank) {
            var tier = TankToSpawn.AiTier;
            if (Modifiers.Map[Modifiers.MASTER])
                tier = Modifiers.VanillaToMasterModeConversions[tier];
            var t = new AITank(tier);
            t.Physics.Position = Position.FlattenZ() / Tank.UNITS_PER_METER;
            t.Position = Position.FlattenZ();
            t.IsDestroyed = false;
            t.Team = TankToSpawn.Team;
        }
        OnOpen?.Invoke(this);
    }
}