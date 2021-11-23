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

namespace WiiPlayTanksRemake.GameContent
{
    public class AITank : Tank
    {
        public bool Stationary { get; private set; }

        private long _treadSoundTimer = 5;
        private int curShootStun;
        private int curShootCooldown;
        private int curMineCooldown;

        public int TierHierarchy => (int)tier;

        public AiBehavior[] Behaviors { get; private set; } = new AiBehavior[10]; // each of these should keep track of an action the tank performs

        public TankTier tier;

        private Texture2D _tankColorTexture, _shadowTexture;

        public Action enactBehavior;

        #region AiTankParams

        public float meanderAngle;
        public int meanderFrequency;
        public float redirectAngle;
        public float redirectDistance;
        public float mustPivotAngle;
        public float pursuitLevel;
        public int pursuitFrequency;

        public float turretMeanderAngle;
        public int turretMeanderFrequency;
        public float turretRotationSpeed;

        public float targetTurretRotation;
        public float targetTankRotation;

        public float inaccuracy;

        public float mineChance;

        public float projectileWarinessRadius;
        public float mineWarinessRadius;

        #endregion

        #region ModelBone & ModelMesh
        public Matrix[] boneTransforms;

        public ModelMesh CannonMesh;
        #endregion

        public static TankTier GetHighestTierActive()
        {
            var highest = TankTier.None;

            foreach (var tank in WPTR.AllAITanks.Where(tnk => !tnk.Dead))
            {
                if (tank.tier > highest)
                    highest = tank.tier;
            }
            return highest;
        }

        public static int GetTankCountOfType(TankTier tier)
            => WPTR.AllAITanks.Count(tnk => tnk.tier == tier);

        public AITank(Vector3 beginPos, TankTier tier = TankTier.None, bool setTankDefaults = true)
        {
            _treadSoundTimer += new Random().Next(-1, 2);
            for (int i = 0; i < Behaviors.Length; i++)
                Behaviors[i] = new();

            Behaviors[0].Label = "TankBaseMovement";
            Behaviors[1].Label = "TankBarrelMovement";
            Behaviors[2].Label = "TankEnvReader";
            Behaviors[3].Label = "TankBulletFire";
            Behaviors[4].Label = "TankMinePlacement";
            Behaviors[5].Label = "TankMineAvoidance";
            Behaviors[6].Label = "TankBulletAvoidance";

            position = beginPos;

            Model = TankGame.TankModel_Enemy;
            _tankColorTexture = GameResources.GetGameResource<Texture2D>($"Assets/textures/enemy/tank_{tier.ToString().ToLower()}");

            CannonMesh = Model.Meshes["polygon0.001"];

            boneTransforms = new Matrix[Model.Bones.Count];

            _shadowTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank_shadow");
            this.tier = tier;

            void setDefaults()
            {
                switch (tier)
                {
                    case TankTier.Brown:
                        Stationary = true;
                        MaxSpeed = 0;
                        break;

                    case TankTier.Ash:
                        TreadPitch = 0.125f;
                        MaxSpeed = 0.8f;
                        break;

                    case TankTier.Marine:
                        meanderAngle = 0.3f;
                        meanderFrequency = 15;
                        turretMeanderFrequency = 10;
                        turretRotationSpeed = 0.1f;
                        inaccuracy = 0.08f;

                        TurningSpeed = 0.2f;
                        MaximalTurn = MathHelper.TwoPi;

                        ShootStun = 20;
                        ShellCooldown = 180;
                        ShellLimit = 1;
                        ShellShootSpeed = 6f;
                        ShellType = ShellTier.Rocket;
                        RicochetCount = 0;

                        TreadPitch = 0.085f;
                        MaxSpeed = 1f;

                        MineCooldown = 0;
                        MineLimit = 0;
                        MineStun = 0;
                        break;

                    case TankTier.Yellow:
                        meanderAngle = MathHelper.Pi;
                        meanderFrequency = 90;
                        turretMeanderFrequency = 20;
                        turretRotationSpeed = 0.02f;
                        inaccuracy = 0.5f;

                        TurningSpeed = 0.08f;
                        MaximalTurn = 2f;

                        ShootStun = 20;
                        ShellCooldown = 90;
                        ShellLimit = 1;
                        ShellShootSpeed = 3f;
                        ShellType = ShellTier.Regular;
                        RicochetCount = 1;

                        TreadPitch = 0.085f;
                        MaxSpeed = 1.8f;

                        MineCooldown = 6;
                        MineLimit = 4;
                        MineStun = 1;
                        break;

                    case TankTier.Pink:
                        TreadPitch = 0.1f;
                        break;

                    case TankTier.Green:
                        ShellType = ShellTier.RicochetRocket;
                        Stationary = true;
                        MaxSpeed = 0;
                        break;

                    case TankTier.Purple:
                        ShootPitch = -0.2f;

                        meanderAngle = 0.8f;
                        meanderFrequency = 20;
                        turretMeanderFrequency = 15;

                        TreadPitch = -0.2f;
                        MaxSpeed = 2.5f;
                        break;

                    case TankTier.White:
                        MaxSpeed = 1.1f;
                        Invisible = true;
                        break;

                    case TankTier.Black:
                        TreadPitch = -0.26f;
                        ShellType = ShellTier.Rocket;
                        MaxSpeed = 5f;
                        break;
                }
            }
            if (setTankDefaults)
                setDefaults();

            if (Invisible)
            {
                var invis = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_invisible");
                var invisSfx = invis.CreateInstance();
                invisSfx.Play();
                invisSfx.Volume = 0.4f;
            }
            GetAIBehavior();
            WPTR.AllAITanks.Add(this);
        }

