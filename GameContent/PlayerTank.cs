using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TanksRebirth.Enums;
using System.Linq;
using TanksRebirth.Internals.Common.GameInput;
using Microsoft.Xna.Framework.Input;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals;
using Microsoft.Xna.Framework.Audio;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Core.Interfaces;
using TanksRebirth.GameContent.GameMechanics;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Framework.Input;
using TanksRebirth.Graphics;
using TanksRebirth.Net;
using TanksRebirth.GameContent.Systems;

namespace TanksRebirth.GameContent
{
    public class PlayerTank : Tank
    {
        public static TankTeam MyTeam;

        public static int StartingLives = 3;
        public static int Lives = 0;

        public static Dictionary<TankTier, int> TanksKillDict = new();

        public static int KillCount = 0;
        
        private bool playerControl_isBindPressed;

        public int PlayerId { get; }
        public PlayerType PlayerType { get; }

        internal Texture2D _tankColorTexture;
        private static Texture2D _shadowTexture;

        public Vector2 preterbedVelocity;

        public static Keybind controlUp = new("Up", Keys.W);
        public static Keybind controlDown = new("Down", Keys.S);
        public static Keybind controlLeft = new("Left", Keys.A);
        public static Keybind controlRight = new("Right", Keys.D);
        public static Keybind controlMine = new("Place Mine", Keys.Space);
        public static GamepadBind FireBullet = new("Fire Bullet", Buttons.RightTrigger);
        public static GamepadBind PlaceMine = new("Place Mine", Buttons.A);

        public Vector2 oldPosition;

        #region ModelBone & ModelMesh
        public Matrix[] boneTransforms;

        public ModelMesh CannonMesh;
        #endregion

        public PlayerTank(PlayerType playerType, bool isPlayerModel = true)
        {
            Model = GameResources.GetGameResource<Model>(isPlayerModel ? "Assets/tank_p" : "Assets/tank_e");
            _tankColorTexture = Assets[$"tank_" + playerType.ToString().ToLower()];

            CannonMesh = Model.Meshes["Cannon"];

            boneTransforms = new Matrix[Model.Bones.Count];

            _shadowTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank_shadow");

            PlayerType = playerType;

            ApplyDefaults();

            if (playerType == PlayerType.Red)
                ShootPitch = 0.1f;

            Team = TankTeam.Red;

            Dead = true;

            int index = Array.IndexOf(GameHandler.AllPlayerTanks, GameHandler.AllAITanks.First(tank => tank is null));

            PlayerId = index;

            GameHandler.AllPlayerTanks[index] = this;

            int index2 = Array.IndexOf(GameHandler.AllTanks, GameHandler.AllTanks.First(tank => tank is null));

            WorldId = index2;

            GameHandler.AllTanks[index2] = this;

            base.Initialize();
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
            Deceleration = 0.6f;
            TurningSpeed = 0.1f;
            MaximalTurn = MathHelper.ToRadians(10); // normally it's 10 degrees, but we want to make it easier for keyboard players.
            // Armor = new(this, 100);

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

            base.Update();

            if (IsIngame)
            {
                if (Client.IsConnected())
                    ChatSystem.SendMessage($"PlayerId: {PlayerId} | ClientId: {NetPlay.CurrentClient.Id}", Color.White);
                if (NetPlay.IsClientMatched(PlayerId) && !IntermissionSystem.IsAwaitingNewMission)
                {
                    if (!TankGame.ThirdPerson)
                    {
                        Vector3 mouseWorldPos = GameUtils.GetWorldPosition(GameUtils.MousePosition, -11f);
                        TurretRotation = (-(new Vector2(mouseWorldPos.X, mouseWorldPos.Z) - Position).ToRotation()) + MathHelper.PiOver2;
                    }
                    else
                    {
                        //Mouse.SetPosition(Input.CurrentMouseSnapshot.X, GameUtils.WindowHeight / 2);
                        if (Input.CurrentMouseSnapshot.X >= GameUtils.WindowWidth)
                            Mouse.SetPosition(1, Input.CurrentMouseSnapshot.Y);
                        if (Input.CurrentMouseSnapshot.X <= 0)
                            Mouse.SetPosition(GameUtils.WindowWidth - 1, Input.CurrentMouseSnapshot.Y);
                        //Mouse.SetPosition((int)GameUtils.WindowCenter.X, (int)GameUtils.WindowCenter.Y);
                        TurretRotation += -TankGame.MouseVelocity.X / 312; // terry evanswood
                    }
                }

                if (GameHandler.InMission)
                {
                    if (CurShootStun <= 0 && CurMineStun <= 0)
                    {
                        if (!Stationary)
                        {
                            if (NetPlay.IsClientMatched(PlayerId))
                            {
                                if (Input.CurrentGamePadSnapshot.IsConnected)
                                    ControlHandle_ConsoleController();
                                else
                                    ControlHandle_Keybinding();
                            }
                        }
                    }
                }

                if (GameHandler.InMission)
                {
                    if (NetPlay.IsClientMatched(PlayerId))
                    {
                        if (Input.CanDetectClick())
                            Shoot();

                        if (!Stationary)
                            UpdatePlayerMovement();
                    }
                }
            }

            timeSinceLastAction++;

            Speed = Acceleration;

            playerControl_isBindPressed = false;

            //if (Client.IsConnected() && IsIngame)
            //Client.SyncPlayer(this);

            CannonMesh.ParentBone.Transform = Matrix.CreateRotationY(TurretRotation + TankRotation);
            Model.Root.Transform = World;

            Model.CopyAbsoluteBoneTransformsTo(boneTransforms);

            oldPosition = Position;
        }

