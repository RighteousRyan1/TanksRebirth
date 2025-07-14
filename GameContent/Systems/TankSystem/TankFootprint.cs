using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.GameContent.Systems.ParticleSystem;
using TanksRebirth.Internals;

namespace TanksRebirth.GameContent;

// TODO: this can definitely become a rendertarget translated into 3d space.
public class TankFootprint {
    public static bool ShouldTracksFade;

    public static TankFootprint[] AllFootprints = new TankFootprint[TankGame.Settings.TankFootprintLimit];

    public Vector3 Scale;
    public Vector3 Position;
    public float Rotation;

    public readonly Tank Owner;
    public Texture2D Texture;

    bool _destroy;
    Particle _track;

    internal static int total_treads_placed;

    public readonly bool alternate;

    public long lifeTime;

    public int Id = 0;

    //public static DecalSystem DecalHandler; // = new(TankGame.SpriteRenderer, TankGame.Instance.GraphicsDevice);

    public TankFootprint(Tank owner, float rotation, bool alt = false) {
        Rotation = rotation;
        alternate = alt;
        Owner = owner;
        if (total_treads_placed + 1 >= TankGame.Settings.TankFootprintLimit) {
            // Old implementation of this code in case of any regressions.
            // footprints[Array.IndexOf(footprints, footprints.Min(x => x.lifeTime > 0))] = null; // i think?

            Span<TankFootprint> footPrints = AllFootprints;
            ref var footprintSearchSpace = ref MemoryMarshal.GetReference(footPrints);

            var lifeTimeOfCurrentOldest = 0L;
            var indexOfOldest = 0;

            // Gets the index of the tank footprint that has the longest life, then sets it as null, deleting it (?)
            for (var i = 1; i < footPrints.Length; i++) {
                if (footPrints.Length <= i) break;
                var currentFprint = Unsafe.Add(ref footprintSearchSpace, i);

                if (currentFprint == null) continue;


                if (currentFprint.lifeTime <= lifeTimeOfCurrentOldest) continue;

                indexOfOldest = i;
                lifeTimeOfCurrentOldest = currentFprint.lifeTime;
            }

            if (footPrints[indexOfOldest] != null && !footPrints[indexOfOldest]._destroy) {
                footPrints[indexOfOldest]
                  ?.Remove(); // The particle will (on next update) Destroy itself and remove itself from the array.
                AllFootprints[indexOfOldest] = null;
                total_treads_placed--;
            }
        }

        alternate = alt;
        Id = total_treads_placed;
        Position = owner.Position3D;

        Texture = GameResources.GetGameResource<Texture2D>(alt
            ? $"Assets/textures/tank_footprint_alt"
            : $"Assets/textures/tank_footprint");

        _track = GameHandler.Particles.MakeParticle(Position, Texture);

        _track.HasAddativeBlending = false;

        _track.Pitch = MathHelper.PiOver2;

        var defScale = new Vector3(0.5f, 0.55f, 0.5f);
        _track.Scale = defScale;
        _track.Alpha = 0.7f;
        _track.Color = Color.White;
        _track.UniqueBehavior = track => {
            track.Position = Position;
            track.Yaw = rotation;
            track.Scale = Scale * defScale;

            if (ShouldTracksFade)
                track.Alpha -= 0.001f * RuntimeData.DeltaTime;

            if (track.Alpha <= 0) {
                Remove();
            }
        };

        AllFootprints[Array.IndexOf(AllFootprints, null)] = this;

        total_treads_placed++;

        /*DecalHandler.AddDecal(texture, MatrixUtils.ConvertWorldToScreen(Vector3.Zero,
            Matrix.CreateTranslation(Position), TankGame.GameView,
            TankGame.GameProjection), null, Color.White, rotation, BlendState.Opaque);*/

        // Render();
    }

    public void Update() => lifeTime++;
    public void Remove() => _track.Destroy();
}