        internal void Update()
        {
            if (curShootStun > 0)
                curShootStun--;
            if (curShootCooldown > 0)
                curShootCooldown--;
            if (curMineCooldown > 0)
                curMineCooldown--;
            if (!Dead)
            {
                position.X = MathHelper.Clamp(position.X, -268, 268);
                position.Z = MathHelper.Clamp(position.Z, -155, 400);

                if (curShootStun > 0 || curMineCooldown > 0)
                    velocity = Vector3.Zero;

                /*if (!targetTankRotation.IsInRangeOf(TankRotation, maxTurnUntilPivot))
                {
                    TankRotation = GameUtils.RoughStep(TankRotation, targetTankRotation, pivotSpeed);
                }
                else */if (velocity != Vector3.Zero)
                {
                    //TankRotation = velocity.ToRotation();
                    TankRotation = Velocity2D.ToRotation();
                }
                Projection = TankGame.GameProjection;
                View = TankGame.GameView;

                World = Matrix.CreateFromYawPitchRoll(-TankRotation + MathHelper.PiOver2, 0, 0)
                    * Matrix.CreateTranslation(position);

                position += velocity * 0.55f;

                GetAIBehavior();

                UpdateCollision();

                oldPosition = position;

            }
            else
            {
                CollisionBox = new();
            }
        }

        private void UpdateCollision()
        {
            CollisionBox = new(position - new Vector3(7, 10, 7), position + new Vector3(10, 10, 10));
            if (WPTR.AllAITanks.Any(tnk => tnk.CollisionBox.Intersects(CollisionBox) && tnk != this))
            {
                position = oldPosition;
            }
            if (WPTR.AllPlayerTanks.Any(tnk => tnk.CollisionBox.Intersects(CollisionBox)))
            {
                position = oldPosition;
            }
        }

        public override void Destroy()
        {
            Dead = true;
            var killSound = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_destroy");
            var killSfx = killSound.CreateInstance();
            killSfx.Play();
            killSfx.Volume = 0.2f;

            // TODO: play fanfare thingy i think
        }
        /// <summary>
        /// Finish bullet implementation!
        /// </summary>
        /// <param name="velocity"></param>
        /// <param name="bulletSpeed"></param>
        public override void Shoot()
        {
            if (curShootCooldown > 0 || OwnedBulletCount >= ShellLimit)
                return;

            SoundEffect shootSound;

            shootSound = ShellType switch
            {
                ShellTier.Rocket => GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_rocket"),
                ShellTier.RicochetRocket => GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_ricochet_rocket"),
                ShellTier.Regular => GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_regular_2"),
                _ => throw new NotImplementedException()
            };

            var sfx = SoundPlayer.PlaySoundInstance(shootSound, SoundContext.Sound, 0.3f);

            sfx.Pitch = ShootPitch;

            var bullet = new Shell(position, Vector3.Zero);
            var new2d = Vector2.UnitY.RotatedByRadians(TurretRotation - MathHelper.PiOver2);

            var newPos = Position2D + new Vector2(0, 20).RotatedByRadians(-TurretRotation - MathHelper.PiOver2);

            bullet.position = new Vector3(newPos.X, 11, newPos.Y);

            bullet.velocity = new Vector3(new2d.X, 0, -new2d.Y) * ShellShootSpeed;

            bullet.owner = this;
            bullet.ricochets = RicochetCount;

            OwnedBulletCount++;

            curShootStun = ShootStun;
            curShootCooldown = ShellCooldown;
        }

