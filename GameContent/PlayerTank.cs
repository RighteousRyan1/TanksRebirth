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
using WiiPlayTanksRemake.GameContent.GameMechanics;
using WiiPlayTanksRemake.Internals.Common.Framework.Audio;
using WiiPlayTanksRemake.Internals.Common.Framework.Input;
using WiiPlayTanksRemake.Graphics;

namespace WiiPlayTanksRemake.GameContent
{
    public class PlayerTank : Tank
    {
        public bool playerControl_isBindPressed;

        private int _treadSoundTimer = 5;

        public int PlayerId { get; }
        public int WorldId { get; }
        public PlayerType PlayerType { get; }

        internal Texture2D _tankColorTexture;
        private static Texture2D _shadowTexture;

        public Vector3 preterbedVelocity;

        public static Keybind controlUp = new("Up", Keys.W);
        public static Keybind controlDown = new("Down", Keys.S);
        public static Keybind controlLeft = new("Left", Keys.A);
        public static Keybind controlRight = new("Right", Keys.D);
        public static Keybind controlMine = new("Place Mine", Keys.Space);
        public static GamepadBind FireBullet = new("Fire Bullet", Buttons.RightTrigger);

        public Vector3 oldPosition;

        #region ModelBone & ModelMesh
        public Matrix[] boneTransforms;

        public ModelMesh CannonMesh;
        #endregion

        public PlayerTank(PlayerType playerType)
        {
            Model = TankGame.TankModel_Player;
            _tankColorTexture = GameResources.GetGameResource<Texture2D>($"Assets/textures/player/tank_{playerType.ToString().ToLower()}");

            CannonMesh = Model.Meshes["Cannon"];

            boneTransforms = new Matrix[Model.Bones.Count];

            _shadowTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank_shadow");

            PlayerType = playerType;

            ApplyDefaults();

            if (playerType == PlayerType.Red)
                ShootPitch = 0.1f;

            //ShellHoming.cooldown = 30;
            //ShellHoming.power = 1f;
            //ShellHoming.radius = 200f;
            //ShellHoming.speed = 3f;

            Team = Team.Red;

            Dead = true;

            int index = Array.IndexOf(GameHandler.AllAITanks, GameHandler.AllAITanks.First(tank => tank is null));

            PlayerId = index;

            GameHandler.AllPlayerTanks[index] = this;

            int index2 = Array.IndexOf(GameHandler.AllTanks, GameHandler.AllTanks.First(tank => tank is null));

            WorldId = index2;

            GameHandler.AllTanks[index2] = this;

            //WPTR.AllPlayerTanks.Add(this);
            //WPTR.AllTanks.Add(this);
        }

        public override void ApplyDefaults()
        {
            ShellCooldown = 5;
            ShootStun = 5;
            ShellSpeed = 3f; // 3f
            MaxSpeed = 1.8f;
            RicochetCount = 1; // 1
            ShellLimit = 5;
            MineLimit = 2;
            MineStun = 8;
            Invisible = false;
            Acceleration = 0.3f;
            TurningSpeed = 0.1f;
            MaximalTurn = 0.8f;

            ShellType = ShellTier.Player;

            ShellHoming = new();

            TankDestructionColor = PlayerType switch
            {
                PlayerType.Blue => Color.Blue,
                PlayerType.Red => Color.Red,
                _ => throw new Exception("What")
            };
        }

        public override void Update()
        {
            // pi/2 = up
            // 0 = down
            // pi/4 = right
            // 3/4pi = left

            if (Dead)
                return;

            Projection = TankGame.GameProjection;
            View = TankGame.GameView;

            World = Matrix.CreateFromYawPitchRoll(-TankRotation, 0, 0)
                * Matrix.CreateTranslation(position);

            Vector3 mouseWorldPos = GameUtils.GetWorldPosition(GameUtils.MousePosition, -11f);

            TurretRotation = (-(new Vector2(mouseWorldPos.X, mouseWorldPos.Z) - Position2D).ToRotation()) + MathHelper.PiOver2;

            if (GameHandler.InMission)
            {
                base.Update();
                if (CurShootStun <= 0 && CurMineStun <= 0)
                {
                    if (!Stationary)
                    {
                        ControlHandle_Keybinding();
                        if (Input.CurrentGamePadSnapshot.IsConnected)
                            ControlHandle_ConsoleController();
                    }
                    else
                        velocity = Vector3.Zero;
                }
                else
                    velocity = Vector3.Zero;
            }
            else
                velocity = Vector3.Zero;

            if (GameHandler.InMission)
            {
                if (Input.CanDetectClick())
                    Shoot();

                if (!Stationary)
                    UpdatePlayerMovement();
            }

            timeSinceLastAction++;

            Speed = Acceleration;

            playerControl_isBindPressed = false;

            CannonMesh.ParentBone.Transform = Matrix.CreateRotationY(TurretRotation + TankRotation);
            Model.Root.Transform = World;

            Model.CopyAbsoluteBoneTransformsTo(boneTransforms); // a

            oldPosition = position;
        }

