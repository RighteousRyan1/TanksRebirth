using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using WiiPlayTanksRemake.Enums;
using System.Linq;
using WiiPlayTanksRemake.Internals.Common.GameInput;
using Microsoft.Xna.Framework.Input;
using WiiPlayTanksRemake.Internals.Common.Utilities;
using WiiPlayTanksRemake.Internals;
using Microsoft.Xna.Framework.Audio;
using WiiPlayTanksRemake.Internals.Common;
using WiiPlayTanksRemake.Internals.Core.Interfaces;

namespace WiiPlayTanksRemake.GameContent
{
    public class AITank : ITank
    {
        public bool Invisible { get; private set; }
        public bool Stationary { get; private set; }
        public bool Dead { get; set; }

        public float Speed { get; set; } = 1f;
        public float BulletShootSpeed { get; set; }
        public float BarrelRotation { get; set; } // do remember this is in radians
        public float TankRotation { get; set; }
        public float TreadPitch { get; set; }
        public float ShootPitch { get; set; }

        private long _treadSoundTimer = 5;

        public int MaxLayableMines { get; set; }
        public int TierHierarchy => (int)tier;
        public int UpdateTicks; // every X ticks, update

        public Vector3 position;
        public Vector3 approachVelocity;
        public Vector3 velocity;

        public Vector2 tankRotationPredicted; // the number of radians which should be rotated to before the tank starts moving

        public Matrix World;
        public Matrix View;
        public Matrix Projection;

        public BoundingBox CollisionBox;

        public Model TankModel { get; }

        public TankTier tier;
        public BulletType BulletType { get; set; } = BulletType.Regular;

        internal Texture2D _tankColorMesh;

        public Action behavior;

        private static Texture2D _shadowTexture;

        public static TankTier GetHighestTierActive()
        {
            var highest = TankTier.None;

            foreach (var tank in WPTR.AllAITanks.Where(tnk => !tnk.Dead))
            {
                if (tank.tier > highest)
                    highest = tank.tier;
            }
            return highest;
        }

        public static int GetTankCountOfType(TankTier tier)
            => WPTR.AllAITanks.Count(tnk => tnk.tier == tier);

        public AITank(Vector3 beginPos, TankTier tier = TankTier.None, bool setTankDefaults = true)
        {
            _shadowTexture = Resources.GetGameResource<Texture2D>("Assets/textures/tank_shadow");
            position = beginPos;
            this.tier = tier;

            CollisionBox = new(new Vector3(100, 100, 0), new Vector3(200, 200, 0));

            TankModel = TankGame.TankModel_Enemy;
            _tankColorMesh = Resources.GetGameResource<Texture2D>($"Assets/textures/enemy/tank_{tier.ToString().ToLower()}");

            void setDefaults()
            {
                if (tier == TankTier.Green)
                {
                    BulletType = BulletType.RicochetRocket;
                    Stationary = true;
                    Speed = 0;
                }
                if (tier == TankTier.White)
                {
                    Speed = 1.1f;
                    Invisible = true;
                }
                if (tier == TankTier.Brown)
                {
                    Stationary = true;
                    Speed = 0;
                }
                if (tier == TankTier.Black)
                {
                    TreadPitch = -0.26f;
                    BulletType = BulletType.Rocket;
                    Speed = 5f;
                }
                if (tier == TankTier.Purple)
                {
                    TreadPitch = -0.2f;
                    Speed = 2.5f;
                }
                if (tier == TankTier.Ash)
                {
                    TreadPitch = 0.125f;
                    Speed = 0.8f;
                }
                if (tier == TankTier.Marine)
                {
                    TreadPitch = 0.075f;
                    BulletType = BulletType.Rocket;
                    Speed = 0.6f;
                }
            }
            if (setTankDefaults)
                setDefaults();

            if (Invisible)
            {
                var invis = Resources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_invisible");
                var invisSfx = invis.CreateInstance();
                invisSfx.Play();
                invisSfx.Volume = 0.4f;
            }

            WPTR.AllAITanks.Add(this);
        }