        public void GetAIBehavior()
        {
            CannonMesh.ParentBone.Transform = Matrix.CreateRotationY(TurretRotation + TankRotation);

            Model.Root.Transform = World;

            Model.CopyAbsoluteBoneTransformsTo(boneTransforms);
            if (velocity != Vector3.Zero)
            {
                if (TankGame.GameUpdateTime % _treadSoundTimer == 0)
                {
                    var treadPlace = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_tread_place_{new Random().Next(1, 5)}");
                    var sfx = SoundPlayer.PlaySoundInstance(treadPlace, SoundContext.Sound, 0.05f);

                    // sfx.Volume = 0.05f;
                    sfx.Pitch = TreadPitch;
                }
            }

            foreach (var behavior in Behaviors)
                behavior.totalUpdateCount++;

            enactBehavior = () =>
            {
                if (!Dead)
                {
                    var player = WPTR.AllPlayerTanks.FirstOrDefault(tnk => !tnk.Dead);

                    if (Behaviors[0].IsBehaviourModuloOf(meanderFrequency))
                    {
                        var meanderRandom = new Random().NextFloat(-meanderAngle, meanderAngle);

                        TankRotation = meanderRandom;

                        var dir = Position2D.RotatedByRadians(TankRotation);

                        velocity.X = dir.X;
                        velocity.Z = dir.Y;

                        velocity.Normalize();

                        velocity *= MaxSpeed;
                    }
                    TurretRotation = GameUtils.RoughStep(TurretRotation, targetTurretRotation, turretRotationSpeed);
                    if (WPTR.AllPlayerTanks.IndexOf(player) > -1)
                    {
                        if (Behaviors[1].IsBehaviourModuloOf(turretMeanderFrequency))
                        {
                            var dirVec = Position2D - player.Position2D;
                            targetTurretRotation = (-dirVec.ToRotation() + MathHelper.Pi) + new Random().NextFloat(-inaccuracy, inaccuracy);
                        }
                        if (Vector3.Distance(position, player.position) < 300)
                        {
                            if (curShootCooldown == 0)
                            {
                                //var randSeed1 = new Random().Next(0, 500);

                                //if (randSeed1 == 0)
                                Shoot();
                            }
                        }
                    }
                    /*if (tier == TankTier.Marine)
                    {
                    }
                    if (tier == TankTier.Yellow)
                    {
                        if (Behaviors[0].IsBehaviourModuloOf(meanderFrequency))
                        {
                            var meanderRandom = new Random().NextFloat(-meanderAngle, meanderAngle);

                            TankRotation = meanderRandom;

                            var dir = Position2D.RotatedByRadians(TankRotation);

                            velocity.X = dir.X;
                            velocity.Z = dir.Y;

                            velocity.Normalize();
                        }

                        TurretRotation = GameUtils.RoughStep(TurretRotation, targetTurretRotation, turretRotationSpeed);
                        if (WPTR.AllPlayerTanks.IndexOf(player) > -1)
                        {
                            if (Behaviors[1].IsBehaviourModuloOf(turretMeanderFrequency))
                            {
                                var dirVec = Position2D - player.Position2D;
                                targetTurretRotation = (-dirVec.ToRotation() + MathHelper.Pi) + new Random().NextFloat(-inaccuracy, inaccuracy);
                            }
                            if (Vector3.Distance(position, player.position) < 300)
                            {
                                if (curShootCooldown == 0)
                                    Shoot();
                            }
                        }
                    }*/
                }
            };
            enactBehavior?.Invoke();
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

                    if (mesh.Name != "polygon1")
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
            => $"tier: {tier} | vel: {velocity} | pos: {position} | mFreq: {meanderFrequency} | OwnedBullets: {OwnedBulletCount}";

        public static bool TryGetBulletNear(PlayerTank tank, float distance, out Shell bullet)
        {
            foreach (var blet in Shell.AllShells)
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
        public static bool TryGetMineNear(PlayerTank tank, float distance, out Mine mine)
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
    }
}