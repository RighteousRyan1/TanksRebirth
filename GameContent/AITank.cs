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
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Graphics;
using TanksRebirth.GameContent.Systems.Coordinates;
using TanksRebirth.Internals.Common.Framework;
using TanksRebirth.Internals.Common.IO;
using System.Reflection;
using TanksRebirth.Net;
using TanksRebirth.IO;
using TanksRebirth.GameContent.Properties;
using TanksRebirth.GameContent.Cosmetics;
using TanksRebirth.GameContent.UI;

namespace TanksRebirth.GameContent
{
    public class AITank : Tank
    {
        // TODO: Make smoke bombs!
        public AiBehavior[] Behaviors { get; private set; } // each of these should keep track of an action the tank performs
        public AiBehavior[] SpecialBehaviors { get; private set; }

        public TankTier Tier;

        private Texture2D _tankTexture, _shadowTexture;

        public Action enactBehavior;

        public int AITankId { get; }

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

            /// <summary>How often this tank will move its turret in the target's direction. It will be inaccurate at the measure of <see cref="AimOffset"/>.</summary>
            public int TurretMeanderFrequency { get; set; }
            /// <summary>How fast this tank's turret rotates towards its target.</summary>
            public float TurretSpeed { get; set; }
            /// <summary>How inaccurate (in radians) this tank is trying to aim at its target.</summary>
            public float AimOffset { get; set; }

            /// <summary>The distance of which this tank is wary of projectiles shot by <see cref="PlayerTank"/>s and tries to move away from them.</summary>
            public float ProjectileWarinessRadius_PlayerShot { get; set; }
            /// <summary>The distance of which this tank is wary of projectiles shot by <see cref="PlayerTank"/>s and tries to move away from them.</summary>
            public float ProjectileWarinessRadius_AIShot { get; set; }
            /// <summary>The distance of which this tank is wary of mines laid by <see cref="PlayerTank"/>s and tries to move away from them.</summary>
            public float MineWarinessRadius_PlayerLaid { get; set; }
            /// <summary>The distance of which this tank is wary of mines laid by <see cref="AITank"/>s and tries to move away from them.</summary>
            public float MineWarinessRadius_AILaid { get; set; }

            /// <summary>On a given tick, it has this chance out of 1 to lay a mine. <para>Do note that this value must be greater than 0 and less than or equal to 1.</para></summary>
            public float MinePlacementChance { get; set; } // 0.0f to 1.0f


            /// <summary>The distance from the main shot calculation ray an enemy must be before this tank is allowed to fire.</summary>
            public float Inaccuracy { get; set; }

            /// <summary>How often this tank shoots when given the opportunity. 0 to 1 values only. Defaults to 1.</summary>
            public float ShootChance { get; set; } = 1f;

            /// <summary>How far ahead of this tank (in the direction the tank is going) that it is aware of obstacles and navigates around them.</summary>
            public int BlockWarinessDistance { get; set; } = 50;
            /// <summary>How often this tank reads the obstacles around it and navigates around them.</summary>
            public int BlockReadTime { get; set; } = 3;
            /// <summary>How far this tank must be from a teammate before it can lay a mine or fire a bullet.</summary>
            public float TankWarinessRadius { get; set; } = 50f;
            /// <summary>Whether or not this tank tries to find calculations all around it. This is not recommended for mobile tanks.</summary>
            public bool SmartRicochets { get; set; }
            /// <summary>Whether or not this tank attempts to lay mines near destructible obstacles rather than randomly. Useless for stationary tanks.</summary>
            public bool SmartMineLaying { get; set; }
            /// <summary>Whether or not this tank's shot raycast resets it's distance check per-bounce.</summary>
            public bool BounceReset { get; set; } = true;
            /// <summary>When this tank finds a wall in its path, it moves away at this angle every <see cref="BlockReadTime"/> ticks.</summary>
            public float RedirectAngle { get; set; } = MathHelper.ToRadians(5);
            /// <summary>Whether or not this tank predics the future position of its target.</summary>
            public bool PredictsPositions { get; set; }
            /// <summary>Whether or not this <see cref="AITank"/> should deflect incoming bullets to prevent being hit.</summary>
            public bool DeflectsBullets { get; set; }
            /// <summary>Whether or not this <see cref="AITank"/> will shoot mines that are near destructible obstacles.</summary>
            public bool ShootsMinesSmartly { get; set; }

            // TODO: make friendly check distances separate for bullets and mines
        }
        /// <summary>The AI parameter collection of this AI Tank.</summary>
        public Params AiParams { get; } = new();

        public Vector2 Aimtarget;

        /// <summary>Whether or not this tank sees its target. Generally should not be set, but the tank will shoot if able when this is true.</summary>
        public bool SeesTarget { get; set; }

        /// <summary>The target rotation for this tank's turret. <see cref="Tank.TurretRotation"/> will move towards this value at a rate of <see cref="TurretSpeed"/>.</summary>
        public float TargetTurretRotation;

        private Vector2 _oldPosition;

        public float BaseExpValue { get; set; }

