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

namespace WiiPlayTanksRemake.GameContent
{
    public class Tank
    {
        public static List<Tank> AllTanks { get; } = new();

        public bool IsAI { get; }
        public bool playerControl_isBindPressed;
        public bool dead;

        public float speed = 1f;
        public float bulletShootSpeed;
        public float barrelRotation; // do remember this is in radians
        public float tankRotation;
        public float tankTreadPitch;
        public float shootPitch;
        public float scale;

        private long _treadSoundTimer = 5;

        public int maxLayableMines;
        public int TierHierarchy => (int)tier;

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
        public PlayerType PlayerType { get; }
        public BulletType BulletType { get; private set; } = BulletType.Regular;

        internal Texture2D _tankColorMesh;

        public Action<Tank> behavior;

        public static Keybind controlUp = new("Up", Keys.W);
        public static Keybind controlDown = new("Down", Keys.S);
        public static Keybind controlLeft = new("Left", Keys.A);
        public static Keybind controlRight = new("Right", Keys.D);
        public static Keybind PlaceMine = new("Place Mine", Keys.Space);
        // public static Keybind FireBullet = new("Fire Bullet", Keys.Space);

        public static TankTier GetHighestTierActive()
        {
            var highest = TankTier.None;

            foreach (Tank tank in AllTanks.Where(tnk => tnk.IsAI && !tnk.dead))
            {
                if (tank.tier > highest)
                    highest = tank.tier;
            }
            return highest;
        }

        public static int GetTankCountOfType(TankTier tier) 
            => AllTanks.Count(tnk => tnk.tier == tier && tnk.IsAI && !tnk.dead);

        public Tank(Vector3 beginPos, bool ai = false, TankTier tier = TankTier.None, PlayerType playerType = PlayerType.IsNotPlayer, bool setTankDefaults = true)
        {
            PlayerType = playerType;
            IsAI = ai;
            position = beginPos;
            this.tier = tier;

            if (ai && playerType != PlayerType.IsNotPlayer)
                throw new Exception("An AI tank cannot have a player declaration.");

            if (!ai && tier != TankTier.None)
                throw new Exception("A player cannot have a tank tier.");

            CollisionBox = new(new Vector3(100, 100, 0), new Vector3(200, 200, 0));

            if (ai)
            {
                TankModel = TankGame.TankModel_Enemy;
                _tankColorMesh = Resources.GetGameResource<Texture2D>($"Assets/textures/enemy/tank_{tier.ToString().ToLower()}");
            }
            else
            {
                TankModel = TankGame.TankModel_Player;
                _tankColorMesh = Resources.GetGameResource<Texture2D>($"Assets/textures/player/tank_{playerType.ToString().ToLower()}");
            }

            if (!ai)
            {
                controlUp.KeybindPressAction = (cUp) =>
                {
                    playerControl_isBindPressed = true;
                    tankRotationPredicted.Y += 5f;
                    velocity.Y += speed / 3;
                    //velocity.Y += speed * 5f;
                    // approachVelocity.Y -= 20f;
                };
                controlDown.KeybindPressAction = (cDown) =>
                {
                    playerControl_isBindPressed = true;
                    tankRotationPredicted.Y -= 5f;
                    velocity.Y -= speed / 3;
                    //velocity.Y -= speed * 5f;
                    //approachVelocity.Y += 20f;
                };
                controlLeft.KeybindPressAction = (cLeft) =>
                {
                    playerControl_isBindPressed = true;
                    tankRotationPredicted.X -= 5f;
                    velocity.X -= speed / 3;
                    //approachVelocity.X -= 20f;
                };
                controlRight.KeybindPressAction = (cRight) =>
                {
                    playerControl_isBindPressed = true;
                    tankRotationPredicted.X += 5f;
                    velocity.X += speed / 3;
                    //approachVelocity.X += 20f;
                };
            }

            void setDefaults()
            {
                if (tier == TankTier.Green)
                {
                    BulletType = BulletType.RicochetRocket;
                }
                if (tier == TankTier.Black)
                {
                    tankTreadPitch = -0.26f;
                    BulletType = BulletType.Rocket;
                }
                if (tier == TankTier.Purple)
                {
                    tankTreadPitch = -0.2f;
                }
                if (tier == TankTier.Ash)
                {
                    tankTreadPitch = 0.125f;
                }
                if (tier == TankTier.Marine)
                {
                    tankTreadPitch = 0.075f;
                    BulletType = BulletType.Rocket;
                }
            }
            if (setTankDefaults)
                setDefaults();
            
            AllTanks.Add(this);
        }