        public override void Remove()
        {
            Dead = true;
            GameHandler.AllPlayerTanks[PlayerId] = null;
            GameHandler.AllTanks[WorldId] = null;
            base.Remove();
        }

        /// <summary>
        /// Controller support soon i hope (this is not working)
        /// </summary>
        private void ControlHandle_ConsoleController()
        {
            TankRotation %= MathHelper.Tau;

            if (TargetTankRotation - TankRotation >= MathHelper.PiOver2)
                TankRotation += MathHelper.Pi;
            else if (TargetTankRotation - TankRotation <= -MathHelper.PiOver2)
                TankRotation -= MathHelper.Pi;

            var leftStick = Input.CurrentGamePadSnapshot.ThumbSticks.Left;
            var rightStick = Input.CurrentGamePadSnapshot.ThumbSticks.Right;
            var dPad = Input.CurrentGamePadSnapshot.DPad;

            var treadPlaceTimer = (int)Math.Round(14 / Velocity.Length()) != 0 ? (int)Math.Round(14 / Velocity.Length()) : 1;

            var accelReal = Acceleration * leftStick.Length() * MaxSpeed * 4;

            preterbedVelocity = Vector2.Zero;

            preterbedVelocity = new(leftStick.X, -leftStick.Y);

            var norm = Vector2.Normalize(preterbedVelocity);

            TargetTankRotation = norm.ToRotation() - MathHelper.PiOver2;

            TankRotation = GameUtils.RoughStep(TankRotation, TargetTankRotation, TurningSpeed);

            var rotationMet = TankRotation > TargetTankRotation - MaximalTurn && TankRotation < TargetTankRotation + MaximalTurn;

            if (!rotationMet)
            {
                if (TankGame.GameUpdateTime % treadPlaceTimer == 0)
                    LayFootprint(false);
                Speed -= Deceleration;
                if (Speed < 0)
                    Speed = 0;
                Body.LinearVelocity = Vector2.Zero;
                Velocity = Vector2.Zero;
                IsTurning = true;
            }
            else
            {
                if (TankGame.ThirdPerson)
                    preterbedVelocity = preterbedVelocity.RotatedByRadians(-TurretRotation + MathHelper.Pi);

                Speed += Acceleration;
                if (Speed > MaxSpeed)
                    Speed = MaxSpeed;
                
                if (leftStick.Length() > 0)
                {
                    playerControl_isBindPressed = true;

                    Velocity.X = leftStick.X * accelReal;
                    Velocity.Y = -leftStick.Y * accelReal;
                }

                if (dPad.Down == ButtonState.Pressed)
                {
                    playerControl_isBindPressed = true;
                    Velocity.Y += Acceleration;
                }
                if (dPad.Up == ButtonState.Pressed)
                {
                    playerControl_isBindPressed = true;
                    Velocity.Y -= Acceleration;
                }
                if (dPad.Left == ButtonState.Pressed)
                {
                    playerControl_isBindPressed = true;
                    Velocity.X -= Acceleration;
                }
                if (dPad.Right == ButtonState.Pressed)
                {
                    playerControl_isBindPressed = true;
                    Velocity.X += Acceleration;
                }
            }

            if (rightStick.Length() > 0)
            {
                var unprojectedPosition = GeometryUtils.ConvertWorldToScreen(new Vector3(0, 11, 0), World, View, Projection);
                Mouse.SetPosition((int)(unprojectedPosition.X + rightStick.X * 250), (int)(unprojectedPosition.Y - rightStick.Y * 250));
                //Mouse.SetPosition((int)(Input.CurrentMouseSnapshot.X + rightStick.X * TankGame.Instance.Settings.ControllerSensitivity), (int)(Input.CurrentMouseSnapshot.Y - rightStick.Y * TankGame.Instance.Settings.ControllerSensitivity));
            }

            if (FireBullet.JustPressed)
                Shoot();
            if (PlaceMine.JustPressed)
                LayMine();
        }
        private void ControlHandle_Keybinding()
        {
            if (TargetTankRotation - TankRotation >= MathHelper.PiOver2)
                TankRotation += MathHelper.Pi;
            else if (TargetTankRotation - TankRotation <= -MathHelper.PiOver2)
                TankRotation -= MathHelper.Pi;

            if (controlMine.JustPressed)
                LayMine();

            IsTurning = false;

            var norm = Vector2.Normalize(preterbedVelocity);

            var treadPlaceTimer = (int)Math.Round(14 / Velocity.Length()) != 0 ? (int)Math.Round(14 / Velocity.Length()) : 1;

            TargetTankRotation = norm.ToRotation() - MathHelper.PiOver2;

            TankRotation = GameUtils.RoughStep(TankRotation, TargetTankRotation, TurningSpeed);

            var rotationMet = TankRotation > TargetTankRotation - MaximalTurn && TankRotation < TargetTankRotation + MaximalTurn;

            TankRotation %= MathHelper.Tau;

            preterbedVelocity = Vector2.Zero;

            if (!rotationMet)
            {
                Speed -= Deceleration;
                if (Speed < 0)
                    Speed = 0;
                // treadPlaceTimer += MaxSpeed / 5;
                if (TankGame.GameUpdateTime % treadPlaceTimer == 0)
                    LayFootprint(false);
                Body.LinearVelocity = Vector2.Zero;
                Velocity = Vector2.Zero;
                IsTurning = true;
            }
            else
            {
                Speed += Acceleration;
                if (Speed > MaxSpeed)
                    Speed = MaxSpeed;
            }


            if (controlDown.IsPressed)
            {
                playerControl_isBindPressed = true;
                preterbedVelocity.Y = 1;
            }
            if (controlUp.IsPressed)
            {
                playerControl_isBindPressed = true;
                preterbedVelocity.Y = -1;
            }
            if (controlLeft.IsPressed)
            {
                playerControl_isBindPressed = true;
                preterbedVelocity.X = -1;
            }
            if (controlRight.IsPressed)
            {
                playerControl_isBindPressed = true;
                preterbedVelocity.X = 1;
            }


            if (TankGame.ThirdPerson)
                preterbedVelocity = preterbedVelocity.RotatedByRadians(-TurretRotation + MathHelper.Pi);
            
            Velocity = preterbedVelocity * Speed * 3;
            //ChatSystem.SendMessage($"{preterbedVelocity} | " + preterbedVelocity.RotatedByRadians(-TurretRotation + MathHelper.Pi), Color.White);
        }

