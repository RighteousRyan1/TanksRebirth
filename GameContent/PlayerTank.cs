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
                Properties.ShootPitch = 0.1f;

            Properties.Team = TankTeam.Red;

            Properties.Dead = true;

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
            Properties.ShellCooldown = 5;
            Properties.ShootStun = 5;
            Properties.ShellSpeed = 3f; // 3f
            Properties.MaxSpeed = 1.8f;
            Properties.RicochetCount = 1; // 1
            Properties.ShellLimit = 5;
            Properties.MineLimit = 2;
            Properties.MineStun = 8;
            Properties.Invisible = false;
            Properties.Acceleration = 0.3f;
            Properties.Deceleration = 0.6f;
            Properties.TurningSpeed = 0.1f;
            Properties.MaximalTurn = MathHelper.ToRadians(10); // normally it's 10 degrees, but we want to make it easier for keyboard players.
            // Armor = new(this, 100);

            Properties.ShellType = ShellType.Player;

            Properties.ShellHoming = new();

            Properties.DestructionColor = PlayerType switch
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

            if (Properties.IsIngame)
            {
                if (Client.IsConnected())
                    ChatSystem.SendMessage($"PlayerId: {PlayerId} | ClientId: {NetPlay.CurrentClient.Id}", Color.White);
                if (NetPlay.IsClientMatched(PlayerId) && !IntermissionSystem.IsAwaitingNewMission)
                {
                    if (!TankGame.ThirdPerson)
                    {
                        Vector3 mouseWorldPos = GameUtils.GetWorldPosition(GameUtils.MousePosition, -11f);
                        Properties.TurretRotation = (-(new Vector2(mouseWorldPos.X, mouseWorldPos.Z) - Properties.Position).ToRotation()) + MathHelper.PiOver2;
                    }
                    else
                    {
                        //Mouse.SetPosition(Input.CurrentMouseSnapshot.X, GameUtils.WindowHeight / 2);
                        if (Input.CurrentMouseSnapshot.X >= GameUtils.WindowWidth)
                            Mouse.SetPosition(1, Input.CurrentMouseSnapshot.Y);
                        if (Input.CurrentMouseSnapshot.X <= 0)
                            Mouse.SetPosition(GameUtils.WindowWidth - 1, Input.CurrentMouseSnapshot.Y);
                        //Mouse.SetPosition((int)GameUtils.WindowCenter.X, (int)GameUtils.WindowCenter.Y);
                        Properties.TurretRotation += -TankGame.MouseVelocity.X / 312; // terry evanswood
                    }
                }

                if (GameHandler.InMission)
                {
                    if (CurShootStun <= 0 && CurMineStun <= 0)
                    {
                        if (!Properties.Stationary)
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

                        if (!Properties.Stationary)
                            UpdatePlayerMovement();
                    }
                }
            }

            timeSinceLastAction++;

            Properties.Speed = Properties.Acceleration;

            playerControl_isBindPressed = false;

            //if (Client.IsConnected() && IsIngame)
            //Client.SyncPlayer(this);

            CannonMesh.ParentBone.Transform = Matrix.CreateRotationY(Properties.TurretRotation + Properties.TankRotation);
            Model.Root.Transform = World;

            Model.CopyAbsoluteBoneTransformsTo(boneTransforms);

            oldPosition = Properties.Position;
        }

        public override void Remove()
        {
            Properties.Dead = true;
            GameHandler.AllPlayerTanks[PlayerId] = null;
            GameHandler.AllTanks[WorldId] = null;
            base.Remove();
        }

        /// <summary>
        /// Controller support soon i hope (this is not working)
        /// </summary>
        private void ControlHandle_ConsoleController()
        {
            Properties.TankRotation %= MathHelper.Tau;

            if (TargetTankRotation - Properties.TankRotation >= MathHelper.PiOver2)
                Properties.TankRotation += MathHelper.Pi;
            else if (TargetTankRotation - Properties.TankRotation <= -MathHelper.PiOver2)
                Properties.TankRotation -= MathHelper.Pi;

            var leftStick = Input.CurrentGamePadSnapshot.ThumbSticks.Left;
            var rightStick = Input.CurrentGamePadSnapshot.ThumbSticks.Right;
            var dPad = Input.CurrentGamePadSnapshot.DPad;

            var treadPlaceTimer = (int)Math.Round(14 / Properties.Velocity.Length()) != 0 ? (int)Math.Round(14 / Properties.Velocity.Length()) : 1;

            var accelReal = Properties.Acceleration * leftStick.Length() * Properties.MaxSpeed * 4;

            preterbedVelocity = Vector2.Zero;

            preterbedVelocity = new(leftStick.X, -leftStick.Y);

            var norm = Vector2.Normalize(preterbedVelocity);

            TargetTankRotation = norm.ToRotation() - MathHelper.PiOver2;

            Properties.TankRotation = GameUtils.RoughStep(Properties.TankRotation, TargetTankRotation, Properties.TurningSpeed);

            var rotationMet = Properties.TankRotation > TargetTankRotation - Properties.MaximalTurn && Properties.TankRotation < TargetTankRotation + Properties.MaximalTurn;

            if (!rotationMet)
            {
                if (TankGame.GameUpdateTime % treadPlaceTimer == 0)
                    LayFootprint(false);
                Properties.Speed -= Properties.Deceleration;
                if (Properties.Speed < 0)
                    Properties.Speed = 0;
                Body.LinearVelocity = Vector2.Zero;
                Properties.Velocity = Vector2.Zero;
                Properties.IsTurning = true;
            }
            else
            {
                if (TankGame.ThirdPerson)
                    preterbedVelocity = preterbedVelocity.RotatedByRadians(-Properties.TurretRotation + MathHelper.Pi);

                Properties.Speed += Properties.Acceleration;
                if (Properties.Speed > Properties.MaxSpeed)
                    Properties.Speed = Properties.MaxSpeed;
                
                if (leftStick.Length() > 0)
                {
                    playerControl_isBindPressed = true;

                    Properties.Velocity.X = leftStick.X * accelReal;
                    Properties.Velocity.Y = -leftStick.Y * accelReal;
                }

                if (dPad.Down == ButtonState.Pressed)
                {
                    playerControl_isBindPressed = true;
                    Properties.Velocity.Y += Properties.Acceleration;
                }
                if (dPad.Up == ButtonState.Pressed)
                {
                    playerControl_isBindPressed = true;
                    Properties.Velocity.Y -= Properties.Acceleration;
                }
                if (dPad.Left == ButtonState.Pressed)
                {
                    playerControl_isBindPressed = true;
                    Properties.Velocity.X -= Properties.Acceleration;
                }
                if (dPad.Right == ButtonState.Pressed)
                {
                    playerControl_isBindPressed = true;
                    Properties.Velocity.X += Properties.Acceleration;
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
            if (TargetTankRotation - Properties.TankRotation >= MathHelper.PiOver2)
                Properties.TankRotation += MathHelper.Pi;
            else if (TargetTankRotation - Properties.TankRotation <= -MathHelper.PiOver2)
                Properties.TankRotation -= MathHelper.Pi;

            if (controlMine.JustPressed)
                LayMine();

            Properties.IsTurning = false;

            var norm = Vector2.Normalize(preterbedVelocity);

            var treadPlaceTimer = (int)Math.Round(14 / Properties.Velocity.Length()) != 0 ? (int)Math.Round(14 / Properties.Velocity.Length()) : 1;

            TargetTankRotation = norm.ToRotation() - MathHelper.PiOver2;

            Properties.TankRotation = GameUtils.RoughStep(Properties.TankRotation, TargetTankRotation, Properties.TurningSpeed);

            var rotationMet = Properties.TankRotation > TargetTankRotation - Properties.MaximalTurn && Properties.TankRotation < TargetTankRotation + Properties.MaximalTurn;

            Properties.TankRotation %= MathHelper.Tau;

            preterbedVelocity = Vector2.Zero;

            if (!rotationMet)
            {
                Properties.Speed -= Properties.Deceleration;
                if (Properties.Speed < 0)
                    Properties.Speed = 0;
                // treadPlaceTimer += MaxSpeed / 5;
                if (TankGame.GameUpdateTime % treadPlaceTimer == 0)
                    LayFootprint(false);
                Body.LinearVelocity = Vector2.Zero;
                Properties.Velocity = Vector2.Zero;
                Properties.IsTurning = true;
            }
            else
            {
                Properties.Speed += Properties.Acceleration;
                if (Properties.Speed > Properties.MaxSpeed)
                    Properties.Speed = Properties.MaxSpeed;
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
                preterbedVelocity = preterbedVelocity.RotatedByRadians(-Properties.TurretRotation + MathHelper.Pi);

            Properties.Velocity = preterbedVelocity * Properties.Speed * 3;
            //ChatSystem.SendMessage($"{preterbedVelocity} | " + preterbedVelocity.RotatedByRadians(-TurretRotation + MathHelper.Pi), Color.White);
        }

        public override void Damage(ITankHurtContext context)
        {
            base.Damage(context);
            // TODO: play player tank death sound
        }
        public override void Destroy(ITankHurtContext context)
        {

            if (context.IsPlayer)
            {
                if (context is TankHurtContext_Bullet cxt1)
                {
                    //if (cxt.Bounces > 0)
                    TankGame.GameData.BulletKills++;
                    TankGame.GameData.TotalKills++;

                }
                if (context is TankHurtContext_Mine cxt2)
                {
                    TankGame.GameData.MineKills++;
                    TankGame.GameData.TotalKills++;
                }
                // check if player id matches client id, if so, increment that player's kill count, then sync to the server
                // TODO: convert TankHurtContext into a struct and use it here
                // Will be used to track the reason of death and who caused the death, if any tank owns a shell or mine
                //
                // if (context.PlayerId == Client.PlayerId)
                // {
                //    PlayerTank.KillCount++;
                //   Client.Send(new TankKillCountUpdateMessage(PlayerTank.KillCount)); // not a bad idea actually
            }

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

            if (Properties.Velocity.Length() > 0 && playerControl_isBindPressed)
            {
                var treadPlaceTimer = (int)Math.Round(14 / Properties.Velocity.Length()) != 0 ? (int)Math.Round(14 / Properties.Velocity.Length()) : 1;
                if (TankGame.GameUpdateTime % treadPlaceTimer == 0  )
                {
                    LayFootprint(false);
                }
                if (TankGame.GameUpdateTime % MathHelper.Clamp(treadPlaceTimer / 2, 4, 6) == 0)
                {
                    var treadPlace = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_tread_place_{GameHandler.GameRand.Next(1, 5)}");
                    var sfx = SoundPlayer.PlaySoundInstance(treadPlace, SoundContext.Effect, 0.2f);
                    sfx.Pitch = Properties.TreadPitch;
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
            var pathPos = Properties.Position + new Vector2(0, 0).RotatedByRadians(-Properties.TurretRotation);
            var pathDir = Vector2.UnitY.RotatedByRadians(Properties.TurretRotation - MathHelper.Pi);
            pathDir.Y *= -1;
            pathDir *= Properties.ShellSpeed;

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

                if (bounces > Properties.RicochetCount)
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

                    if (!Properties.HasTurret)
                        if (mesh.Name == "Cannon")
                            return;

                    if (mesh.Name != "Shadow")
                    {
                        effect.Alpha = 1f;
                        effect.Texture = _tankColorTexture;

                        if (Properties.IsHoveredByMouse)
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
                        if (Properties.IsIngame)
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
            if (Properties.Dead)
                return;

            if (DebugUtils.DebugLevel == 1)
                DrawShootPath();

            var info = new string[]
            {
                $"Team: {Properties.Team}",
                $"OwnedShellCount: {Properties.OwnedShellCount}"
            };

            // TankGame.spriteBatch.Draw(GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel"), CollisionBox2D, Color.White * 0.75f);

            for (int i = 0; i < info.Length; i++)
                DebugUtils.DrawDebugString(TankGame.spriteBatch, info[i], GeometryUtils.ConvertWorldToScreen(Vector3.Zero, World, View, Projection) - new Vector2(0, (info.Length * 20) + (i * 20)), 1, centered: true);

            if (Properties.Invisible && GameHandler.InMission)
                return;

            RenderModel();
            Properties.Armor?.Render();
        }

        public override string ToString()
            => $"pos: {Properties.Position} | vel: {Properties.Velocity} | dead: {Properties.Dead} | rotation: {Properties.TankRotation} | OwnedBullets: {Properties.OwnedShellCount}";
    }
}