using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tainicom.Aether.Physics2D.Dynamics;
using TanksRebirth.GameContent.ID;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Systems.PingSystem;

// TODO: finish images rendering
public class IngamePing {
    // 4 players, 7 pings possible per player
    public static IngamePing[] AllIngamePings = new IngamePing[28];
    private float _lifeTime;
    public float _scaleEase;
    public Vector3 Position { get; set; }
    public int PingId { get; set; }
    public int Id { get; private set; }
    public Color Color;
    private Model _model;
    private bool _delete;
    private static Texture2D _pingTexture;
    private Texture2D _pingGraphic;
    public static float MaxLifeTime = 60 * 15; // * seconds

    public Tank? TrackedTank;

    public IngamePing(Vector3 position, int pingId, Color color) {
        foreach (var ping in AllIngamePings) {
            if (ping is not null && ping.PingId == pingId && ping.Color == color) {
                ping._lifeTime = MaxLifeTime;
            }
        }
        _model = GameResources.GetGameResource<Model>("Assets/ping");
        _pingTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/ping/ping_tex");
        _pingGraphic = PingMenu.PingIdToTexture[pingId];
        Id = Array.IndexOf(AllIngamePings, null); // will be bad if pings despawn at unassigned times.
        PingId = pingId;
        Position = position;
        Color = color;
        SoundPlayer.PlaySoundInstance($"Assets/sounds/ping/tr_ping_{PingMenu.PingIdToName[pingId].Replace(' ', '_').ToLower()}.ogg", SoundContext.Effect);
        var p = GameHandler.Particles.MakeParticle(Position + new Vector3(0, 0.1f, 0), GameResources.GetGameResource<Texture2D>("Assets/textures/misc/light_particle"));
        p.Scale = new(0f);

        for (int i = 0; i < GameHandler.AllTanks.Length; i++) {
            var tank = GameHandler.AllTanks[i];
            if (tank is not null && Vector3.Distance(tank.Position3D, position) < 20) {
                TrackedTank = tank;
                break;
            }
        }

        //float x = 0f;
        p.UniqueBehavior = (a) => {
            //x += 0.003f * TankGame.DeltaTime;
            //var val = Easings.InOutElastic(x);
            var val = 0.005f;
            GeometryUtils.Add(ref p.Scale, val); //Easings.OutSine()
            p.Alpha -= val * 1.8f * TankGame.DeltaTime;
            p.Roll = MathHelper.PiOver2;

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
        _lifeTime += TankGame.DeltaTime;

        var easeSpeed = 0.025f;

        if (TrackedTank != null) {
            Position = TrackedTank.Position3D;
        }

        if (_lifeTime > MaxLifeTime) {
            _scaleEase -= TankGame.DeltaTime * easeSpeed;
            if (_scaleEase <= 0) {
                _delete = true;
            }
        } else {
            _scaleEase += TankGame.DeltaTime * easeSpeed;
        }
        _scaleEase = MathHelper.Clamp(_scaleEase, 0, 1);
    }

    public void Render() {  
        var easing = EasingFunction.InOutSine;
        var fullEase = Easings.GetEasingBehavior(easing, _scaleEase);
        foreach (ModelMesh mesh in _model.Meshes) {
            foreach (BasicEffect effect in mesh.Effects) {
                effect.World = Matrix.CreateScale(fullEase * 20f, fullEase * 20, fullEase * 30) * Matrix.CreateRotationX(-MathHelper.PiOver2) * Matrix.CreateRotationY(MathHelper.PiOver2) * Matrix.CreateTranslation(Position);
                effect.View = TankGame.GameView;
                effect.Projection = TankGame.GameProjection;

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
        TankGame.SpriteRenderer.Draw(_pingGraphic, 
            MatrixUtils.ConvertWorldToScreen(Position + new Vector3(0, fullEase * 2 * 30, 0), Matrix.Identity, TankGame.GameView, TankGame.GameProjection), 
            null, Color, 0f, _pingGraphic.Size() / 2, Vector2.One * 0.25f * fullEase, default, 0f);
    }
    public static IngamePing CreateFromTankSender(Vector3 position3d, int pingId, int playerId, bool send = false) {
        if (send)
            Client.SendMapPing(position3d, pingId, playerId);
        return new IngamePing(
            position3d, 
            pingId, 
            PlayerID.PlayerTankColors[playerId].ToColor());
    }
    public static IngamePing CreateFromTankSender(Vector2 position2d, int pingId, int playerId, bool send = false) {
        if (send)
            Client.SendMapPing(MatrixUtils.GetWorldPosition(position2d), pingId, playerId);
        return new IngamePing(
            MatrixUtils.GetWorldPosition(position2d),
            pingId,
            PlayerID.PlayerTankColors[playerId].ToColor());
    }
}