        public override void RemoveSilently()
        {
            GameHandler.AllPlayerTanks[PlayerId] = null;
            GameHandler.AllTanks[WorldId] = null;
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
                var unprojectedPosition = GeometryUtils.ConvertWorldToScreen(default, World, View, Projection);
                Mouse.SetPosition((int)(unprojectedPosition.X + rightStick.X * 250), (int)(unprojectedPosition.Y - rightStick.Y * 250));
                //Mouse.SetPosition((int)(Input.CurrentMouseSnapshot.X + rightStick.X * TankGame.Instance.Settings.ControllerSensitivity), (int)(Input.CurrentMouseSnapshot.Y - rightStick.Y * TankGame.Instance.Settings.ControllerSensitivity));
            }

            if (FireBullet.JustPressed)
            {
                Shoot();
            }
        }
        private float treadPlaceTimer;
        private void ControlHandle_Keybinding()
        {
            if (controlMine.JustPressed)
                LayMine();

            IsTurning = false;
            bool rotationMet = false;

            var norm = Vector3.Normalize(preterbedVelocity);

            treadPlaceTimer = (int)Math.Round(14 / velocity.Length()) != 0 ? (int)Math.Round(14 / velocity.Length()) : 1;

            var targetTnkRotation = norm.FlattenZ().ToRotation() - MathHelper.PiOver2;

            TankRotation = GameUtils.RoughStep(TankRotation, targetTnkRotation, TurningSpeed);

            if (TankRotation > targetTnkRotation - MaximalTurn && TankRotation < targetTnkRotation + MaximalTurn)
                rotationMet = true;
            else
            {
                // treadPlaceTimer += MaxSpeed / 5;
                if (TankGame.GameUpdateTime % treadPlaceTimer == 0)
                    LayFootprint(false);
                velocity = Vector3.Zero;
                IsTurning = true;
            }

            preterbedVelocity = Vector3.Zero;

            if (controlDown.IsPressed)
            {
                playerControl_isBindPressed = true;
                preterbedVelocity.Z += 1;

                if (rotationMet)
                    velocity.Z += Acceleration;
            }
            if (controlUp.IsPressed)
            {
                playerControl_isBindPressed = true;
                preterbedVelocity.Z -= 1;


                if (rotationMet)
                    velocity.Z -= Acceleration;
            }
            if (controlLeft.IsPressed)
            {
                playerControl_isBindPressed = true;
                preterbedVelocity.X -= 1;

                if (rotationMet)
                    velocity.X -= Acceleration;
            }
            if (controlRight.IsPressed)
            {
                playerControl_isBindPressed = true;
                preterbedVelocity.X += 1;

                if (rotationMet)
                    velocity.X += Acceleration;
            }

            TankRotation %= MathHelper.Tau;

            if (targetTnkRotation - TankRotation >= MathHelper.PiOver2)
                TankRotation += MathHelper.Pi;
            else if (targetTnkRotation - TankRotation <= -MathHelper.PiOver2)
                TankRotation -= MathHelper.Pi;

            //if (playerControl_isBindPressed)
                // GameHandler.ClientLog.Write(targetTnkRotation - TankRotation, LogType.Info);
        }

        public override void UpdateCollision()
        {
            if (IsIngame)
                base.UpdateCollision();
        }

        public override void Destroy()
        {
            base.Destroy();
            GameHandler.AllPlayerTanks[PlayerId] = null;
            GameHandler.AllTanks[WorldId] = null;
            // TODO: play player tank death sound
        }

        public override void LayMine()
        {
            base.LayMine();
        }

