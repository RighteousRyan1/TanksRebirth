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
using TanksRebirth.GameContent.GameMechanics;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Framework.Input;
using TanksRebirth.Graphics;
using TanksRebirth.Net;
using TanksRebirth.GameContent.Systems;
using FontStashSharp;
using TanksRebirth.GameContent.Properties;
using TanksRebirth.GameContent.UI;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.RebirthUtils;

namespace TanksRebirth.GameContent
{
    public class PlayerTank : Tank
    {
        /// <summary>The amount of lives for every existing player.<para>Lives[0] = Blue Tank's Lives</para>
        /// Lives[1] = Red Tank's Lives
        /// <para>Lives[2] = Green Tank's Lives</para>
        /// Lives[3] = Yellow Tank's Lives
        /// <para>Note that lives are always synced on multiplayer.</para>
        /// </summary>
        public static int[] Lives = new int[4];

        /// <summary>In multiplayer, gets the lives of the client that this code is currently being called on.</summary>
        public static int GetMyLives() => Client.IsConnected() ? Lives[NetPlay.CurrentClient.Id] : Lives[0];
        /// <summary>
        /// Adds lives to the player in Single-Player, adds to the lives of all players in Multiplayer.
        /// </summary>
        /// <param name="num">How many lives to add.</param>
        public static void AddLives(int num)
        {
            if (Client.IsConnected())
                Lives[NetPlay.CurrentClient.Id] += num;
            else
                for (int i = 0; i < Lives.Length; i++)
                    Lives[i] += num;
        }
        /// <summary>
        /// Sets the lives of the player in Single-Player, sets the lives of all players in Multiplayer.
        /// </summary>
        /// <param name="num">How many lives to set the player(s) to.</param>
        public static void SetLives(int num)
        {
            if (Client.IsConnected())
                Lives[NetPlay.CurrentClient.Id] = num;
            else
                for (int i = 0; i < Lives.Length; i++)
                    Lives[i] = num;
        }

        #region The Rest
        public static int MyTeam;
        public static int MyTankType;

        public static int StartingLives = 3;

        // public static Dictionary<PlayerType, Dictionary<TankTier, int>> TanksKillDict = new(); // this campaign only!
        public static Dictionary<int, int> TankKills = new(); // this campaign only!

        public struct DeterministicPlayerStats {
            public int ShellsShotThisCampaign;
            public int ShellHitsThisCampaign;
            public int MinesLaidThisCampaign;
            public int MineHitsThisCampaign;
            public int SuicidesThisCampaign; // self-damage this campaign?
        }

        public static DeterministicPlayerStats PlayerStatistics;

        public static bool _drawShotPath;

        public static int KillCount = 0;

        public int PlayerId { get; }
        public int PlayerType { get; }

        private Texture2D _tankTexture;
        private static Texture2D _shadowTexture;

        public static Keybind controlUp = new("Up", Keys.W);
        public static Keybind controlDown = new("Down", Keys.S);
        public static Keybind controlLeft = new("Left", Keys.A);
        public static Keybind controlRight = new("Right", Keys.D);
        public static Keybind controlMine = new("Place Mine", Keys.Space);
        public static Keybind controlFirePath = new("Draw Shot Path", Keys.Q);
        public static GamepadBind FireBullet = new("Fire Bullet", Buttons.RightTrigger);
        public static GamepadBind PlaceMine = new("Place Mine", Buttons.A);

        private bool playerControl_isBindPressed;

        public Vector2 oldPosition;

        private bool _isPlayerModel;

