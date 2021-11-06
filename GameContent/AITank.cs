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
    public class AITank : Tank
    {
        public bool Invisible { get; private set; }
        public bool Stationary { get; private set; }

        private long _treadSoundTimer = 5;
        public int TierHierarchy => (int)tier;
        public int UpdateTicks; // every X ticks, update

        public Vector2 tankRotationPredicted; // the number of radians which should be rotated to before the tank starts moving

        public BoundingBox CollisionBox;

        public TankTier tier;

        internal Texture2D _tankColorTexture;

        public Action behavior;

        private static Texture2D _shadowTexture;

        #region ModelBone & ModelMesh
        public Matrix[] boneTransforms;

        public ModelMesh CannonMesh;
        #endregion

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
            position = beginPos;

            Model = TankGame.TankModel_Enemy;
            _tankColorTexture = GameResources.GetGameResource<Texture2D>($"Assets/textures/enemy/tank_{tier.ToString().ToLower()}");

            CannonMesh = Model.Meshes["polygon0.001"];

            boneTransforms = new Matrix[Model.Bones.Count];

            _shadowTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank_shadow");
            this.tier = tier;

            void setDefaults()
            {
                if (tier == TankTier.Green)
                {
                    BulletType = BulletType.RicochetRocket;
                    Stationary = true;
                    Speed = 0;
                }
                if (tier == TankTier.Pink)
                {
                    TreadPitch = 0.1f;
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
                if (tier == TankTier.Yellow)
                {
                    TreadPitch = 0.08f;
                    Speed = 2.2f;
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
                    TreadPitch = 0.11f;
                    BulletType = BulletType.Rocket;
                    Speed = 0.6f;
                }
            }
            if (setTankDefaults)
                setDefaults();

            if (Invisible)
            {
                var invis = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_invisible");
                var invisSfx = invis.CreateInstance();
                invisSfx.Play();
                invisSfx.Volume = 0.4f;
            }

            WPTR.AllAITanks.Add(this);
        }

        internal void Update()
        {
            if (!Dead)
            {
                position.X = MathHelper.Clamp(position.X, -268, 268);
                position.Z = MathHelper.Clamp(position.Z, -155, 400);

                if (velocity != Vector3.Zero)
                {
                    //GameUtils.RoughStep(ref tankRotation, tankRotationPredicted.ToRotation(), 0.5f);
                    TankRotation = velocity.ToRotation();
                    // make the stop not go wack
                }
                Projection = TankGame.GameProjection;
                View = TankGame.GameView;

                World = Matrix.CreateFromYawPitchRoll(TankRotation + MathHelper.PiOver2, 0, 0)
                    * Matrix.CreateTranslation(position.X, position.Y, position.Z);

                Model.Root.Transform = World;

                // if ((tankRotation + MathHelper.PiOver2).IsInRangeOf(tankRotationPredicted.ToRotation(), 1.5f))
                position += velocity;

                GetAIBehavior();

                UpdateCollision();

                oldPosition = position;
                velocity *= 0.8f;
            }
        }

        private void UpdateCollision()
        {
            CollisionBox = new(position - new Vector3(7, 10, 7), position + new Vector3(10, 10, 10));
            if (WPTR.AllAITanks.Any(tnk => tnk.CollisionBox.Intersects(CollisionBox)))
            {
            }
            if (WPTR.AllPlayerTanks.Any(tnk => tnk.CollisionBox.Intersects(CollisionBox)))
            {
                position = oldPosition;
            }
        }

        public void Destroy()
        {
            Dead = true;
            var killSound = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_destroy");
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
                BulletType.Rocket => GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_rocket"),
                BulletType.RicochetRocket => GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_ricochet"),
                _ => GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_regular_1")
            };

            var sfx = shootSound.CreateInstance();

            sfx.Volume = 0.3f;
            sfx.Play();
        }

        public void GetAIBehavior()
        {
            behavior?.Invoke();
            if (velocity != Vector3.Zero)
            {
                if (TankGame.GameUpdateTime % _treadSoundTimer == 0)
                {
                    var treadPlace = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_tread_place_{new Random().Next(1, 5)}");
                    var treadPlaceSfx = treadPlace.CreateInstance();
                    treadPlaceSfx.Play();
                    treadPlaceSfx.Volume = 0.15f;
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
                    var tank_tryget = WPTR.AllPlayerTanks.FirstOrDefault(tnk => Vector2.Distance(tnk.position.FlattenZ(), position.FlattenZ()) < 300f);

                    if (WPTR.AllPlayerTanks.IndexOf(tank_tryget) > -1)
                    {
                        if (!Stationary)
                        {

                            if (TankGame.GameUpdateTime % 10 == 0)
                                BarrelRotation = tank_tryget.position.ToRotation();

                            var randSeed1 = new Random().Next(0, 1500 / TierHierarchy);

                            if (randSeed1 == 0)
                                Shoot(BarrelRotation, BulletShootSpeed);

                            // velocity += position.DirectionOf(target) / 500 * Speed;
                        }
                    }
                }
            };
        }

        private void RenderModel()
        {
            CannonMesh.ParentBone.Transform = Matrix.CreateRotationY(BarrelRotation);

            Model.CopyAbsoluteBoneTransformsTo(boneTransforms);

            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = boneTransforms[mesh.ParentBone.Index];
                    effect.View = View;
                    effect.Projection = Projection;

                    if (mesh.Name != "polygon1")
                        effect.Texture = _tankColorTexture;

                    effect.EnableDefaultLighting();
                }
                mesh.Draw();
            }
        }
        internal void DrawBody()
        {
            if (Dead)
                return;

            RenderModel();
        }

        public override string ToString()
            => $"tier: {tier} | velocity/achievable: {velocity}/{approachVelocity}";



        public static bool TryGetBulletNear(PlayerTank tank, float distance, out Bullet bullet)
        {
            foreach (var blet in Bullet.AllBullets)
            {
                if (Vector3.Distance(tank.position, blet.position) < distance)
                {
                    bullet = blet;
                    return true;
                }
            }
            bullet = null;
            return false;
        }
        public static bool TryGetMineNear(PlayerTank tank, float distance, out Mine mine)
        {
            foreach (var yours in Mine.AllMines)
            {
                if (Vector3.Distance(tank.position, yours.position) < distance)
                {
                    mine = yours;
                    return true;
                }
            }
            mine = null;
            return false;
        }
    }
}