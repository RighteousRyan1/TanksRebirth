using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tainicom.Aether.Physics2D.Dynamics;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.Systems.PingSystem;

// TODO: finish images rendering
public class IngamePing {
    public static List<IngamePing> AllIngamePings = new();
    private float _lifeTime;
    public Vector3 Position { get; set; }
    public int PingID { get; set; }
    public int Id { get; private set; }
    public Color Color;
    private Model _model;
    private bool _delete;
    private static Texture2D _pingTexture;
    public static float MaxLifeTime = 60 * 15;

    public IngamePing(Vector3 position, int pingId, Color color) {
        _model = GameResources.GetGameResource<Model>("Assets/ping");
        _pingTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/ping/ping_tex");
        Id = AllIngamePings.Count; // will be bad if pings despawn at unassigned times.
        PingID = pingId;
        Position = position;
        Color = color;
        var p = GameHandler.ParticleSystem.MakeParticle(Position + new Vector3(0, 0.1f, 0), GameResources.GetGameResource<Texture2D>("Assets/textures/misc/light_particle"));
        p.Scale = new(0f);

        //float x = 0f;
        p.UniqueBehavior = (a) => {
            //x += 0.003f * TankGame.DeltaTime;
            //var val = Easings.InOutElastic(x);
            var val = 0.008f;
            GeometryUtils.Add(ref p.Scale, val); //Easings.OutSine()
            p.Alpha -= val * 0.6f;
            p.Roll = MathHelper.PiOver2;

            p.Color = Color;

            /*if (val >= 1 || val <= -1) {
                x = 0;
                p.Scale = Vector3.Zero;
            }*/
            if (p.Scale.X > 1.5f) {
                p.Scale = new(0.1f);
                p.Alpha = 1f;
            }

            if (_delete) {
                AllIngamePings[Id] = null;
                p.Destroy();
            }
        };
        AllIngamePings.Add(this);
    }

    public void Update() {
        _lifeTime += TankGame.DeltaTime;

        if (_lifeTime > MaxLifeTime) {
            _delete = true;
        }
    }

    public void Render() {
        foreach (ModelMesh mesh in _model.Meshes) {
            foreach (BasicEffect effect in mesh.Effects) {
                effect.World = Matrix.CreateScale(20f, 20f, 30f) * Matrix.CreateRotationX(-MathHelper.PiOver2) * Matrix.CreateTranslation(Position);
                effect.View = TankGame.GameView;
                effect.Projection = TankGame.GameProjection;

                effect.TextureEnabled = true;
                effect.Texture = _pingTexture;
                effect.Alpha = 0.50f;

                effect.DiffuseColor = Color.ToVector3();

                effect.SetDefaultGameLighting_IngameEntities();
            }
            mesh.Draw();
        }
    }
}