        #endregion
        public void SwapTankTexture(Texture2D texture) => _tankTexture = texture;
        public PlayerTank(int playerType, bool isPlayerModel = true, int copyTier = TankID.None)
        {
            Model = GameResources.GetGameResource<Model>(isPlayerModel ? "Assets/tank_p" : "Assets/tank_e");
            if (copyTier == TankID.None)
                _tankTexture = Assets[$"tank_" + PlayerID.Collection.GetKey(playerType).ToLower()];
            else
            {
                _tankTexture = Assets[$"tank_" + TankID.Collection.GetKey(copyTier).ToLower()];
                var dummy = new AITank(copyTier, default, true, false, false);

                // ugly hardcode fix lol - prevents nantuple instead of triple bounces
                // maybe inefficient on memory
                // TODO: should probably be written better


				// TODO: hardcode hell. bad. suck. balls
                if (Difficulties.Types["BulletHell"])
                    dummy.Properties.RicochetCount /= 3;

                Properties = dummy.Properties;

                dummy.Remove(true);
            }

            _isPlayerModel = isPlayerModel;

            //CannonMesh = Model.Meshes["Cannon"];

            //boneTransforms = new Matrix[Model.Bones.Count];

            _shadowTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank_shadow");

            PlayerType = playerType;

            Team = TeamID.Red;

            Dead = true;

            //int index = Array.IndexOf(GameHandler.AllPlayerTanks, GameHandler.AllAITanks.First(tank => tank is null));

            PlayerId = playerType; //index;

            GameHandler.AllPlayerTanks[PlayerId] = this;
            
            if (copyTier == TankID.None)
                ApplyDefaults(ref Properties);

            var newTankIndex = Array.IndexOf(GameHandler.AllTanks, null);

            WorldId = newTankIndex;

            GameHandler.AllTanks[newTankIndex] = this;

            base.Initialize();
        }

        public sealed override void ApplyDefaults(ref TankProperties properties)  {
            properties.TreadVolume = 0.2f;
            properties.ShellCooldown = 5; // 5
            properties.ShootStun = 5; // 5
            properties.ShellSpeed = 3f; // 3f
            properties.MaxSpeed = 1.8f; // 1.8
            properties.RicochetCount = 1; // 1
            properties.ShellLimit = 5; // 5
            properties.MineLimit = 2; // 2
            properties.MineStun = 8; // 8
            properties.Invisible = false;
            properties.Acceleration = 0.3f;
            properties.Deceleration = 0.6f;
            properties.TurningSpeed = 0.1f;
            properties.MaximalTurn = MathHelper.ToRadians(10); // normally it's 10 degrees, but we want to make it easier for keyboard players.

            Properties.ShootPitch = 0.1f * PlayerType;

            properties.ShellType = ShellID.Player;

            properties.ShellHoming = new();

            properties.DestructionColor = PlayerType switch {
                PlayerID.Blue => Color.Blue,
                PlayerID.Red => Color.Crimson,
                PlayerID.GreenPlr => Color.Lime,
                PlayerID.YellowPlr => Color.Yellow,
                _ => throw new Exception($"The player type with number \"{PlayerType}\" is not mapped to a color!"),
            };
            
            base.ApplyDefaults(ref properties);
        }

