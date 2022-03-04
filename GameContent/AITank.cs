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
using WiiPlayTanksRemake.GameContent.Systems;
using WiiPlayTanksRemake.Internals.Common.Framework.Audio;
using WiiPlayTanksRemake.Graphics;

namespace WiiPlayTanksRemake.GameContent
{
    public class AITank : Tank
    {
        private int treadSoundTimer = 5;
        private int curShootStun;
        private int curShootCooldown;
        private int curMineCooldown;
        private int curMineStun;
        public int TierHierarchy => (int)tier;

        public AiBehavior[] Behaviors { get; private set; } = new AiBehavior[10]; // each of these should keep track of an action the tank performs

        public TankTier tier;

        /// <summary>Should always be -1 if this tank is not modded content.</summary>
        public int modTier = -1;

        private Texture2D _tankColorTexture, _shadowTexture;

        public Action enactBehavior;

        public int AITankId { get; }
        public int WorldId { get; }

        #region AiTankParams

        public record Params
        {
            /// <summary>The max angle of which this tank will "meander," or change its movement direction.</summary>
            public float MeanderAngle { get; set; }
            /// <summary>How often this tank will take a turn at <see cref="MeanderAngle"/> radians.</summary>
            public int MeanderFrequency { get; set; }

            /// <summary>Not implemented (yet). Determines how much this tank will move un attempt to get closer to its target.</summary>
            public float PursuitLevel { get; set; }
            /// <summary>Not implemented (yet). Determines how often this tank will try to move towards its target.</summary>
            public int PursuitFrequency { get; set; }

            /// <summary>How often this tank will move its turret in the target's direction. It will be inaccurate at the measure of <see cref="Inaccuracy"/>.</summary>
            public int TurretMeanderFrequency { get; set; }
            /// <summary>How fast this tank's turret rotates towards its target.</summary>
            public float TurretSpeed { get; set; }
            /// <summary>How inaccurate (in radians) this tank is trying to aim at its target.</summary>
            public float Inaccuracy { get; set; }

            /// <summary>The distance of which this tank is wary of projectiles and tries to move away from them.</summary>
            public float ProjectileWarinessRadius { get; set; }
            /// <summary>The distance of which this tank is wary of mines and tries to move away from them.</summary>
            public float MineWarinessRadius { get; set; }

            /// <summary>On a given tick, it has this chance out of 1 to lay a mine. <para>Do note that this value must be greater than 0 and less than or equal to 1.</para></summary>
            public float MinePlacementChance { get; set; } // 0.0f to 1.0f

            /// <summary>How long (in ticks) this tank moves away from a mine that it places.</summary>
            public int MoveFromMineTime { get; set; }

            /// <summary>The distance from the main shot calculation ray an enemy must be before this tank is allowed to fire.</summary>
            public int MissDistance { get; set; }

            /// <summary>How often this tank shoots when given the opportunity. 0 to 1 values only. Defaults to 1.</summary>
            public float ShootChance { get; set; } = 1f;

            /// <summary>How far ahead of this tank (in the direction the tank is going) that it is aware of obstacles and navigates around them.</summary>
            public int BlockWarinessDistance { get; set; } = 60;
            /// <summary>How often this tank reads the obstacles around it and navigates around them.</summary>
            public int BlockReadTime { get; set; } = 30;
            /// <summary>How far this tank must be from a teammate before it can lay a mine or fire a bullet.</summary>
            public float TeammateTankWariness { get; set; } = 50f;
            /// <summary>Whether or not this tank tries to find calculations all around it. This is not recommended for mobile tanks.</summary>
            public bool SmartRicochets { get; set; }
            /// <summary>Whether or not this tank attempts to lay mines near destructible obstacles rather than randomly. Useless for stationary tanks.</summary>
            public bool SmartMineLaying { get; set; }
        }
        /// <summary>The AI parameter collection of this AI Tank.</summary>
        public Params AiParams { get; } = new();

        internal int TimeSinceLastMinePlaced { get; set; } = 999999;
        internal int TimeSinceLastMineFound { get; set; } = 999999;

        public Vector3 aimTarget;

        /// <summary>Whether or not this tank sees its target. Generally should not be set, but the tank will shoot if able when this is true.</summary>
        public bool SeesTarget { get; set; }

        public float targetTankRotation;

        /// <summary>The target rotation for this tank's turret. <see cref="Tank.TurretRotation"/> will move towards this value at a rate of <see cref="TurretSpeed"/>.</summary>
        public float targetTurretRotation;

        #endregion

        #region TankWatchingParams

        public Ray tankTurretRay;

        #endregion

        public Vector3 oldPosition;

        #region ModelBone & ModelMesh
        public Matrix[] boneTransforms;

        public ModelMesh CannonMesh;
        #endregion

        public static TankTier GetHighestTierActive()
        {
            var highest = TankTier.None;

            foreach (var tank in GameHandler.AllAITanks)
            {
                if (tank is not null && !tank.Dead)
                    if (tank.tier > highest)
                        highest = tank.tier;
            }
            return highest;
        }

        public static int CountAll()
            => GameHandler.AllAITanks.Count(tnk => tnk is not null && !tnk.Dead);

        public static int GetTankCountOfType(TankTier tier)
            => GameHandler.AllAITanks.Count(tnk => tnk is not null && tnk.tier == tier && !tnk.Dead);

        private bool isIngame;

        /// <summary>
        /// Creates a new <see cref="AITank"/>.
        /// </summary>
        /// <param name="tier">The tier of this <see cref="AITank"/>.</param>
        /// <param name="setTankDefaults">Whether or not to give this <see cref="AITank"/> the default values.</param>
        public AITank(TankTier tier, bool setTankDefaults = true, bool isIngame = true)
        {
            treadSoundTimer += new Random().Next(-1, 2);
            for (int i = 0; i < Behaviors.Length; i++)
                Behaviors[i] = new();

            this.isIngame = isIngame;

            Behaviors[0].Label = "TankBaseMovement";
            Behaviors[1].Label = "TankBarrelMovement";
            Behaviors[2].Label = "TankEnvReader";
            Behaviors[3].Label = "TankBulletFire";
            Behaviors[4].Label = "TankMinePlacement";
            Behaviors[5].Label = "TankMineAvoidance";
            Behaviors[6].Label = "TankBulletAvoidance";

            Dead = true;

            #region Non-Special
            if ((int)tier <= (int)TankTier.Black)
                _tankColorTexture = GameResources.GetGameResource<Texture2D>($"Assets/textures/enemy/tank_{tier.ToString().ToLower()}");
            else if ((int)tier > (int)TankTier.Black && (int)tier <= (int)TankTier.Obsidian)
                _tankColorTexture = GameResources.GetGameResource<Texture2D>($"Assets/textures/enemy/master/tank_{tier.ToString().ToLower()}");
            else if ((int)tier > (int)TankTier.Obsidian && (int)tier <= (int)TankTier.Marble)
                _tankColorTexture = GameResources.GetGameResource<Texture2D>($"Assets/textures/enemy/fate/tank_{tier.ToString().ToLower()}");
            #endregion

            #region Special

            if (tier == TankTier.Commando)
            {
                Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_commando");

                _tankColorTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/enemy/wee/tank_commando");

                foreach (var mesh in Model.Meshes)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        if (mesh.Name == "Laser_Beam")
                            effect.Texture = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/laser");
                        if (mesh.Name == "Barrel_Laser")
                            effect.Texture = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/armor");
                        if (mesh.Name == "Dish")
                            effect.Texture = GameResources.GetGameResource<Texture2D>("Assets/textures/enemy/wee/tank_commando");
                    }
                }
                // fix?
            }
            else if (tier == TankTier.Assassin)
            {
                Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_assassin");

                _tankColorTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/enemy/wee/tank_assassin");
            }
            else if (tier == TankTier.RocketDefender)
            {
                Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_rocket");

                _tankColorTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/enemy/wee/tank_rocket");
            }
            else if (tier == TankTier.Electro)
            {
                Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_electro");

                _tankColorTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/enemy/wee/tank_electro");
            }
            else if (tier == TankTier.Explosive)
            {
                Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_explosive");

                _tankColorTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/enemy/wee/tank_explosive");
            }
            else
            {
                Model = GameResources.GetRawGameAsset<Model>("Assets/tank_e");
            }

            #endregion

            CannonMesh = Model.Meshes["Cannon"];

