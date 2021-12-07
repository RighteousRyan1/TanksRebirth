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

        private int _treadPlaceTimer = 5;
        private int _treadSoundTimer = 5;
        public int curShootStun;
        public int curShootCooldown;
        private int curMineCooldown;
        private int curMineStun;

        public int PlayerId { get; }
        public int WorldId { get; }
        public PlayerType PlayerType { get; }

        public Vector2 unprojectedPosition;

        internal Texture2D _tankColorTexture;
        private static Texture2D _shadowTexture;

        public static Keybind controlUp = new("Up", Keys.W);
        public static Keybind controlDown = new("Down", Keys.S);
        public static Keybind controlLeft = new("Left", Keys.A);
        public static Keybind controlRight = new("Right", Keys.D);
        public static Keybind PlaceMine = new("Place Mine", Keys.Space);
        public static GamepadBind FireBullet = new("Fire Bullet", Buttons.RightTrigger);

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
            MaxSpeed = 1.8f; //1.8f;
            RicochetCount = 1;
            ShellLimit = 5;
            MineLimit = 2;
            MineStun = 8;
            Invisible = false;
            Acceleration = 0.3f;
            TurningSpeed = 0.08f;

            if (MaxSpeed > 5f)
                _treadPlaceTimer = 1;
            else
                _treadPlaceTimer = 5; ///= (int)(MaxSpeed * 2);

            Team = Team.Red;

            int index = Array.IndexOf(WPTR.AllAITanks, WPTR.AllAITanks.First(tank => tank is null));

            PlayerId = index;

            WPTR.AllPlayerTanks[index] = this;

            int index2 = Array.IndexOf(WPTR.AllTanks, WPTR.AllTanks.First(tank => tank is null));

            WorldId = index2;

            WPTR.AllTanks[index2] = this;

            //WPTR.AllPlayerTanks.Add(this);
            //WPTR.AllTanks.Add(this);
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
            if (curMineStun > 0)
                curMineStun--;
            if (curMineCooldown > 0)
                curMineCooldown--;
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

            unprojectedPosition = GeometryUtils.ConvertWorldToScreen(Vector3.Zero, World, View, Projection);

            if (WPTR.InMission)
            {
                if (curShootStun <= 0 && curMineStun <= 0)
                {
                    ControlHandle_Keybinding();
                    if (Input.CurrentGamePadSnapshot.IsConnected)
                        ControlHandle_ConsoleController();
                }
                else
                    velocity = Vector3.Zero;

                position += velocity * 0.55f;
            }
            else
                velocity = Vector3.Zero;

            Vector3 mouseWorldPos = GameUtils.GetWorldPosition(GameUtils.MousePosition);

            TurretRotation = (-(new Vector2(mouseWorldPos.X, mouseWorldPos.Z) - Position2D).ToRotation()) + MathHelper.PiOver2;

            if (WPTR.InMission)
            {
                if (Input.CanDetectClick())
                    Shoot();

                UpdatePlayerMovement();
                UpdateCollision();
            }

            Speed = Acceleration;

            playerControl_isBindPressed = false;

            Old = this;
        }

        /// <summary>
        /// Controller support soon i hope (this is not working)
        /// </summary>
        private void ControlHandle_ConsoleController()
        {
            var leftStick = Input.CurrentGamePadSnapshot.ThumbSticks.Left;
            var rightStick = Input.CurrentGamePadSnapshot.ThumbSticks.Right;
            var dPad = Input.CurrentGamePadSnapshot.DPad;

            var accelReal = Acceleration * leftStick.Length() * MaxSpeed * 4;

            if (leftStick.Length() > 0)
            {
                playerControl_isBindPressed = true;

                velocity.X = leftStick.X * accelReal;
                velocity.Z = -leftStick.Y * accelReal;
            }

            if (dPad.Down == ButtonState.Pressed)
            {
                playerControl_isBindPressed = true;
                velocity.Z += Acceleration;
            }
            if (dPad.Up == ButtonState.Pressed)
            {
                playerControl_isBindPressed = true;
                velocity.Z -= Acceleration;
            }
            if (dPad.Left == ButtonState.Pressed)
            {
                playerControl_isBindPressed = true;
                velocity.X -= Acceleration;
            }
            if (dPad.Right == ButtonState.Pressed)
            {
                playerControl_isBindPressed = true;
                velocity.X += Acceleration;
            }

            if (rightStick.Length() > 0)
            {
                Mouse.SetPosition((int)(unprojectedPosition.X + rightStick.X * 250), (int)(unprojectedPosition.Y - rightStick.Y * 250));
                //Mouse.SetPosition((int)(Input.CurrentMouseSnapshot.X + rightStick.X * TankGame.Instance.Settings.ControllerSensitivity), (int)(Input.CurrentMouseSnapshot.Y - rightStick.Y * TankGame.Instance.Settings.ControllerSensitivity));
            }

            if (FireBullet.JustPressed)
            {
                Shoot();
            }
        }

        private void ControlHandle_Keybinding()
        {
            if (PlaceMine.JustPressed)
                LayMine();
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
            CollisionBox = new(position - new Vector3(12, 15, 12), position + new Vector3(12, 15, 12));
            if (Old is null)
                return;
            if (WPTR.AllAITanks.Any(tnk => tnk is not null && tnk.CollisionBox.Intersects(CollisionBox)))
            {
                position = Old.position;
                //System.Diagnostics.Debug.WriteLine(new Random().Next(0, 100).ToString());
            }
            if (WPTR.AllPlayerTanks.Any(tnk => tnk is not null && tnk.CollisionBox.Intersects(CollisionBox) && tnk != this))
            {
                position = Old.position;
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

            WPTR.AllPlayerTanks[PlayerId] = null;
            WPTR.AllTanks[WorldId] = null;
            // TODO: play player tank death sound
        }

        public override void LayMine()
        {
            if (curMineCooldown > 0 || OwnedMineCount >= MineLimit)
                return;
            // fix stun
            curMineStun = MineStun;
            curMineCooldown = MineCooldown;
            var sound = GameResources.GetGameResource<SoundEffect>("Assets/sounds/mine_place");
            SoundPlayer.PlaySoundInstance(sound, SoundContext.Sound, 0.5f);
            OwnedMineCount++;
            var mine = new Mine(this, position, 600);
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

                if (TankGame.GameUpdateTime % _treadPlaceTimer == 0)
                    LayFootprint();
                if (TankGame.GameUpdateTime % _treadSoundTimer == 0)
                {
                    var treadPlace = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_tread_place_{new Random().Next(1, 5)}");
                    var sfx = SoundPlayer.PlaySoundInstance(treadPlace, SoundContext.Sound, 0.2f);
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
        public override void Shoot()
        {
            if (!WPTR.InMission)
                return;
            if (curShootCooldown > 0 || OwnedBulletCount >= ShellLimit)
                return;

            SoundEffectInstance sfx;

            sfx = ShellType switch
            {
                ShellTier.Regular => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_regular_1"), SoundContext.Sound, 0.3f),
                ShellTier.Rocket => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_rocket"), SoundContext.Sound, 0.3f),
                ShellTier.RicochetRocket => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_ricochet_rocket"), SoundContext.Sound, 0.3f),
                _ => throw new NotImplementedException()
            };

            sfx.Pitch = ShootPitch;

            var bullet = new Shell(position, Vector3.Zero, homing: ShellHoming);
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
                }
                mesh.Draw();
            }
        }

        internal void DrawBody()
        {
            if (Dead)
                return;

            var info = $"{Team}";

            DebugUtils.DrawDebugString(TankGame.spriteBatch, info, GeometryUtils.ConvertWorldToScreen(Vector3.Zero, World, View, Projection), 1, centerIt: true);

            RenderModel();
        }

        public override string ToString()
            => $"pos: {position} | vel: {velocity} | dead: {Dead} | rotation: {TankRotation} | OwnedBullets: {OwnedBulletCount}";
    }
}