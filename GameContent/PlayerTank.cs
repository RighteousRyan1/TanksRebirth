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
using WiiPlayTanksRemake.Net;

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

        public Vector2 preterbedVelocity;

        public static Keybind controlUp = new("Up", Keys.W);
        public static Keybind controlDown = new("Down", Keys.S);
        public static Keybind controlLeft = new("Left", Keys.A);
        public static Keybind controlRight = new("Right", Keys.D);
        public static Keybind controlMine = new("Place Mine", Keys.Space);
        public static GamepadBind FireBullet = new("Fire Bullet", Buttons.RightTrigger);

        public Vector2 oldPosition;

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

            GameHandler.OnMissionStart += () =>
            {
                if (Invisible && !Dead)
                {
                    var invis = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_invisible");
                    SoundPlayer.PlaySoundInstance(invis, SoundContext.Effect, 0.3f);

                    var lightParticle = ParticleSystem.MakeParticle(Position3D, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/light_particle"));

                    lightParticle.Scale = new(0.25f);
                    lightParticle.Opacity = 0f;
                    lightParticle.is2d = true;

                    lightParticle.UniqueBehavior = (lp) =>
                    {
                        lp.position = Position3D;
                        if (lp.Scale.X < 5f)
                            GeometryUtils.Add(ref lp.Scale, 0.12f);
                        if (lp.Opacity < 1f && lp.Scale.X < 5f)
                            lp.Opacity += 0.02f;

                        if (lp.lifeTime > 90)
                            lp.Opacity -= 0.005f;

                        if (lp.Scale.X < 0f)
                            lp.Destroy();
                    };

                    const int NUM_LOCATIONS = 8;

                    for (int i = 0; i < NUM_LOCATIONS; i++)
                    {
                        var lp = ParticleSystem.MakeParticle(Position3D + new Vector3(0, 5, 0), GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smokes"));

                        var velocity = Vector2.UnitY.RotatedByRadians(MathHelper.ToRadians(360f / NUM_LOCATIONS * i));

                        lp.Scale = new(1f);

                        lp.UniqueBehavior = (elp) =>
                        {
                            elp.position.X += velocity.X;
                            elp.position.Z += velocity.Y;

                            if (elp.lifeTime > 15)
                            {
                                GeometryUtils.Add(ref elp.Scale, -0.03f);
                                elp.Opacity -= 0.03f;
                            }

                            if (elp.Scale.X <= 0f || elp.Opacity <= 0f)
                                elp.Destroy();
                        };
                    }
                }
            };

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
            Deceleration = 0.3f;
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

            base.Update();

            if (IsIngame)
            {
                if (Client.IsClientConnected())
                    Systems.ChatSystem.SendMessage($"PlayerId: {PlayerId} | ClientId: {NetPlay.CurrentClient.Id}", Color.White);
                if (NetPlay.IsIdEqualTo(PlayerId))
                {
                    Vector3 mouseWorldPos = GameUtils.GetWorldPosition(GameUtils.MousePosition, -11f);
                    TurretRotation = (-(new Vector2(mouseWorldPos.X, mouseWorldPos.Z) - Position).ToRotation()) + MathHelper.PiOver2;
                }

                if (GameHandler.InMission)
                {
                    if (CurShootStun <= 0 && CurMineStun <= 0)
                    {
                        if (!Stationary)
                        {
                            if (NetPlay.IsIdEqualTo(PlayerId))
                            {
                                ControlHandle_Keybinding();
                                if (Input.CurrentGamePadSnapshot.IsConnected)
                                    ControlHandle_ConsoleController();
                            }
                        }
                    }
                    else
                        Velocity = Vector2.Zero;
                }

                if (GameHandler.InMission)
                {
                    if (NetPlay.IsIdEqualTo(PlayerId))
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
            var leftStick = Input.CurrentGamePadSnapshot.ThumbSticks.Left;
            var rightStick = Input.CurrentGamePadSnapshot.ThumbSticks.Right;
            var dPad = Input.CurrentGamePadSnapshot.DPad;

            var rotationMet = false;

            var treadPlaceTimer = (int)Math.Round(14 / Velocity.Length()) != 0 ? (int)Math.Round(14 / Velocity.Length()) : 1;

            var accelReal = Acceleration * leftStick.Length() * MaxSpeed * 4;

            preterbedVelocity = Vector2.Zero;

            preterbedVelocity = new(leftStick.X, -leftStick.Y);

            var norm = Vector2.Normalize(preterbedVelocity);

            TargetTankRotation = norm.ToRotation() - MathHelper.PiOver2;

            TankRotation = GameUtils.RoughStep(TankRotation, TargetTankRotation, TurningSpeed);

            if (TankRotation > TargetTankRotation - MaximalTurn && TankRotation < TargetTankRotation + MaximalTurn)
                rotationMet = true;
            else
            {
                if (TankGame.GameUpdateTime % treadPlaceTimer == 0)
                    LayFootprint(false);
                Velocity = Vector2.Zero;
                IsTurning = true;
            }

            if (rotationMet)
            {
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
            {
                Shoot();
            }
        }
        private void ControlHandle_Keybinding()
        {
            if (controlMine.JustPressed)
                LayMine();

            IsTurning = false;
            bool rotationMet = false;

            var norm = Vector2.Normalize(preterbedVelocity);

            var treadPlaceTimer = (int)Math.Round(14 / Velocity.Length()) != 0 ? (int)Math.Round(14 / Velocity.Length()) : 1;

            TargetTankRotation = norm.ToRotation() - MathHelper.PiOver2;

            TankRotation = GameUtils.RoughStep(TankRotation, TargetTankRotation, TurningSpeed);

            if (TankRotation > TargetTankRotation - MaximalTurn && TankRotation < TargetTankRotation + MaximalTurn)
                rotationMet = true;
            else
            {
                // treadPlaceTimer += MaxSpeed / 5;
                if (TankGame.GameUpdateTime % treadPlaceTimer == 0)
                    LayFootprint(false);
                Velocity = Vector2.Zero;
                IsTurning = true;
            }

            preterbedVelocity = Vector2.Zero;

            if (controlDown.IsPressed)
            {
                playerControl_isBindPressed = true;
                preterbedVelocity.Y += 1;

                if (rotationMet)
                    Velocity.Y += Acceleration;
            }
            if (controlUp.IsPressed)
            {
                playerControl_isBindPressed = true;
                preterbedVelocity.Y -= 1;


                if (rotationMet)
                    Velocity.Y -= Acceleration;
            }
            if (controlLeft.IsPressed)
            {
                playerControl_isBindPressed = true;
                preterbedVelocity.X -= 1;

                if (rotationMet)
                    Velocity.X -= Acceleration;
            }
            if (controlRight.IsPressed)
            {
                playerControl_isBindPressed = true;
                preterbedVelocity.X += 1;

                if (rotationMet)
                    Velocity.X += Acceleration;
            }

            TankRotation %= MathHelper.Tau;

            if (TargetTankRotation - TankRotation >= MathHelper.PiOver2)
                TankRotation += MathHelper.Pi;
            else if (TargetTankRotation - TankRotation <= -MathHelper.PiOver2)
                TankRotation -= MathHelper.Pi;

            //if (playerControl_isBindPressed)
                // GameHandler.ClientLog.Write(targetTnkRotation - TankRotation, LogType.Info);
        }

        public override void Destroy()
        {
            base.Destroy();
            GameHandler.AllPlayerTanks[PlayerId] = null;
            GameHandler.AllTanks[WorldId] = null;
            // TODO: play player tank death sound
        }

        public void UpdatePlayerMovement()
        {
            var leftStick = Input.CurrentGamePadSnapshot.ThumbSticks.Left;
            if (!GameHandler.InMission)
                return;
            if (!controlDown.IsPressed && !controlUp.IsPressed && leftStick.Y == 0)
                Velocity.Y = 0;
            if (!controlLeft.IsPressed && !controlRight.IsPressed && leftStick.X == 0)
                Velocity.X = 0;

            if (Velocity.X > MaxSpeed)
                Velocity.X = MaxSpeed;
            if (Velocity.X < -MaxSpeed)
                Velocity.X = -MaxSpeed;
            if (Velocity.Y > MaxSpeed)
                Velocity.Y = MaxSpeed;
            if (Velocity.Y < -MaxSpeed)
                Velocity.Y = -MaxSpeed;

            if (Velocity.Length() > 0 && playerControl_isBindPressed)
            {
                var treadPlaceTimer = (int)Math.Round(14 / Velocity.Length()) != 0 ? (int)Math.Round(14 / Velocity.Length()) : 1;
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
                Collision.HandleCollisionSimple_ForBlocks(pathHitbox, pathDir, ref dummyPos, out var dir, out var type, false, (c) => c.IsSolid);


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
        }

        public override string ToString()
            => $"pos: {Position} | vel: {Velocity} | dead: {Dead} | rotation: {TankRotation} | OwnedBullets: {OwnedShellCount}";
    }
}