        public void UpdatePlayerMovement()
        {
            var leftStick = Input.CurrentGamePadSnapshot.ThumbSticks.Left;
            if (!GameHandler.InMission)
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
                treadPlaceTimer = (int)Math.Round(14 / velocity.Length()) != 0 ? (int)Math.Round(14 / velocity.Length()) : 1;
                if (TankGame.GameUpdateTime % treadPlaceTimer == 0  )
                {
                    LayFootprint(false);
                }
                if (TankGame.GameUpdateTime % _treadSoundTimer == 0)
                {
                    var treadPlace = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_tread_place_{GameHandler.GameRand.Next(1, 5)}");
                    var sfx = SoundPlayer.PlaySoundInstance(treadPlace, SoundContext.Effect, 0.2f);
                    sfx.Pitch = TreadPitch;
                }
            }
        }

        public override void LayFootprint(bool alt)
        {
            base.LayFootprint(alt);
        }

        private void DrawShootPath()
        {
            const int MAX_PATH_UNITS = 500;

            var whitePixel = GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel");
            var pathPos = Position2D + new Vector2(0, 0).RotatedByRadians(-TurretRotation);
            var pathDir = Vector2.UnitY.RotatedByRadians(TurretRotation - MathHelper.Pi);
            pathDir.Y *= -1;
            pathDir *= ShellSpeed;

            var bounces = 0;


            for (int i = 0; i < MAX_PATH_UNITS; i++)
            {
                var dummyPos = Vector3.Zero;

                if (pathPos.X < MapRenderer.MIN_X || pathPos.X > MapRenderer.MAX_X)
                {
                    bounces++;
                    pathDir.X *= -1;
                }
                if (pathPos.Y < MapRenderer.MIN_Y || pathPos.Y > MapRenderer.MAX_Y)
                {
                    bounces++;
                    pathDir.Y *= -1;
                }

                var pathHitbox = new Rectangle((int)pathPos.X - 3, (int)pathPos.Y - 3, 6, 6);

                // Why is velocity passed by reference here lol
                Collision.HandleCollisionSimple_ForBlocks(pathHitbox, ref pathDir, ref dummyPos, out var dir, false, (c) => c.IsSolid);


                switch (dir)
                {
                    case Collision.CollisionDirection.Up:
                    case Collision.CollisionDirection.Down:
                        pathDir.Y *= -1;
                        bounces++;
                        break;
                    case Collision.CollisionDirection.Left:
                    case Collision.CollisionDirection.Right:
                        pathDir.X *= -1;
                        bounces++;
                        break;
                }

                if (bounces > RicochetCount)
                    return;

                pathPos += pathDir;
                
                var pathPosScreen = GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(pathPos.X, 11, pathPos.Y), TankGame.GameView, TankGame.GameProjection);
                TankGame.spriteBatch.Draw(whitePixel, pathPosScreen, null, Color.White, 0, whitePixel.Size() / 2, 2 + (float)Math.Sin(i * Math.PI / 5 - TankGame.GameUpdateTime * 0.3f), default, default);
            }
        }

        /// <summary>
        /// Finish bullet implementation!
        /// </summary>
        public override void Shoot()
        {
            base.Shoot();
        }

        private void RenderModel()
        {

            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = boneTransforms[mesh.ParentBone.Index];
                    effect.View = View;
                    effect.Projection = Projection;
                    effect.TextureEnabled = true;

                    if (!HasTurret)
                        if (mesh.Name == "Cannon")
                            return;

                    if (mesh.Name != "Shadow")
                    {
                        effect.Texture = _tankColorTexture;

                        /*var ex = new Color[1024];

                        Array.Fill(ex, Team != Team.NoTeam ? (Color)typeof(Color).GetProperty(Team.ToString(), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).GetValue(null) : default);

                        if (Team != Team.NoTeam)
                        {
                            effect.Texture.SetData(0, new Rectangle(0, 0, 32, 9), ex, 0, 288);
                            effect.Texture.SetData(0, new Rectangle(0, 23, 32, 9), ex, 0, 288);
                        }*/
                    }

                    else
                        effect.Texture = _shadowTexture;

                    effect.SetDefaultGameLighting_IngameEntities();
                }
                mesh.Draw();
            }
        }

        internal void DrawBody()
        {
            if (Dead)
                return;

            if (DebugUtils.DebugLevel == 1)
                DrawShootPath();

            var info = new string[]
            {
                $"Team: {Team}",
                $"OwnedShellCount: {OwnedShellCount}"
            };

            // TankGame.spriteBatch.Draw(GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel"), CollisionBox2D, Color.White * 0.75f);

            for (int i = 0; i < info.Length; i++)
                DebugUtils.DrawDebugString(TankGame.spriteBatch, info[i], GeometryUtils.ConvertWorldToScreen(Vector3.Zero, World, View, Projection) - new Vector2(0, (info.Length * 20) + (i * 20)), 1, centered: true);

            if (Invisible && GameHandler.InMission)
                return;

            RenderModel();
        }

        public override string ToString()
            => $"pos: {position} | vel: {velocity} | dead: {Dead} | rotation: {TankRotation} | OwnedBullets: {OwnedShellCount}";
    }
}