        #endregion

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
                    if (tank.Tier > highest)
                        highest = tank.Tier;
            }
            return highest;
        }

        public static int CountAll()
            => GameHandler.AllAITanks.Count(tnk => tnk is not null && !tnk.Dead);

        public static int GetTankCountOfType(TankTier tier)
            => GameHandler.AllAITanks.Count(tnk => tnk is not null && tnk.Tier == tier && !tnk.Dead);

        public void Swap(TankTier tier, bool setDefaults = true)
        {
            Tier = tier;

            if ((int)tier <= (int)TankTier.Marble)
                _tankTexture = Assets[$"tank_" + tier.ToString().ToLower()];
            #region Special

            if (tier == TankTier.Commando)
            {
                Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_commando");

                _tankTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_commando");

                foreach (var mesh in Model.Meshes)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        if (mesh.Name == "Laser_Beam")
                            effect.Texture = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/laser");
                        if (mesh.Name == "Barrel_Laser")
                            effect.Texture = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/armor");
                        if (mesh.Name == "Dish")
                            effect.Texture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_commando");
                    }
                }
                // fix?
            }
            else if (tier == TankTier.Assassin)
            {
                Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_assassin");

                _tankTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_assassin");
            }
            else if (tier == TankTier.RocketDefender)
            {
                Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_rocket");

                _tankTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_rocket");
            }
            else if (tier == TankTier.Electro)
            {
                Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_electro");

                _tankTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_electro");
            }
            else if (tier == TankTier.Explosive)
            {
                Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_explosive");

                _tankTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_explosive");
            }
            else
            {
                Model = GameResources.GetGameResource<Model>("Assets/tank_e");
            }

            #endregion

            if (setDefaults)
                ApplyDefaults(ref Properties);
        }

        /// <summary>
        /// Creates a new <see cref="AITank"/>.
        /// </summary>
        /// <param name="tier">The tier of this <see cref="AITank"/>. If '<see cref="TankTier.Random"/>', it will be randomly chosen.</param>
        /// <param name="setTankDefaults">Whether or not to give this <see cref="AITank"/> the default values.</param>
        /// /// <param name="isIngame">Whether or not this <see cref="AITank"/> is a gameplay tank or a cosmetic tank (i.e: display models on menus, etc).</param>
        public AITank(TankTier tier, Range<TankTier> tankRange = default, bool setTankDefaults = true, bool isIngame = true, bool isRandomizable = true)
        {
            if (isIngame)
            {
                if (Difficulties.Types["BumpUp"])
                    tier += 1;
                if (Difficulties.Types["MeanGreens"])
                    tier = TankTier.Green;
                if (Difficulties.Types["MasterModBuff"] && !Difficulties.Types["MarbleModBuff"])
                    tier += 9;
                if (Difficulties.Types["MarbleModBuff"] && !Difficulties.Types["MasterModBuff"])
                    tier += 18;
                if (Difficulties.Types["RandomizedTanks"])
                {
                    tier = TankTier.Random;
                    tankRange = new Range<TankTier>(TankTier.Brown, TankTier.Marble); // set to commando when the time comes
                }
            }
            if (isRandomizable)
                if (tier == TankTier.Random)
                    tier = (TankTier)GameHandler.GameRand.Next((int)tankRange.Min, (int)tankRange.Max + 1);
            IsIngame = isIngame;
            if (isIngame)
            {
                Behaviors = new AiBehavior[10];
                SpecialBehaviors = new AiBehavior[3];

                for (int i = 0; i < Behaviors.Length; i++)
                    Behaviors[i] = new();

                for (int i = 0; i < SpecialBehaviors.Length; i++)
                    SpecialBehaviors[i] = new();

                Behaviors[0].Label = "TankBaseMovement";
                Behaviors[1].Label = "TankBarrelMovement";
                Behaviors[2].Label = "TankEnvReader";
                Behaviors[3].Label = "TankBulletFire";
                Behaviors[4].Label = "TankMinePlacement";
                Behaviors[5].Label = "TankMineAvoidance";
                Behaviors[6].Label = "TankBulletAvoidance";

                SpecialBehaviors[0].Label = "SpecialBehavior1"; // for special tanks (such as commando, etc)
                SpecialBehaviors[1].Label = "SpecialBehavior2";
                SpecialBehaviors[2].Label = "SpecialBehavior3";

                Dead = true;
            }

            if (tier <= TankTier.Cherry)
                _tankTexture = Assets[$"tank_" + tier.ToString().ToLower()];
            
            #region Special

            if (tier == TankTier.Commando)
            {
                Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_commando");

                _tankTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_commando");
                // fix?
            }
            else if (tier == TankTier.Assassin)
            {
                Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_assassin");

                _tankTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_assassin");
            }
            else if (tier == TankTier.RocketDefender)
            {
                Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_rocket");

                _tankTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_rocket");
            }
            else if (tier == TankTier.Electro)
            {
                Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_electro");

                _tankTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_electro");
            }
            else if (tier == TankTier.Explosive)
            {
                Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_explosive");

                _tankTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_explosive");
            }
            else
            {
                Model = GameResources.GetGameResource<Model>("Assets/tank_e");
            }

            #endregion

            CannonMesh = Model.Meshes["Cannon"];

            boneTransforms = new Matrix[Model.Bones.Count];

            _shadowTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank_shadow");

            Tier = tier;

            if (setTankDefaults)
                ApplyDefaults(ref Properties);

            int index = Array.IndexOf(GameHandler.AllAITanks, GameHandler.AllAITanks.First(tank => tank is null));

            AITankId = index;

            GameHandler.AllAITanks[index] = this;

            int index2 = Array.IndexOf(GameHandler.AllTanks, GameHandler.AllTanks.First(tank => tank is null));

            WorldId = index2;

            GameHandler.AllTanks[index2] = this;

            GameProperties.OnMissionStart += OnMissionStart;
            
            base.Initialize();
        }

        private void OnMissionStart()
        {
            // other things can be done here
            //UpdateAim(new List<Tank>(), true);
        }

        public override void ApplyDefaults(ref TankProperties properties)
        {
            switch (Tier)
            {
                #region VanillaTanks
                case TankTier.Brown:
                    properties.Stationary = true;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 60;

                    properties.DestructionColor = new(152, 96, 26);

                    AiParams.TurretMeanderFrequency = 30;
                    AiParams.TurretSpeed = 0.01f;
                    AiParams.AimOffset = MathHelper.ToRadians(170);
                    AiParams.Inaccuracy = 1.6f;

                    properties.TurningSpeed = 0f;
                    properties.MaximalTurn = 0;

                    properties.ShootStun = 20;
                    properties.ShellCooldown = 300;
                    properties.ShellLimit = 1;
                    properties.ShellSpeed = 3f;
                    properties.ShellType = ShellType.Standard;
                    properties.RicochetCount = 1;

                    properties.Invisible = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = 0;
                    properties.MaxSpeed = 0f;

                    properties.MineCooldown = 0;
                    properties.MineLimit = 0;
                    properties.MineStun = 0;

                    BaseExpValue = 0.01f;

                    break;

                case TankTier.Ash:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 15;
                    AiParams.TurretMeanderFrequency = 40;
                    AiParams.TurretSpeed = 0.01f;
                    AiParams.AimOffset = MathHelper.ToRadians(40);

                    AiParams.Inaccuracy = 0.9f;

                    properties.DestructionColor = Color.Gray;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 40;
                    AiParams.MineWarinessRadius_PlayerLaid = 40;

                    properties.TurningSpeed = 0.08f;
                    properties.MaximalTurn = MathHelper.ToRadians(10);

                    properties.ShootStun = 3;
                    properties.ShellCooldown = 180;
                    properties.ShellLimit = 1;
                    properties.ShellSpeed = 3f;
                    properties.ShellType = ShellType.Standard;
                    properties.RicochetCount = 1;

                    properties.Invisible = false;
                    properties.Stationary = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = 0.085f;
                    properties.MaxSpeed = 1.2f;

                    properties.MineCooldown = 0;
                    properties.MineLimit = 0;
                    properties.MineStun = 0;

                    AiParams.BlockWarinessDistance = 25;

                    BaseExpValue = 0.015f;
                    break;

                case TankTier.Marine:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 10;
                    AiParams.TurretSpeed = 0.1f;
                    AiParams.AimOffset = MathHelper.ToRadians(0);

                    properties.DestructionColor = Color.Teal;

                    AiParams.Inaccuracy = 0.15f;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 40;
                    AiParams.MineWarinessRadius_PlayerLaid = 80;

                    properties.TurningSpeed = 0.2f;
                    properties.MaximalTurn = MathHelper.ToRadians(10);

                    properties.ShootStun = 20;
                    properties.ShellCooldown = 180;
                    properties.ShellLimit = 1;
                    properties.ShellSpeed = 6f;
                    properties.ShellType = ShellType.Rocket;
                    properties.RicochetCount = 0;

                    properties.Invisible = false;
                    properties.Stationary = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = 0.085f;
                    properties.MaxSpeed = 1f;

                    properties.MineCooldown = 0;
                    properties.MineLimit = 0;
                    properties.MineStun = 0;

                    AiParams.BlockWarinessDistance = 40;

                    BaseExpValue = 0.04f;
                    break;

                case TankTier.Yellow:
                    AiParams.MeanderAngle = MathHelper.ToRadians(40);
                    AiParams.MeanderFrequency = 15;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.02f;
                    AiParams.AimOffset = 0.5f;

                    AiParams.Inaccuracy = 1.5f;

                    properties.Acceleration = 0.3f;
                    properties.Deceleration = 0.6f;

                    properties.DestructionColor = Color.Yellow;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 40;
                    AiParams.MineWarinessRadius_PlayerLaid = 160;

                    properties.TurningSpeed = 0.08f;
                    properties.MaximalTurn = MathHelper.ToRadians(10);

                    properties.ShootStun = 20;
                    properties.ShellCooldown = 90;
                    properties.ShellLimit = 1;
                    properties.ShellSpeed = 3f;
                    properties.ShellType = ShellType.Standard;
                    properties.RicochetCount = 1;

                    properties.Invisible = false;
                    properties.Stationary = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = 0.085f;
                    properties.MaxSpeed = 1.8f;

                    properties.MineCooldown = 600;
                    properties.MineLimit = 4;
                    properties.MineStun = 5;

                    AiParams.MinePlacementChance = 0.3f;

                    BaseExpValue = 0.035f;

                    if (Difficulties.Types["PieFactory"])
                    {
                        properties.VulnerableToMines = false;
                        properties.MineCooldown = 10;
                        properties.MineLimit = 20;
                        properties.MineStun = 0;
                        AiParams.MinePlacementChance = 1f;
                        AiParams.MineWarinessRadius_PlayerLaid = 0;
                    }
                    break;

                case TankTier.Pink:
                    AiParams.MeanderAngle = MathHelper.ToRadians(40);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 40;
                    AiParams.TurretSpeed = 0.03f;
                    AiParams.AimOffset = 0.2f;

                    AiParams.Inaccuracy = 1.3f;

                    properties.DestructionColor = Color.Pink;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 40;
                    AiParams.MineWarinessRadius_PlayerLaid = 160;

                    properties.TurningSpeed = 0.08f;
                    properties.MaximalTurn = MathHelper.ToRadians(10);

                    properties.ShootStun = 5;
                    properties.ShellCooldown = 30;
                    properties.ShellLimit = 3;
                    properties.ShellSpeed = 3f;
                    properties.ShellType = ShellType.Standard;
                    properties.RicochetCount = 1;

                    properties.Invisible = false;
                    properties.Stationary = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = 0.1f;
                    properties.MaxSpeed = 1.2f;

                    properties.MineCooldown = 0;
                    properties.MineLimit = 0;
                    properties.MineStun = 0;

                    AiParams.BlockWarinessDistance = 35;

                    BaseExpValue = 0.08f;
                    break;

                case TankTier.Violet:
                    AiParams.MeanderAngle = MathHelper.ToRadians(40);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 25;

                    AiParams.Inaccuracy = 0.8f;

                    properties.DestructionColor = Color.Purple;

                    AiParams.TurretSpeed = 0.03f;
                    AiParams.AimOffset = 0.18f;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 60;
                    AiParams.MineWarinessRadius_PlayerLaid = 160;

                    properties.TurningSpeed = 0.06f;
                    properties.MaximalTurn = MathHelper.ToRadians(10);

                    properties.ShootStun = 5;
                    properties.ShellCooldown = 30;
                    properties.ShellLimit = 5;
                    properties.ShellSpeed = 3f;
                    properties.ShellType = ShellType.Standard;
                    properties.RicochetCount = 1;

                    properties.Invisible = false;
                    properties.Stationary = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = -0.2f;
                    properties.MaxSpeed = 1.8f;
                    properties.Acceleration = 0.3f;

                    properties.MineCooldown = 700;
                    properties.MineLimit = 2;
                    properties.MineStun = 10;

                    AiParams.MinePlacementChance = 0.05f;

                    AiParams.BlockWarinessDistance = 45;

                    BaseExpValue = 0.1f;
                    break;

                case TankTier.Green:
                    properties.Stationary = true;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 70;

                    AiParams.TurretMeanderFrequency = 30;
                    AiParams.TurretSpeed = 0.02f;
                    AiParams.AimOffset = MathHelper.ToRadians(80);
                    AiParams.Inaccuracy = MathHelper.ToRadians(25);

                    properties.DestructionColor = Color.LimeGreen;

                    properties.TurningSpeed = 0f;
                    properties.MaximalTurn = 0;

                    properties.ShootStun = 5;
                    properties.ShellCooldown = 60;
                    properties.ShellLimit = 2;
                    properties.ShellSpeed = 6f; // 6f
                    properties.ShellType = ShellType.TrailedRocket;
                    properties.RicochetCount = 2; // 2

                    properties.Invisible = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = 0;
                    properties.MaxSpeed = 0f;

                    properties.MineCooldown = 0;
                    properties.MineLimit = 0;
                    properties.MineStun = 0;

                    BaseExpValue = 0.12f;
                    break;

                case TankTier.White:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.03f;
                    AiParams.AimOffset = MathHelper.ToRadians(40);

                    properties.Track = TrackType.Thick;

                    AiParams.Inaccuracy = 0.8f;

                    properties.DestructionColor = Color.White;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 40;
                    AiParams.MineWarinessRadius_PlayerLaid = 160;

                    properties.TurningSpeed = 0.08f;
                    properties.MaximalTurn = MathHelper.ToRadians(10);

                    properties.ShootStun = 5;
                    properties.ShellCooldown = 30;
                    properties.ShellLimit = 5;
                    properties.ShellSpeed = 3f;
                    properties.ShellType = ShellType.Standard;
                    properties.RicochetCount = 1;

                    properties.Stationary = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = -0.35f;
                    properties.MaxSpeed = 1.2f;
                    properties.Acceleration = 0.3f;

                    properties.MineCooldown = 1000;
                    properties.MineLimit = 2;
                    properties.MineStun = 8;

                    AiParams.MinePlacementChance = 0.08f;

                    AiParams.BlockWarinessDistance = 30;

                    properties.Invisible = true;

                    BaseExpValue = 0.125f;
                    break;

                case TankTier.Black:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.03f;
                    AiParams.AimOffset = MathHelper.ToRadians(5);

                    AiParams.Inaccuracy = 0.35f;

                    properties.DestructionColor = Color.Black;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 100;
                    AiParams.MineWarinessRadius_PlayerLaid = 60;

                    properties.TurningSpeed = 0.06f;
                    properties.MaximalTurn = MathHelper.ToRadians(5);

                    properties.ShootStun = 5;
                    properties.ShellCooldown = 90;
                    properties.ShellLimit = 3;
                    properties.ShellSpeed = 6f;
                    properties.ShellType = ShellType.Rocket;
                    properties.RicochetCount = 0;

                    properties.Invisible = false;
                    properties.Stationary = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = -0.26f;
                    properties.MaxSpeed = 2.4f;
                    properties.Acceleration = 0.3f;

                    properties.ShootPitch = -0.2f; // 0.2f

                    properties.MineCooldown = 850;
                    properties.MineLimit = 2;
                    properties.MineStun = 10;

                    AiParams.MinePlacementChance = 0.05f;

                    AiParams.BlockWarinessDistance = 60;

                    BaseExpValue = 0.145f;
                    break;
                #endregion
                #region MasterMod
                case TankTier.Bronze:
                    AiParams.TurretMeanderFrequency = 15;
                    AiParams.TurretSpeed = 0.05f;
                    AiParams.AimOffset = 0.005f;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 140;

                    AiParams.Inaccuracy = 0.2f;

                    properties.DestructionColor = new(152, 96, 26);

                    properties.ShellCooldown = 50;
                    properties.ShellLimit = 2;
                    properties.ShellSpeed = 3f;
                    properties.ShellType = ShellType.Standard;
                    properties.RicochetCount = 1;

                    properties.Invisible = false;
                    properties.Stationary = true;
                    properties.ShellHoming = new();

                    AiParams.MinePlacementChance = 0.05f;

                    BaseExpValue = 0.025f;
                    break;
                case TankTier.Silver:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 60;
                    AiParams.TurretSpeed = 0.045f;
                    AiParams.AimOffset = 0.9f;

                    AiParams.Inaccuracy = 0.4f;

                    properties.DestructionColor = Color.Silver;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 70;
                    AiParams.MineWarinessRadius_PlayerLaid = 140;

                    properties.TurningSpeed = 0.13f;
                    properties.MaximalTurn = MathHelper.PiOver2;

                    properties.ShootStun = 0;
                    properties.ShellCooldown = 15;
                    properties.ShellLimit = 8;
                    properties.ShellSpeed = 4f;
                    properties.ShellType = ShellType.Standard;
                    properties.RicochetCount = 1;

                    properties.Invisible = false;
                    properties.Stationary = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = 0.2f;
                    properties.MaxSpeed = 1.6f;
                    properties.Acceleration = 0.3f;
                    properties.Deceleration = 0.6f;

                    properties.MineCooldown = 60 * 20;
                    properties.MineLimit = 1;
                    properties.MineStun = 10;

                    AiParams.MinePlacementChance = 0.05f;

                    BaseExpValue = 0.07f;
                    break;
                case TankTier.Sapphire:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 15;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.025f;
                    AiParams.AimOffset = 0.01f;

                    AiParams.Inaccuracy = 0.4f;

                    properties.DestructionColor = Color.DeepSkyBlue;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 40;
                    AiParams.MineWarinessRadius_PlayerLaid = 70;

                    properties.TurningSpeed = 0.15f;
                    properties.MaximalTurn = MathHelper.PiOver2;

                    properties.ShootStun = 20;
                    properties.ShellCooldown = 10;
                    properties.ShellLimit = 3;
                    properties.ShellSpeed = 5.5f;
                    properties.ShellType = ShellType.Rocket;
                    properties.RicochetCount = 0;

                    properties.Invisible = false;
                    properties.Stationary = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = 0.08f;
                    properties.MaxSpeed = 1.4f;
                    properties.Acceleration = 0.3f;
                    properties.Deceleration = 0.6f;

                    properties.MineCooldown = 1000;
                    properties.MineLimit = 1;
                    properties.MineStun = 0;

                    AiParams.MinePlacementChance = 0.05f;

                    BaseExpValue = 0.095f;
                    break;
                case TankTier.Ruby:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.025f;
                    AiParams.AimOffset = 0.05f;

                    AiParams.Inaccuracy = 0.6f;

                    properties.DestructionColor = Color.IndianRed;

                    //AiParams.PursuitLevel = 0.1f;
                    //AiParams.PursuitFrequency = 30;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 50;
                    AiParams.MineWarinessRadius_PlayerLaid = 70;

                    properties.TurningSpeed = 0.5f;
                    properties.MaximalTurn = MathHelper.PiOver2;

                    properties.ShootStun = 0;
                    properties.ShellCooldown = 8;
                    properties.ShellLimit = 10;
                    properties.ShellSpeed = 3f;
                    properties.ShellType = ShellType.Standard;
                    properties.RicochetCount = 0;

                    properties.Invisible = false;
                    properties.Stationary = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = 0.08f;
                    properties.MaxSpeed = 1.2f;
                    properties.Acceleration = 0.4f;
                    properties.Deceleration = 0.6f;

                    properties.MineCooldown = 0;
                    properties.MineLimit = 0;
                    properties.MineStun = 0;

                    AiParams.MinePlacementChance = 0;

                    AiParams.BlockWarinessDistance = 30;

                    BaseExpValue = 0.13f;
                    break;
                case TankTier.Citrine:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 30;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.035f;
                    AiParams.AimOffset = 0.3f;

                    AiParams.Inaccuracy = 0.25f;

                    properties.DestructionColor = Color.Yellow;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 80;
                    AiParams.MineWarinessRadius_PlayerLaid = 140;

                    properties.TurningSpeed = 0.08f;
                    properties.MaximalTurn = 1.4f;

                    properties.ShootStun = 10;
                    properties.ShellCooldown = 60;
                    properties.ShellLimit = 3;
                    properties.ShellSpeed = 6f;
                    properties.ShellType = ShellType.Standard;
                    properties.RicochetCount = 0;

                    properties.Invisible = false;
                    properties.Stationary = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = -0.08f;
                    properties.MaxSpeed = 3.2f;
                    properties.Acceleration = 0.2f;
                    properties.Deceleration = 0.4f;

                    properties.MineCooldown = 360;
                    properties.MineLimit = 4;
                    properties.MineStun = 5;

                    AiParams.MinePlacementChance = 0.15f;

                    AiParams.ShootChance = 0.95f;

                    AiParams.BlockWarinessDistance = 60;

                    BaseExpValue = 0.09f;
                    break;
                case TankTier.Amethyst:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 5;
                    AiParams.TurretMeanderFrequency = 15;
                    AiParams.TurretSpeed = 0.05f;
                    AiParams.AimOffset = 0.3f;

                    AiParams.Inaccuracy = 0.65f;

                    properties.DestructionColor = Color.Purple;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 70;
                    AiParams.MineWarinessRadius_PlayerLaid = 140;

                    properties.TurningSpeed = 0.1f;
                    properties.MaximalTurn = MathHelper.ToRadians(30);

                    properties.ShootStun = 5;
                    properties.ShellCooldown = 25;
                    properties.ShellLimit = 5;
                    properties.ShellSpeed = 3.5f; // 3.5
                    properties.ShellType = ShellType.Standard;
                    properties.RicochetCount = 1; // 1

                    properties.Invisible = false;
                    properties.Stationary = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = -0.2f;
                    properties.MaxSpeed = 2f;
                    properties.Acceleration = 0.6f;
                    properties.Deceleration = 0.9f;

                    properties.MineCooldown = 360;
                    properties.MineLimit = 3;
                    properties.MineStun = 10;

                    AiParams.MinePlacementChance = 0.05f;

                    BaseExpValue = 0.095f;
                    break;
                case TankTier.Emerald:
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.04f;
                    AiParams.AimOffset = 1f;

                    AiParams.Inaccuracy = 0.35f;

                    properties.DestructionColor = Color.Green;

                    properties.ShellCooldown = 60;
                    properties.ShellLimit = 3;
                    properties.ShellSpeed = 8f;
                    properties.ShellType = ShellType.TrailedRocket;
                    properties.RicochetCount = 2;

                    properties.Stationary = true;
                    properties.Invisible = true;
                    properties.ShellHoming = new();

                    AiParams.SmartRicochets = true;

                    BaseExpValue = 0.14f;
                    break;

                case TankTier.Gold:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 20;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.02f;
                    AiParams.AimOffset = 0.14f;

                    AiParams.Inaccuracy = 0.4f;

                    properties.DestructionColor = Color.Gold;

                    AiParams.ShootChance = 0.7f;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 80;
                    AiParams.MineWarinessRadius_PlayerLaid = 120;

                    properties.CanLayTread = false;

                    properties.TurningSpeed = 0.06f;
                    properties.MaximalTurn = 1.4f;

                    properties.ShootStun = 5;
                    properties.ShellCooldown = 30;
                    properties.ShellLimit = 3;
                    properties.ShellSpeed = 4f;
                    properties.ShellType = ShellType.Standard;
                    properties.RicochetCount = 1;

                    properties.Stationary = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = -0.1f;
                    properties.MaxSpeed = 0.9f;
                    properties.Acceleration = 0.8f;
                    properties.Deceleration = 0.5f;

                    properties.MineCooldown = 700;
                    properties.MineLimit = 2;
                    properties.MineStun = 10;

                    AiParams.MinePlacementChance = 0.01f;

                    properties.Invisible = true;

                    BaseExpValue = 0.16f;
                    break;

                case TankTier.Obsidian:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 20;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.05f;
                    AiParams.AimOffset = 0.18f;

                    AiParams.Inaccuracy = 0.9f;

                    properties.DestructionColor = Color.Black;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 70;
                    AiParams.MineWarinessRadius_PlayerLaid = 140;

                    properties.TurningSpeed = 0.1f;
                    properties.MaximalTurn = MathHelper.PiOver4;

                    properties.ShootStun = 5;
                    properties.ShellCooldown = 25;
                    properties.ShellLimit = 3;
                    properties.ShellSpeed = 8.5f;
                    properties.ShellType = ShellType.Rocket;
                    properties.RicochetCount = 2;

                    properties.Invisible = false;
                    properties.Stationary = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = -0.26f;
                    properties.MaxSpeed = 3f;
                    properties.Acceleration = 0.6f;
                    properties.Deceleration = 0.8f;

                    properties.MineCooldown = 850;
                    properties.MineLimit = 2;
                    properties.MineStun = 10;

                    AiParams.MinePlacementChance = 0.1f;

                    AiParams.BlockWarinessDistance = 50;
                    AiParams.BlockReadTime = 30;

                    BaseExpValue = 0.175f;
                    break;
                #endregion
                #region AdvancedMod
                case TankTier.Granite:
                    AiParams.MeanderAngle = 0.8f;
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.09f;
                    AiParams.AimOffset = 0f;

                    AiParams.Inaccuracy = 0.5f;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 150;
                    AiParams.MineWarinessRadius_PlayerLaid = 90;

                    properties.TurningSpeed = 0.3f;
                    properties.MaximalTurn = MathHelper.PiOver4;

                    properties.ShootStun = 60;
                    properties.ShellCooldown = 40;
                    properties.ShellLimit = 2;
                    properties.ShellSpeed = 5f;
                    properties.ShellType = ShellType.Standard;
                    properties.RicochetCount = 1;

                    properties.Invisible = false;
                    properties.Stationary = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = 0.07f;
                    properties.MaxSpeed = 0.9f;
                    properties.Acceleration = 0.3f;
                    properties.Deceleration = 0.4f;

                    AiParams.SmartRicochets = true;

                    BaseExpValue = 0.02f;
                    break;
                case TankTier.Bubblegum:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.045f;
                    AiParams.AimOffset = MathHelper.ToRadians(30);

                    AiParams.Inaccuracy = 0.4f;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 140;
                    AiParams.MineWarinessRadius_PlayerLaid = 140;

                    properties.TurningSpeed = 0.1f;
                    properties.MaximalTurn = 0.5f;

                    properties.ShootStun = 0;
                    properties.ShellCooldown = 15;
                    properties.ShellLimit = 8;
                    properties.ShellSpeed = 4f;
                    properties.ShellType = ShellType.Standard;
                    properties.RicochetCount = 1;

                    properties.Invisible = false;
                    properties.Stationary = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = 0.08f;
                    properties.MaxSpeed = 1.3f;
                    properties.Acceleration = 0.3f;
                    properties.Deceleration = 0.6f;

                    properties.MineCooldown = 940;
                    properties.MineLimit = 1;
                    properties.MineStun = 5;

                    AiParams.MinePlacementChance = 0.02f;
                    AiParams.ShootChance = 0.2f;

                    BaseExpValue = 0.035f;
                    break;
                case TankTier.Water:
                    AiParams.MeanderAngle = 0.25f;
                    AiParams.MeanderFrequency = 15;
                    AiParams.TurretMeanderFrequency = 10;
                    AiParams.TurretSpeed = 0.03f;
                    AiParams.AimOffset = MathHelper.ToRadians(10);

                    AiParams.Inaccuracy = 0.5f;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 90;
                    AiParams.MineWarinessRadius_PlayerLaid = 150;

                    properties.TurningSpeed = 0.2f;
                    properties.MaximalTurn = MathHelper.PiOver4;

                    properties.ShootStun = 20;
                    properties.ShellCooldown = 25;
                    properties.ShellLimit = 2;
                    properties.ShellSpeed = 5.5f;
                    properties.ShellType = ShellType.Rocket;
                    properties.RicochetCount = 1;

                    properties.Invisible = false;
                    properties.Stationary = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = 0.14f;
                    properties.MaxSpeed = 1.7f;
                    properties.Acceleration = 0.4f;
                    properties.Deceleration = 0.6f;

                    BaseExpValue = 0.08f;
                    break;
                case TankTier.Crimson:
                    AiParams.MeanderAngle = 0.12f;
                    AiParams.MeanderFrequency = 8;
                    AiParams.TurretMeanderFrequency = 60;
                    AiParams.TurretSpeed = 0.07f;
                    AiParams.AimOffset = 0.04f;

                    AiParams.Inaccuracy = 0.2f;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 50;
                    AiParams.MineWarinessRadius_PlayerLaid = 50;

                    properties.TurningSpeed = 0.1f;
                    properties.MaximalTurn = 0.5f;

                    properties.ShootStun = 1;
                    properties.ShellCooldown = 5;
                    properties.ShellLimit = 5;
                    properties.ShellSpeed = 3f;
                    properties.ShellType = ShellType.Standard;
                    properties.RicochetCount = 0;

                    properties.Invisible = false;
                    properties.Stationary = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = 0.08f;
                    properties.MaxSpeed = 1.3f;
                    properties.Acceleration = 0.6f;
                    properties.Deceleration = 0.8f;

                    properties.MineCooldown = 340;
                    properties.MineLimit = 6;
                    properties.MineStun = 3;

                    AiParams.MinePlacementChance = 0.02f;
                    AiParams.ShootChance = 0.2f;

                    BaseExpValue = 0.095f;
                    break;
                case TankTier.Tiger:
                    AiParams.MeanderAngle = 0.30f;
                    AiParams.MeanderFrequency = 2;
                    AiParams.TurretMeanderFrequency = 40;
                    AiParams.TurretSpeed = 0.1f;
                    AiParams.AimOffset = 0.12f;

                    AiParams.Inaccuracy = 0.7f;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 90;
                    AiParams.MineWarinessRadius_PlayerLaid = 120;

                    properties.TurningSpeed = 0.1f;
                    properties.MaximalTurn = MathHelper.PiOver2;

                    properties.ShootStun = 0;
                    properties.ShellCooldown = 20;
                    properties.ShellLimit = 4;
                    properties.ShellSpeed = 4f;
                    properties.ShellType = ShellType.Standard;
                    properties.RicochetCount = 1;

                    properties.Invisible = false;
                    properties.Stationary = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = 0.14f;
                    properties.MaxSpeed = 2f;
                    properties.Acceleration = 0.6f;
                    properties.Deceleration = 0.8f;

                    properties.MineCooldown = 1;
                    properties.MineLimit = 10;
                    properties.MineStun = 0;

                    AiParams.MinePlacementChance = 0.05f;

                    BaseExpValue = 0.1f;
                    break;
                case TankTier.Fade:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 8;
                    AiParams.TurretMeanderFrequency = 40;
                    AiParams.TurretSpeed = 0.05f;
                    AiParams.AimOffset = 0.22f;

                    AiParams.Inaccuracy = 1.1f;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 100;
                    AiParams.MineWarinessRadius_PlayerLaid = 100;

                    properties.TurningSpeed = 0.12f;
                    properties.MaximalTurn = MathHelper.ToRadians(30);

                    properties.ShootStun = 5;
                    properties.ShellCooldown = 25;
                    properties.ShellLimit = 5;
                    properties.ShellSpeed = 3.5f;
                    properties.ShellType = ShellType.Standard;
                    properties.RicochetCount = 1;

                    properties.Invisible = false;
                    properties.Stationary = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = -0.2f;
                    properties.MaxSpeed = 1.9f;
                    properties.Acceleration = 0.6f;
                    properties.Deceleration = 0.9f;

                    properties.MineCooldown = 680;
                    properties.MineLimit = 3;
                    properties.MineStun = 10;

                    AiParams.MinePlacementChance = 0.05f;

                    BaseExpValue = 0.105f;
                    break;
                case TankTier.Creeper:
                    AiParams.MeanderAngle = 0.2f;
                    AiParams.MeanderFrequency = 25;
                    AiParams.TurretMeanderFrequency = 40;
                    AiParams.TurretSpeed = 0.085f;
                    AiParams.AimOffset = 1f;

                    AiParams.SmartRicochets = true;

                    AiParams.Inaccuracy = 0.6f;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 150;
                    AiParams.MineWarinessRadius_PlayerLaid = 110;

                    properties.TurningSpeed = 0.3f;
                    properties.MaximalTurn = MathHelper.PiOver4;

                    properties.ShootStun = 20;
                    properties.ShellCooldown = 60;
                    properties.ShellLimit = 2;
                    properties.ShellSpeed = 8f;
                    properties.ShellType = ShellType.TrailedRocket;
                    properties.RicochetCount = 3;

                    properties.Invisible = false;
                    properties.Stationary = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = -0.07f;
                    properties.MaxSpeed = 1f;
                    properties.Acceleration = 0.3f;
                    properties.Deceleration = 0.4f;

                    BaseExpValue = 0.17f;
                    break;
                case TankTier.Gamma:
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.08f;
                    AiParams.AimOffset = 0.01f;

                    AiParams.Inaccuracy = 0.15f;

                    properties.Invisible = false;
                    properties.ShellHoming = new();

                    properties.ShellCooldown = 40;
                    properties.ShellLimit = 6;
                    properties.ShellSpeed = 12.5f;
                    properties.ShellType = ShellType.Standard;
                    properties.RicochetCount = 0;

                    properties.Stationary = true;

                    BaseExpValue = 0.13f;
                    break;
                case TankTier.Marble:
                    AiParams.MeanderAngle = MathHelper.PiOver2;
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.08f;
                    AiParams.AimOffset = 0.11f;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 70;
                    AiParams.MineWarinessRadius_PlayerLaid = 140;

                    properties.TurningSpeed = 0.1f;
                    properties.MaximalTurn = MathHelper.PiOver4;

                    AiParams.Inaccuracy = 0.6f;

                    properties.ShootStun = 5;
                    properties.ShellCooldown = 25;
                    properties.ShellLimit = 3;
                    properties.ShellSpeed = 10f;
                    properties.ShellType = ShellType.Rocket;
                    properties.RicochetCount = 1;

                    properties.TreadPitch = -0.26f;
                    properties.MaxSpeed = 2.6f;
                    properties.Acceleration = 0.6f;
                    properties.Deceleration = 0.8f;

                    properties.Stationary = false;
                    properties.Invisible = false;
                    properties.ShellHoming = new();

                    properties.MineCooldown = 850;
                    properties.MineLimit = 2;
                    properties.MineStun = 10;

                    AiParams.MinePlacementChance = 0.05f;

                    BaseExpValue = 0.195f;
                    break;
                #endregion
                // unimplemented XP values
                #region Special
                case TankTier.Explosive:
                    properties.Armor = new(this, 3);
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 60;
                    AiParams.TurretSpeed = 0.045f;
                    AiParams.AimOffset = 0.04f;

                    AiParams.Inaccuracy = 1.2f;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 140;
                    AiParams.MineWarinessRadius_PlayerLaid = 140;

                    properties.TurningSpeed = 0.1f;
                    properties.MaximalTurn = 0.4f;

                    properties.ShootStun = 0;
                    properties.ShellCooldown = 90;
                    properties.ShellLimit = 2;
                    properties.ShellSpeed = 2f;
                    properties.ShellType = ShellType.Explosive;
                    properties.RicochetCount = 0;

                    properties.ShootPitch = -0.1f;

                    properties.Invisible = false;
                    properties.Stationary = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = -0.8f;
                    properties.MaxSpeed = 0.8f;
                    properties.Acceleration = 0.3f;
                    properties.Deceleration = 0.6f;

                    properties.MineCooldown = 940;
                    properties.MineLimit = 1;
                    properties.MineStun = 5;

                    AiParams.MinePlacementChance = 0.02f;
                    AiParams.ShootChance = 0.2f;
                    break;

                case TankTier.Electro:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 60;
                    AiParams.TurretSpeed = 0.045f;
                    AiParams.AimOffset = 0.04f;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 140;
                    AiParams.MineWarinessRadius_PlayerLaid = 140;

                    properties.TurningSpeed = 0.1f;
                    properties.MaximalTurn = 0.5f;

                    properties.ShootStun = 0;
                    properties.ShellCooldown = 15;
                    properties.ShellLimit = 8;
                    properties.ShellSpeed = 4f;
                    properties.ShellType = ShellType.Standard;
                    properties.RicochetCount = 1;

                    properties.Invisible = false;
                    properties.Stationary = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = 0.08f;
                    properties.MaxSpeed = 1.3f;
                    properties.Acceleration = 0.3f;
                    properties.Deceleration = 0.6f;

                    properties.MineCooldown = 940;
                    properties.MineLimit = 1;
                    properties.MineStun = 5;

                    AiParams.MinePlacementChance = 0.02f;
                    AiParams.ShootChance = 0.2f;
                    break;

                case TankTier.RocketDefender:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 60;
                    AiParams.TurretSpeed = 0.045f;
                    AiParams.AimOffset = 0.04f;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 140;
                    AiParams.MineWarinessRadius_PlayerLaid = 140;

                    properties.TurningSpeed = 0.1f;
                    properties.MaximalTurn = 0.5f;

                    properties.ShootStun = 0;
                    properties.ShellCooldown = 15;
                    properties.ShellLimit = 8;
                    properties.ShellSpeed = 4f;
                    properties.ShellType = ShellType.Standard;
                    properties.RicochetCount = 1;

                    properties.Invisible = false;
                    properties.Stationary = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = 0.08f;
                    properties.MaxSpeed = 1.3f;
                    properties.Acceleration = 0.3f;
                    properties.Deceleration = 0.6f;

                    properties.MineCooldown = 940;
                    properties.MineLimit = 1;
                    properties.MineStun = 5;

                    AiParams.MinePlacementChance = 0.02f;
                    AiParams.ShootChance = 0.2f;
                    break;

                case TankTier.Assassin:
                    AiParams.MeanderAngle = MathHelper.ToRadians(40);
                    AiParams.MeanderFrequency = 15;
                    AiParams.TurretMeanderFrequency = 1;
                    AiParams.TurretSpeed = 0.1f;
                    AiParams.AimOffset = 0f;

                    AiParams.Inaccuracy = 0.25f;
                    AiParams.BounceReset = false;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 140;
                    AiParams.MineWarinessRadius_PlayerLaid = 140;

                    properties.TurningSpeed = 0.1f;
                    properties.MaximalTurn = 0.2f;

                    properties.ShootStun = 25;
                    properties.ShellCooldown = 100;
                    properties.ShellLimit = 1;
                    properties.ShellSpeed = 9.5f;
                    properties.ShellType = ShellType.Supressed;
                    properties.RicochetCount = 1;

                    properties.Invisible = false;
                    properties.Stationary = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = -0.4f;
                    properties.MaxSpeed = 1.2f;
                    properties.Acceleration = 0.3f;
                    properties.Deceleration = 0.6f;

                    properties.MineCooldown = 0;
                    properties.MineLimit = 0;
                    properties.MineStun = 0;

                    AiParams.MinePlacementChance = 0.02f;
                    AiParams.ShootChance = 0.2f;

                    AiParams.SmartRicochets = true;
                    break;
                case TankTier.Cherry:
                    AiParams.MeanderAngle = MathHelper.ToRadians(20);
                    AiParams.MeanderFrequency = 15;
                    AiParams.TurretMeanderFrequency = 10;
                    AiParams.TurretSpeed = 0.1f;
                    AiParams.AimOffset = 0.08f;

                    AiParams.Inaccuracy = 0.4f;
                    AiParams.BounceReset = false;

                    AiParams.ProjectileWarinessRadius_PlayerShot = 140;
                    AiParams.MineWarinessRadius_PlayerLaid = 140;

                    properties.TurningSpeed = 0.1f;
                    properties.MaximalTurn = 0.2f;

                    properties.ShootStun = 0;
                    properties.ShellCooldown = 10;
                    properties.ShellLimit = 10;
                    properties.ShellSpeed = 6f;
                    properties.ShellType = ShellType.Rocket;
                    properties.RicochetCount = 0;

                    properties.Invisible = false;
                    properties.Stationary = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = -0.1f;
                    properties.MaxSpeed = 1.35f;
                    properties.Acceleration = 0.3f;
                    properties.Deceleration = 0.6f;

                    properties.MineCooldown = 0;
                    properties.MineLimit = 0;
                    properties.MineStun = 0;

                    AiParams.MinePlacementChance = 0.02f;
                    AiParams.ShootChance = 0.2f;

                    AiParams.SmartRicochets = false;
                    break;

                case TankTier.Commando:
                    properties.Armor = new(this, 3);
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 15;
                    AiParams.TurretSpeed = 0.05f;
                    AiParams.AimOffset = 0.03f;
                    AiParams.Inaccuracy = MathHelper.ToRadians(10);

                    AiParams.ProjectileWarinessRadius_PlayerShot = 140;
                    AiParams.MineWarinessRadius_PlayerLaid = 140;

                    properties.TurningSpeed = 0.05f;
                    properties.MaximalTurn = MathHelper.ToRadians(20);

                    properties.ShootStun = 25;
                    properties.ShellCooldown = 50;
                    properties.ShellLimit = 1;
                    properties.ShellSpeed = 6f;
                    properties.ShellType = ShellType.TrailedRocket;
                    properties.RicochetCount = 0;

                    properties.Invisible = false;
                    properties.Stationary = false;
                    properties.ShellHoming = new();

                    properties.TreadPitch = -0.08f;
                    properties.MaxSpeed = 1.4f;
                    properties.Acceleration = 0.3f;
                    properties.Deceleration = 0.6f;

                    properties.MineCooldown = 0;
                    properties.MineLimit = 0;
                    properties.MineStun = 0;

                    AiParams.MinePlacementChance = 0.02f;
                    AiParams.ShootChance = 0.2f;
                    break;
                    #endregion
            }
            if (Difficulties.Types["TanksAreCalculators"])
                if (properties.RicochetCount >= 1)
                    if (properties.HasTurret)
                        AiParams.SmartRicochets = true;

            if (Difficulties.Types["UltraMines"])
                AiParams.MineWarinessRadius_PlayerLaid *= 3;

            if (Difficulties.Types["AllInvisible"])
            {
                properties.Invisible = true;
                properties.CanLayTread = false;
            }
            if (Difficulties.Types["AllStationary"])
                properties.Stationary = true;

            if (Difficulties.Types["AllHoming"])
            {
                properties.ShellHoming = new();
                properties.ShellHoming.Radius = 200f;
                properties.ShellHoming.Speed = properties.ShellSpeed;
                properties.ShellHoming.Power = 0.1f * properties.ShellSpeed;
                // ShellHoming.isHeatSeeking = true;

                AiParams.Inaccuracy *= 4;
            }

            if (Difficulties.Types["BulletBlocking"])
                AiParams.DeflectsBullets = true;

            if (Difficulties.Types["Armored"])
            {
                if (properties.Armor is null)
                    properties.Armor = new(this, 3);
                else
                    properties.Armor = new(this, properties.Armor.HitPoints + 3);
            }

            if (Difficulties.Types["Predictions"])
                AiParams.PredictsPositions = true;

            /*foreach (var aifld in AiParams.GetType().GetProperties())
                if (aifld.GetValue(AiParams) is int)
                    aifld.SetValue(AiParams, GameHandler.GameRand.Next(1, 60));
                else if (aifld.GetValue(AiParams) is float)
                    aifld.SetValue(AiParams, GameHandler.GameRand.NextFloat(0.01f, 2f));
                else if (aifld.GetValue(AiParams) is bool)
                    aifld.SetValue(AiParams, GameHandler.GameRand.Next(0, 2) == 0);
            foreach (var fld in GetType().GetProperties())
            {
                if (fld.SetMethod != null && fld == typeof(Enum) && !fld.Name.ToLower().Contains("behavior") && !fld.Name.Contains("Id"))
                {
                    if (fld.GetValue(this) is int)
                        fld.SetValue(this, GameHandler.GameRand.Next(1, 60));
                    else if (fld.GetValue(this) is float)
                        fld.SetValue(this, GameHandler.GameRand.NextFloat(0.01f, 60));
                    else if (fld.GetValue(this) is bool && fld.Name != "Dead")
                        fld.SetValue(this, GameHandler.GameRand.Next(0, 2) == 0);
                }
            }

            foreach (var fld in Properties.GetType().GetProperties())
            {
                if (fld.GetValue(Properties) is int)
                    fld.SetValue(Properties, GameHandler.GameRand.Next(1, 60));
                else if (fld.GetValue(Properties) is float)
                    fld.SetValue(Properties, GameHandler.GameRand.NextFloat(0.01f, 60));
                else if (fld.GetValue(Properties) is bool && fld.Name != "Immortal")
                    fld.SetValue(Properties, GameHandler.GameRand.Next(0, 2) == 0);
            }*/
            base.ApplyDefaults(ref properties);
        }
        public override void Update()
        {
            base.Update();

            CannonMesh.ParentBone.Transform = Matrix.CreateRotationY(TurretRotation + TankRotation + (Flip ? MathHelper.Pi : 0));

            if (Tier == TankTier.Commando)
            {
                Model.Meshes["Laser_Beam"].ParentBone.Transform = Matrix.CreateRotationY(TurretRotation + TankRotation + (Flip ? MathHelper.Pi : 0));
                Model.Meshes["Barrel_Laser"].ParentBone.Transform = Matrix.CreateRotationY(TurretRotation + TankRotation + (Flip ? MathHelper.Pi : 0));
                Model.Meshes["Dish"].ParentBone.Transform = Matrix.CreateRotationY(TurretRotation + TankRotation + (Flip ? MathHelper.Pi : 0));
            }

            if (!Dead && IsIngame)
            {

                // TargetTankRotation = (GeometryUtils.ConvertWorldToScreen(Vector3.Zero, World, View, Projection) - GameUtils.MousePosition).ToRotation() - MathHelper.PiOver2;

                timeSinceLastAction++;

                if (!GameProperties.InMission || IntermissionSystem.IsAwaitingNewMission || LevelEditor.Active)
                    Velocity = Vector2.Zero;
                else
                    DoAi(true, true, true);
            }

            _oldPosition = Position;

            Model.Root.Transform = World;

            Model.CopyAbsoluteBoneTransformsTo(boneTransforms);
        }

        public override void Remove()
        {
            GameProperties.OnMissionStart -= OnMissionStart;
            Dead = true;
            Behaviors = null;
            SpecialBehaviors = null;
            GameHandler.AllAITanks[AITankId] = null;
            GameHandler.AllTanks[WorldId] = null;
            base.Remove();
        }
        public override void Destroy(ITankHurtContext context)
        {
            if (!Client.IsConnected())
            {
                PlayerTank.KillCount++;

                if (!PlayerTank.TanksKillDict.ContainsKey(Tier))
                    PlayerTank.TanksKillDict.Add(Tier, 1);
                else
                    PlayerTank.TanksKillDict[Tier]++;

                if (context.IsPlayer)
                {
                    if (context is TankHurtContext_Bullet cxt1)
                    {
                        //if (cxt.Bounces > 0)
                        TankGame.GameData.BulletKills++;
                        TankGame.GameData.TotalKills++;

                        if (cxt1.Bounces > 0)
                            TankGame.GameData.BounceKills++;

                    }
                    if (context is TankHurtContext_Mine cxt2)
                    {
                        TankGame.GameData.MineKills++;
                        TankGame.GameData.TotalKills++;
                    }

                    TankGame.GameData.TankKills[Tier]++;

                    // haaaaaaarddddddcode
                    if (!LevelEditor.Editing)
                    {
                        var rand = GameHandler.GameRand.NextFloat(-(BaseExpValue * 0.25f), BaseExpValue * 0.25f);
                        var gain = BaseExpValue + rand;
                        // i will keep this commented if anything else happens.
                        //var gain = (BaseExpValue + rand) * GameData.UniversalExpMultiplier;
                        TankGame.GameData.ExpLevel += gain;


                        var p = ParticleSystem.MakeParticle(Position3D + new Vector3(0, 30, 0), $"+{gain * 100:0.00} XP");

                        p.Scale = new(0.5f);
                        p.Roll = MathHelper.Pi;
                        p.Origin2D = TankGame.TextFont.MeasureString($"+{gain * 100:0.00} XP") / 2;

                        p.UniqueBehavior = (p) =>
                        {
                            p.Position.Y += 0.1f;

                            p.Alpha -= 0.01f;

                            if (p.Alpha <= 0)
                                p.Destroy();
                        };
                    }
                }
            }
            else
            {
                    // check if player id matches client id, if so, increment that player's kill count, then sync to the server
                    // TODO: convert TankHurtContext into a struct and use it here
                    // Will be used to track the reason of death and who caused the death, if any tank owns a shell or mine
                    //
                    // if (context.PlayerId == Client.PlayerId)
                    // {
                    //    PlayerTank.KillCount++;
                    //    Client.Send(new TankKillCountUpdateMessage(PlayerTank.KillCount)); // not a bad idea actually
            }
            GameHandler.AllAITanks[AITankId] = null;
            GameHandler.AllTanks[WorldId] = null;
            base.Destroy(context);
        }
        
        public bool pathBlocked;

        public bool isEnemySpotted;

        private bool seeks;
        private float seekRotation = 0;

        public bool nearDestructibleObstacle;

        // make a new method for just any rectangle
        private List<Tank> GetTanksInPath(Vector2 pathDir, out Vector2 rayEndpoint, bool draw = false, Vector2 offset = default, float missDist = 0f, Func<Block, bool> pattern = null, bool doBounceReset = true)
        {
            rayEndpoint = new(-999999, -999999);
            List<Tank> tanks = new();
            if (pattern is null)
                pattern = (c) => c.IsSolid || c.Type == Block.BlockType.Teleporter;

            const int MAX_PATH_UNITS = 1000;
            const int PATH_UNIT_LENGTH = 8;

            // 20, 30

            var whitePixel = GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel");
            var pathPos = Position + offset.RotatedByRadians(-TurretRotation);

            pathDir.Y *= -1;
            pathDir *= PATH_UNIT_LENGTH;
            int pathRicochetCount = 0;

            int uninterruptedIterations = 0;

            bool goneThroughTeleporter = false;
            int tpidx = -1;
            Vector2 tpos = Vector2.Zero;

            for (int i = 0; i < MAX_PATH_UNITS; i++)
            {
                var dummyPos = Vector2.Zero;

                uninterruptedIterations++;

                if (pathPos.X < MapRenderer.MIN_X || pathPos.X > MapRenderer.MAX_X)
                {
                    pathDir.X *= -1;
                    pathRicochetCount++;
                    resetIterations();
                }
                if (pathPos.Y < MapRenderer.MIN_Y || pathPos.Y > MapRenderer.MAX_Y)
                {
                    pathDir.Y *= -1;
                    pathRicochetCount++;
                    resetIterations();
                }

                var pathHitbox = new Rectangle((int)pathPos.X - 5, (int)pathPos.Y - 5, 8, 8);

                // Why is velocity passed by reference here lol
                Collision.HandleCollisionSimple_ForBlocks(pathHitbox, pathDir, ref dummyPos, out var dir, out var block, out bool corner, false, pattern);
                if (corner)
                    return tanks;

                if (block is not null)
                {
                    if (block.Type == Block.BlockType.Teleporter)
                    {
                        if (!goneThroughTeleporter)
                        {
                            var otherTp = Block.AllBlocks.FirstOrDefault(bl => bl != null && bl != block && bl.TpLink == block.TpLink);

                            if (Array.IndexOf(Block.AllBlocks, otherTp) > -1)
                            {
                                //pathPos = otherTp.Position;
                                tpos = otherTp.Position;
                                goneThroughTeleporter = true;
                                tpidx = i + 1;
                            }
                        }
                    }
                    else
                    {
                        switch (dir)
                        {
                            case CollisionDirection.Up:
                            case CollisionDirection.Down:
                                pathDir.Y *= -1;
                                pathRicochetCount++;
                                resetIterations();
                                break;
                            case CollisionDirection.Left:
                            case CollisionDirection.Right:
                                pathDir.X *= -1;
                                pathRicochetCount++;
                                resetIterations();
                                break;
                        }
                    }
                }

                if (goneThroughTeleporter && i == tpidx)
                {
                    pathPos = tpos;
                }

                void resetIterations() { if (doBounceReset) uninterruptedIterations = 0; }

                if (i == 0 && Block.AllBlocks.Any(x => x is not null && x.Hitbox.Intersects(pathHitbox) && pattern is not null ? pattern.Invoke(x) : false))
                {
                    rayEndpoint = pathPos;
                    return tanks;
                }

                if (i < (int)Properties.ShellSpeed / 2 && pathRicochetCount > 0)
                {
                    rayEndpoint = pathPos;
                    return tanks;
                }

                if (pathRicochetCount > Properties.RicochetCount)
                {
                    rayEndpoint = pathPos;
                    return tanks;
                }

                pathPos += pathDir;
                var realMiss = 1f + (missDist * uninterruptedIterations);
                if (draw)
                {
                    var pathPosScreen = GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(pathPos.X, 11, pathPos.Y), TankGame.GameView, TankGame.GameProjection);
                    TankGame.SpriteRenderer.Draw(whitePixel, pathPosScreen, null, Color.White * 0.9f, 0, whitePixel.Size() / 2, /*2 + (float)Math.Sin(i * Math.PI / 5 - TankGame.GameUpdateTime * 0.1f) * */realMiss, default, default);
                    // DebugUtils.DrawDebugString(TankGame.spriteBatch, $"{goneThroughTeleporter}:{(block is not null ? $"{block.Type}" : "N/A")}", GeometryUtils.ConvertWorldToScreen(new Vector3(0, 11, 0), Matrix.CreateTranslation(pathPos.X, 0, pathPos.Y), View, Projection), 1, centered: true);
                }

                foreach (var enemy in GameHandler.AllTanks)
                    if (enemy is not null)
                    {
                        if (!tanks.Contains(enemy))
                        {
                            if (i > 15)
                            {
                                if (GameUtils.Distance_WiiTanksUnits(enemy.Position, pathPos) <= realMiss)
                                    tanks.Add(enemy);
                            }
                            else if (enemy.CollisionBox.Intersects(pathHitbox))
                            {
                                tanks.Add(enemy);
                            }
                        }
                    }

            }
            return tanks;
        }

        private bool IsObstacleInWay(int checkDist, Vector2 pathDir, out Vector2 endpoint, out Vector2[] reflectPoints, bool draw = false)
        {
            const int PATH_UNIT_LENGTH = 1;

            bool hasCollided = false;

            var list = new List<Vector2>();

            var whitePixel = GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel");
            var pathPos = Position;

            pathDir.Y *= -1;
            pathDir *= PATH_UNIT_LENGTH;

            for (int i = 0; i < checkDist; i++)
            {
                var dummyPos = Vector2.Zero;

                if (pathPos.X < MapRenderer.MIN_X || pathPos.X > MapRenderer.MAX_X)
                {
                    pathDir.X *= -1;
                    hasCollided = true;
                    list.Add(pathPos);
                }
                if (pathPos.Y < MapRenderer.MIN_Y || pathPos.Y > MapRenderer.MAX_Y)
                {
                    pathDir.Y *= -1;
                    hasCollided = true;
                    list.Add(pathPos);
                }

                var pathHitbox = new Rectangle((int)pathPos.X, (int)pathPos.Y, 1, 1);

                // Why is velocity passed by reference here lol
                Collision.HandleCollisionSimple_ForBlocks(pathHitbox, pathDir, ref dummyPos, out var dir, out var block, out bool corner, false, null);

                switch (dir)
                {
                    case CollisionDirection.Up:
                    case CollisionDirection.Down:
                        hasCollided = true;
                        pathDir.Y *= -1;
                        list.Add(pathPos);
                        break;
                    case CollisionDirection.Left:
                    case CollisionDirection.Right:
                        pathDir.X *= -1;
                        hasCollided = true;
                        list.Add(pathPos);
                        break;
                }

                pathPos += pathDir;

                if (draw)
                {
                    var pathPosScreen = GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(pathPos.X, 11, pathPos.Y), TankGame.GameView, TankGame.GameProjection);
                    TankGame.SpriteRenderer.Draw(whitePixel, pathPosScreen, null, Color.White, 0, whitePixel.Size() / 2, 2 + (float)Math.Sin(i * Math.PI / 5 - TankGame.GameUpdateTime * 0.3f), default, default);
                }
            }
            reflectPoints = list.ToArray();
            endpoint = pathDir;
            return hasCollided;
        }

        private bool _predicts;

        public void UpdateAim(List<Tank> tanksNear, bool fireWhen)
        {
            _predicts = false;
            SeesTarget = false;

            bool tooCloseToExplosiveShell = false;

            List<Tank> tanksDef;

            if (Properties.ShellType == ShellType.Explosive)
            {
                tanksDef = GetTanksInPath(Vector2.UnitY.RotatedByRadians(TurretRotation - MathHelper.Pi), out var rayEndpoint, offset: Vector2.UnitY * 20, pattern: x => (!x.IsDestructible && x.IsSolid) || x.Type == Block.BlockType.Teleporter, missDist: AiParams.Inaccuracy, doBounceReset: AiParams.BounceReset);
                if (GameUtils.Distance_WiiTanksUnits(rayEndpoint, Position) < 150f) // TODO: change from hardcode to normalcode :YES:
                    tooCloseToExplosiveShell = true;
            }
            else
                tanksDef = GetTanksInPath( 
                    Vector2.UnitY.RotatedByRadians(TurretRotation - MathHelper.Pi), 
                    out var rayEndpoint, offset: Vector2.UnitY * 20,
                    missDist: AiParams.Inaccuracy, doBounceReset: AiParams.BounceReset);
            if (AiParams.PredictsPositions)
            {
                if (TargetTank is not null)
                {
                    var calculation = Position.Distance(TargetTank.Position) / (float)(Properties.ShellSpeed * 1.2f);
                    float rot = -GameUtils.DirectionOf(Position,
                        GeometryUtils.PredictFuturePosition(TargetTank.Position, TargetTank.Velocity, calculation))
                        .ToRotation() - MathHelper.PiOver2;

                    tanksDef = GetTanksInPath(
                    Vector2.UnitY.RotatedByRadians(-GameUtils.DirectionOf(Position, TargetTank.Position).ToRotation() - MathHelper.PiOver2),
                    out var rayEndpoint, offset: AiParams.PredictsPositions ? Vector2.Zero : Vector2.UnitY * 20,
                    missDist: AiParams.Inaccuracy, doBounceReset: AiParams.BounceReset);

                    var targ = GeometryUtils.PredictFuturePosition(TargetTank.Position, TargetTank.Velocity, calculation);
                    var posPredict = GetTanksInPath(Vector2.UnitY.RotatedByRadians(rot), out var rayEndpoint2, offset: Vector2.UnitY * 20, missDist: AiParams.Inaccuracy, doBounceReset: AiParams.BounceReset);

                    if (tanksDef.Contains(TargetTank))
                    {
                        _predicts = true;
                        TargetTurretRotation = rot + MathHelper.Pi;
                    }
                }
            }
            var findsEnemy = tanksDef.Any(tnk => tnk is not null && (tnk.Team != Team || tnk.Team == TankTeam.NoTeam) && tnk != this);
            var findsSelf = tanksDef.Any(tnk => tnk is not null && tnk == this);
            var findsFriendly = tanksDef.Any(tnk => tnk is not null && (tnk.Team == Team && tnk.Team != TankTeam.NoTeam));

            if (findsEnemy && !tooCloseToExplosiveShell)
                SeesTarget = true;

            // ChatSystem.SendMessage($"tier: {tier} | enemy: {findsEnemy} | self: {findsSelf} | friendly: {findsFriendly} | Count: {tanksDef.Count}", Color.White);

            if (AiParams.SmartRicochets)
            {
                //if (!seeks)
                seekRotation += AiParams.TurretSpeed;
                var canShoot = !(CurShootCooldown > 0 || OwnedShellCount >= Properties.ShellLimit);
                if (canShoot)
                {
                    var tanks = GetTanksInPath(Vector2.UnitY.RotatedByRadians(seekRotation), out var rayEndpoint, false, default, AiParams.Inaccuracy, doBounceReset: AiParams.BounceReset);

                    var findsEnemy2 = tanks.Any(tnk => tnk is not null && (tnk.Team != Team || tnk.Team == TankTeam.NoTeam) && tnk != this);
                    // var findsSelf2 = tanks.Any(tnk => tnk is not null && tnk == this);
                    var findsFriendly2 = tanks.Any(tnk => tnk is not null && (tnk.Team == Team && tnk.Team != TankTeam.NoTeam));
                    // ChatSystem.SendMessage($"{findsEnemy2} {findsFriendly2} | seek: {seeks}", Color.White);
                    if (findsEnemy2/* && !findsFriendly2*/)
                    {
                        seeks = true;
                        TurretRotationMultiplier = 3f;
                        TargetTurretRotation = seekRotation - MathHelper.Pi;
                    }
                }

                if (TurretRotation == TargetTurretRotation || !canShoot)
                    seeks = false;
            }

            bool checkNoTeam = Team == TankTeam.NoTeam || !tanksNear.Any(x => x.Team == Team);

            if (AiParams.PredictsPositions)
            {
                if (SeesTarget && checkNoTeam && fireWhen)
                    if (CurShootCooldown <= 0)
                    {
                        Shoot();
                    }
            }
            else
            {
                if (SeesTarget && checkNoTeam && !findsSelf && !findsFriendly && fireWhen)
                    if (CurShootCooldown <= 0)
                    {
                        Shoot();
                    }
            }
        }

        public Tank TargetTank;

        public float TurretRotationMultiplier = 1f;

        public void DoAi(bool doMoveTowards = true, bool doMovements = true, bool doFire = true)
        {
            TurretRotationMultiplier = 1f;
            // AiParams.DeflectsBullets = true;
            if (GameProperties.InMission)
            {
                for (int i = 0; i < Behaviors.Length; i++)
                    Behaviors[i].Value++;

                var treadPlaceTimer = (int)Math.Round(14 / Velocity.Length()) != 0 ? (int)Math.Round(14 / Velocity.Length()) : 1;

                if (Position - _oldPosition != Vector2.Zero && !Properties.Stationary)
                {
                    if (!Properties.IsSilent)
                    {
                        if (TankGame.GameUpdateTime % MathHelper.Clamp(treadPlaceTimer / 2, 4, 6) == 0)
                        {
                            var treadPlace = $"Assets/sounds/tnk_tread_place_{GameHandler.GameRand.Next(1, 5)}";
                            var sfx = SoundPlayer.PlaySoundInstance(treadPlace, SoundContext.Effect, 0.05f);
                            sfx.Instance.Pitch = MathHelper.Clamp(Properties.TreadPitch, 0, 1);
                        }
                    }

                    if (TankGame.GameUpdateTime % treadPlaceTimer == 0)
                        LayFootprint(Properties.Track == TrackType.Thick);
                }
                enactBehavior = () =>
                {
                    TargetTank = GameHandler.AllTanks.FirstOrDefault(tnk => tnk is not null && !tnk.Dead && (tnk.Team != Team || tnk.Team == TankTeam.NoTeam) && tnk != this);

                    foreach (var tank in GameHandler.AllTanks)
                    {
                        if (tank is not null && !tank.Dead && (tank.Team != Team || tank.Team == TankTeam.NoTeam) && tank != this)
                            if (GameUtils.Distance_WiiTanksUnits(tank.Position, Position) < GameUtils.Distance_WiiTanksUnits(TargetTank.Position, Position))
                                if ((tank.Properties.Invisible && tank.timeSinceLastAction < 60) || !tank.Properties.Invisible)
                                    TargetTank = tank;
                    }

                    // ai stuff not implemented for AIShot and AILaid
                    bool isShellNear = TryGetShellNear(AiParams.ProjectileWarinessRadius_PlayerShot, out var shell);
                    bool isMineNear = TryGetMineNear(AiParams.MineWarinessRadius_PlayerLaid, out var mine);

                    var tanksNearMe = new List<Tank>();
                    var cubesNearMe = new List<Block>();

                    foreach (var tank in GameHandler.AllTanks)
                        if (tank != this && tank is not null && !tank.Dead && GameUtils.Distance_WiiTanksUnits(tank.Position, Position) <= AiParams.TankWarinessRadius)
                            tanksNearMe.Add(tank);

                    foreach (var block in Block.AllBlocks)
                        if (block is not null && GameUtils.Distance_WiiTanksUnits(Position, block.Position) < AiParams.BlockWarinessDistance)
                            cubesNearMe.Add(block);

                    if (AiParams.DeflectsBullets)
                    {
                        if (isShellNear)
                        {
                            if (shell.LifeTime > 60)
                            {
                                var dir = GameUtils.DirectionOf(Position, shell.Position2D);
                                var rotation = dir.ToRotation();
                                var calculation = (Position.Distance(shell.Position2D) - 20f) / (float)(Properties.ShellSpeed * 1.2f);
                                float rot = -GameUtils.DirectionOf(Position,
                                    GeometryUtils.PredictFuturePosition(shell.Position2D, shell.Velocity2D, calculation))
                                    .ToRotation() + MathHelper.PiOver2;

                                TargetTurretRotation = rot;

                                TurretRotationMultiplier = 4f;

                                rot %= MathHelper.Tau;

                                //if ((-TurretRotation + MathHelper.PiOver2).IsInRangeOf(TargetTurretRotation, 0.15f))
                                    Shoot();
                            }
                        }
                    }

                    #region TurretHandle

                    TargetTurretRotation %= MathHelper.TwoPi;

                    TurretRotation %= MathHelper.TwoPi;

                    var diff = TargetTurretRotation - TurretRotation;

                    if (diff > MathHelper.Pi)
                        TargetTurretRotation -= MathHelper.TwoPi;
                    else if (diff < -MathHelper.Pi)
                        TargetTurretRotation += MathHelper.TwoPi;

                    TurretRotation = GameUtils.RoughStep(TurretRotation, TargetTurretRotation, AiParams.TurretSpeed * TurretRotationMultiplier);
                    bool targetExists = Array.IndexOf(GameHandler.AllTanks, TargetTank) > -1 && TargetTank is not null;
                    if (targetExists)
                    {
                        if (!seeks && !_predicts)
                        {
                            if (Behaviors[1].IsModOf(AiParams.TurretMeanderFrequency))
                            {
                                isEnemySpotted = false;
                                if (TargetTank.Properties.Invisible && TargetTank.timeSinceLastAction < 60)
                                {
                                    Aimtarget = TargetTank.Position;
                                    isEnemySpotted = true;
                                }

                                if (!TargetTank.Properties.Invisible)
                                {
                                    Aimtarget = TargetTank.Position;
                                    isEnemySpotted = true;
                                }

                                var dirVec = Position - Aimtarget;
                                TargetTurretRotation = -dirVec.ToRotation() - MathHelper.PiOver2 + GameHandler.GameRand.NextFloat(-AiParams.AimOffset, AiParams.AimOffset);
                            }
                        }

                        if (doFire)
                            UpdateAim(tanksNearMe, !isMineNear);
                    }

                    #endregion
                    if (doMovements)
                    {
                        if (Properties.Stationary)
                            return;

                        #region CubeNav
                        if (Behaviors[2].IsModOf(AiParams.BlockReadTime) && !isMineNear && !isShellNear)
                        {
                            pathBlocked = IsObstacleInWay(AiParams.BlockWarinessDistance, Vector2.UnitY.RotatedByRadians(-TargetTankRotation), out var travelPath, out var refPoints);
                            if (pathBlocked)
                            {
                                if (refPoints.Length > 0)
                                {
                                    // float AngleSmoothStep(float angle, float target, float amount) => GameUtils.AngleLerp(angle, target, amount * amount * (3f - 2f * amount));
                                    // why does this never work no matter what i do
                                    var refAngle = GameUtils.DirectionOf(Position, travelPath - new Vector2(0, 10000)).ToRotation();

                                    // AngleSmoothStep(TargetTankRotation, refAngle, refAngle / 3);
                                    // TargetTankRotation = -TargetTankRotation + MathHelper.PiOver2;
                                    GameUtils.RoughStep(ref TargetTankRotation, TargetTankRotation <= MathHelper.Pi ? -refAngle + MathHelper.PiOver2 : refAngle, refAngle / 6);
                                }
                            }
                            // TODO: i literally do not understand this
                        }

                        #endregion

                        #region GeneralMovement

                        if (!isMineNear && !isShellNear && !IsTurning && CurMineStun <= 0 && CurShootStun <= 0)
                        {
                            if (!pathBlocked)
                            {
                                if (Behaviors[0].IsModOf(AiParams.MeanderFrequency))
                                {
                                    float dir = -100;

                                    if (targetExists)
                                        dir = GameUtils.DirectionOf(Position, TargetTank.Position).ToRotation();

                                    var random = GameHandler.GameRand.NextFloat(-AiParams.MeanderAngle / 2, AiParams.MeanderAngle / 2);

                                    TargetTankRotation += random;
                                }
                                if (targetExists)
                                {
                                    if (AiParams.PursuitFrequency != 0)
                                    {
                                        if (Behaviors[0].IsModOf(AiParams.PursuitFrequency))
                                        {
                                            float dir = -100;

                                            if (targetExists)
                                                dir = GameUtils.DirectionOf(Position, TargetTank.Position).ToRotation();

                                            var random = GameHandler.GameRand.NextFloat(-AiParams.MeanderAngle / 2, AiParams.MeanderAngle / 2);

                                            var meanderRandom = dir != -100 ? random + (dir + MathHelper.PiOver2) + (0.2f * AiParams.PursuitLevel) : random;

                                            TargetTankRotation = meanderRandom;
                                        }
                                    }
                                }
                            }
                        }
                        #endregion

                        #region ShellAvoidance

                        var indif = 3;

                        if (isShellNear)
                        {
                            if (Behaviors[6].IsModOf(indif))
                            {
                                var direction = Vector2.UnitY.RotatedByRadians(shell.Position2D.DirectionOf(Position, false).ToRotation());

                                TargetTankRotation = direction.ToRotation();
                            }
                        }

                        #endregion

                        #region MineHandle / MineAvoidance
                        if (!isMineNear && !isShellNear)
                        {
                            if (Properties.MineLimit > 0)
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
                                                TargetTankRotation = new Vector2(100, 100).RotatedByRadians(GameHandler.GameRand.NextFloat(0, MathHelper.TwoPi)).ExpandZ().ToRotation();
                                                LayMine();
                                            }
                                        }
                                        else
                                        {
                                            if (GameHandler.GameRand.NextFloat(0, 1) <= AiParams.MinePlacementChance)
                                            {
                                                TargetTankRotation = new Vector2(100, 100).RotatedByRadians(GameHandler.GameRand.NextFloat(0, MathHelper.TwoPi)).ExpandZ().ToRotation();
                                                LayMine();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (isMineNear && !isShellNear)
                        {
                            if (Behaviors[5].IsModOf(10))
                            {
                                var direction = Vector2.UnitY.RotatedByRadians(mine.Position.DirectionOf(Position, false).ToRotation());

                                TargetTankRotation = direction.ToRotation();
                            }
                        }
                        #endregion

                    }

                    #region Special Tank Behavior

                    if (Tier == TankTier.Creeper)
                    {
                        if (Array.IndexOf(GameHandler.AllTanks, TargetTank) > -1 && TargetTank is not null)
                        {
                            float explosionDist = 90f;
                            if (GameUtils.Distance_WiiTanksUnits(TargetTank.Position, Position) < explosionDist)
                            {
                                Destroy(new TankHurtContext_Mine(false, WorldId));

                                new Explosion(Position, 10f, this, 0.2f);
                            }
                        }
                    }
                    else if (Tier == TankTier.Cherry)
                    {
                        if (SeesTarget)
                        {
                            if (SpecialBehaviors[0].Value <= 0)
                            {
                                SoundPlayer.PlaySoundInstance("Assets/sounds/tnk_event/alert", SoundContext.Effect, 0.6f);
                                Add2DCosmetic(CosmeticChest.Anger, () => SpecialBehaviors[0].Value <= 0);
                            }
                            SpecialBehaviors[0].Value = 300;
                        }
                        else
                            if (SpecialBehaviors[0].Value > 0)
                            SpecialBehaviors[0].Value--;
                        if (SpecialBehaviors[0].Value > 0)
                        {

                            Properties.MaxSpeed = 2.2f;
                        }
                        else
                            Properties.MaxSpeed = 1.35f;
                    }
                    else if (Tier == TankTier.Commando)
                    {
                        SpecialBehaviors[0].Value++;
                        if (SpecialBehaviors[0].Value > 500)
                        {
                            SpecialBehaviors[0].Value = 0;

                            var crate = Crate.SpawnCrate(CubeMapPosition.Convert3D(new CubeMapPosition(GameHandler.GameRand.Next(0, CubeMapPosition.MAP_WIDTH),
                                GameHandler.GameRand.Next(0, CubeMapPosition.MAP_HEIGHT))) + new Vector3(0, 500, 0), 2f);
                            crate.TankToSpawn = new TankTemplate()
                            {
                                AiTier = PickRandomTier(),
                                IsPlayer = false,
                                Team = Team
                            };

                            /*foreach (var mesh in Model.Meshes)
                            {
                                foreach (BasicEffect effect in mesh.Effects)
                                {
                                    if (mesh.Name == "Dish")
                                    {
                                        effect.AmbientLightColor = Color.Red.ToVector3();
                                        effect.SpecularColor = Color.Red.ToVector3();
                                        effect.DiffuseColor = Color.Red.ToVector3();
                                        effect.FogColor = Color.Red.ToVector3();
                                        effect.EmissiveColor = Color.Red.ToVector3();
                                    }
                                }
                            }*/
                        }
                    }

                    #endregion

                    #region TankRotation

                    var targ = TargetTankRotation + MathHelper.Pi;

                    if (doMoveTowards)
                    {
                        if (IsTurning)
                        {
                            // var real = TankRotation + MathHelper.PiOver2;
                            if (targ - TankRotation >= MathHelper.PiOver2)
                            {
                                TankRotation += MathHelper.Pi;
                                Flip = !Flip;
                            }
                            else if (targ - TankRotation <= -MathHelper.PiOver2)
                            {
                                TankRotation -= MathHelper.Pi;
                                Flip = !Flip;
                            }
                        }

                        if (TankRotation > targ - Properties.MaximalTurn - MathHelper.ToRadians(5) && TankRotation < targ + Properties.MaximalTurn + MathHelper.ToRadians(5))
                        {
                            IsTurning = false;
                            Properties.Speed += Properties.Acceleration;
                            if (Properties.Speed > Properties.MaxSpeed)
                                Properties.Speed = Properties.MaxSpeed;
                        }
                        else
                        {
                            Properties.Speed -= Properties.Deceleration;
                            if (Properties.Speed < 0)
                                Properties.Speed = 0;
                            treadPlaceTimer = (int)Math.Round(14 / Properties.TurningSpeed) != 0 ? (int)Math.Round(14 / Properties.TurningSpeed) : 1;
                            if (TankGame.GameUpdateTime % treadPlaceTimer == 0)
                                LayFootprint(Properties.Track == TrackType.Thick);
                            IsTurning = true;
                        }

                        var dir = Vector2.UnitY.RotatedByRadians(TankRotation);
                        Velocity.X = dir.X;
                        Velocity.Y = dir.Y;

                        Velocity.Normalize();

                        Velocity *= Properties.Speed;
                        TankRotation = GameUtils.RoughStep(TankRotation, targ, Properties.TurningSpeed);
                    }

                    #endregion
                };
            }
            enactBehavior?.Invoke();
        }
        public override void Render()
        {
            TankGame.Instance.GraphicsDevice.BlendState = BlendState.AlphaBlend;
            DrawExtras();
            if (Properties.Invisible && GameProperties.InMission)
                return;

            for (int i = 0; i < (Lighting.AccurateShadows ? 2 : 1); i++)
            {
                foreach (ModelMesh mesh in Model.Meshes)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        effect.World = i == 0 ? boneTransforms[mesh.ParentBone.Index] : boneTransforms[mesh.ParentBone.Index] * Matrix.CreateShadow(Lighting.AccurateLightingDirection, new(Vector3.UnitY, 0)) * Matrix.CreateTranslation(0, 0.2f, 0);
                        effect.View = View;
                        effect.Projection = Projection;

                        effect.TextureEnabled = true;

                        if (!Properties.HasTurret)
                            if (mesh.Name == "Cannon")
                                return;

                        if (mesh.Name == "Shadow")
                        {
                            if (!Lighting.AccurateShadows)
                            {
                                effect.Texture = _shadowTexture;
                                if (IsIngame)
                                {
                                    effect.Alpha = 0.5f;
                                    mesh.Draw();
                                }
                            }
                        }
                        else
                        {
                            if (IsHoveredByMouse)
                                effect.EmissiveColor = Color.White.ToVector3();
                            else
                                effect.EmissiveColor = Color.Black.ToVector3();

                            var tex = _tankTexture;

                            effect.Texture = tex;

                            effect.Alpha = 1;
                            mesh.Draw();
                            /*var ex = new Color[1024];

                            Array.Fill(ex, new Color(GameHandler.GameRand.Next(0, 256), GameHandler.GameRand.Next(0, 256), GameHandler.GameRand.Next(0, 256)));

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

                        effect.SetDefaultGameLighting_IngameEntities(0.9f);
                    }
                }
            }

            base.Render();
        }
        private void DrawExtras()
        {
            if (Dead)
                return;

            if (IsIngame)
            {
                if (DebugUtils.DebugLevel == 1)
                {
                    float calculation = 0f;

                    if (AiParams.PredictsPositions && TargetTank is not null)
                    {
                        calculation = Position.Distance(TargetTank.Position) / (float)(Properties.ShellSpeed * 1.2f);
                    }

                    if (AiParams.SmartRicochets)
                        GetTanksInPath(Vector2.UnitY.RotatedByRadians(seekRotation), out var rayEndpoint, true, missDist: AiParams.Inaccuracy, doBounceReset: AiParams.BounceReset);
                    var poo = GetTanksInPath(Vector2.UnitY.RotatedByRadians(TurretRotation - MathHelper.Pi), out var rayEnd, true, offset: Vector2.UnitY * 20, pattern: x => x.IsSolid | x.Type == Block.BlockType.Teleporter, missDist: AiParams.Inaccuracy, doBounceReset: AiParams.BounceReset);
                    if (AiParams.PredictsPositions)
                    {
                        float rot = -GameUtils.DirectionOf(Position, TargetTank is not null ?
                            GeometryUtils.PredictFuturePosition(TargetTank.Position, TargetTank.Velocity, calculation) :
                            Aimtarget).ToRotation() - MathHelper.PiOver2;
                        GetTanksInPath(Vector2.UnitY.RotatedByRadians(rot), out var rayEnd2, true, Vector2.Zero, pattern: x => x.IsSolid | x.Type == Block.BlockType.Teleporter, missDist: AiParams.Inaccuracy, doBounceReset: AiParams.BounceReset);
                    }
                    DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"{Tier}: {poo.Count}", GeometryUtils.ConvertWorldToScreen(new Vector3(0, 11, 0), World, View, Projection), 1, centered: true);
                    if (!Properties.Stationary)
                    {
                        IsObstacleInWay(AiParams.BlockWarinessDistance, Vector2.UnitY.RotatedByRadians(-TargetTankRotation), out var travelPos, out var refPoints, true);
                        DebugUtils.DrawDebugString(TankGame.SpriteRenderer, "TRAVELENDPOINT", GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(travelPos.X, 11, travelPos.Y), View, Projection), 1, centered: true);
                        DebugUtils.DrawDebugString(TankGame.SpriteRenderer, "ENDPOINT", GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(rayEnd.X, 11, rayEnd.Y), View, Projection), 1, centered: true);

                        /*var pos = GameUtils.DirectionOf(Position, travelPos);
                        var rot = pos.ToRotation();
                        var posNew = new Vector2(50, 0).RotatedByRadians(rot);
                        DebugUtils.DrawDebugString(TankGame.spriteBatch, "here?",
                            GeometryUtils.ConvertWorldToScreen(new(posNew.X, 11, posNew.Y), 
                            Matrix.CreateTranslation(Position.X, 
                            0, Position.Y), View, Projection), 
                            1, centered: true);*/
                        
                        foreach (var pt in refPoints)
                        {
                            DebugUtils.DrawDebugString(TankGame.SpriteRenderer, "pt", GeometryUtils.ConvertWorldToScreen(new Vector3(0, 11, 0), Matrix.CreateTranslation(pt.X, 0, pt.Y), View, Projection), 1, centered: true);
                        }

                        DebugUtils.DrawDebugString(TankGame.SpriteRenderer, "end", GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(GameUtils.DirectionOf(Position, travelPos).X, 0, GameUtils.DirectionOf(Position, travelPos).Y), View, Projection), 1, centered: true);
                        DebugUtils.DrawDebugString(TankGame.SpriteRenderer, "me", GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.Identity, View, Projection/*Matrix.CreateTranslation(Position.X, 11, Position.Y), View, Projection)*/), 1, centered: true);
                        //TankGame.spriteBatch.Draw(GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel"), new Rectangle((int)travelPos.X - 1, (int)travelPos.Y - 1, 20, 20), Color.White);

                        // draw future
                        // DebugUtils.DrawDebugString(TankGame.spriteBatch, "FUT", GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(GeometryUtils.PredictFuturePosition(TargetTank.Position, TargetTank.Velocity, calculation).ExpandZ() + new Vector3(0, 11, 0)), View, Projection), 1, centered: true);
                    }

                }
            }

            if (Properties.Invisible && GameProperties.InMission)
                return;

            Properties.Armor?.Render();
        }

        public bool TryGetShellNear(float distance, out Shell shell)
        {
            shell = null;

            Shell closest = null;

            bool returned = false;

            foreach (var bullet in Shell.AllShells)
            {
                if (bullet is not null)
                {
                    if (bullet.LifeTime > 30)
                    {
                        var dist = GameUtils.Distance_WiiTanksUnits(Position, bullet.Position2D);
                        if (dist < distance)
                        {
                            if (closest == null)
                                closest = bullet;
                            else if (GameUtils.Distance_WiiTanksUnits(Position, bullet.Position2D) < GameUtils.Distance_WiiTanksUnits(Position, closest.Position2D))
                                closest = bullet;
                            //var rotationTo = GameUtils.DirectionOf(Position, bullet.Position2D).ToRotation();

                            //if (Math.Abs(rotationTo - TankRotation + MathHelper.Pi) < MathHelper.PiOver2 /*|| Vector2.Distance(Position, bullet.Position2D) < distance / 2*/)
                            //
                            returned = true;
                            //
                        }
                    }
                }
            }

            shell = closest;
            return returned;
        }
        public bool TryGetMineNear(float distance, out Mine mine)
        {
            /*mine = null;
            foreach (var yours in Mine.AllMines)
            {
                if (yours is not null)
                {
                    if (Vector2.Distance(Position, yours.Position) < distance)
                    {
                        mine = yours;
                        return true;
                    }
                }
            }*/
            mine = null;

            Mine closest = null;

            bool returned = false;

            foreach (var min in Mine.AllMines)
            {
                if (min is not null)
                {
                    if (GameUtils.Distance_WiiTanksUnits(Position, min.Position) < distance)
                    {
                        if (closest == null)
                            closest = min;
                        else if (GameUtils.Distance_WiiTanksUnits(Position, min.Position) < GameUtils.Distance_WiiTanksUnits(Position, closest.Position))
                            closest = min;
                        //var rotationTo = GameUtils.DirectionOf(Position, bullet.Position2D).ToRotation();

                        //if (Math.Abs(rotationTo - TankRotation + MathHelper.Pi) < MathHelper.PiOver2 /*|| Vector2.Distance(Position, bullet.Position2D) < distance / 2*/)
                        //
                        returned = true;
                        //
                    }
                }
            }

            mine = closest;
            return returned;
        }

        private static readonly TankTier[] workingTiers =
        {
            TankTier.Brown, TankTier.Marine, TankTier.Yellow, TankTier.Black, TankTier.White, TankTier.Pink, TankTier.Violet, TankTier.Green, TankTier.Ash,
            TankTier.Bronze, TankTier.Silver, TankTier.Sapphire, TankTier.Ruby, TankTier.Citrine, TankTier.Amethyst, TankTier.Emerald, TankTier.Gold, TankTier.Obsidian,
            TankTier.Granite, TankTier.Bubblegum, TankTier.Water, TankTier.Crimson, TankTier.Tiger, TankTier.Creeper, TankTier.Gamma, TankTier.Marble,
            // TankTier.Assassin
        };
        public static TankTier PickRandomTier()
            => workingTiers[GameHandler.GameRand.Next(0, workingTiers.Length)];
    }
}