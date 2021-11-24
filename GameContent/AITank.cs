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

        private int treadPlaceTimer = 5;
        private int treadSoundTimer = 5;
        private int curShootStun;
        private int curShootCooldown;
        private int curMineCooldown;

        public int TierHierarchy => (int)tier;

        public AiBehavior[] Behaviors { get; private set; } = new AiBehavior[10]; // each of these should keep track of an action the tank performs

        public TankTier tier;

        private Texture2D _tankColorTexture, _shadowTexture;

        public Action enactBehavior;

        public int AITankId { get; }
        public int WorldId { get; }

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

        public float projectileWarinessRadius;
        public float mineWarinessRadius;

        public float minePlacementChance; // 0.0f to 1.0f

        public int moveFromMineTime;
        public int timeSinceLastMinePlaced = 999999;

        #endregion

        #region ModelBone & ModelMesh
        public Matrix[] boneTransforms;

        public ModelMesh CannonMesh;
        #endregion

        public static TankTier GetHighestTierActive()
        {
            var highest = TankTier.None;

            foreach (var tank in WPTR.AllAITanks.Where(tnk => tnk is not null && !tnk.Dead))
            {
                if (tank.tier > highest)
                    highest = tank.tier;
            }
            return highest;
        }

        public static int CountAll()
            => WPTR.AllAITanks.Count(tnk => tnk is not null && !tnk.Dead);

        public static int GetTankCountOfType(TankTier tier)
            => WPTR.AllAITanks.Count(tnk => tnk is not null && tnk.tier == tier);

        public AITank(Vector3 beginPos, TankTier tier = TankTier.None, bool setTankDefaults = true)
        {
            treadSoundTimer += new Random().Next(-1, 2);
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

                        turretMeanderFrequency = 30;
                        turretRotationSpeed = 0.01f;
                        inaccuracy = 1.2f;

                        TurningSpeed = 0f;
                        MaximalTurn = 0;

                        ShootStun = 20;
                        ShellCooldown = 300;
                        ShellLimit = 1;
                        ShellSpeed = 3f;
                        ShellType = ShellTier.Regular;
                        RicochetCount = 1;

                        TreadPitch = 0;
                        MaxSpeed = 0f;

                        MineCooldown = 0;
                        MineLimit = 0;
                        MineStun = 0;

                        break;

                    case TankTier.Ash:
                        // fix lol
                        meanderAngle = 0f;
                        meanderFrequency = 1;
                        turretMeanderFrequency = 1;
                        turretRotationSpeed = 1f;
                        inaccuracy = 0;

                        TurningSpeed = 0;
                        MaximalTurn = 0;

                        ShootStun = 0;
                        ShellCooldown = 0;
                        ShellLimit = 0;
                        ShellSpeed = 6f;
                        ShellType = ShellTier.Rocket;
                        RicochetCount = 0;

                        TreadPitch = 0.085f;
                        MaxSpeed = 0;

                        MineCooldown = 0;
                        MineLimit = 0;
                        MineStun = 0;
                        break;

                    case TankTier.Marine:
                        meanderAngle = 0.4f;
                        meanderFrequency = 15;
                        turretMeanderFrequency = 10;
                        turretRotationSpeed = 0.1f;
                        inaccuracy = 0.04f;

                        projectileWarinessRadius = 120;
                        mineWarinessRadius = 0;

                        TurningSpeed = 0.2f;
                        MaximalTurn = MathHelper.TwoPi;

                        ShootStun = 20;
                        ShellCooldown = 180;
                        ShellLimit = 1;
                        ShellSpeed = 6f;
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

                        projectileWarinessRadius = 40;
                        mineWarinessRadius = 160;

                        TurningSpeed = 0.08f;
                        MaximalTurn = 2f;

                        ShootStun = 20;
                        ShellCooldown = 90;
                        ShellLimit = 1;
                        ShellSpeed = 3f;
                        ShellType = ShellTier.Regular;
                        RicochetCount = 1;

                        TreadPitch = 0.085f;
                        MaxSpeed = 1.8f;

                        MineCooldown = 600;
                        MineLimit = 4;
                        MineStun = 1;
                        minePlacementChance = 0.3f;
                        moveFromMineTime = 60;
                        break;

                    case TankTier.Pink:
                        meanderAngle = 0.3f;
                        meanderFrequency = 15;
                        turretMeanderFrequency = 40;
                        turretRotationSpeed = 0.03f;
                        inaccuracy = 0.2f;

                        projectileWarinessRadius = 40;
                        mineWarinessRadius = 160;

                        TurningSpeed = 0.08f;
                        MaximalTurn = MathHelper.TwoPi;

                        ShootStun = 5;
                        ShellCooldown = 30;
                        ShellLimit = 5;
                        ShellSpeed = 3f;
                        ShellType = ShellTier.Regular;
                        RicochetCount = 1;

                        TreadPitch = 0.1f;
                        MaxSpeed = 1.2f;

                        treadSoundTimer = 6;
                        treadPlaceTimer = 6;

                        MineCooldown = 0;
                        MineLimit = 0;
                        MineStun = 0;
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
                        meanderAngle = 0.9f;
                        meanderFrequency = 60;
                        turretMeanderFrequency = 20;
                        turretRotationSpeed = 0.03f;
                        inaccuracy = 0.2f;

                        projectileWarinessRadius = 40;
                        mineWarinessRadius = 160;

                        TurningSpeed = 0.08f;
                        MaximalTurn = MathHelper.PiOver4;

                        ShootStun = 5;
                        ShellCooldown = 30;
                        ShellLimit = 5;
                        ShellSpeed = 3f;
                        ShellType = ShellTier.Regular;
                        RicochetCount = 1;

                        TreadPitch = -0.18f;
                        MaxSpeed = 1.2f;
                        Acceleration = 0.3f;

                        treadSoundTimer = 6;
                        treadPlaceTimer = 2;

                        MineCooldown = 1000;
                        MineLimit = 2;
                        MineStun = 1;
                        moveFromMineTime = 40;
                        minePlacementChance = 0.1f;

                        Invisible = true;
                        break;

                    case TankTier.Black:
                        meanderAngle = MathHelper.Pi;
                        meanderFrequency = 45;
                        turretMeanderFrequency = 20;
                        turretRotationSpeed = 0.03f;
                        inaccuracy = 0.12f;

                        projectileWarinessRadius = 100;
                        mineWarinessRadius = 0;

                        TurningSpeed = 0.06f;
                        MaximalTurn = MathHelper.PiOver4;

                        ShootStun = 5;
                        ShellCooldown = 90;
                        ShellLimit = 3;
                        ShellSpeed = 6f;
                        ShellType = ShellTier.Rocket;
                        RicochetCount = 0;

                        TreadPitch = -0.26f;
                        MaxSpeed = 2.4f;
                        Acceleration = 0.3f;

                        treadSoundTimer = 4;
                        treadPlaceTimer = 4;

                        MineCooldown = 850;
                        MineLimit = 2;
                        MineStun = 1;
                        moveFromMineTime = 40;
                        minePlacementChance = 0.2f;
                        break;
                }
                Team = Team.Blue;
            }
            if (setTankDefaults)
                setDefaults();

            GetAIBehavior();

            int index = Array.IndexOf(WPTR.AllAITanks, WPTR.AllAITanks.First(tank => tank is null));

            AITankId = index;

            WPTR.AllAITanks[index] = this;

            int index2 = Array.IndexOf(WPTR.AllTanks, WPTR.AllTanks.First(tank => tank is null));

            WorldId = index2;

            WPTR.AllTanks[index2] = this;

            WPTR.OnMissionStart += OnMissionStart;

            //WPTR.AllAITanks.Add(this);
            //WPTR.AllTanks.Add(this);
        }

        private void OnMissionStart()
        {
            if (Invisible && !Dead)
            {
                var invis = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_invisible");
                SoundPlayer.PlaySoundInstance(invis, SoundContext.Sound, 0.3f);
            }
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
                position.X = MathHelper.Clamp(position.X, MapRenderer.TANKS_MIN_X, MapRenderer.TANKS_MAX_X);
                position.Z = MathHelper.Clamp(position.Z, MapRenderer.TANKS_MIN_Y, MapRenderer.TANKS_MAX_Y);

                if (curShootStun > 0 || curMineCooldown > 0)
                    velocity = Vector3.Zero;

                /*if (!targetTankRotation.IsInRangeOf(TankRotation, maxTurnUntilPivot))
                {
                    TankRotation = GameUtils.RoughStep(TankRotation, targetTankRotation, pivotSpeed);
                }
                else */
                if (velocity != Vector3.Zero)
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
            if (WPTR.AllAITanks.Any(tnk => tnk is not null && tnk.CollisionBox.Intersects(CollisionBox) && tnk != this))
            {
                position = oldPosition;
            }
            if (WPTR.AllPlayerTanks.Any(tnk => tnk is not null && tnk.CollisionBox.Intersects(CollisionBox)))
            {
                position = oldPosition;
            }
        }

        public override void LayMine()
        {
            if (curMineCooldown > 0 || OwnedMineCount >= MineLimit)
                return;

            curMineCooldown = MineCooldown;
            var sound = GameResources.GetGameResource<SoundEffect>("Assets/sounds/mine_place");
            SoundPlayer.PlaySoundInstance(sound, SoundContext.Sound, 0.5f);
            OwnedMineCount++;
            timeSinceLastMinePlaced = 0;
            var mine = new Mine(this, position, 600);
        }

        public override void Destroy()
        {
            Dead = true;
            var killSound1 = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_destroy");
            var killSound2 = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_destroy_enemy");

            SoundPlayer.PlaySoundInstance(killSound1, SoundContext.Sound, 0.2f);
            SoundPlayer.PlaySoundInstance(killSound2, SoundContext.Sound, 0.3f);

            var dmark = new TankDeathMark(TankDeathMark.CheckColor.White)
            {
                location = position + new Vector3(0, 0.1f, 0)
            };

            WPTR.AllAITanks[AITankId] = null;
            WPTR.AllTanks[WorldId] = null;
            // TODO: play fanfare thingy i think
        }

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
                ShellTier.Regular => GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_regular_2"),
                _ => throw new NotImplementedException()
            };

            var sfx = SoundPlayer.PlaySoundInstance(shootSound, SoundContext.Sound, 0.3f);

            sfx.Pitch = ShootPitch;

            var bullet = new Shell(position, Vector3.Zero);
            var new2d = Vector2.UnitY.RotatedByRadians(TurretRotation - MathHelper.PiOver2);

            var newPos = Position2D + new Vector2(0, 20).RotatedByRadians(-TurretRotation - MathHelper.PiOver2);

            bullet.position = new Vector3(newPos.X, 11, newPos.Y);

            bullet.velocity = new Vector3(new2d.X, 0, -new2d.Y) * ShellSpeed;

            bullet.owner = this;
            bullet.ricochets = RicochetCount;

            OwnedBulletCount++;

            curShootStun = ShootStun;
            curShootCooldown = ShellCooldown;
        }

        public override void LayFootprint()
        {
            var fp = new TankFootprint()
            {
                location = position + new Vector3(0, 0.1f, 0),
                rotation = -TankRotation + MathHelper.PiOver2
            };
        }

        public string uhoh;

        private float randomMovementAngle;
        public void GetAIBehavior()
        {
            timeSinceLastMinePlaced++;

            CannonMesh.ParentBone.Transform = Matrix.CreateRotationY(TurretRotation + TankRotation);

            Model.Root.Transform = World;

            Model.CopyAbsoluteBoneTransformsTo(boneTransforms);

            if (!WPTR.InMission)
                return;

            if (velocity != Vector3.Zero && !Stationary)
            {
                if (TankGame.GameUpdateTime % treadSoundTimer == 0)
                {
                    var treadPlace = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_tread_place_{new Random().Next(1, 5)}");
                    var sfx = SoundPlayer.PlaySoundInstance(treadPlace, SoundContext.Sound, 0.05f);
                    sfx.Pitch = TreadPitch;
                }
                if (TankGame.GameUpdateTime % treadPlaceTimer == 0)
                {
                    LayFootprint();
                }
            }

            foreach (var behavior in Behaviors)
                behavior.totalUpdateCount++;

            enactBehavior = () =>
            {
                // TODO: Work on why mines cause the tanks to freeze
                // TODO: MAJOR - Work on meandering ai and avoidance ai.
                if (!Dead)
                {
                    var enemy = WPTR.AllTanks.FirstOrDefault(tnk => tnk is not null && !tnk.Dead && tnk.Team != Team);

                    /*foreach (var tank in WPTR.AllTanks.Where(tnk => tnk is not null && !tnk.Dead))
                        if (Vector3.Distance(tank.position, position) < Vector3.Distance(enemy.position, position))
                            enemy = tank;*/

                    if (!Stationary)
                    {
                        /*if (Behaviors[2].IsBehaviourModuloOf(4))
                        {

                            if (tier == TankTier.Marine || tier == TankTier.Black)
                            {
                                var meanderRand = new Random().NextFloat(-meanderAngle, meanderAngle);
                                TankRotation = meanderRand;

                                AccountForWalls(50, meanderAngle * 10, ref meanderRand, out var isNearWall);
                            }
                        }*/

                        if (Behaviors[6].IsBehaviourModuloOf(5))
                        {
                            if (TryGetBulletNear(projectileWarinessRadius, out var shell))
                            {
                                var dir = new Vector2(0, 5).RotatedByRadians(shell.Position2D.DirectionOf(Position2D).ToRotation());

                                velocity.X = dir.X;
                                velocity.Z = -dir.Y;

                                velocity.Normalize();

                                velocity *= MaxSpeed;
                            }
                        }
                        if (Behaviors[5].IsBehaviourModuloOf(5))
                        {
                            uhoh = "Mine!";
                            if (TryGetMineNear(mineWarinessRadius, out var mine))
                            {
                                var dir = new Vector2(0, -5).RotatedByRadians(mine.Position2D.DirectionOf(Position2D).ToRotation());

                                velocity.X = dir.X;
                                velocity.Z = dir.Y;

                                velocity.Normalize();

                                velocity *= MaxSpeed;
                            }
                        }
                        if (MineLimit > 0)
                        {
                            if (Behaviors[4].IsBehaviourModuloOf(60))
                            {
                                if (new Random().NextFloat(0, 1) <= minePlacementChance)
                                {
                                    randomMovementAngle = new Random().NextFloat(-meanderAngle, meanderAngle);
                                    LayMine();
                                }
                            }
                        }

                        bool movingFromMine = timeSinceLastMinePlaced < moveFromMineTime;

                        if (movingFromMine)
                        {
                            TankRotation = randomMovementAngle;

                            var dir = Position2D.RotatedByRadians(TankRotation);

                            velocity.X = dir.X;
                            velocity.Z = dir.Y;

                            velocity.Normalize();

                            velocity *= MaxSpeed;
                        }

                        if (!movingFromMine)
                        {
                            if (!TryGetBulletNear(projectileWarinessRadius, out var sh))
                            {
                                uhoh = "Wegud";
                                if (Behaviors[0].IsBehaviourModuloOf(meanderFrequency / 2))
                                {
                                    var meanderRandom = new Random().NextFloat(-meanderAngle / 10, meanderAngle / 10);

                                    TankRotation = meanderRandom;

                                    var dir = Position2D.RotatedByRadians(TankRotation);

                                    velocity.X = dir.X;
                                    velocity.Z = dir.Y;

                                    velocity.Normalize();

                                    velocity *= MaxSpeed;
                                }
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
                            }
                            else
                            {
                                uhoh = "Uh Oh";
                            }
                        }
                    }
                    TurretRotation = GameUtils.RoughStep(TurretRotation, targetTurretRotation, turretRotationSpeed);
                    if (Array.IndexOf(WPTR.AllTanks, enemy) > -1 && enemy is not null)
                    {
                        if (Behaviors[1].IsBehaviourModuloOf(turretMeanderFrequency))
                        {
                            var dirVec = Position2D - enemy.Position2D;
                            targetTurretRotation = (-dirVec.ToRotation() + MathHelper.Pi) + new Random().NextFloat(-inaccuracy, inaccuracy);
                            // targetTurretRotation = dirVec.ToRotation() + new Random().NextFloat(-inaccuracy, inaccuracy);
                        }
                        if (Vector3.Distance(position, enemy.position) < 1500)
                        {
                            if (curShootCooldown == 0)
                            {
                                Shoot();
                            }
                        }
                    }

                    /*if (tier == TankTier.Ash)
                    {
                        var dirVec = Position2D - player.Position2D;
                         targetTurretRotation = (-dirVec.ToRotation() + MathHelper.Pi) + new Random().NextFloat(-inaccuracy, inaccuracy);
                        //targetTurretRotation = dirVec.ToRotation() + new Random().NextFloat(-inaccuracy, inaccuracy);

                        System.Diagnostics.Debug.WriteLine($"TurRot: {targetTurretRotation}");
                    }*/
                }
            };
            enactBehavior?.Invoke();
        }

        public void MoveTo(Vector3 position)
        {
            var dirTo = this.position.DirectionOf(position);

            velocity += dirTo;

            velocity.Normalize();

            velocity *= MaxSpeed;
        }

        private void AccountForWalls(int deviation, float rotationFactor, ref float rotationAffector, out bool isAccounting)
        {
            var seed = new Random();

            var radsRolled = seed.NextFloat(-rotationAffector, rotationFactor);

            isAccounting = false;
            if (position.X < MapRenderer.TANKS_MIN_X + deviation)
            {
                isAccounting = true;

                rotationAffector += radsRolled;

            }
            if (position.X > MapRenderer.TANKS_MAX_X - deviation)
            {
                isAccounting = true;

                rotationAffector += radsRolled;
            }
            if (position.Y < MapRenderer.TANKS_MIN_Y + deviation)
            {
                isAccounting = true;

                rotationAffector += radsRolled;
            }
            if (position.Y < MapRenderer.TANKS_MAX_Y - deviation)
            {
                isAccounting = true;

                rotationAffector += radsRolled;
            }
        }

        private void AccountForWalls_Experimental(int deviation, float rotationFactor, ref float rotationAffector, out bool isAccounting)
        {
            var seed = new Random();

            var radsRolled = seed.NextFloat(-rotationAffector, rotationFactor);

            isAccounting = false;
            if (position.X < MapRenderer.TANKS_MIN_X + deviation)
            {
                isAccounting = true;

                rotationAffector = radsRolled;
            }
            if (position.X > MapRenderer.TANKS_MAX_X - deviation)
            {
                isAccounting = true;

                rotationAffector += radsRolled;
            }
            if (position.Y < MapRenderer.TANKS_MIN_Y + deviation)
            {
                isAccounting = true;

                rotationAffector += radsRolled;
            }
            if (position.Y < MapRenderer.TANKS_MAX_Y - deviation)
            {
                isAccounting = true;

                rotationAffector += radsRolled;
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

            var info = $"{Team}:{uhoh}\n{timeSinceLastMinePlaced}/{moveFromMineTime} - rand: {randomMovementAngle}";

            DebugUtils.DrawDebugString(TankGame.spriteBatch, info, GeometryUtils.ConvertWorldToScreen(Vector3.Zero, World, View, Projection), 1, centerIt: true);

            if (Invisible && WPTR.InMission)
                return;
            RenderModel();
        }

        public override string ToString()
            => $"tier: {tier} | team: {Team} | vel: {velocity} | pos: {position} | mFreq: {meanderFrequency} | OwnedBullets: {OwnedBulletCount}";

        public bool TryGetBulletNear(float distance, out Shell bullet)
        {
            bullet = null;
            foreach (var blet in Shell.AllShells.Where(shel => shel is not null))
            {
                if (Vector3.Distance(position, blet.position) < distance)
                {
                    bullet = blet;
                    return true;
                }
            }
            return false;
        }
        public bool TryGetMineNear(float distance, out Mine mine)
        {
            mine = null;
            foreach (var yours in Mine.AllMines.Where(mine => mine is not null))
            {
                if (Vector3.Distance(position, yours.position) < distance)
                {
                    mine = yours;
                    return true;
                }
            }
            return false;
        }

        public static TankTier PICK_ANY_THAT_ARE_IMPLEMENTED()
        {
            TankTier[] workingTiers = { TankTier.Brown, TankTier.Marine, TankTier.Yellow, TankTier.Black, TankTier.White, TankTier.Pink };

            return workingTiers[new Random().Next(0, workingTiers.Length)];
        }
    }
}