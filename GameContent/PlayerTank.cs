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
    public class PlayerTank : ITank
    {
        public bool playerControl_isBindPressed;
        public bool Dead { get; set; }

        public float Speed { get; set; } = 1f;
        public float BulletShootSpeed { get; set; }
        public float BarrelRotation { get; set; } // do remember this is in radians
        public float TankRotation { get; set; }
        public float TreadPitch { get; set; }
        public float ShootPitch { get; set; }

        private long _treadSoundTimer = 5;

        public int MaxLayableMines { get; set; }

        public Vector3 position;
        public Vector3 approachVelocity;
        public Vector3 velocity;

        public Vector2 tankRotationPredicted; // the number of radians which should be rotated to before the tank starts moving

        public Matrix World;
        public Matrix View;
        public Matrix Projection;

        public BoundingBox CollisionBox;

        public Model TankModel { get; }

        public PlayerType PlayerType { get; }
        public BulletType BulletType { get; set; } = BulletType.Regular;

        internal Texture2D _tankColorMesh;
        private static Texture2D _shadowTexture;

        public static Keybind controlUp = new("Up", Keys.W);
        public static Keybind controlDown = new("Down", Keys.S);
        public static Keybind controlLeft = new("Left", Keys.A);
        public static Keybind controlRight = new("Right", Keys.D);
        public static Keybind PlaceMine = new("Place Mine", Keys.Space);
        // public static Keybind FireBullet = new("Fire Bullet", Keys.Space);
        public PlayerTank(Vector3 beginPos, PlayerType playerType = PlayerType.IsNotPlayer, bool setTankDefaults = true)
        {

            // make Z position changing work

            _shadowTexture = Resources.GetGameResource<Texture2D>("Assets/textures/tank_shadow");

            PlayerType = playerType;

            position = beginPos;

            CollisionBox = new(new Vector3(100, 100, 0), new Vector3(200, 200, 0));

            TankModel = TankGame.TankModel_Player;
            _tankColorMesh = Resources.GetGameResource<Texture2D>($"Assets/textures/player/tank_{playerType.ToString().ToLower()}");

            controlUp.KeybindPressAction = (cUp) =>
            {
                playerControl_isBindPressed = true;
                tankRotationPredicted.Y += 5f;
                velocity.Z -= Speed / 3;
                //velocity.Y += Speed / 3;
                // approachVelocity.Y -= 20f;
            };
            controlDown.KeybindPressAction = (cDown) =>
            {
                playerControl_isBindPressed = true;
                tankRotationPredicted.Y -= 5f;
                velocity.Z += Speed / 3;
                //velocity.Y -= Speed / 3;
                //approachVelocity.Y += 20f;
            };
            controlLeft.KeybindPressAction = (cLeft) =>
            {
                playerControl_isBindPressed = true;
                tankRotationPredicted.X -= 5f;
                velocity.X -= Speed / 3;
                //approachVelocity.X -= 20f;
            };
            controlRight.KeybindPressAction = (cRight) =>
            {
                playerControl_isBindPressed = true;
                tankRotationPredicted.X += 5f;
                velocity.X += Speed / 3;
                //approachVelocity.X += 20f;
            };

            WPTR.AllPlayerTanks.Add(this);
        }

        internal void Update()
        {

            if (velocity != Vector3.Zero)
            {
                //GameUtils.RoughStep(ref tankRotation, tankRotationPredicted.ToRotation(), 0.5f);
                TankRotation = velocity.FlattenZ().ToRotation() + MathHelper.Pi;
                // make the stop not go wack
            }
            // tankRotation = MathHelper.SmoothStep(velocity.ToRotation(), tankRotationPredicted.ToRotation(), 100f);
            Projection = TankGame.GameProjection;
            View = TankGame.GameView;

            // yaw = tankRotation;
            //yaw = GameUtils.MousePosition.X / (GameUtils.WindowWidth / 2);

            // roll = GameUtils.MousePosition.X / (GameUtils.WindowHeight / 2);

            World = Matrix.CreateFromYawPitchRoll(TankRotation + MathHelper.PiOver2, 0, 0)
                // * Matrix.CreateRotationX(0.6208f)
                * Matrix.CreateTranslation(position.X, position.Y, position.Z);

            // if ((tankRotation + MathHelper.PiOver2).IsInRangeOf(tankRotationPredicted.ToRotation(), 1.5f))
            position += velocity;

            BarrelRotation = (position.Flatten() - GameUtils.MousePosition).ToRotation();

            if (Input.CanDetectClick())
            {
                Shoot(BarrelRotation, BulletShootSpeed);
            }
            UpdatePlayerMovement();
            velocity *= 0.8f;
            playerControl_isBindPressed = false;
        }

        public void Destroy()
        {
            Dead = true;
            var killSound = Resources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_destroy");
            var killSfx = killSound.CreateInstance();
            killSfx.Play();
            killSfx.Volume = 0.2f;

            // TODO: play player tank death sound
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

        internal void DrawBody()
        {
            var display = $"rotationX: {TankRotation + MathHelper.PiOver2}" +
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

            if (Dead)
                return;

            int i = 0;
            foreach (var bone in TankModel.Bones)
            {
                TankGame.spriteBatch.DrawString(TankGame.Fonts.Default, $"{bone.Name}: {bone.Index}", new Vector2(10, 10 * (i * 2)), Color.White);
                i++;
            }


            var bon = TankModel.Bones.First(bne => bne.Name == "cannon"); // dont forget cannon_end and cannon_end_end

            bon.Transform = Matrix.CreateRotationX(BarrelRotation);

            foreach (var mesh in TankModel.Meshes)
            {
                //TankGame.spriteBatch.Begin(transformMatrix: World);

                //TankGame.spriteBatch.End();

                foreach (IEffectMatrices effect in mesh.Effects)
                {
                    effect.View = View;
                    //effect.World = World;
                    effect.World = mesh.ParentBone.Transform * World;
                    effect.Projection = Projection;

                    if (_tankColorMesh != null)
                    {
                        var fx = effect as BasicEffect;

                        fx.TextureEnabled = true;
                        if (mesh.Name == "polygon1")
                        {
                            fx.Texture = _tankColorMesh;
                        }
                        else if (mesh.Name == "polygon0")
                        {
                            fx.Texture = _shadowTexture;
                        }
                    }
                }

                mesh.Draw();
            }

            //TankModel.Draw(World, View, Projection);
        }

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

        public override string ToString()
            => $"velocity/achievable: {velocity}/{approachVelocity}";
    }
}