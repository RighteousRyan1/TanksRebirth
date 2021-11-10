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
    public class PlayerTank : Tank
    {
        public bool playerControl_isBindPressed;

        private long _treadSoundTimer = 5;

        public BoundingBox CollisionBox;

        public PlayerType PlayerType { get; }

        internal Texture2D _tankColorTexture;
        private static Texture2D _shadowTexture;

        public static Keybind controlUp = new("Up", Keys.W);
        public static Keybind controlDown = new("Down", Keys.S);
        public static Keybind controlLeft = new("Left", Keys.A);
        public static Keybind controlRight = new("Right", Keys.D);
        public static Keybind PlaceMine = new("Place Mine", Keys.Space);
        // public static Keybind FireBullet = new("Fire Bullet", Keys.Space);

        #region ModelBone & ModelMesh
        public Matrix[] boneTransforms;

        public ModelMesh CannonMesh;
        #endregion

        public PlayerTank(Vector3 beginPos, PlayerType playerType)
        {

            Model = TankGame.TankModel_Player;
            _tankColorTexture = GameResources.GetGameResource<Texture2D>($"Assets/textures/player/tank_{playerType.ToString().ToLower()}");

            CannonMesh = Model.Meshes["polygon1.001"];

            boneTransforms = new Matrix[Model.Bones.Count];

            _shadowTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank_shadow");

            PlayerType = playerType;

            position = beginPos;

            controlUp.KeybindPressAction = (cUp) =>
            {
                playerControl_isBindPressed = true;
                velocity.Z -= Acceleration;
                //velocity.Y += Speed / 3;
                // approachVelocity.Y -= 20f;
            };
            controlDown.KeybindPressAction = (cDown) =>
            {
                playerControl_isBindPressed = true;
                velocity.Z += Acceleration;
                //velocity.Y -= Speed / 3;
                //approachVelocity.Y += 20f;
            };
            controlLeft.KeybindPressAction = (cLeft) =>
            {
                playerControl_isBindPressed = true;
                velocity.X -= Acceleration;
                //approachVelocity.X -= 20f;
            };
            controlRight.KeybindPressAction = (cRight) =>
            {
                playerControl_isBindPressed = true;
                velocity.X += Acceleration;
                //approachVelocity.X += 20f;
            };

            WPTR.AllPlayerTanks.Add(this);
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
                    TankRotation = Velocity2D.ToRotation();
                    // make the stop not go wack
                }
                Projection = TankGame.GameProjection;
                View = TankGame.GameView;

                World = Matrix.CreateFromYawPitchRoll(TankRotation, 0, 0)
                    * Matrix.CreateTranslation(position.X, position.Y, position.Z);
                // if ((tankRotation + MathHelper.PiOver2).IsInRangeOf(tankRotationPredicted.ToRotation(), 1.5f))
                if (Input.CurrentGamePadSnapshot.IsConnected)
                    ControlHandle_ConsoleController();
                position += velocity;

                var normal = Position2D - GameUtils.MousePosition;

                BarrelRotation = normal.ToRotation();

                if (Input.CanDetectClick())
                    Shoot(BarrelRotation, BulletShootSpeed);

                UpdatePlayerMovement();
                UpdateCollision();

                playerControl_isBindPressed = false;

                oldPosition = position;
            }
            else
            {
                CollisionBox = new();
            }
        }

        /// <summary>
        /// Controller support soon i hope
        /// </summary>
        private void ControlHandle_ConsoleController()
        {
            var leftStick = Input.CurrentGamePadSnapshot.ThumbSticks.Left;

            velocity.X += leftStick.X;
            velocity.Z -= leftStick.Y;
        }

        private void ControlHandle_Keybinding()
        {
        }

        private void UpdateCollision()
        {
            CollisionBox = new(position - new Vector3(12, 10, 12), position + new Vector3(12, 10, 12));
            if (WPTR.AllAITanks.Any(tnk => tnk.CollisionBox.Intersects(CollisionBox)))
            {
                position = oldPosition;
                //System.Diagnostics.Debug.WriteLine(new Random().Next(0, 100).ToString());
            }
            if (WPTR.AllPlayerTanks.Any(tnk => tnk.CollisionBox.Intersects(CollisionBox)))
            {
            }
        }

        public void Destroy()
        {
            Dead = true;
            var killSound = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_destroy");
            var killSfx = killSound.CreateInstance();
            killSfx.Play();
            killSfx.Volume = 0.2f;

            // TODO: play player tank death sound
        }

        public void UpdatePlayerMovement()
        {
            if (!controlDown.IsPressed && !controlUp.IsPressed)
                velocity.Z = 0;
            if (!controlLeft.IsPressed && !controlRight.IsPressed)
                velocity.X = 0;

            if (velocity.X > MaxSpeed)
                velocity.X = MaxSpeed;
            if (velocity.X < -MaxSpeed)
                velocity.X = -MaxSpeed;
            if (velocity.Z > MaxSpeed)
                velocity.Z = MaxSpeed;
            if (velocity.Z < -MaxSpeed)
                velocity.Z = -MaxSpeed;

            if (Velocity2D.Length() > 0 && playerControl_isBindPressed)
            {
                if (TankGame.GameUpdateTime % _treadSoundTimer == 0)
                {
                    var treadPlace = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_tread_place_{new Random().Next(1, 5)}");
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
                BulletType.Rocket => GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_rocket"),
                BulletType.RicochetRocket => GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_ricochet"),
                _ => GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_regular_1")
            };

            var sfx = shootSound.CreateInstance();

            sfx.Volume = 0.3f;
            sfx.Play();
        }

        private void RenderModel()
        {
            Model.Root.Transform = World;
            CannonMesh.ParentBone.Transform = Matrix.CreateRotationY(BarrelRotation); // a

            Model.CopyAbsoluteBoneTransformsTo(boneTransforms); // a

            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = boneTransforms[mesh.ParentBone.Index];
                    effect.View = View;
                    effect.Projection = Projection;
                    effect.TextureEnabled = true;

                    if (mesh.Name != "polygon0.001")
                        effect.Texture = _tankColorTexture;

                    else
                        effect.Texture = _shadowTexture;

                    //effect.DirectionalLight0.Direction = new(0, 1, 0);
                    effect.EnableDefaultLighting();
                }
                mesh.Draw();
            }
        }

        internal void DrawBody()
        {
            /*var display = $"rotationX: {TankRotation + MathHelper.PiOver2}" +
                $"\nvelPredicted: {tankRotationPredicted.ToRotation()}" +
                $"\nvel: {velocity}";*/

            if (Dead)
                return;

            RenderModel();
        }

        public override string ToString()
            => $"pos: {position} | vel: {velocity} | dead: {Dead}";
    }
}