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

namespace WiiPlayTanksRemake.GameContent
{
    public class Tank
    {
        public static List<Tank> AllTanks { get; } = new();

        public Vector3 position;
        public Vector3 approachVelocity;
        public Vector3 velocity;
        public float speed = 1f;
        public float bulletShootSpeed;
        public float barrelRotation; // do remember this is in radians
        public float tankRotation;

        public Vector2 tankRotationPredicted; // the number of radians which should be rotated to before the tank starts moving

        public float scale;
        public int maxLayableMines;

        public Matrix World;
        public Matrix View;
        public Matrix Projection;

        public Model TankModel { get; }

        public bool IsAI { get; }

        public TankTier tier;

        private Texture2D _tankColorMesh;

        private long _treadSoundTimer = 5;

        public int TierHierarchy => (int)tier;

        public Action<Tank> behavior;

        public bool playerControl_isBindPressed;

        public static TankTier GetHighestTierActive()
        {
            var highest = TankTier.None;

            foreach (Tank tank in AllTanks)
            {
                if (tank.tier > highest)
                    highest = tank.tier;
            }
            return highest;
        }

        public Keybind cUp = new("Up", Keys.W);
        public Keybind cDown = new("Down", Keys.S);
        public Keybind cLeft = new("Left", Keys.A);
        public Keybind cRight = new("Right", Keys.D);

        public Tank(Vector3 beginPos, bool ai = false, TankTier tier = TankTier.None, PlayerType playerType = PlayerType.IsNotPlayer)
        {
            IsAI = ai;
            position = beginPos;
            this.tier = tier;

            if (ai && playerType != PlayerType.IsNotPlayer)
                throw new Exception("An AI tank cannot have a player declaration.");

            if (!ai && tier != TankTier.None)
                throw new Exception("A player cannot have a tank tier.");

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
                cUp.KeybindPressAction = (cUp) =>
                {
                    playerControl_isBindPressed = true;
                    tankRotationPredicted.Y += 1f;
                    velocity.Y += 5f * speed;
                    // approachVelocity.Y -= 20f;
                };
                cDown.KeybindPressAction = (cDown) =>
                {
                    playerControl_isBindPressed = true;
                    tankRotationPredicted.Y -= 1f;
                    velocity.Y -= 5f * speed;
                    //approachVelocity.Y += 20f;
                };
                cLeft.KeybindPressAction = (cLeft) =>
                {
                    playerControl_isBindPressed = true;
                    tankRotationPredicted.X -= 1f;
                    velocity.X -= 5f * speed;
                    //approachVelocity.X -= 20f;
                };
                cRight.KeybindPressAction = (cRight) =>
                {
                    playerControl_isBindPressed = true;
                    tankRotationPredicted.X += 1f;
                    velocity.X += 5f * speed;
                    //approachVelocity.X += 20f;
                };
            }
            
            AllTanks.Add(this);
        }

        // readonly float yaw = -1.7f;//-1.7145833f; // sideways
        readonly float pitch = 0f;
        readonly float roll = 0;//-0.8f;

        internal void Update()
        {
            tankRotation = velocity.ToRotation(); //tankRotationPredicted.ToRotation();
            Projection = TankGame.GameProjection;
            View = TankGame.GameView;

            // yaw = tankRotation;
            //yaw = GameUtils.MousePosition.X / (GameUtils.WindowWidth / 2);

            // roll = GameUtils.MousePosition.X / (GameUtils.WindowHeight / 2);

            World = Matrix.CreateScale(2 * scale) 
                * Matrix.CreateFromYawPitchRoll(tankRotation + MathHelper.PiOver2, pitch, roll)
                // * Matrix.CreateRotationX(0.6208f)
                + Matrix.CreateTranslation(position.X, position.Y, position.Z);

            if (tankRotation.InDistanceOf(tankRotationPredicted.ToRotation(), 0.5f))
            {
                position += velocity;
            }

            position += velocity;
            if (IsAI)
            {
                GetAIBehavior();
                behavior?.Invoke(this);
            }
            else
            {
                UpdatePlayerMovement();
            }

            velocity *= 0.8f;
            playerControl_isBindPressed = false;
        }

        public void UpdatePlayerMovement()
        {
            if (velocity != Vector3.Zero && playerControl_isBindPressed)
            {
                if (TankGame.GameUpdateTime.TotalGameTime.Ticks % _treadSoundTimer == 0)
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

        public void Shoot(Vector2 velocity, float bulletSpeed)
        {

        }

        public void GetAIBehavior()
        {
            if (tier == TankTier.Ash)
            {
                behavior = (tank) => {
                    if (TryGetBulletNear(tank, 50f, out var bullet))
                    {
                        tank.velocity = tank.position - bullet.position;
                    }
                };
            }
        }

        internal void DrawBody()
        {
            // TankGame.spriteBatch.DrawString(TankGame.Fonts.Default, $"yaw: {tankRotation + MathHelper.PiOver2}\npitch: {pitch}\nroll: {roll}", new(10, 10), Color.White);

            // TankModel.Meshes[0].ParentBone.Transform = Matrix.CreateTranslation(new(5, 5, 0));

            /*var mesh = TankModel.Meshes[0]; // the body

            if (_tankColorMesh != null)
            {
                var fx = mesh.Effects[0] as BasicEffect;

                fx.TextureEnabled = true;

                fx.Texture = _tankColorMesh;
            }


            mesh.Draw();*/

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