        internal void Update()
        {

            if (velocity != Vector3.Zero)
            {
                GameUtils.RoughStep(ref tankRotation, tankRotationPredicted.ToRotation(), 0.5f);
                // make the stop not go wack
            }
            // tankRotation = MathHelper.SmoothStep(velocity.ToRotation(), tankRotationPredicted.ToRotation(), 100f);
            Projection = TankGame.GameProjection;
            View = TankGame.GameView;

            // yaw = tankRotation;
            //yaw = GameUtils.MousePosition.X / (GameUtils.WindowWidth / 2);

            // roll = GameUtils.MousePosition.X / (GameUtils.WindowHeight / 2);

            World = Matrix.CreateScale(scale)
                * Matrix.CreateFromYawPitchRoll(tankRotation + MathHelper.PiOver2, 0, 0)
                // * Matrix.CreateRotationX(0.6208f)
                * Matrix.CreateTranslation(position.X, position.Y, position.Z);

            // if ((tankRotation + MathHelper.PiOver2).IsInRangeOf(tankRotationPredicted.ToRotation(), 1.5f))
            {
                position += velocity;
            }
            if (IsAI)
            {
                GetAIBehavior();
                behavior?.Invoke(this);
            }
            else
            {
                if (Input.CanDetectClick())
                {
                    Shoot(barrelRotation, bulletShootSpeed);
                }
                UpdatePlayerMovement();
            }
            velocity *= 0.8f;
            playerControl_isBindPressed = false;
        }

        public void Destroy()
        {
            dead = true;
            var killSound = Resources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_destroy");
            var killSfx = killSound.CreateInstance();
            killSfx.Play();
            killSfx.Volume = 0.2f;

            if (IsAI)
            {
                // TODO: play fanfare thingy i think
            }
        }

        public void UpdatePlayerMovement()
        {
            if (velocity != Vector3.Zero && playerControl_isBindPressed)
            {
                if (TankGame.GameUpdateTime % _treadSoundTimer == 0)
                {
                    var treadPlace = Resources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_tread_place_{new Random().Next(1, 5)}");
                    var treadPlaceSfx = treadPlace.CreateInstance();
                    treadPlaceSfx.Play();
                    treadPlaceSfx.Volume = 0.2f;
                }
            }
            //velocity += approachVelocity / 10;
            // barrelRotation = GameUtils.DirectionOf(GameUtils.MousePosition.ToVector3(), position).ToRotation();
            // approachVelocity = Vector3.Zero;
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

        public void GetAIBehavior()
        {
            if (velocity != Vector3.Zero)
            {
                if (TankGame.GameUpdateTime % _treadSoundTimer == 0)
                {
                    var treadPlace = Resources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_tread_place_{new Random().Next(1, 5)}");
                    var treadPlaceSfx = treadPlace.CreateInstance();
                    treadPlaceSfx.Play();
                    treadPlaceSfx.Volume = 0.2f;
                    treadPlaceSfx.Pitch = tankTreadPitch;
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

            behavior = (tank) =>
            {
                if (!tank.dead)
                {
                    var tank_tryget = AllTanks.FirstOrDefault(tnk => !tnk.IsAI && Vector3.Distance(tnk.position, tank.position) < 300f);

                    if (AllTanks.IndexOf(tank_tryget) > -1)
                    {
                        tank.tankRotation = (tank_tryget.position - tank.position).ToRotation();
                    }

                    var randSeed1 = new Random().Next(0, 1500 / tank.TierHierarchy);

                    if (randSeed1 == 0)
                        tank.Shoot(tank.barrelRotation, bulletShootSpeed);
                }
            };
        }

        internal void DrawBody()
        {
            var display = $"rotationX: {tankRotation + MathHelper.PiOver2}" +
                $"\nvelPredicted: {tankRotationPredicted.ToRotation()}" +
                $"\nvel: {velocity}";


            // TankGame.spriteBatch.DrawString(TankGame.Fonts.Default, display, position.Flatten(), Color.White);

            // TankModel.Meshes[0].ParentBone.Transform = Matrix.CreateTranslation(new(5, 5, 0));

            /*var mesh = TankModel.Meshes[0]; // the body

            if (_tankColorMesh != null)
            {
                var fx = mesh.Effects[0] as BasicEffect;

                fx.TextureEnabled = true;

                fx.Texture = _tankColorMesh;
            }


            mesh.Draw();*/

            if (dead)
                return;

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

                        fx.Texture = _tankColorMesh;
                    }
                }

                mesh.Draw();
            }
        }

        public static bool TryGetBulletNear(Tank tank, float distance, out Bullet bullet)
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
        public static bool TryGetMineNear(Tank tank, float distance, out Mine mine)
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

        public override string ToString()
            => $"tier: {tier} | velocity/achievable: {velocity}/{approachVelocity}";
    }
}