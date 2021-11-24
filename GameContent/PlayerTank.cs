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
        public int curShootStun;
        public int curShootCooldown;
        private int curMineCooldown;

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

            ShellCooldown = 5;
            ShootStun = 5;
            ShellSpeed = 3f;
            MaxSpeed = 1.8f;
            RicochetCount = 1;
            ShellLimit = 5;
            MineLimit = 2;
            MineStun = 1;
            Invisible = false;
            Acceleration = 0.3f;
            TurningSpeed = 0.08f;

            Team = Team.Red;

            WPTR.AllPlayerTanks.Add(this);
            WPTR.AllTanks.Add(this);
        }

        internal void Update()
        {
            // pi/2 = up
            // 0 = down
            // pi/4 = right
            // 3/4pi = left
            if (curShootStun > 0)
                curShootStun--;
            if (curShootCooldown > 0)
                curShootCooldown--;
            if (curMineCooldown > 0)
                curMineCooldown--;
            if (!Dead)
            {
                position.X = MathHelper.Clamp(position.X, MapRenderer.TANKS_MIN_X, MapRenderer.TANKS_MAX_X);
                position.Z = MathHelper.Clamp(position.Z, MapRenderer.TANKS_MIN_Y, MapRenderer.TANKS_MAX_Y);
                if (velocity != Vector3.Zero)
                {
                    //GameUtils.RoughStep(ref tankRotation, tankRotationPredicted.ToRotation(), 0.5f);
                    TankRotation = Velocity2D.ToRotation() - MathHelper.PiOver2;
                }
                Projection = TankGame.GameProjection;
                View = TankGame.GameView;

                World = Matrix.CreateFromYawPitchRoll(-TankRotation, 0, 0)
                    * Matrix.CreateTranslation(position);
                if (WPTR.InMission)
                {
                    if (curShootStun > 0 || curMineCooldown > 0)
                        velocity = Vector3.Zero;

                    // if ((tankRotation + MathHelper.PiOver2).IsInRangeOf(tankRotationPredicted.ToRotation(), 1.5f))

                    if (curShootStun <= 0)
                    {
                        ControlHandle_Keybinding();
                        if (Input.CurrentGamePadSnapshot.IsConnected)
                            ControlHandle_ConsoleController();
                    }
                    position += velocity * 0.55f;
                }
                else
                    velocity = Vector3.Zero;

                //var normal = ((Position2D + GameUtils.WindowCenter) - (GameUtils.MousePosition));

                var tankscreen = TankGame.Instance.GraphicsDevice.Viewport.Unproject(position, Projection, View, World);

                TurretRotation = (new Vector2(tankscreen.X, tankscreen.Y) - GameUtils.MousePosition).ToRotation(); //-normal.ToRotation() - MathHelper.PiOver2;

                if (WPTR.InMission)
                {
                    if (Input.CanDetectClick())
                        Shoot();

                    UpdatePlayerMovement();
                    UpdateCollision();
                }

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

            if (leftStick != Vector2.Zero) {
                playerControl_isBindPressed = true;
            }
        }

        private void ControlHandle_Keybinding()
        {
            if (controlDown.IsPressed)
            {
                playerControl_isBindPressed = true;
                velocity.Z += Acceleration;
            }
            if (controlUp.IsPressed)
            {
                playerControl_isBindPressed = true;
                velocity.Z -= Acceleration;
            }
            if (controlLeft.IsPressed)
            {
                playerControl_isBindPressed = true;
                velocity.X -= Acceleration;
            }
            if (controlRight.IsPressed)
            {
                playerControl_isBindPressed = true;
                velocity.X += Acceleration;
            }
        }

        private void UpdateCollision()
        {
            CollisionBox = new(position - new Vector3(12, 10, 12), position + new Vector3(12, 10, 12));
            if (WPTR.AllAITanks.Any(tnk => tnk.CollisionBox.Intersects(CollisionBox)))
            {
                position = oldPosition;
                //System.Diagnostics.Debug.WriteLine(new Random().Next(0, 100).ToString());
            }
            if (WPTR.AllPlayerTanks.Any(tnk => tnk.CollisionBox.Intersects(CollisionBox) && tnk != this))
            {
                position = oldPosition;
            }

            /*foreach (var cube in Cube.cubes.Where(c => Vector3.Distance(c.position, position) < 100))
            {
                var dir = cube.GetCollisionDirection(Position2D);

                switch (dir)
                {
                    case Cube.CubeCollisionDirection.Up:
                        if (velocity.Z > 0)
                            velocity.Z = 0;
                        break;
                    case Cube.CubeCollisionDirection.Down:
                        if (velocity.Z < 0)
                            velocity.Z = 0;
                        break;
                    case Cube.CubeCollisionDirection.Left:
                        if (velocity.X > 0)
                            velocity.X = 0;
                        break;
                    case Cube.CubeCollisionDirection.Right:
                        if (velocity.X < 0)
                            velocity.X = 0;
                        break;
                }
            }*/
        }

        public override void Destroy()
        {
            Dead = true;
            var killSound1 = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_destroy");
            var killSound2 = GameResources.GetGameResource<SoundEffect>($"Assets/fanfares/tank_player_death");
            SoundPlayer.PlaySoundInstance(killSound1, SoundContext.Sound, 0.2f);
            SoundPlayer.PlaySoundInstance(killSound2, SoundContext.Sound, 0.2f);

            var dmark = new TankDeathMark(PlayerType == PlayerType.Blue ? TankDeathMark.CheckColor.Blue : TankDeathMark.CheckColor.Red)
            {
                location = position + new Vector3(0, 0.1f, 0)
            };
            // TODO: play player tank death sound
        }

        public void UpdatePlayerMovement()
        {
            var leftStick = Input.CurrentGamePadSnapshot.ThumbSticks.Left;
            if (!WPTR.InMission)
                return;
            if (!controlDown.IsPressed && !controlUp.IsPressed && leftStick.Y == 0)
                velocity.Z = 0;
            if (!controlLeft.IsPressed && !controlRight.IsPressed && leftStick.X == 0)
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
                    var sfx = SoundPlayer.PlaySoundInstance(treadPlace, SoundContext.Sound, 0.2f);
                    LayFootprint();
                    sfx.Pitch = TreadPitch;
                }
            }
            //velocity += approachVelocity / 10;
            // barrelRotation = GameUtils.DirectionOf(GameUtils.MousePosition.ToVector3(), position).ToRotation();
            // approachVelocity = Vector3.Zero;
        }

        public override void LayFootprint()
        {
            var fp = new TankFootprint()
            {
                location = position + new Vector3(0, 0.1f, 0),
                rotation = -TankRotation
            };
        }


        /// <summary>
        /// Finish bullet implementation!
        /// </summary>
        /// <param name="velocity"></param>
        /// <param name="bulletSpeed"></param>
        public override void Shoot()
        {
            if (!WPTR.InMission)
                return;
            if (curShootCooldown > 0 || OwnedBulletCount >= ShellLimit)
                return;

            SoundEffect shootSound;

            shootSound = ShellType switch
            {
                ShellTier.Rocket => GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_rocket"),
                ShellTier.RicochetRocket => GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_ricochet_rocket"),
                ShellTier.Regular => GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_regular_1"),
                _ => throw new NotImplementedException()
            };

            var sfx = SoundPlayer.PlaySoundInstance(shootSound, SoundContext.Sound, 0.3f);

            sfx.Pitch = ShootPitch;

            var bullet = new Shell(position, Vector3.Zero);
            var new2d = Vector2.UnitY.RotatedByRadians(TurretRotation - MathHelper.Pi);

            var newPos = Position2D + new Vector2(0, 20).RotatedByRadians(-TurretRotation);

            bullet.position = new Vector3(newPos.X, 11, newPos.Y);

            bullet.velocity = new Vector3(new2d.X, 0, -new2d.Y) * ShellSpeed;

            bullet.owner = this;
            bullet.ricochets = RicochetCount;

            OwnedBulletCount++;

            curShootStun = ShootStun;
            curShootCooldown = ShellCooldown;
        }

        private void RenderModel()
        {
            CannonMesh.ParentBone.Transform = Matrix.CreateRotationY(TurretRotation + TankRotation);
            Model.Root.Transform = World;

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
            => $"pos: {position} | vel: {velocity} | dead: {Dead} | rotation: {TankRotation} | OwnedBullets: {OwnedBulletCount}";
    }
}