        public override void Damage(TankHurtContext context)
        {
            base.Damage(context);
            // TODO: play player tank death sound
        }
        public override void Destroy(TankHurtContext context)
        {
            GameHandler.AllPlayerTanks[PlayerId] = null;
            GameHandler.AllTanks[WorldId] = null;
            base.Destroy(context);
        }

        public void UpdatePlayerMovement()
        {
            var leftStick = Input.CurrentGamePadSnapshot.ThumbSticks.Left;
            if (!GameHandler.InMission)
                return;
            //if (!controlDown.IsPressed && !controlUp.IsPressed && leftStick.Y == 0)
                //Velocity.Y = 0;
            //if (!controlLeft.IsPressed && !controlRight.IsPressed && leftStick.X == 0)
                //Velocity.X = 0;

            if (Velocity.Length() > 0 && playerControl_isBindPressed)
            {
                var treadPlaceTimer = (int)Math.Round(14 / Velocity.Length()) != 0 ? (int)Math.Round(14 / Velocity.Length()) : 1;
                if (TankGame.GameUpdateTime % treadPlaceTimer == 0  )
                {
                    LayFootprint(false);
                }
                if (TankGame.GameUpdateTime % MathHelper.Clamp(treadPlaceTimer / 2, 4, 6) == 0)
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
            const int MAX_PATH_UNITS = 10000;

            var whitePixel = GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel");
            var pathPos = Position + new Vector2(0, 0).RotatedByRadians(-TurretRotation);
            var pathDir = Vector2.UnitY.RotatedByRadians(TurretRotation - MathHelper.Pi);
            pathDir.Y *= -1;
            pathDir *= ShellSpeed;

            var bounces = 0;


            for (int i = 0; i < MAX_PATH_UNITS; i++)
            {
                var dummyPos = Vector2.Zero;

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
                Collision.HandleCollisionSimple_ForBlocks(pathHitbox, pathDir, ref dummyPos, out var dir, out var type, out bool corner, false, (c) => c.IsSolid);

                if (corner)
                    return;
                switch (dir)
                {
                    case CollisionDirection.Up:
                    case CollisionDirection.Down:
                        pathDir.Y *= -1;
                        bounces++;
                        break;
                    case CollisionDirection.Left:
                    case CollisionDirection.Right:
                        pathDir.X *= -1;
                        bounces++;
                        break;
                }

                if (bounces > RicochetCount)
                    return;

                pathPos += pathDir;
                // tainicom.Aether.Physics2D.Collision.
                var pathPosScreen = GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(pathPos.X, 11, pathPos.Y), TankGame.GameView, TankGame.GameProjection);
                TankGame.spriteBatch.Draw(whitePixel, pathPosScreen, null, Color.White, 0, whitePixel.Size() / 2, 2 + (float)Math.Sin(i * Math.PI / 5 - TankGame.GameUpdateTime * 0.3f), default, default);
            }
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
                        effect.Alpha = 1f;
                        effect.Texture = _tankColorTexture;

                        if (IsHoveredByMouse)
                            effect.EmissiveColor = Color.White.ToVector3();
                        else
                            effect.EmissiveColor = Color.Black.ToVector3();

                        /*var ex = new Color[1024];

                        Array.Fill(ex, Team != Team.NoTeam ? (Color)typeof(Color).GetProperty(Team.ToString(), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).GetValue(null) : default);

                        if (Team != Team.NoTeam)
                        {
                            effect.Texture.SetData(0, new Rectangle(0, 0, 32, 9), ex, 0, 288);
                            effect.Texture.SetData(0, new Rectangle(0, 23, 32, 9), ex, 0, 288);
                        }*/
                        mesh.Draw();
                    }

                    else
                    {
                        if (IsIngame)
                        {
                            effect.Alpha = 0.5f;
                            effect.Texture = _shadowTexture;
                            mesh.Draw();
                        }
                    }

                    effect.SetDefaultGameLighting_IngameEntities(specular: true, ambientMultiplier: 1.5f);
                }
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
            Armor?.Render();
        }

        public override string ToString()
            => $"pos: {Position} | vel: {Velocity} | dead: {Dead} | rotation: {TankRotation} | OwnedBullets: {OwnedShellCount}";
    }
}