            boneTransforms = new Matrix[Model.Bones.Count];

            _shadowTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank_shadow");
             this.tier = tier;

            if (setTankDefaults)
                ApplyDefaults();

            Team = Team.Blue;

            int index = Array.IndexOf(GameHandler.AllAITanks, GameHandler.AllAITanks.First(tank => tank is null));

            AITankId = index;

            GameHandler.AllAITanks[index] = this;

            int index2 = Array.IndexOf(GameHandler.AllTanks, GameHandler.AllTanks.First(tank => tank is null));

            WorldId = index2;

            GameHandler.AllTanks[index2] = this;

            GameHandler.OnMissionStart += OnMissionStart;
        }

        /// <summary>
        /// Applies this <see cref="AITank"/>'s defaults.
        /// </summary>
        public override void ApplyDefaults()
        {
            switch (tier)
            {
                #region VanillaTanks
                case TankTier.Brown:
                    Stationary = true;

                    AiParams.TurretMeanderFrequency = 30;
                    AiParams.TurretSpeed = 0.01f;
                    AiParams.Inaccuracy = MathHelper.ToRadians(170);

                    TurningSpeed = 0f;
                    MaximalTurn = 0;

                    ShootStun = 20;
                    ShellCooldown = 300;
                    ShellLimit = 1;
                    ShellSpeed = 3f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Invisible = false;
                    ShellHoming = new();

                    TreadPitch = 0;
                    MaxSpeed = 0f;

                    MineCooldown = 0;
                    MineLimit = 0;
                    MineStun = 0;

                    AiParams.MissDistance = 0;

                    TankDestructionColor = Color.Brown;

                    break;

                case TankTier.Ash:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 15;
                    AiParams.TurretMeanderFrequency = 40;
                    AiParams.TurretSpeed = 0.01f;
                    AiParams.Inaccuracy = 0.24f;

                    AiParams.ProjectileWarinessRadius = 40;
                    AiParams.MineWarinessRadius = 40;

                    TurningSpeed = 0.08f;
                    MaximalTurn = 0.7f;

                    ShootStun = 3;
                    ShellCooldown = 180;
                    ShellLimit = 1;
                    ShellSpeed = 3f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = 0.085f;
                    MaxSpeed = 1.2f;

                    MineCooldown = 0;
                    MineLimit = 0;
                    MineStun = 0;
                    break;

                case TankTier.Marine:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 5;
                    AiParams.TurretMeanderFrequency = 10;
                    AiParams.TurretSpeed = 0.1f;
                    AiParams.Inaccuracy = 0.005f;

                    AiParams.ProjectileWarinessRadius = 40;
                    AiParams.MineWarinessRadius = 80;

                    TurningSpeed = 0.2f;
                    MaximalTurn = MathHelper.PiOver2;

                    ShootStun = 20;
                    ShellCooldown = 180;
                    ShellLimit = 1;
                    ShellSpeed = 6f;
                    ShellType = ShellTier.Rocket;
                    RicochetCount = 0;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = 0.085f;
                    MaxSpeed = 1f;

                    MineCooldown = 0;
                    MineLimit = 0;
                    MineStun = 0;

                    AiParams.BlockWarinessDistance = 40;

                    break;

                case TankTier.Yellow:
                    AiParams.MeanderAngle = MathHelper.Pi;
                    AiParams.MeanderFrequency = 30;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.02f;
                    AiParams.Inaccuracy = 0.5f;

                    AiParams.ProjectileWarinessRadius = 40;
                    AiParams.MineWarinessRadius = 160;

                    TurningSpeed = 0.08f;
                    MaximalTurn = MathHelper.PiOver2;

                    ShootStun = 20;
                    ShellCooldown = 90;
                    ShellLimit = 1;
                    ShellSpeed = 3f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = 0.085f;
                    MaxSpeed = 1.8f;

                    MineCooldown = 600;
                    MineLimit = 4;
                    MineStun = 5;

                    treadSoundTimer = 4;

                    AiParams.MinePlacementChance = 0.3f;
                    AiParams.MoveFromMineTime = 120;
                    break;

                case TankTier.Pink:
                    AiParams.MeanderAngle = 0.3f;
                    AiParams.MeanderFrequency = 15;
                    AiParams.TurretMeanderFrequency = 40;
                    AiParams.TurretSpeed = 0.03f;
                    AiParams.Inaccuracy = 0.2f;

                    AiParams.ProjectileWarinessRadius = 40;
                    AiParams.MineWarinessRadius = 160;

                    TurningSpeed = 0.08f;
                    MaximalTurn = MathHelper.PiOver4;

                    ShootStun = 5;
                    ShellCooldown = 30;
                    ShellLimit = 5;
                    ShellSpeed = 3f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = 0.1f;
                    MaxSpeed = 1.2f;

                    treadSoundTimer = 6;

                    MineCooldown = 0;
                    MineLimit = 0;
                    MineStun = 0;
                    break;

                case TankTier.Green:
                    Stationary = true;

                    AiParams.TurretMeanderFrequency = 30;
                    AiParams.TurretSpeed = 0.02f;
                    AiParams.Inaccuracy = 0.4f;

                    TurningSpeed = 0f;
                    MaximalTurn = 0;

                    ShootStun = 5;
                    ShellCooldown = 60;
                    ShellLimit = 2;
                    ShellSpeed = 6f;
                    ShellType = ShellTier.RicochetRocket;
                    RicochetCount = 2; //2;

                    Invisible = false;
                    ShellHoming = new();

                    TreadPitch = 0;
                    MaxSpeed = 0f;

                    MineCooldown = 0;
                    MineLimit = 0;
                    MineStun = 0;

                    AiParams.SmartRicochets = true;
                    break;

                case TankTier.Purple:
                    AiParams.MeanderAngle = 1f;
                    AiParams.MeanderFrequency = 20;
                    AiParams.TurretMeanderFrequency = 25;

                    AiParams.TurretSpeed = 0.03f;
                    AiParams.Inaccuracy = 0.18f;

                    AiParams.ProjectileWarinessRadius = 60;
                    AiParams.MineWarinessRadius = 160;

                    TurningSpeed = 0.06f;
                    MaximalTurn = MathHelper.PiOver2;

                    ShootStun = 5;
                    ShellCooldown = 30;
                    ShellLimit = 5;
                    ShellSpeed = 3f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = -0.2f;
                    MaxSpeed = 1.8f;
                    Acceleration = 0.3f;

                    treadSoundTimer = 4;

                    MineCooldown = 700;
                    MineLimit = 2;
                    MineStun = 10;

                    AiParams.MoveFromMineTime = 60;
                    AiParams.MinePlacementChance = 0.05f;
                    break;

                case TankTier.White:
                    AiParams.MeanderAngle = 0.9f;
                    AiParams.MeanderFrequency = 60;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.03f;
                    AiParams.Inaccuracy = 0.2f;

                    AiParams.ProjectileWarinessRadius = 40;
                    AiParams.MineWarinessRadius = 160;

                    TurningSpeed = 0.08f;
                    MaximalTurn = MathHelper.PiOver4;

                    ShootStun = 5;
                    ShellCooldown = 30;
                    ShellLimit = 5;
                    ShellSpeed = 3f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = -0.35f;
                    MaxSpeed = 1.2f;
                    Acceleration = 0.3f;

                    treadSoundTimer = 6;

                    MineCooldown = 1000;
                    MineLimit = 2;
                    MineStun = 8;

                    AiParams.MoveFromMineTime = 40;
                    AiParams.MinePlacementChance = 0.08f;

                    Invisible = true;
                    break;

                case TankTier.Black:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 45;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.03f;
                    AiParams.Inaccuracy = 0.12f;

                    AiParams.ProjectileWarinessRadius = 100;
                    AiParams.MineWarinessRadius = 60;

                    TurningSpeed = 0.06f;
                    MaximalTurn = MathHelper.ToRadians(5);

                    ShootStun = 5;
                    ShellCooldown = 90;
                    ShellLimit = 3;
                    ShellSpeed = 6f;
                    ShellType = ShellTier.Rocket;
                    RicochetCount = 0;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = -0.26f;
                    MaxSpeed = 2.4f;
                    Acceleration = 0.3f;

                    treadSoundTimer = 4;

                    MineCooldown = 850;
                    MineLimit = 2;
                    MineStun = 10;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.05f;

                    AiParams.SmartMineLaying = true;
                    break;
                #endregion
                #region MasterMod
                case TankTier.Bronze:
                    AiParams.TurretMeanderFrequency = 15;
                    AiParams.TurretSpeed = 0.05f;
                    AiParams.Inaccuracy = 0.005f;

                    ShellCooldown = 50;
                    ShellLimit = 2;
                    ShellSpeed = 3f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = true;
                    ShellHoming = new();

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.05f;

                    AiParams.SmartRicochets = true;
                    break;
                case TankTier.Silver:
                    AiParams.MeanderAngle = 0.5f;
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 60;
                    AiParams.TurretSpeed = 0.045f;
                    AiParams.Inaccuracy = 0.9f;

                    AiParams.ProjectileWarinessRadius = 70;
                    AiParams.MineWarinessRadius = 140;

                    TurningSpeed = 0.13f;
                    MaximalTurn = MathHelper.PiOver2;

                    ShootStun = 0;
                    ShellCooldown = 15;
                    ShellLimit = 8;
                    ShellSpeed = 4f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = 0.2f;
                    MaxSpeed = 1.6f;
                    Acceleration = 0.3f;
                    Deceleration = 0.6f;

                    treadSoundTimer = 4;

                    MineCooldown = 60 * 20;
                    MineLimit = 1;
                    MineStun = 10;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.05f;
                    break;
                case TankTier.Sapphire:
                    AiParams.MeanderAngle = 0.25f;
                    AiParams.MeanderFrequency = 15;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.025f;
                    AiParams.Inaccuracy = 0.01f;

                    AiParams.ProjectileWarinessRadius = 40;
                    AiParams.MineWarinessRadius = 70;

                    TurningSpeed = 0.15f;
                    MaximalTurn = MathHelper.PiOver2;

                    ShootStun = 20;
                    ShellCooldown = 10;
                    ShellLimit = 3;
                    ShellSpeed = 5.5f;
                    ShellType = ShellTier.Rocket;
                    RicochetCount = 0;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = 0.08f;
                    MaxSpeed = 1.4f;
                    Acceleration = 0.3f;
                    Deceleration = 0.6f;

                    treadSoundTimer = 4;

                    MineCooldown = 1000;
                    MineLimit = 1;
                    MineStun = 0;

                    AiParams.MoveFromMineTime = 90;
                    AiParams.MinePlacementChance = 0.05f;
                    break;
                case TankTier.Ruby:
                    AiParams.MeanderAngle = 0.5f;
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.025f;
                    AiParams.Inaccuracy = 0.05f;

                    //AiParams.PursuitLevel = 0.1f;
                    //AiParams.PursuitFrequency = 30;

                    AiParams.ProjectileWarinessRadius = 50;
                    AiParams.MineWarinessRadius = 0;

                    TurningSpeed = 0.5f;
                    MaximalTurn = MathHelper.PiOver2;

                    ShootStun = 0;
                    ShellCooldown = 8;
                    ShellLimit = 10;
                    ShellSpeed = 3f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 0;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = 0.08f;
                    MaxSpeed = 1.2f;
                    Acceleration = 0.4f;
                    Deceleration = 0.6f;

                    treadSoundTimer = 4;

                    MineCooldown = 0;
                    MineLimit = 0;
                    MineStun = 0;

                    AiParams.MoveFromMineTime = 0;
                    AiParams.MinePlacementChance = 0;

                    AiParams.BlockWarinessDistance = 30;
                    break;
                case TankTier.Citrine:
                    AiParams.MeanderAngle = 0.7f;
                    AiParams.MeanderFrequency = 30;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.035f;
                    AiParams.Inaccuracy = 0.3f;

                    AiParams.ProjectileWarinessRadius = 80;
                    AiParams.MineWarinessRadius = 140;

                    TurningSpeed = 0.08f;
                    MaximalTurn = 1.4f;

                    ShootStun = 10;
                    ShellCooldown = 60;
                    ShellLimit = 3;
                    ShellSpeed = 6f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 0;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = -0.08f;
                    MaxSpeed = 3.2f;
                    Acceleration = 0.2f;
                    Deceleration = 0.4f;

                    treadSoundTimer = 4;

                    MineCooldown = 360;
                    MineLimit = 4;
                    MineStun = 5;

                    AiParams.MoveFromMineTime = 40;
                    AiParams.MinePlacementChance = 0.15f;

                    AiParams.ShootChance = 0.95f;
                    break;
                case TankTier.Amethyst:
                    AiParams.MeanderAngle = 0.3f;
                    AiParams.MeanderFrequency = 5;
                    AiParams.TurretMeanderFrequency = 15;
                    AiParams.TurretSpeed = 0.05f;
                    AiParams.Inaccuracy = 0.3f;

                    AiParams.ProjectileWarinessRadius = 70;
                    AiParams.MineWarinessRadius = 140;

                    TurningSpeed = 0.1f;
                    MaximalTurn = MathHelper.PiOver2 + 0.5f;

                    ShootStun = 5;
                    ShellCooldown = 25;
                    ShellLimit = 5;
                    ShellSpeed = 3f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = -0.2f;
                    MaxSpeed = 2f;
                    Acceleration = 0.6f;
                    Deceleration = 0.9f;

                    treadSoundTimer = 4;

                    MineCooldown = 360;
                    MineLimit = 3;
                    MineStun = 10;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.05f;
                    break;
                case TankTier.Emerald:
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.04f;
                    AiParams.Inaccuracy = 1f;

                    ShellCooldown = 60;
                    ShellLimit = 3;
                    ShellSpeed = 8f;
                    ShellType = ShellTier.RicochetRocket;
                    RicochetCount = 2;

                    Stationary = true;
                    Invisible = true;
                    ShellHoming = new();

                    AiParams.SmartRicochets = true;
                    break;

                case TankTier.Gold:
                    AiParams.MeanderAngle = 1.8f;
                    AiParams.MeanderFrequency = 20;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.02f;
                    AiParams.Inaccuracy = 0.14f;

                    AiParams.ShootChance = 0.7f;

                    AiParams.ProjectileWarinessRadius = 80;
                    AiParams.MineWarinessRadius = 120;

                    CanLayTread = false;

                    TurningSpeed = 0.06f;
                    MaximalTurn = 1.4f;

                    ShootStun = 5;
                    ShellCooldown = 30;
                    ShellLimit = 3;
                    ShellSpeed = 4f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = -0.1f;
                    MaxSpeed = 0.9f;
                    Acceleration = 0.8f;
                    Deceleration = 0.5f;

                    treadSoundTimer = 5;

                    MineCooldown = 700;
                    MineLimit = 2;
                    MineStun = 10;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.01f;

                    Invisible = true;
                    break;

                case TankTier.Obsidian:
                    AiParams.MeanderAngle = 1.2f;
                    AiParams.MeanderFrequency = 20;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.05f;
                    AiParams.Inaccuracy = 0.18f;

                    AiParams.ProjectileWarinessRadius = 70;
                    AiParams.MineWarinessRadius = 140;

                    TurningSpeed = 0.1f;
                    MaximalTurn = MathHelper.PiOver4;

                    ShootStun = 5;
                    ShellCooldown = 25;
                    ShellLimit = 3;
                    ShellSpeed = 6f;
                    ShellType = ShellTier.Rocket;
                    RicochetCount = 2;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = -0.26f;
                    MaxSpeed = 3f;
                    Acceleration = 0.6f;
                    Deceleration = 0.8f;

                    treadSoundTimer = 4;

                    MineCooldown = 850;
                    MineLimit = 2;
                    MineStun = 10;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.1f;
                    break;
                #endregion
                #region AdvancedMod
                case TankTier.Granite:
                    AiParams.MeanderAngle = 0.8f;
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.09f;
                    AiParams.Inaccuracy = 0f;

                    AiParams.ProjectileWarinessRadius = 150;
                    AiParams.MineWarinessRadius = 90;

                    TurningSpeed = 0.3f;
                    MaximalTurn = MathHelper.PiOver4;

                    ShootStun = 60;
                    ShellCooldown = 40;
                    ShellLimit = 2;
                    ShellSpeed = 5f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = 0.07f;
                    MaxSpeed = 0.9f;
                    Acceleration = 0.3f;
                    Deceleration = 0.4f;

                    treadSoundTimer = 4;

                    AiParams.SmartRicochets = true;
                    break;
                case TankTier.Bubblegum:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 60;
                    AiParams.TurretSpeed = 0.045f;
                    AiParams.Inaccuracy = 0.04f;

                    AiParams.ProjectileWarinessRadius = 140;
                    AiParams.MineWarinessRadius = 140;

                    TurningSpeed = 0.1f;
                    MaximalTurn = 0.5f;

                    ShootStun = 0;
                    ShellCooldown = 15;
                    ShellLimit = 8;
                    ShellSpeed = 4f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = 0.08f;
                    MaxSpeed = 1.3f;
                    Acceleration = 0.3f;
                    Deceleration = 0.6f;

                    MineCooldown = 940;
                    MineLimit = 1;
                    MineStun = 5;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.02f;
                    AiParams.ShootChance = 0.2f;
                    break;
                case TankTier.Water:
                    AiParams.MeanderAngle = 0.25f;
                    AiParams.MeanderFrequency = 15;
                    AiParams.TurretMeanderFrequency = 10;
                    AiParams.TurretSpeed = 0.03f;
                    AiParams.Inaccuracy = 0.08f;

                    AiParams.ProjectileWarinessRadius = 90;
                    AiParams.MineWarinessRadius = 150;

                    TurningSpeed = 0.2f;
                    MaximalTurn = MathHelper.PiOver4;

                    ShootStun = 20;
                    ShellCooldown = 25;
                    ShellLimit = 2;
                    ShellSpeed = 5.5f;
                    ShellType = ShellTier.Rocket;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = 0.14f;
                    MaxSpeed = 1.7f;
                    Acceleration = 0.4f;
                    Deceleration = 0.6f;

                    treadSoundTimer = 4;
                    break;
                case TankTier.Crimson:
                    AiParams.MeanderAngle = 0.12f;
                    AiParams.MeanderFrequency = 8;
                    AiParams.TurretMeanderFrequency = 60;
                    AiParams.TurretSpeed = 0.07f;
                    AiParams.Inaccuracy = 0.04f;

                    AiParams.ProjectileWarinessRadius = 50;
                    AiParams.MineWarinessRadius = 50;

                    TurningSpeed = 0.1f;
                    MaximalTurn = 0.5f;

                    ShootStun = 1;
                    ShellCooldown = 5;
                    ShellLimit = 5;
                    ShellSpeed = 3f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 0;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = 0.08f;
                    MaxSpeed = 1.3f;
                    Acceleration = 0.6f;
                    Deceleration = 0.8f;

                    MineCooldown = 340;
                    MineLimit = 6;
                    MineStun = 3;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.02f;
                    AiParams.ShootChance = 0.2f;
                    break;
                case TankTier.Tiger:
                    AiParams.MeanderAngle = 0.30f;
                    AiParams.MeanderFrequency = 2;
                    AiParams.TurretMeanderFrequency = 40;
                    AiParams.TurretSpeed = 0.1f;
                    AiParams.Inaccuracy = 0.12f;

                    AiParams.ProjectileWarinessRadius = 90;
                    AiParams.MineWarinessRadius = 120;

                    TurningSpeed = 0.1f;
                    MaximalTurn = MathHelper.PiOver2;

                    ShootStun = 0;
                    ShellCooldown = 20;
                    ShellLimit = 4;
                    ShellSpeed = 4f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = 0.14f;
                    MaxSpeed = 2f;
                    Acceleration = 0.6f;
                    Deceleration = 0.8f;

                    MineCooldown = 1;
                    MineLimit = 10;
                    MineStun = 0;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.05f;
                    break;
                case TankTier.Fade:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 8;
                    AiParams.TurretMeanderFrequency = 40;
                    AiParams.TurretSpeed = 0.05f;
                    AiParams.Inaccuracy = 0.22f;

                    AiParams.ProjectileWarinessRadius = 100;
                    AiParams.MineWarinessRadius = 100;

                    TurningSpeed = 0.12f;
                    MaximalTurn = MathHelper.ToRadians(30);

                    ShootStun = 5;
                    ShellCooldown = 25;
                    ShellLimit = 5;
                    ShellSpeed = 3.5f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = -0.2f;
                    MaxSpeed = 1.9f;
                    Acceleration = 0.6f;
                    Deceleration = 0.9f;

                    MineCooldown = 680;
                    MineLimit = 3;
                    MineStun = 10;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.05f;
                    break;
                case TankTier.Creeper:
                    AiParams.MeanderAngle = 0.2f;
                    AiParams.MeanderFrequency = 25;
                    AiParams.TurretMeanderFrequency = 40;
                    AiParams.TurretSpeed = 0.085f;
                    AiParams.Inaccuracy = 1f;

                    AiParams.ProjectileWarinessRadius = 150;
                    AiParams.MineWarinessRadius = 110;

                    TurningSpeed = 0.3f;
                    MaximalTurn = MathHelper.PiOver4;

                    ShootStun = 20;
                    ShellCooldown = 60;
                    ShellLimit = 2;
                    ShellSpeed = 8f;
                    ShellType = ShellTier.RicochetRocket;
                    RicochetCount = 3;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = -0.07f;
                    MaxSpeed = 1f;
                    Acceleration = 0.3f;
                    Deceleration = 0.4f;

                    treadSoundTimer = 4;
                    break;
                case TankTier.Gamma:
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.08f;
                    AiParams.Inaccuracy = 0.01f;

                    Invisible = false;
                    ShellHoming = new();

                    ShellCooldown = 40;
                    ShellLimit = 6;
                    ShellSpeed = 12.5f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 0;

                    Stationary = true;
                    break;
                case TankTier.Marble:
                    AiParams.MeanderAngle = MathHelper.PiOver2;
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.08f;
                    AiParams.Inaccuracy = 0.11f;

                    AiParams.ProjectileWarinessRadius = 70;
                    AiParams.MineWarinessRadius = 140;

                    TurningSpeed = 0.1f;
                    MaximalTurn = MathHelper.PiOver4;

                    ShootStun = 5;
                    ShellCooldown = 25;
                    ShellLimit = 3;
                    ShellSpeed = 10f;
                    ShellType = ShellTier.Rocket;
                    RicochetCount = 1;

                    TreadPitch = -0.26f;
                    MaxSpeed = 2.6f;
                    Acceleration = 0.6f;
                    Deceleration = 0.8f;

                    Stationary = false;
                    Invisible = false;
                    ShellHoming = new();

                    treadSoundTimer = 4;

                    MineCooldown = 850;
                    MineLimit = 2;
                    MineStun = 10;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.05f;
                    break;
                #endregion

                #region Special
                case TankTier.Explosive:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 60;
                    AiParams.TurretSpeed = 0.045f;
                    AiParams.Inaccuracy = 0.04f;

                    AiParams.ProjectileWarinessRadius = 140;
                    AiParams.MineWarinessRadius = 140;

                    TurningSpeed = 0.1f;
                    MaximalTurn = 0.4f;

                    ShootStun = 0;
                    ShellCooldown = 90;
                    ShellLimit = 2;
                    ShellSpeed = 2f;
                    ShellType = ShellTier.Explosive;
                    RicochetCount = 0;

                    ShootPitch = -0.1f;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = -0.8f;
                    MaxSpeed = 0.8f;
                    Acceleration = 0.3f;
                    Deceleration = 0.6f;

                    MineCooldown = 940;
                    MineLimit = 1;
                    MineStun = 5;

                    treadSoundTimer = 9;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.02f;
                    AiParams.ShootChance = 0.2f;
                    break;

                case TankTier.Electro:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 60;
                    AiParams.TurretSpeed = 0.045f;
                    AiParams.Inaccuracy = 0.04f;

                    AiParams.ProjectileWarinessRadius = 140;
                    AiParams.MineWarinessRadius = 140;

                    TurningSpeed = 0.1f;
                    MaximalTurn = 0.5f;

                    ShootStun = 0;
                    ShellCooldown = 15;
                    ShellLimit = 8;
                    ShellSpeed = 4f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = 0.08f;
                    MaxSpeed = 1.3f;
                    Acceleration = 0.3f;
                    Deceleration = 0.6f;

                    MineCooldown = 940;
                    MineLimit = 1;
                    MineStun = 5;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.02f;
                    AiParams.ShootChance = 0.2f;
                    break;

                case TankTier.RocketDefender:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 60;
                    AiParams.TurretSpeed = 0.045f;
                    AiParams.Inaccuracy = 0.04f;

                    AiParams.ProjectileWarinessRadius = 140;
                    AiParams.MineWarinessRadius = 140;

                    TurningSpeed = 0.1f;
                    MaximalTurn = 0.5f;

                    ShootStun = 0;
                    ShellCooldown = 15;
                    ShellLimit = 8;
                    ShellSpeed = 4f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = 0.08f;
                    MaxSpeed = 1.3f;
                    Acceleration = 0.3f;
                    Deceleration = 0.6f;

                    MineCooldown = 940;
                    MineLimit = 1;
                    MineStun = 5;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.02f;
                    AiParams.ShootChance = 0.2f;
                    break;

                case TankTier.Assassin:
                    AiParams.MeanderAngle = MathHelper.ToRadians(40);
                    AiParams.MeanderFrequency = 15;
                    AiParams.TurretMeanderFrequency = 1;
                    AiParams.TurretSpeed = 0.1f;
                    AiParams.Inaccuracy = 0f;

                    AiParams.ProjectileWarinessRadius = 140;
                    AiParams.MineWarinessRadius = 140;

                    TurningSpeed = 0.1f;
                    MaximalTurn = 0.2f;

                    ShootStun = 25;
                    ShellCooldown = 100;
                    ShellLimit = 1;
                    ShellSpeed = 9.5f;
                    ShellType = ShellTier.Supressed;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = -0.4f;
                    MaxSpeed = 1.2f;
                    Acceleration = 0.3f;
                    Deceleration = 0.6f;

                    MineCooldown = 0;
                    MineLimit = 0;
                    MineStun = 0;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.02f;
                    AiParams.ShootChance = 0.2f;

                    AiParams.SmartRicochets = true;
                    break;

                case TankTier.Commando:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 60;
                    AiParams.TurretSpeed = 0.045f;
                    AiParams.Inaccuracy = 0.04f;

                    AiParams.ProjectileWarinessRadius = 140;
                    AiParams.MineWarinessRadius = 140;

                    TurningSpeed = 0.1f;
                    MaximalTurn = 0.5f;

                    ShootStun = 0;
                    ShellCooldown = 15;
                    ShellLimit = 8;
                    ShellSpeed = 4f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = 0.08f;
                    MaxSpeed = 1.3f;
                    Acceleration = 0.3f;
                    Deceleration = 0.6f;

                    MineCooldown = 940;
                    MineLimit = 1;
                    MineStun = 5;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.02f;
                    AiParams.ShootChance = 0.2f;
                    break;
                    #endregion
            }
        }
        private void OnMissionStart()
        {
            //AiParams.targetTurretRotation -= MathHelper.TwoPi;
            //targetTankRotation -= MathHelper.TwoPi;
            if (Invisible && !Dead)
            {
                var invis = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_invisible");
                SoundPlayer.PlaySoundInstance(invis, SoundContext.Effect, 0.3f);

                var lightParticle = ParticleSystem.MakeParticle(position, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/light_particle"));

                lightParticle.Scale = 0.25f;
                lightParticle.Opacity = 0f;
                lightParticle.is2d = true;

                lightParticle.UniqueBehavior = (lp) =>
                {
                    lp.position = position;
                    if (lp.Scale < 5f)
                        lp.Scale += 0.12f;
                    if (lp.Opacity < 1f && lp.Scale < 5f)
                        lp.Opacity += 0.02f;

                    if (lp.lifeTime > 90)
                        lp.Opacity -= 0.005f;

                    if (lp.Scale < 0f)
                        lp.Destroy();
                };

                const int NUM_LOCATIONS = 8;

                for (int i = 0; i < NUM_LOCATIONS; i++)
                {
                    var lp = ParticleSystem.MakeParticle(position + new Vector3(0, 5, 0), GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smokes"));

                    var velocity = Vector2.UnitY.RotatedByRadians(MathHelper.ToRadians(360f / NUM_LOCATIONS * i));

                    lp.Scale = 1f;

                    lp.UniqueBehavior = (elp) =>
                    {
                        elp.position.X += velocity.X;
                        elp.position.Z += velocity.Y;

                        if (elp.lifeTime > 15)
                        {
                            elp.Scale -= 0.03f;
                            elp.Opacity -= 0.03f;
                        }

                        if (elp.Scale <= 0f || elp.Opacity <= 0f)
                            elp.Destroy();
                    };
                }
            }
        }

        internal void Update()
        {
            if (Dead)
                return;

            if (curShootStun > 0)
                curShootStun--;
            if (curShootCooldown > 0)
                curShootCooldown--;
            if (curMineStun > 0)
                curMineStun--;
            if (curMineCooldown > 0)
                curMineCooldown--;

            if (curShootStun > 0 || curMineStun > 0 || Stationary && isIngame)
                velocity = Vector3.Zero;

            if (isIngame)
            {
                Projection = TankGame.GameProjection;
                View = TankGame.GameView;
            }

            World = Matrix.CreateFromYawPitchRoll(-TankRotation, 0, 0)
                * Matrix.CreateTranslation(position);

            position += velocity * 0.55f; //* 60 * (float)TankGame.LastGameTime.ElapsedGameTime.TotalSeconds;

            DoAi();

            timeSinceLastAction++;

            if (isIngame)
                UpdateCollision();

            oldPosition = position;
        }

        public override void RemoveSilently()
        {
            Dead = true;
            GameHandler.AllAITanks[AITankId] = null;
            GameHandler.AllTanks[WorldId] = null;
        }

        private void UpdateCollision()
        {
            CollisionBox = new(position - new Vector3(7, 15, 7), position + new Vector3(10, 15, 10));
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

            foreach (var c in Block.blocks)
            {
                if (c is not null)
                {
                    var dummyVel = Velocity2D;
                    Collision.HandleCollisionSimple(CollisionBox2D, c.collider2d, ref dummyVel, ref position);

                    velocity.X = dummyVel.X;
                    velocity.Z = dummyVel.Y;
                }
            }
            position.X = MathHelper.Clamp(position.X, MapRenderer.TANKS_MIN_X, MapRenderer.TANKS_MAX_X);
            position.Z = MathHelper.Clamp(position.Z, MapRenderer.TANKS_MIN_Y, MapRenderer.TANKS_MAX_Y);
        }

        /// <summary>
        /// Causes this <see cref="AITank"/> to lay a <see cref="Mine"/> at its current position.
        /// </summary>
        public override void LayMine()
        {
            if (curMineCooldown > 0 || OwnedMineCount >= MineLimit)
                return;

            curMineCooldown = MineCooldown;
            curMineStun = MineStun;
            var sound = GameResources.GetGameResource<SoundEffect>("Assets/sounds/mine_place");
            SoundPlayer.PlaySoundInstance(sound, SoundContext.Effect, 0.5f);
            OwnedMineCount++;
            TimeSinceLastMinePlaced = 0;

            timeSinceLastAction = 0;

            var mine = new Mine(this, position, 600);
        }

        /// <summary>
        /// Destroys this <see cref="AITank"/>.
        /// </summary>
        public override void Destroy()
        {
            Dead = true;
            var killSound1 = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_destroy");
            var killSound2 = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_destroy_enemy");

            SoundPlayer.PlaySoundInstance(killSound1, SoundContext.Effect, 0.2f);
            SoundPlayer.PlaySoundInstance(killSound2, SoundContext.Effect, 0.3f);

            new TankDeathMark(TankDeathMark.CheckColor.White)
            {
                location = position + new Vector3(0, 0.1f, 0)
            };

            GameHandler.AllAITanks[AITankId] = null;
            GameHandler.AllTanks[WorldId] = null;
            // TODO: play fanfare thingy i think
        }

        /// <summary>
        /// Causes this <see cref="AITank"/> to fire a <see cref="Shell"/>.
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
                ShellTier.Standard => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_regular_2"), SoundContext.Effect, 0.3f),
                ShellTier.Rocket => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_rocket"), SoundContext.Effect, 0.3f),
                ShellTier.RicochetRocket => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_ricochet_rocket"), SoundContext.Effect, 0.3f),
                ShellTier.Supressed => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_silencer"), SoundContext.Effect, 0.3f),
                ShellTier.Explosive => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_regular_2"), SoundContext.Effect, 0.3f),
                _ => throw new NotImplementedException()
            };
            sfx.Pitch = ShootPitch;

            var bullet = new Shell(position, Vector3.Zero, homing: ShellHoming);
            var new2d = Vector2.UnitY.RotatedByRadians(TurretRotation);

            var newPos = Position2D + new Vector2(0, 20).RotatedByRadians(-TurretRotation);

            bullet.position = new Vector3(newPos.X, 11, newPos.Y);

            bullet.velocity = new Vector3(-new2d.X, 0, new2d.Y) * ShellSpeed;

            bullet.owner = this;
            bullet.ricochets = RicochetCount;

            var hit = ParticleSystem.MakeParticle(bullet.position, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/bot_hit"));
            var smoke = ParticleSystem.MakeParticle(bullet.position, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/tank_smokes"));

            hit.rotationX = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;
            smoke.rotationX = -TankGame.DEFAULT_ORTHOGRAPHIC_ANGLE;

            smoke.Scale = 0.35f;
            hit.Scale = 0.5f;

            smoke.color = new(84, 22, 0, 255);

            smoke.isAddative = false;

            int achieveable = 80;
            int step = 1;

            hit.UniqueBehavior = (part) =>
            {
                part.color = Color.Orange;

                if (part.lifeTime > 1)
                    part.Opacity -= 0.1f;
                if (part.Opacity <= 0)
                    part.Destroy();
            };
            smoke.UniqueBehavior = (part) =>
            {
                part.color.R = (byte)GameUtils.RoughStep(part.color.R, achieveable, step);
                part.color.G = (byte)GameUtils.RoughStep(part.color.G, achieveable, step);
                part.color.B = (byte)GameUtils.RoughStep(part.color.B, achieveable, step);

                part.Scale += 0.004f;

                if (part.color.G == achieveable)
                {
                    part.color.B = (byte)achieveable;
                    part.Opacity -= 0.04f;

                    if (part.Opacity <= 0)
                        part.Destroy();
                }
            };

            OwnedShellCount++;

            timeSinceLastAction = 0;

            curShootStun = ShootStun;
            curShootCooldown = ShellCooldown;
        }

        public override void LayFootprint(bool alt)
        {
            if (!CanLayTread)
                return;
            new TankFootprint(alt)
            {
                location = position + new Vector3(0, 0.1f, 0),
                rotation = -TankRotation
            };
        }

        public Ray tankPathRay;
        
        public bool pathBlocked;

        private int treadPlaceTimer;
        public bool isEnemySpotted;

        private bool seeks;
        private float seekRotation = 0;

        public bool nearDestructibleObstacle;

        // make a new method for just any rectangle
        private bool IsTankInPath(Vector2 pathDir, Tank enemy = null, bool draw = false, Vector2 offset = default, Func<Block, bool> pattern = null)
        {
            if (pattern is null)
                pattern = (c) => c.IsSolid;

            const int MAX_PATH_UNITS = 250;
            const int PATH_UNIT_LENGTH = 8;

            // 20, 30

            var whitePixel = GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel");
            var pathPos = Position2D + offset.RotatedByRadians(-TurretRotation);

            pathDir.Y *= -1;
            pathDir *= PATH_UNIT_LENGTH;
            int pathRicochetCount = 0;

            for (int i = 0; i < MAX_PATH_UNITS; i++)
            {
                var dummyPos = Vector3.Zero;

                if (pathPos.X < MapRenderer.MIN_X || pathPos.X > MapRenderer.MAX_X)
                {
                    pathDir.X *= -1;
                    pathRicochetCount++;
                }
                if (pathPos.Y < MapRenderer.MIN_Y || pathPos.Y > MapRenderer.MAX_Y)
                {
                    pathDir.Y *= -1;
                    pathRicochetCount++;
                }

                var pathHitbox = new Rectangle((int)pathPos.X - 3, (int)pathPos.Y - 3, 6, 6);

                // Why is velocity passed by reference here lol
                Collision.HandleCollisionSimple_ForBlocks(pathHitbox, ref pathDir, ref dummyPos, out var dir, false, pattern);

                switch (dir)
                {
                    case Collision.CollisionDirection.Up:
                    case Collision.CollisionDirection.Down:
                        pathDir.Y *= -1;
                        pathRicochetCount++;
                        break;
                    case Collision.CollisionDirection.Left:
                    case Collision.CollisionDirection.Right:
                        pathDir.X *= -1;
                        pathRicochetCount++;
                        break;
                }

                if (i == 0 && Block.blocks.Any(x => x is not null && x.collider2d.Intersects(pathHitbox)))
                    return false;

                if (i < (int)ShellSpeed && pathRicochetCount > 0)
                    return false;

                if (pathRicochetCount > RicochetCount)
                    return false;

                pathPos += pathDir;

                if (draw)
                {
                    var pathPosScreen = GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(pathPos.X, 11, pathPos.Y), TankGame.GameView, TankGame.GameProjection);
                    TankGame.spriteBatch.Draw(whitePixel, pathPosScreen, null, Color.White, 0, whitePixel.Size() / 2, 2 + (float)Math.Sin(i * Math.PI / 5 - TankGame.GameUpdateTime * 0.3f), default, default);
                }

                if (enemy == null)
                    continue;
                if (enemy.CollisionBox2D.Intersects(pathHitbox))
                    return true;
            }
            return false;
        }

        private bool IsObstacleInWay(int checkDist, Vector2 pathDir, out Vector2 reflectDir, bool draw = false)
        {
            const int PATH_UNIT_LENGTH = 1;

            bool hasCollided = false;

            // 20, 30

            var whitePixel = GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel");
            var pathPos = Position2D + Vector2.Zero.RotatedByRadians(-TurretRotation);

            pathDir.Y *= -1;
            pathDir *= PATH_UNIT_LENGTH;

            for (int i = 0; i < checkDist; i++)
            {
                var dummyPos = Vector3.Zero;

                if (pathPos.X < MapRenderer.MIN_X || pathPos.X > MapRenderer.MAX_X)
                {
                    pathDir.X *= -1;
                    hasCollided = true;
                }
                if (pathPos.Y < MapRenderer.MIN_Y || pathPos.Y > MapRenderer.MAX_Y)
                {
                    pathDir.Y *= -1;
                    hasCollided = true;
                }

                var pathHitbox = new Rectangle((int)pathPos.X, (int)pathPos.Y, 1, 1);

                // Why is velocity passed by reference here lol
                Collision.HandleCollisionSimple_ForBlocks(pathHitbox, ref pathDir, ref dummyPos, out var dir, false);

                switch (dir)
                {
                    case Collision.CollisionDirection.Up:
                    case Collision.CollisionDirection.Down:
                        hasCollided = true;
                        pathDir.Y *= -1;
                        break;
                    case Collision.CollisionDirection.Left:
                    case Collision.CollisionDirection.Right:
                        pathDir.X *= -1;
                        hasCollided = true;
                        break;
                }

                pathPos += pathDir;

                if (draw)
                {
                    var pathPosScreen = GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(pathPos.X, 11, pathPos.Y), TankGame.GameView, TankGame.GameProjection);
                    TankGame.spriteBatch.Draw(whitePixel, pathPosScreen, null, Color.White, 0, whitePixel.Size() / 2, 2 + (float)Math.Sin(i * Math.PI / 5 - TankGame.GameUpdateTime * 0.3f), default, default);
                }
            }

            reflectDir = pathDir; // pos?
            return hasCollided;
        }

        public void DoAi(bool doMoveTowards = true, bool doMovements = true, bool doFire = true)
        {
            TimeSinceLastMinePlaced++;

            CannonMesh.ParentBone.Transform = Matrix.CreateRotationY(TurretRotation + TankRotation);

            if (tier == TankTier.Commando)
            {
                Model.Meshes["Laser_Beam"].ParentBone.Transform = Matrix.CreateRotationY(TurretRotation + TankRotation);
                Model.Meshes["Barrel_Laser"].ParentBone.Transform = Matrix.CreateRotationY(TurretRotation + TankRotation);
                Model.Meshes["Dish"].ParentBone.Transform = Matrix.CreateRotationY(TurretRotation + TankRotation);
            }

            Model.Root.Transform = World;

            Model.CopyAbsoluteBoneTransformsTo(boneTransforms);

            if (GameHandler.InMission)
            {
                foreach (var behavior in Behaviors)
                    behavior.totalUpdateCount++;

                treadPlaceTimer = (int)Math.Round(14 / velocity.Length()) != 0 ? (int)Math.Round(14 / velocity.Length()) : 1;

                if (velocity != Vector3.Zero && !Stationary)
                {
                    if (TankGame.GameUpdateTime % treadSoundTimer == 0)
                    {
                        var treadPlace = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_tread_place_{new Random().Next(1, 5)}");
                        var sfx = SoundPlayer.PlaySoundInstance(treadPlace, SoundContext.Effect, 0.05f);
                        sfx.Pitch = TreadPitch;
                    }

                    if (TankGame.GameUpdateTime % treadPlaceTimer == 0)
                        LayFootprint(tier == TankTier.White ? true : false);
                }
                enactBehavior = () =>
                {
                    var enemy = GameHandler.AllTanks.FirstOrDefault(tnk => tnk is not null && !tnk.Dead && (tnk.Team != Team || tnk.Team == Team.NoTeam) && tnk != this);

                    foreach (var tank in GameHandler.AllTanks)
                    {
                        if (tank is not null && !tank.Dead && (tank.Team != Team || tank.Team == Team.NoTeam) && tank != this)
                            if (Vector3.Distance(tank.position, position) < Vector3.Distance(enemy.position, position))
                                if ((tank.Invisible && tank.timeSinceLastAction < 60) || !tank.Invisible)
                                    enemy = tank;
                    }

                    var tanksNearMe = new List<Tank>();
                    var cubesNearMe = new List<Block>();

                    foreach (var tank in GameHandler.AllTanks)
                        if (tank != this && tank is not null && !tank.Dead && Vector3.Distance(tank.position, position) <= AiParams.TeammateTankWariness)
                            tanksNearMe.Add(tank);

                    foreach (var cube in Block.blocks)
                        if (cube is not null && Vector3.Distance(position, cube.position) < AiParams.BlockWarinessDistance)
                            cubesNearMe.Add(cube);

                    #region TurretHandle

                    targetTurretRotation %= MathHelper.TwoPi;

                    TurretRotation %= MathHelper.TwoPi;

                    var diff = targetTurretRotation - TurretRotation;

                    if (diff > MathHelper.Pi)
                        targetTurretRotation -= MathHelper.TwoPi;
                    else if (diff < -MathHelper.Pi)
                        targetTurretRotation += MathHelper.TwoPi;

                    TurretRotation = GameUtils.RoughStep(TurretRotation, targetTurretRotation, seeks ? AiParams.TurretSpeed * 3 : AiParams.TurretSpeed);
                    bool targetExists = Array.IndexOf(GameHandler.AllTanks, enemy) > -1 && enemy is not null;
                    if (targetExists)
                    {
                        if (!seeks)
                        {
                            if (Behaviors[1].IsModOf(AiParams.TurretMeanderFrequency))
                            {
                                isEnemySpotted = false;
                                if (enemy.Invisible && enemy.timeSinceLastAction < 60)
                                {
                                    aimTarget = enemy.Position2D.Expand_Z();
                                    isEnemySpotted = true;
                                }

                                if (!enemy.Invisible)
                                {
                                    aimTarget = enemy.Position2D.Expand_Z();
                                    isEnemySpotted = true;
                                }

                                var dirVec = Position2D - aimTarget.FlattenZ();
                                targetTurretRotation = -dirVec.ToRotation() - MathHelper.PiOver2 + new Random().NextFloat(-AiParams.Inaccuracy, AiParams.Inaccuracy);
                            }
                        }

                        if (doFire)
                        {
                            seekRotation += AiParams.TurretSpeed;

                            SeesTarget = false;

                            var findsFriendly = IsTankInPath(Vector2.UnitY.RotatedByRadians(TurretRotation - MathHelper.Pi), enemy, offset: Vector2.UnitY * 20);
                            var findsEnemy = IsTankInPath(Vector2.UnitY.RotatedByRadians(TurretRotation - MathHelper.Pi), enemy, offset: Vector2.UnitY * 20);
                            var findsSelf = IsTankInPath(Vector2.UnitY.RotatedByRadians(TurretRotation - MathHelper.Pi), this, offset: Vector2.UnitY * 20);
                            if (findsEnemy && isEnemySpotted)
                                SeesTarget = true;

                            if (AiParams.SmartRicochets)
                            {
                                var canShoot = !(curShootCooldown > 0 || OwnedShellCount >= ShellLimit);
                                if (canShoot)
                                {
                                    if (IsTankInPath(Vector2.UnitY.RotatedByRadians(seekRotation), enemy))
                                    {
                                        seeks = true;
                                        targetTurretRotation = seekRotation - MathHelper.Pi;
                                    }
                                }

                                if (TurretRotation == targetTurretRotation || !canShoot)
                                    seeks = false;
                            }

                            bool checkNoTeam = Team == Team.NoTeam ? true : !tanksNearMe.Any(x => x.Team == Team);

                            if (SeesTarget && checkNoTeam && !findsSelf)
                                if (curShootCooldown <= 0)
                                    Shoot();
                        }
                    }

                    #endregion
                    if (doMovements)
                    {

                        if (Stationary)
                            return;

                        /*TankRotation %= MathHelper.Pi;
                        targetTankRotation %= MathHelper.Tau;
                        if (targetTankRotation - TankRotation >= MathHelper.PiOver2)
                            TankRotation += MathHelper.Pi;
                        else if (targetTankRotation - TankRotation <= -MathHelper.PiOver2)
                            TankRotation -= MathHelper.Pi;*/

                        bool isBulletNear = TryGetShellNear(AiParams.ProjectileWarinessRadius, out var shell);
                        bool isMineNear = TryGetMineNear(AiParams.MineWarinessRadius, out var mine);

                        bool movingFromMine = TimeSinceLastMinePlaced < AiParams.MoveFromMineTime;
                        bool movingFromOtherMine = TimeSinceLastMineFound < AiParams.MoveFromMineTime / 2;

                        #region CubeNav

                        pathBlocked = IsObstacleInWay(AiParams.BlockWarinessDistance, Vector2.UnitY.RotatedByRadians(-targetTankRotation), out var travelPath);

                        if (pathBlocked && Behaviors[2].IsModOf(3) && !isMineNear && !isBulletNear && !movingFromMine && !movingFromOtherMine)
                        {
                            var targe = GameUtils.DirectionOf(Position2D, travelPath).ToRotation();
                            GameUtils.RoughStep(ref targetTankRotation, targe, targe / 4);

                            // TODO: i literally do not understand this
                        }

                        #endregion

                        #region GeneralMovement
                        if (!isMineNear && !isBulletNear && !movingFromMine && !movingFromOtherMine && !IsTurning && curMineStun == 0 && curShootStun == 0)
                        {
                            if (!pathBlocked)
                            {
                                if (Behaviors[0].IsModOf(AiParams.MeanderFrequency))
                                {
                                    float dir = -100;

                                    if (targetExists)
                                        dir = GameUtils.DirectionOf(Position2D, enemy.Position2D).ToRotation();

                                    var random = new Random().NextFloat(-AiParams.MeanderAngle / 2, AiParams.MeanderAngle / 2);

                                    targetTankRotation += random;
                                }
                                if (targetExists)
                                {
                                    if (AiParams.PursuitFrequency != 0)
                                    {
                                        if (Behaviors[0].IsModOf(AiParams.PursuitFrequency))
                                        {
                                            float dir = -100;

                                            if (targetExists)
                                                dir = GameUtils.DirectionOf(Position2D, enemy.Position2D).ToRotation();

                                            var random = new Random().NextFloat(-AiParams.MeanderAngle / 2, AiParams.MeanderAngle / 2);

                                            var meanderRandom = dir != -100 ? random + (dir + MathHelper.PiOver2) + (0.2f * AiParams.PursuitLevel) : random;

                                            targetTankRotation = meanderRandom;
                                        }
                                    }
                                }
                            }
                        }
                        #endregion

                        #region ShellAvoidance

                        var indif = 1;

                        if (Behaviors[6].IsModOf(indif) && !isMineNear)
                        {
                            if (isBulletNear)
                            {
                                if (shell.owner == this)
                                {
                                    if (shell.lifeTime > 30)
                                    {
                                        var dire = Vector2.UnitY.RotatedByRadians(shell.Position2D.DirectionOf(Position2D, false).ToRotation());

                                        targetTankRotation = dire.ToRotation();
                                    }
                                }
                                else
                                {
                                    var direction = Vector2.UnitY.RotatedByRadians(shell.Position2D.DirectionOf(Position2D, false).ToRotation());

                                    targetTankRotation = direction.ToRotation();
                                }
                            }
                        }

                        #endregion

                        #region MineHandle / MineAvoidance
                        if (MineLimit > 0)
                        {
                            if (Behaviors[4].IsModOf(60))
                            {
                                if (!tanksNearMe.Any(x => x.Team == Team))
                                {
                                    nearDestructibleObstacle = cubesNearMe.Any(c => c.IsDestructible);
                                    if (AiParams.SmartMineLaying)
                                    {
                                        if (nearDestructibleObstacle)
                                        {
                                            targetTankRotation = new Vector2(100, 100).RotatedByRadians(new Random().NextFloat(0, MathHelper.TwoPi)).Expand_Z().ToRotation();
                                            LayMine();
                                        }
                                    }
                                    else
                                    {
                                        if (new Random().NextFloat(0, 1) <= AiParams.MinePlacementChance)
                                        {
                                            targetTankRotation = new Vector2(100, 100).RotatedByRadians(new Random().NextFloat(0, MathHelper.TwoPi)).Expand_Z().ToRotation();
                                            LayMine();
                                        }
                                    }
                                }
                            }
                        }
                        if (isMineNear)
                        {
                            if (Behaviors[5].IsModOf(10))
                            {
                                if (TimeSinceLastMinePlaced > AiParams.MoveFromMineTime)
                                {
                                    var direction = Vector2.UnitY.RotatedByRadians(mine.Position2D.DirectionOf(Position2D, false).ToRotation());

                                    targetTankRotation = direction.ToRotation();
                                }
                            }
                        }
                        #endregion

                    }

                    #region Special Tank Behavior

                    if (tier == TankTier.Creeper)
                    {
                        if (Array.IndexOf(GameHandler.AllTanks, enemy) > -1 && enemy is not null)
                        {
                            float explosionDist = 90f;
                            if (Vector3.Distance(enemy.position, position) < explosionDist)
                            {
                                Destroy();

                                new MineExplosion(position, 10f, 0.2f);

                                foreach (var tnk in GameHandler.AllTanks)
                                    if (tnk is not null && Vector3.Distance(tnk.position, position) < explosionDist)
                                        tnk.Destroy();
                            }
                        }
                    }

                    #endregion

                    #region TankRotation

                    var targ = targetTankRotation + MathHelper.Pi;

                    // IsTurning = false;

                    /*targetTankRotation %= MathHelper.TwoPi;
                    TankRotation %= MathHelper.TwoPi;

                    var diff2 = targetTankRotation - TankRotation;

                    if (diff2 > MathHelper.Pi)
                        targetTankRotation -= MathHelper.TwoPi;
                    else if (diff2 < -MathHelper.Pi)
                        targetTankRotation += MathHelper.TwoPi;*/

                    if (doMoveTowards)
                    {
                        if (!IsTurning)
                        {
                            if (TankRotation > targ - MaximalTurn && TankRotation < targ + MaximalTurn)
                            {
                                // TankRotation = targ;
                                var dir = Vector2.UnitY.RotatedByRadians(TankRotation);
                                velocity.X = dir.X;
                                velocity.Z = dir.Y;

                                velocity.Normalize();
                                // velocity *= MaxSpeed;
                                velocity *= MaxSpeed;
                            }
                            else
                            {
                                treadPlaceTimer = (int)Math.Round(14 / TurningSpeed) != 0 ? (int)Math.Round(14 / TurningSpeed) : 1;
                                if (TankGame.GameUpdateTime % treadPlaceTimer == 0)
                                {
                                    LayFootprint(tier == TankTier.White);
                                }
                                IsTurning = true;
                                velocity = Vector3.Zero;
                            }
                        }
                        else
                        {
                            if (TankRotation >= targ - 0.05f || TankRotation <= targ + 0.05f)
                                IsTurning = false;
                        }

                        TankRotation = GameUtils.RoughStep(TankRotation, targ, TurningSpeed);
                    }

                    #endregion
                };
            }
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

                    if (!HasTurret)
                        if (mesh.Name == "Cannon")
                            return;

                    if (mesh.Name == "Shadow")
                    {
                        effect.Alpha = 0.5f;
                        effect.Texture = _shadowTexture;
                    }
                    else
                    {
                        effect.Alpha = 1;
                        if (IsHoveredByMouse)
                            effect.EmissiveColor = Color.White.ToVector3();
                        else
                            effect.EmissiveColor = Color.Black.ToVector3();

                        var tex = _tankColorTexture;

                        effect.Texture = tex;
                        /*var ex = new Color[1024];

                        Array.Fill(ex, new Color(new Random().Next(0, 256), new Random().Next(0, 256), new Random().Next(0, 256)));

                        effect.Texture.SetData(0, new Rectangle(0, 8, 32, 15), ex, 0, 480);*/

                        /*if (Team != Team.NoTeam)
                        {
                            var ex = new Color[1024];

                            Array.Fill(ex, (Color)typeof(Color).GetProperty(Team.ToString(), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).GetValue(null));

                            tex.SetData(0, new Rectangle(0, 0, 32, 9), ex, 0, 288);
                            tex.SetData(0, new Rectangle(0, 23, 32, 9), ex, 0, 288);
                        }*/
                        // removed temporarily
                    }

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
            {
                if (AiParams.SmartRicochets)
                    IsTankInPath(Vector2.UnitY.RotatedByRadians(seekRotation), default, true);
                IsTankInPath(Vector2.UnitY.RotatedByRadians(TurretRotation - MathHelper.Pi), default, true);

                if (!Stationary)
                {
                    IsObstacleInWay(AiParams.BlockWarinessDistance, Vector2.UnitY.RotatedByRadians(-targetTankRotation), out var travelPath, true);
                    DebugUtils.DrawDebugString(TankGame.spriteBatch, travelPath, GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(travelPath.X, 11, travelPath.Y), View, Projection), 1, centered: true);
                }
            }

            var info = new string[]
            {
                $"Team: {Team}",
                $"OwnedShellCount: {OwnedShellCount}"
            };

            for (int i = 0; i < info.Length; i++)
                DebugUtils.DrawDebugString(TankGame.spriteBatch, info[i], GeometryUtils.ConvertWorldToScreen(Vector3.Zero, World, View, Projection) - new Vector2(0, (info.Length * 20) + (i * 20)), 1, centered: true);

            if (Invisible && GameHandler.InMission)
                return;

            RenderModel();
        }

        public bool TryGetShellNear(float distance, out Shell shell)
        {
            shell = null;

            Shell bulletCompare = null;

            foreach (var blet in Shell.AllShells)
            {
                if (blet is not null)
                {
                    if (Vector3.Distance(position, blet.position) < distance)
                    {
                        if (bulletCompare == null)
                            shell = blet;
                        else
                        {
                            if (Vector3.Distance(position, blet.position).CompareTo(Vector3.Distance(position, bulletCompare.position)) < 0)
                                shell = bulletCompare;
                        }
                        // bullet = blet;
                        return true;
                    }
                }
            }
            return false;
        }
        public bool TryGetMineNear(float distance, out Mine mine)
        {
            mine = null;
            foreach (var yours in Mine.AllMines)
            {
                if (yours is not null)
                {
                    if (Vector3.Distance(position, yours.position) < distance)
                    {
                        mine = yours;
                        return true;
                    }
                }
            }
            return false;
        }

        public static TankTier PICK_ANY_THAT_ARE_IMPLEMENTED()
        {
            TankTier[] workingTiers = 
            { 
                TankTier.Brown, TankTier.Marine, TankTier.Yellow, TankTier.Black, TankTier.White, TankTier.Pink, TankTier.Purple, TankTier.Green, TankTier.Ash, 
                TankTier.Bronze, TankTier.Silver, TankTier.Sapphire, TankTier.Ruby, TankTier.Citrine, TankTier.Amethyst, TankTier.Emerald, TankTier.Gold, TankTier.Obsidian,
                TankTier.Granite, TankTier.Bubblegum, TankTier.Water, TankTier.Crimson, /*TankTier.Tiger,*/ TankTier.Creeper, TankTier.Gamma, TankTier.Marble,
                TankTier.Assassin
            };

            return workingTiers[new Random().Next(0, workingTiers.Length)];
        }
    }
}