        public override void Update()  {
            /*if (Input.KeyJustPressed(Keys.P))
                foreach (var m in TankDeathMark.deathMarks)
                    m?.ResurrectTank();*/
            // FIXME: reference?

            // pi/2 = up
            // 0 = down
            // pi/4 = right
            // 3/4pi = left

            base.Update();
            
            if (LevelEditor.Active) return;
            
            if (NetPlay.IsClientMatched(PlayerId))
                Client.SyncPlayerTank(this);

            if (Dead)
                return;

            //CannonMesh.ParentBone.Transform = Matrix.CreateRotationY(TurretRotation + TankRotation + (Flip ? MathHelper.Pi : 0));
            //Model.Root.Transform = World;

            //Model.CopyAbsoluteBoneTransformsTo(boneTransforms);

            if (GameProperties.InMission) {
                if (TargetTankRotation - TankRotation >= MathHelper.PiOver2) {
                    TankRotation += MathHelper.Pi;
                    Flip = !Flip;
                }
                else if (TargetTankRotation - TankRotation <= -MathHelper.PiOver2) {
                    TankRotation -= MathHelper.Pi;
                    Flip = !Flip;
                }
            }

            if (IsIngame) {
                //if (Client.IsConnected())
                    //ChatSystem.SendMessage($"PlayerId: {PlayerId} | ClientId: {NetPlay.CurrentClient.Id}", Color.White);
                if (NetPlay.IsClientMatched(PlayerId) && !IntermissionSystem.IsAwaitingNewMission) {
                    if (!Difficulties.Types["ThirdPerson"] || LevelEditor.Active || MainMenu.Active) {
                        Vector3 mouseWorldPos = MatrixUtils.GetWorldPosition(MouseUtils.MousePosition, -11f);
                        if (!LevelEditor.Active)
                            TurretRotation = -(new Vector2(mouseWorldPos.X, mouseWorldPos.Z) - Position).ToRotation() + MathHelper.PiOver2;
                        else
                            TurretRotation = TankRotation;
                    }
                    else if (!GameUI.Paused) {
                        Mouse.SetPosition(InputUtils.CurrentMouseSnapshot.X, WindowUtils.WindowHeight / 2);
                        //Mouse.SetPosition(Input.CurrentMouseSnapshot.X, WindowUtils.WindowHeight / 2);
                        if (InputUtils.CurrentMouseSnapshot.X >= WindowUtils.WindowWidth)
                            Mouse.SetPosition(1, InputUtils.CurrentMouseSnapshot.Y);
                        if (InputUtils.CurrentMouseSnapshot.X <= 0)
                            Mouse.SetPosition(WindowUtils.WindowWidth - 1, WindowUtils.WindowHeight / 2);
                        //Mouse.SetPosition((int)GameUtils.WindowCenter.X, (int)GameUtils.WindowCenter.Y);
                        TurretRotation += -TankGame.MouseVelocity.X / (312.ToResolutionX()); // terry evanswood
                    }
                }

                if (GameProperties.InMission && !LevelEditor.Active && !ChatSystem.ActiveHandle) {
                    if (CurShootStun <= 0 && CurMineStun <= 0) {
                        if (!Properties.Stationary) {
                            if (NetPlay.IsClientMatched(PlayerId)) {
                                if (InputUtils.CurrentGamePadSnapshot.IsConnected)
                                    ControlHandle_ConsoleController();
                                else
                                    ControlHandle_Keybinding();
                            }
                        }
                    }
                } else {
                    // Due to a problem of Inheritance, the tank will move 1.8 in the x coordinate for no reason. Thanks OOP, a revolution in Computer Science.
                    // To avoid this, just force the damn speed to zero when we are not in a mission.
                    Speed = 0;
                }

                if (GameProperties.InMission && !LevelEditor.Active) {
                    if (NetPlay.IsClientMatched(PlayerId)) {
                        if (InputUtils.CanDetectClick())
                            if (!ChatSystem.ChatBoxHover && !ChatSystem.ActiveHandle)
                                Shoot(false);

                        if (!Properties.Stationary)
                            UpdatePlayerMovement();
                    }
                }
            }

            timeSinceLastAction++;

            playerControl_isBindPressed = false;

            //if (Client.IsConnected() && IsIngame)
            //Client.SyncPlayer(this);

            oldPosition = Position;
        }

        public override void Remove(bool nullifyMe)
        {
            if (nullifyMe) {
                GameHandler.AllPlayerTanks[PlayerId] = null;
                GameHandler.AllTanks[WorldId] = null;
            }
            base.Remove(nullifyMe);
        }

        public override void Shoot(bool fxOnly) {
            PlayerStatistics.ShellsShotThisCampaign++;
            base.Shoot(false);
        }

        public override void LayMine() {
            PlayerStatistics.MinesLaidThisCampaign++;
            base.LayMine();
        }