        internal void Update()
        {

            if (velocity != Vector3.Zero)
            {
                //GameUtils.RoughStep(ref tankRotation, tankRotationPredicted.ToRotation(), 0.5f);
                TankRotation = velocity.ToRotation();
                // make the stop not go wack
            }
            // tankRotation = MathHelper.SmoothStep(velocity.ToRotation(), tankRotationPredicted.ToRotation(), 100f);
            Projection = TankGame.GameProjection;
            View = TankGame.GameView;

            // yaw = tankRotation;
            //yaw = GameUtils.MousePosition.X / (GameUtils.WindowWidth / 2);

            // roll = GameUtils.MousePosition.X / (GameUtils.WindowHeight / 2);

            World = Matrix.CreateFromYawPitchRoll(TankRotation + MathHelper.PiOver2, 0, 0)
                * Matrix.CreateTranslation(position.X, position.Y, position.Z);

            // if ((tankRotation + MathHelper.PiOver2).IsInRangeOf(tankRotationPredicted.ToRotation(), 1.5f))
            position += velocity;
            GetAIBehavior();
            velocity *= 0.8f;
        }

        public void Destroy()
        {
            Dead = true;
            var killSound = Resources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_destroy");
            var killSfx = killSound.CreateInstance();
            killSfx.Play();
            killSfx.Volume = 0.2f;

            // TODO: play fanfare thingy i think
        }

        /// <summary>
        /// Finish bullet implementation!
        /// </summary>
        /// <param name="velocity"></param>
        /// <param name="bulletSpeed"></param>
        public void Shoot(float radians, float bulletSpeed)
        {
            SoundEffect shootSound;

            shootSound = BulletType switch
            {
                BulletType.Rocket => Resources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_rocket"),
                BulletType.RicochetRocket => Resources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_ricochet"),
                _ => Resources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_regular_1")
            };

            var sfx = shootSound.CreateInstance();

            sfx.Volume = 0.3f;
            sfx.Play();
        }

        private Vector3 target;

        public void GetAIBehavior()
        {
            behavior?.Invoke();
            if (velocity != Vector3.Zero)
            {
                if (TankGame.GameUpdateTime % _treadSoundTimer == 0)
                {
                    var treadPlace = Resources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_tread_place_{new Random().Next(1, 5)}");
                    var treadPlaceSfx = treadPlace.CreateInstance();
                    treadPlaceSfx.Play();
                    treadPlaceSfx.Volume = 0.05f;
                    treadPlaceSfx.Pitch = TreadPitch;
                }
            }

            /*if (tier == TankTier.Ash)
            {
                behavior = (tank) => {
                    if (TryGetBulletNear(tank, 50f, out var bullet))
                    {
                        tank.velocity = tank.position.DirectionOf(bullet.position, true); //tank.position - bullet.position;
                    }
                };
            }*/

            behavior = () =>
            {
                if (!Dead)
                {
                    var tank_tryget = WPTR.AllPlayerTanks.FirstOrDefault(tnk => Vector3.Distance(tnk.position, position) < 300f);

                    if (WPTR.AllPlayerTanks.IndexOf(tank_tryget) > -1)
                    {
                        if (TankGame.GameUpdateTime % 10 == 0)
                            target = tank_tryget.position;

                        TankRotation = (tank_tryget.position - position).ToRotation();
                        if (!Stationary)
                        {

                            var randSeed1 = new Random().Next(0, 1500 / TierHierarchy);

                            if (randSeed1 == 0)
                                Shoot(BarrelRotation, BulletShootSpeed);

                            // velocity += position.DirectionOf(target) / 500 * Speed;
                        }
                    }
                }
            };
        }

        internal void DrawBody()
        {
            if (Dead)
                return;

            int i = 0;
            foreach (var bone in TankModel.Bones)
            {
                TankGame.spriteBatch.DrawString(TankGame.Fonts.Default, $"{bone.Name}: {bone.Index}", new Vector2(250, 10 * (i * 2)), Color.White);
                i++;
            }

            foreach (var mesh in TankModel.Meshes)
            {
                foreach (IEffectMatrices effect in mesh.Effects)
                {
                    effect.View = View;
                    effect.World = World;
                    effect.Projection = Projection;

                    if (_tankColorMesh != null)
                    {
                        var fx = effect as BasicEffect;

                        fx.TextureEnabled = true;
                        if (mesh.Name == "polygon0")
                        {
                            fx.Texture = _tankColorMesh;
                        }
                        else if (mesh.Name == "polygon1")
                        {
                            fx.Texture = _shadowTexture;
                        }
                    }
                }

                mesh.Draw();
            }
        }

        public override string ToString()
            => $"tier: {tier} | velocity/achievable: {velocity}/{approachVelocity}";
    }
}