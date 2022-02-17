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
        public int curShootStun;
        public int curShootCooldown;
        private int curMineCooldown;
        private int curMineStun;

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
            ShellSpeed = 3f;
            MaxSpeed = 1.8f;
            RicochetCount = 1;
            ShellLimit = 5;
            MineLimit = 2;
            MineStun = 8;
            Invisible = false;
            Acceleration = 0.3f;
            TurningSpeed = 0.1f;
            MaximalTurn = 0.8f;

            ShellHoming = new();
        }

        internal void Update()
        {
            // pi/2 = up
            // 0 = down
            // pi/4 = right
            // 3/4pi = left

            if (Dead)
                return;

            UpdateCollision();

            if (curShootStun > 0)
                curShootStun--;
            if (curShootCooldown > 0)
                curShootCooldown--;
            if (curMineStun > 0)
                curMineStun--;
            if (curMineCooldown > 0)
                curMineCooldown--;

            if (velocity != Vector3.Zero)
            {
                //GameUtils.RoughStep(ref tankRotation, tankRotationPredicted.ToRotation(), 0.5f);
                //  TankRotation = Velocity2D.ToRotation() - MathHelper.PiOver2;
            }
            Projection = TankGame.GameProjection;
            View = TankGame.GameView;

            World = Matrix.CreateFromYawPitchRoll(-TankRotation, 0, 0)
                * Matrix.CreateTranslation(position);

            Vector3 mouseWorldPos = GameUtils.GetWorldPosition(GameUtils.MousePosition, -11f);

            TurretRotation = (-(new Vector2(mouseWorldPos.X, mouseWorldPos.Z) - Position2D).ToRotation()) + MathHelper.PiOver2;

            if (GameHandler.InMission)
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

            if (GameHandler.InMission)
            {
                if (Input.CanDetectClick())
                    Shoot();

                UpdatePlayerMovement();
            }

            Speed = Acceleration;

            playerControl_isBindPressed = false;

            position.X = MathHelper.Clamp(position.X, MapRenderer.TANKS_MIN_X, MapRenderer.TANKS_MAX_X);
            position.Z = MathHelper.Clamp(position.Z, MapRenderer.TANKS_MIN_Y, MapRenderer.TANKS_MAX_Y);
                
            oldPosition = position;
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

            var targetTnkRotation = norm.FlattenZ().ToRotation() - MathHelper.PiOver2;

            TankRotation = GameUtils.RoughStep(TankRotation, targetTnkRotation, TurningSpeed);

            if (TankRotation > targetTnkRotation - MaximalTurn && TankRotation < targetTnkRotation + MaximalTurn)
                rotationMet = true;
            else
            {
                // treadPlaceTimer += MaxSpeed / 5;
                if (treadPlaceTimer > MaxSpeed)
                {
                    treadPlaceTimer = 0;
                    LayFootprint(false);
                }
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
        }

        private void UpdateCollision()
        {
            CollisionBox = new(position - new Vector3(12, 15, 12), position + new Vector3(12, 15, 12));

            foreach (var c in Cube.cubes)
            {
                if (c is not null)
                {
                    var dummyVel = Velocity2D;
                    Collision.HandleCollisionSimple(CollisionBox2D, c.collider2d, ref dummyVel, ref position);

                    velocity.X = dummyVel.X;
                    velocity.Z = dummyVel.Y;
                }
            }


            foreach (var tank in GameHandler.AllTanks)
            {
                if (tank is not null)
                {
                    var dummyVel = Velocity2D;
                    Collision.HandleCollisionSimple(CollisionBox2D, tank.CollisionBox2D, ref dummyVel, ref position);

                    velocity.X = dummyVel.X;
                    velocity.Z = dummyVel.Y;
                }
            }
        }

        public override void Destroy()
        {
            Dead = true;
            var killSound1 = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_destroy");
            var killSound2 = GameResources.GetGameResource<SoundEffect>($"Assets/fanfares/tank_player_death");
            SoundPlayer.PlaySoundInstance(killSound1, SoundContext.Effect, 0.2f);
            SoundPlayer.PlaySoundInstance(killSound2, SoundContext.Effect, 0.2f);

            var dmark = new TankDeathMark(PlayerType == PlayerType.Blue ? TankDeathMark.CheckColor.Blue : TankDeathMark.CheckColor.Red)
            {
                location = position + new Vector3(0, 0.1f, 0)
            };

            GameHandler.AllPlayerTanks[PlayerId] = null;
            GameHandler.AllTanks[WorldId] = null;
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
            SoundPlayer.PlaySoundInstance(sound, SoundContext.Effect, 0.5f);
            OwnedMineCount++;
            var mine = new Mine(this, position, 600);
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
                treadPlaceTimer += MaxSpeed / 5;
                if (treadPlaceTimer > MaxSpeed)
                {
                    treadPlaceTimer = 0;
                    LayFootprint(false);
                }
                if (TankGame.GameUpdateTime % _treadSoundTimer == 0)
                {
                    var treadPlace = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_tread_place_{new Random().Next(1, 5)}");
                    var sfx = SoundPlayer.PlaySoundInstance(treadPlace, SoundContext.Effect, 0.2f);
                    sfx.Pitch = TreadPitch;
                }
            }
        }

        public override void LayFootprint(bool alt)
        {
            if (!CanLayTread)
                return;
            var fp = new TankFootprint(alt)
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
            if (!GameHandler.InMission || !HasTurret)
                return;
            if (curShootCooldown > 0 || OwnedShellCount >= ShellLimit)
                return;

            SoundEffectInstance sfx;

            sfx = ShellType switch
            {
                ShellTier.Standard => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_regular_1"), SoundContext.Effect, 0.3f),
                ShellTier.Rocket => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_rocket"), SoundContext.Effect, 0.3f),
                ShellTier.RicochetRocket => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_ricochet_rocket"), SoundContext.Effect, 0.3f),
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

            OwnedShellCount++;

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

            var info = new string[]
            {
                $"Team: {Team}",
                $"Preturbed: {preterbedVelocity}",
                $"Actual / Target: {TankRotation} / {preterbedVelocity.FlattenZ().ToRotation()}",
                $"AnyCubeTouch: {Cube.cubes.Any(c => c is not null && c.collider.Intersects(CollisionBox))}",
                $"AnyCubeTouch2D: {Cube.cubes.Any(c => c is not null && c.collider2d.Intersects(CollisionBox2D))}",
                $"OwnedShellCount: {OwnedShellCount}"
            };

            TankGame.spriteBatch.Draw(GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel"), CollisionBox2D, Color.White * 0.75f);

            for (int i = 0; i < info.Length; i++)
                DebugUtils.DrawDebugString(TankGame.spriteBatch, info[i], GeometryUtils.ConvertWorldToScreen(Vector3.Zero, World, View, Projection) - new Vector2(0, (info.Length * 20) + (i * 20)), 1, centerIt: true);

            if (Invisible && GameHandler.InMission)
                return;

            RenderModel();
        }

        public override string ToString()
            => $"pos: {position} | vel: {velocity} | dead: {Dead} | rotation: {TankRotation} | OwnedBullets: {OwnedShellCount}";
    }
}