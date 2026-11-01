using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Tanks;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Systems.PingSystem;

// TODO: finish images rendering
// maybe allow pinging ingame objects like shells or mines, etc
public class IngamePing {
    // 4 players, 1 ping possible per player. a few buffered spots since pings animate out of existence
    public static IngamePing[] AllIngamePings = new IngamePing[8];

    float _lifeTime;
    float _scaleEase;

    bool _delete;

    static Texture2D? _pingTexture;
    readonly Texture2D _pingGraphic;
    readonly Model _model;

    public Vector3 Position { get; set; }
    public int Type { get; set; }
    public int Id { get; private set; }
    public int Owner { get; private set; }
    public Color Color { get; set; }
    public static float MaxLifeTime = 60 * 10; // * seconds. 15 seconds was far too long imo

    public Tank? TrackedTank;

    /// <summary>
    /// Creates a new <see cref="IngamePing"/>. This method is thread-agnostic.
    /// </summary>
    /// <returns>The created <see cref="IngamePing"/>, or <see langword="null"/> if every ping slot is in use.</returns>
    public static IngamePing? Create(Vector3 position, int type, Color color, int owner) {
        var ping = TankGame.ThreadAgnostic(() => Array.IndexOf(AllIngamePings, null) < 0 ? null : new IngamePing(position, type, color, owner));
        return ping;
    }

    IngamePing(Vector3 position, int pingType, Color color, int owner) {
        foreach (var ping in AllIngamePings) {
            if (ping == null) continue;

            // tells the ping to fade away if the same owner already has another ping active
            if (ping.Owner == owner)
                ping._lifeTime = MaxLifeTime;
        }
        _model = ModelGlobals.Ping.Asset;
        _pingTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/ping/ping_tex");
        _pingGraphic = PingMenu.PingIdToTexture[pingType];
        Id = Array.IndexOf(AllIngamePings, null); // will be bad if pings despawn at unassigned times.
        Type = pingType;
        Position = position;
        Color = color;
        SoundPlayer.PlaySoundInstance($"Assets/sounds/ping/tr_ping_{PingMenu.PingIdToName[pingType].Replace(' ', '_').ToLower()}.ogg", SoundContext.Effect, position: Position);
        var p = GameHandler.Particles.MakeParticle(Position + new Vector3(0, 0.1f, 0), GameResources.GetGameResource<Texture2D>("Assets/textures/misc/light_particle"));
        p.Scale = new(0f);

        for (int i = 0; i < GameHandler.AllTanks.Length; i++) {
            var tank = GameHandler.AllTanks[i];
            if (tank is null) continue;
            if (tank.IsDestroyed) continue;
            if (Vector3.Distance(tank.Position3D, position) > 20) continue;
            TrackedTank = tank;
        }

        //float x = 0f;
        p.UpdateCallback = (a) => {
            //x += 0.003f * RuntimeData.DeltaFrames;
            //var val = Easings.InOutElastic(x);
            var val = 0.005f;
            GeometryUtils.Add(ref p.Scale, val); //Easings.OutSine()
            p.Alpha -= val * 1.8f * RuntimeData.DeltaFrames;
            p.Pitch = MathHelper.PiOver2;

            p.Color = Color;

            if (TrackedTank != null)
                p.Position = TrackedTank.Position3D;

            if (p.Scale.X > 1.5f) {
                p.Scale = new(0.1f);
                p.Alpha = 1f;
            }

            if (_delete) {
                AllIngamePings[Id] = null;
                p.Destroy();
            }
        };

        AllIngamePings[Id] = this;
    }

    public void Update() {
        _lifeTime += RuntimeData.DeltaFrames;

        var easeSpeed = 0.05f;

        if (TrackedTank != null)
            Position = TrackedTank.Position3D;
        
        if (_lifeTime > MaxLifeTime) {
            _scaleEase -= RuntimeData.DeltaFrames * easeSpeed;
            if (_scaleEase <= 0)
                _delete = true;
        } else
            _scaleEase += RuntimeData.DeltaFrames * easeSpeed;

        _scaleEase = MathHelper.Clamp(_scaleEase, 0, 1);
    }

    public void Render() {  
        var easing = EasingFunction.InOutSine;
        var fullEase = Easings.ComputeEase(easing, _scaleEase);
        foreach (ModelMesh mesh in _model.Meshes) {
            foreach (BasicEffect effect in mesh.Effects) {
                effect.World = Matrix.CreateScale(fullEase * 20f, fullEase * 20, fullEase * 30) * Matrix.CreateRotationX(-MathHelper.PiOver2) * Matrix.CreateRotationY(MathHelper.PiOver2) * Matrix.CreateTranslation(Position);
                effect.View = CameraGlobals.GameView;
                effect.Projection = CameraGlobals.GameProjection;

                effect.TextureEnabled = true;
                effect.Texture = _pingTexture;
                effect.Alpha = 0.50f;


                effect.SetDefaultGameLighting_IngameEntities();
                effect.DiffuseColor = Color.ToVector3();
                effect.AmbientLightColor = Color.ToVector3();
                effect.EmissiveColor = Color.ToVector3();
                effect.SpecularColor = Color.ToVector3();
                effect.FogColor = Color.ToVector3();
            }
            mesh.Draw();
        }
        var matProj = MatrixUtils.ConvertWorldToScreen(Position + new Vector3(0, fullEase * 2 * 30, 0), Matrix.Identity, CameraGlobals.GameView, CameraGlobals.GameProjection);
        TankGame.SpriteRenderer.Draw(_pingGraphic, 
            matProj, null, Color, 0f, 
            _pingGraphic.Size() / 2, (Vector2.One * 0.75f * fullEase).ToResolution(), default, 0f);
    }
    public static IngamePing? CreateFromTankSender(Vector3 position3d, int pingId, int playerId, bool send = false) {
        if (send)
            Client.SendMapPing(position3d, pingId, playerId);
        return Create(
            position3d, 
            pingId, 
            PlayerID.PlayerTankColors[playerId], playerId);
    }
}