        private void ControlHandle_ConsoleController()
        {

            var leftStick = InputUtils.CurrentGamePadSnapshot.ThumbSticks.Left;
            var rightStick = InputUtils.CurrentGamePadSnapshot.ThumbSticks.Right;
            var dPad = InputUtils.CurrentGamePadSnapshot.DPad;

            var preterbedVelocity = new Vector2(leftStick.X, -leftStick.Y);

            var rotationMet = TankRotation > TargetTankRotation - Properties.MaximalTurn && TankRotation < TargetTankRotation + Properties.MaximalTurn;

            if (!rotationMet)
            {
                Speed -= Properties.Deceleration * TankGame.DeltaTime;
                if (Speed < 0)
                    Speed = 0;
                IsTurning = true;
            }
            else
            {
                if (Difficulties.Types["ThirdPerson"])
                    preterbedVelocity = preterbedVelocity.RotatedByRadians(-TurretRotation + MathHelper.Pi);

                Speed += Properties.Acceleration * TankGame.DeltaTime;
                if (Speed > Properties.MaxSpeed)
                    Speed = Properties.MaxSpeed;
                
                if (leftStick.Length() > 0)
                {
                    playerControl_isBindPressed = true;
                }

                if (dPad.Down == ButtonState.Pressed)
                {
                    playerControl_isBindPressed = true;
                    preterbedVelocity.Y = 1;
                }
                if (dPad.Up == ButtonState.Pressed)
                {
                    playerControl_isBindPressed = true;
                    preterbedVelocity.Y = -1;
                }
                if (dPad.Left == ButtonState.Pressed)
                {
                    playerControl_isBindPressed = true;
                    preterbedVelocity.X = -1;
                }
                if (dPad.Right == ButtonState.Pressed)
                {
                    playerControl_isBindPressed = true;
                    preterbedVelocity.X = 1;
                }
            }

            var norm = Vector2.Normalize(preterbedVelocity);

            TargetTankRotation = norm.ToRotation() - MathHelper.PiOver2;

            TankRotation = MathUtils.RoughStep(TankRotation, TargetTankRotation, Properties.TurningSpeed * TankGame.DeltaTime);

            if (rightStick.Length() > 0)
            {
                var unprojectedPosition = MatrixUtils.ConvertWorldToScreen(new Vector3(0, 11, 0), World, View, Projection);
                Mouse.SetPosition((int)(unprojectedPosition.X + rightStick.X * 250), (int)(unprojectedPosition.Y - rightStick.Y * 250));
                //Mouse.SetPosition((int)(Input.CurrentMouseSnapshot.X + rightStick.X * TankGame.Instance.Settings.ControllerSensitivity), (int)(Input.CurrentMouseSnapshot.Y - rightStick.Y * TankGame.Instance.Settings.ControllerSensitivity));
            }

            Velocity = Vector2.UnitY.RotatedByRadians(TankRotation) * Speed;

            if (FireBullet.JustPressed)
                Shoot(false);
            if (PlaceMine.JustPressed)
                LayMine();
        }
        private void ControlHandle_Keybinding()
        {
            if (controlFirePath.JustPressed)
                _drawShotPath = !_drawShotPath;
            if (controlMine.JustPressed)
                LayMine();

            IsTurning = false;

            //var rotationMet = TankRotation > TargetTankRotation - Properties.MaximalTurn && TankRotation < TargetTankRotation + Properties.MaximalTurn;

            var preterbedVelocity = Vector2.Zero;

            TankRotation %= MathHelper.Tau;

            /*if (!rotationMet)
            {
                Speed -= Properties.Deceleration;
                if (Speed < 0)
                    Speed = 0;
                IsTurning = true;
            }
            else
            {
                Speed += Properties.Acceleration;
                if (Speed > Properties.MaxSpeed)
                    Speed = Properties.MaxSpeed;
            }*/


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

            if (Difficulties.Types["ThirdPerson"])
                preterbedVelocity = preterbedVelocity.RotatedByRadians(-TurretRotation + MathHelper.Pi);

            var norm = Vector2.Normalize(preterbedVelocity);

            TargetTankRotation = norm.ToRotation() - MathHelper.PiOver2;

            TankRotation = MathUtils.RoughStep(TankRotation, TargetTankRotation, Properties.TurningSpeed * TankGame.DeltaTime);

            Velocity = Vector2.UnitY.RotatedByRadians(TankRotation) * Speed;
        }
        public override void Destroy(ITankHurtContext context)
        {
            if (Client.IsConnected())
            {
                TankGame.SpectateValidTank(PlayerId, true);
                if (NetPlay.IsClientMatched(PlayerId))
                    AddLives(-1);
            }

            if (context is not null)
            {
                if (context.IsPlayer)
                {
                    // this probably makes sense
                    TankGame.GameData.Suicides++;
                    PlayerStatistics.SuicidesThisCampaign++;
                    // check if player id matches client id, if so, increment that player's kill count, then sync to the server
                    // TODO: convert TankHurtContext into a struct and use it here
                    // Will be used to track the reason of death and who caused the death, if any tank owns a shell or mine
                    //
                    // if (context.PlayerId == Client.PlayerId)
                    // {
                    //    PlayerTank.KillCount++;
                    //    Client.Send(new TankKillCountUpdateMessage(PlayerTank.KillCount)); 
                    //    not a bad idea actually
                }
            }
            TankGame.GameData.Deaths++;

            Remove(false);
            base.Destroy(context);
        }
        public void UpdatePlayerMovement()
        {
            if (!GameProperties.InMission)
                return;
            //if (!controlDown.IsPressed && !controlUp.IsPressed && leftStick.Y == 0)
                //Velocity.Y = 0;
            //if (!controlLeft.IsPressed && !controlRight.IsPressed && leftStick.X == 0)
                //Velocity.X = 0;
        }
        private void DrawShootPath()
        {
            const int MAX_PATH_UNITS = 10000;

            var whitePixel = GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel");
            var pathPos = Position + new Vector2(0, 18).RotatedByRadians(-TurretRotation);
            var pathDir = Vector2.UnitY.RotatedByRadians(TurretRotation - MathHelper.Pi);
            pathDir.Y *= -1;
            pathDir *= Properties.ShellSpeed;

            var pathRicochetCount = 0;


            for (int i = 0; i < MAX_PATH_UNITS; i++)
            {
                var dummyPos = Vector2.Zero;

                if (pathPos.X < MapRenderer.MIN_X || pathPos.X > MapRenderer.MAX_X)
                {
                    pathRicochetCount++;
                    pathDir.X *= -1;
                }
                if (pathPos.Y < MapRenderer.MIN_Y || pathPos.Y > MapRenderer.MAX_Y)
                {
                    pathRicochetCount++;
                    pathDir.Y *= -1;
                }

                var pathHitbox = new Rectangle((int)pathPos.X - 3, (int)pathPos.Y - 3, 6, 6);

                // Why is velocity passed by reference here lol
                Collision.HandleCollisionSimple_ForBlocks(pathHitbox, pathDir, ref dummyPos, out var dir, out var block, out bool corner, false, (c) => c.IsSolid);

                if (corner)
                    return;
                if (block != null)
                {
                    if (block.AllowShotPathBounce)
                    {
                        switch (dir)
                        {
                            case CollisionDirection.Up:
                            case CollisionDirection.Down:
                                pathDir.Y *= -1;
                                pathRicochetCount += block.PathBounceCount;
                                break;
                            case CollisionDirection.Left:
                            case CollisionDirection.Right:
                                pathDir.X *= -1;
                                pathRicochetCount += block.PathBounceCount;
                                break;
                        }
                    }
                }

                if (pathRicochetCount > Properties.RicochetCount)
                    return;

                if (GameHandler.AllTanks.Any(tnk => tnk is not null && !tnk.Dead && tnk.CollisionCircle.Intersects(new Internals.Common.Framework.Circle() { Center = pathPos, Radius = 4 })))
                    return;

                pathPos += pathDir;
                // tainicom.Aether.Physics2D.Collision.
                var pathPosScreen = MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(pathPos.X, 11, pathPos.Y), TankGame.GameView, TankGame.GameProjection);
                var off = (float)Math.Sin(i * Math.PI / 5 - TankGame.UpdateCount * 0.3f);
                TankGame.SpriteRenderer.Draw(whitePixel, pathPosScreen, null, (Color.White.ToVector3() * off).ToColor(), 0, whitePixel.Size() / 2, 2 + off, default, default);
            }
        }
        public override void Render()
        {
            base.Render();
            if (Dead)
                return;
            DrawExtras();
            if (Properties.Invisible && GameProperties.InMission)
                return;
            for (int i = 0; i < (Lighting.AccurateShadows ? 2 : 1); i++)
            {
                foreach (ModelMesh mesh in Model.Meshes)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        effect.World = i == 0 ? _boneTransforms[mesh.ParentBone.Index] : _boneTransforms[mesh.ParentBone.Index] * Matrix.CreateShadow(Lighting.AccurateLightingDirection, new(Vector3.UnitY, 0)) * Matrix.CreateTranslation(0, 0.2f, 0);
                        effect.View = View;
                        effect.Projection = Projection;
                        effect.TextureEnabled = true;

                        if (!Properties.HasTurret)
                            if (mesh.Name == "Cannon")
                                return;


                        if (mesh.Name != "Shadow")
                        {
                            effect.Alpha = 1f;
                            effect.Texture = _tankTexture;

                            if (IsHoveredByMouse)
                                effect.EmissiveColor = Color.White.ToVector3();
                            else
                                effect.EmissiveColor = Color.Black.ToVector3();
                            //effect.SpecularColor = Color.White.ToVector3();
                            //effect.SpecularPower = 10f;
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
                            if (!Lighting.AccurateShadows)
                            {
                                if (IsIngame)
                                {
                                    effect.Alpha = 0.5f;
                                    effect.Texture = _shadowTexture;
                                    mesh.Draw();
                                }
                            }
                        }

                        effect.SetDefaultGameLighting_IngameEntities(specular: _isPlayerModel, ambientMultiplier: _isPlayerModel ? 2f : 0.9f);
                    }
                }
            }
        }
        private void DrawExtras()
        {
            if (Dead)
                return;
            if (NetPlay.IsClientMatched(PlayerId)) {
                var tex = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/bullet_ui");
                var scale = 0.5f; // the graphic gets smaller for each availiable shell.
                for (int i = 0; i < Properties.ShellLimit; i++) {
                    var scalar = 0.95f + (i * 0.001f); //changetankproperty ShellLimit 
                    scalar = MathHelper.Clamp(scalar, 0f, 0.99f);
                    scale *= scalar;
                }
                var realSize = (tex.Size() * scale).ToResolution();
                for (int i = 1; i <= Properties.ShellLimit; i++) {
                    var colorToUse = i > OwnedShellCount ? ColorUtils.DiscoPartyColor : Color.DarkGray;
                    var position = new Vector2(WindowUtils.WindowWidth - realSize.X, realSize.Y + 40.ToResolutionY());
                    TankGame.SpriteRenderer.Draw(tex, position + new Vector2(0, i * (realSize.Y + (scale * 20).ToResolutionY())), null, colorToUse, 0f, new Vector2(tex.Size().X, 0), new Vector2(scale).ToResolution(), default, default);
                }
            }

            bool needClarification = GameProperties.ShouldMissionsProgress && !GameProperties.InMission && IsIngame && !IntermissionSystem.IsAwaitingNewMission;

            var playerColor = PlayerID.PlayerTankColors[PlayerType].ToColor();
            var pos = MatrixUtils.ConvertWorldToScreen(Vector3.Zero, World, View, Projection) - new Vector2(0, 125).ToResolution(); ;

            bool flip = false;

            float rotation = 0f;

            if (pos.Y <= 150) {
                flip = true;
                pos.Y += 225;
                rotation = MathHelper.Pi;
            }


            if (needClarification)
            {
                var tex1 = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/chevron_border");
                var tex2 = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/chevron_inside");

                // var p = GameHandler.AllPlayerTanks;
                //string pText = "nerd";
                string pText = Client.IsConnected() ? Server.ConnectedClients[PlayerId].Name : $"P{PlayerId + 1}"; // heeheeheeha

                TankGame.SpriteRenderer.Draw(tex1, pos, null, Color.White, rotation, tex1.Size() / 2, 0.5f.ToResolution(), default, default);
                TankGame.SpriteRenderer.Draw(tex2, pos, null, playerColor, rotation, tex2.Size() / 2, 0.5f.ToResolution(), default, default);

                SpriteFontUtils.DrawBorderedText(TankGame.SpriteRenderer, TankGame.TextFontLarge, pText, new(pos.X, pos.Y + (flip ? 100 : -125).ToResolutionY()), playerColor, Color.White, Vector2.One.ToResolution(), 0f, 2f);
            }

            if (DebugManager.DebugLevel == 1 || _drawShotPath)
                DrawShootPath();

            if (Properties.Invisible && GameProperties.InMission)
                return;

            Properties.Armor?.Render();
        }

        public override string ToString()
            => $"pos: {Position} | vel: {Velocity} | dead: {Dead} | rotation: {TankRotation} | OwnedBullets: {OwnedShellCount}";
    }
}