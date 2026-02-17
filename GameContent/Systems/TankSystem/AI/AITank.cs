using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using tainicom.Aether.Physics2D.Dynamics;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.GameMechanics;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.ModSupport;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Systems.CommandsSystem;
using TanksRebirth.GameContent.Systems.TankSystem;
using TanksRebirth.GameContent.Systems.TankSystem.AI;
using TanksRebirth.GameContent.UI.LevelEditor;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Systems.AI;

// eventually: AITank will be the basis for all AI controlled tanks...
// e.g: VioletTank : AITank, NecromancerTank : AITank, etc.
// this will allow for easier management of AI tanks and their unique behaviors without adding bloat for specific tank kinds
public partial class AITank : Tank {
    public ModTank? ModdedData { get; private set; }
    /// <summary>A list of all active dangers on the map to <see cref="AITank"/>s. Includes <see cref="Shell"/>s, <see cref="Mine"/>s,
    /// and <see cref="Explosion"/>s by default. To make an <see cref="AITank"/> behave towards any thing you would like, make it inherit from <see cref="IAITankDanger"/>
    /// and change the tank's behavior when running away by hooking into <see cref="WhileDangerDetected"/>.</summary>
    public static readonly List<IAITankDanger> Dangers = [];

    public delegate void PostExecuteAI(AITank tank);
    public static event PostExecuteAI? OnPostUpdateAI;

    public delegate void DangerDetected(AITank tank, IAITankDanger danger);
    public static event DangerDetected? WhileDangerDetected;

    public delegate void InstancedDestroy();
    public event InstancedDestroy? OnDestroy;
    /// <summary>Each of these keep track of certain behaviors that take place during the AI Cycle, including, but not limited to:<para></para>
    /// Navigation, Shell/Mine avoidance, Mine Laying, Shell Shooting</summary>
    public AITimer[] Behaviors { get; private set; }
    /// <summary>The invoked method for performing the actions of the tank's AI.</summary>
    public Action? AIBehaviorAction;
    /// <summary>The position of this <see cref="AITank"/> in the <see cref="GameHandler.AllAITanks"/> array.</summary>
    public int AITankId { get; private set; }
    /// <summary>The AI Tank Tier/Type of this <see cref="AITank"/>. For instance, a Brown tank would be <see cref="TankID.Brown"/>.</summary>
    public int AiTankType { get; set; }
    /// <summary>Only use if you know what you're doing!</summary>
    /// <param name="newId">The new ID to be assigned to this <see cref="AITank"/>.</param>
    public void ReassignId(int newId) => AITankId = newId;
    /// <summary>The colors of the explosion particles when this <see cref="AITank"/> is destroyed.</summary>
    public static Dictionary<int, Color> TankDestructionColors = new() {
        [TankID.Brown] = new(152, 96, 26),
        [TankID.Ash] = Color.Gray,
        [TankID.Marine] = Color.Teal,
        [TankID.Yellow] = Color.Yellow,
        [TankID.Pink] = Color.HotPink,
        [TankID.Green] = Color.LimeGreen,
        [TankID.Violet] = Color.Purple,
        [TankID.White] = Color.White,
        [TankID.Black] = Color.Black,
        [TankID.Bronze] = new(152, 96, 26),
        [TankID.Silver] = Color.Silver,
        [TankID.Sapphire] = Color.DeepSkyBlue,
        [TankID.Ruby] = Color.IndianRed,
        [TankID.Citrine] = Color.Yellow,
        [TankID.Amethyst] = Color.Purple,
        [TankID.Emerald] = Color.Green,
        [TankID.Gold] = Color.Gold,
        [TankID.Obsidian] = Color.Black,
    };
    /// <summary>Change the texture of this <see cref="AITank"/>.</summary>
    /// <param name="texture">The new texture.</param>
    public void SwapTankTexture(Texture2D texture) => DrawParamsTank.TankTexture = texture;
    /// <summary>The AI parameter collection of this AI Tank.</summary>
    public AIParameters Parameters = new();
    /// <summary>The position of the target this <see cref="AITank"/> is currently attempting to aim at.</summary>
    public Vector2 AimTarget { get; set; }
    /// <summary>Whether or not this tank sees its target. Generally should not be set, but the tank will shoot if able when this is true.</summary>
    public bool SeesTarget { get; set; }
    /// <summary>The target rotation for this tank's turret. <see cref="Tank.TurretRotation"/> will move towards this value at a rate of <see cref="AIParameters.TurretSpeed"/>.</summary>
    public float TargetTurretRotation { get; set; }
    public bool AutoEnactAIBehavior = true;

    /// <summary>Changes this <see cref="AITank"/> to a completely different type of tank. Should only be used in special cases.</summary>
    /// <param name="tier">The new tier that this tank will be.</param>
    /// <param name="setDefaults">Whether or not to set the associated defaults of this tank in accordance to <paramref name="tier"/>.</param>
    public void Swap(int tier, bool setDefaults = true) {
        AiTankType = tier;

        var tierName = TankID.Collection.GetKey(tier)!.ToLower();

        SwapTankTexture(Assets[$"tank_" + tierName]!);

        if (!UsesCustomModel)
            DrawParamsTank.Model = ModelGlobals.TankEnemy.Duplicate();

        if (setDefaults)
            ApplyDefaults(ref Properties);
    }

    /// <summary>
    /// Creates a new <see cref="AITank"/>.
    /// </summary>
    /// <param name="tier">The tier of this <see cref="AITank"/>.</param>
    /// <param name="applyDefaults">Whether or not to give this <see cref="AITank"/> the default values.</param>
    /// <param name="ignoreRegister">Whether or not this <see cref="AITank"/> is a gameplay tank or a manually-managed tank.</param>
    public AITank(int tier, bool applyDefaults = true, bool ignoreRegister = false) : base(ignoreRegister) {
        // looking at this code makes me want to barf.
        // maybe move this stuff to events within Difficulties.cs
        if (Modifiers.Map[Modifiers.BUMP])       tier++;
        if (Modifiers.Map[Modifiers.MONOCHROME]) tier = Modifiers.MonochromeValue;
        if (Modifiers.Map[Modifiers.MASTER])     tier = Modifiers.VanillaToMasterModeConversions[tier];

        NearbyDangers = [];

        AiTankType = tier;
        Behaviors = new AITimer[4];

        for (int i = 0; i < Behaviors.Length; i++)
            Behaviors[i] = new();

        // behaviors are definitely stupid, do something about them later
        Behaviors[0].Label = "TankChassisMovement";
        Behaviors[1].Label = "TankTurretMovement";
        Behaviors[2].Label = "TankShellFire";
        Behaviors[3].Label = "TankMinePlacement";

        DrawParams.LightPower = TankDrawParams.AI_AMB_MUL;

        // create modded data
        for (int i = 0; i < ModLoader.ModTanks.Length; i++) {
            var modTank = ModLoader.ModTanks[i];

            // associate values properly for modded data
            if (AiTankType == modTank.Type) {
                ModdedData = modTank.Clone();
                ModdedData.AITank = this;
            }
        }

        var tierName = TankID.Collection.GetKey(tier)!.ToLower();
        if (!UsesCustomModel) {
            DrawParamsTank.Model = ModelGlobals.TankEnemy.Asset;
            var tnkAsset = Assets[$"tank_" + tierName];
            DrawParamsTank.TankTexture = tnkAsset!.Duplicate(TankGame.Instance.GraphicsDevice);
        }

        // for debugging custom models
        // Model = GameResources.GetGameResource<Model>("Assets/models/rebirth_tanks/tank_necro");

        DrawParamsTank.ShadowTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank_shadow");

        if (applyDefaults)
            ApplyDefaults(ref Properties);

        if (!ignoreRegister) {
            int aiIndex = Array.IndexOf(GameHandler.AllAITanks, null);
            if (aiIndex < 0) return;

            AITankId = aiIndex;
            GameHandler.AllAITanks[aiIndex] = this;

            int worldIndex = Array.IndexOf(GameHandler.AllTanks, null);
            if (worldIndex < 0) {
                WorldId = -1;
                GC.Collect(); // guh?
                return;
            }

            WorldId = worldIndex;
            GameHandler.AllTanks[worldIndex] = this;
        }

        Initialize();

        // Props.Add(CosmeticChest.WitchHat);
    }
    public override void Initialize() {
        base.Initialize();

        Physics.OnCollision += Physics_OnCollision;
    }

    public override void ApplyDefaults(ref TankProperties properties) {
        properties.DestructionColor = TankDestructionColors[AiTankType];
        Parameters = AIManager.GetAIParameters(AiTankType);
        Properties = AIManager.GetAITankProperties(AiTankType);

        // initialize these so we don't divide by zero in modulus operations
        CurrentRandomMove = Client.ClientRandom.Next(Parameters.RandomTimerMinMove, Parameters.RandomTimerMaxMove);
        CurrentRandomMineLay = Client.ClientRandom.Next(Parameters.RandomTimerMinMine, Parameters.RandomTimerMaxMine);
        CurrentRandomShoot = Client.ClientRandom.Next(Parameters.RandomTimerMinShoot, Parameters.RandomTimerMaxShoot);

        // unfortunately these are just miserable
        if (Modifiers.Map[Modifiers.EXTRA_CALCS])
            if (properties.RicochetCount >= 1)
                if (properties.HasTurret)
                    Parameters.SmartRicochets = true;

        if (Modifiers.Map[Modifiers.BIG_MINES])
            Parameters.AwarenessHostileMine *= 3;

        if (Modifiers.Map[Modifiers.INVIS]) {
            properties.Invisible = true;
            properties.CanLayTread = false;
        }
        if (Modifiers.Map[Modifiers.STATIONARY])
            properties.Stationary = true;

        if (Modifiers.Map[Modifiers.HOMING]) {
            properties.ShellHoming = new() {
                Radius = 200f,
                Speed = properties.ShellSpeed,
                Power = 0.1f * properties.ShellSpeed
            };

            Parameters.DetectionForgivenessHostile *= 2;
        }

        if (Modifiers.Map[Modifiers.DEFLECT])
            Parameters.DeflectsBullets = true;

        if (Modifiers.Map[Modifiers.ARMOR]) {
            if (properties.Armor == null)
                properties.Armor = new(this, 3);
            else
                properties.Armor = new(this, properties.Armor.HitPoints + 3);
        }

        if (Modifiers.Map[Modifiers.PREDICTIONS])
            Parameters.PredictsPositions = true;
        properties.TreadVolume = 0.05f;

        base.ApplyDefaults(ref properties);

        ModdedData?.PostApplyDefaults();
    }
    public List<Tank> TanksNearMineAwareness = [];
    public List<Tank> TanksNearShootAwareness = [];
    public List<Block> BlocksNear = [];
    public override void Update() {
        // why did i not do this sooner?
        ModdedData?.PreUpdate();
        base.Update();
        ModdedData?.PostUpdate();
    }
    public override void Shoot(bool fxOnly = false, bool netSend = true) {
        base.Shoot(fxOnly, netSend);

        // only called once
        ModdedData?.Fire();
    }
    public override void Remove(bool nullifyMe) {
        if (nullifyMe) {
            GameHandler.AllAITanks[AITankId] = null!;
            GameHandler.AllTanks[WorldId] = null!;

            // NO DISPOSING FOR NOW, it causes weird BUGS with modded tanks.... WACK!
            // _tankTexture?.Dispose();
        }
        Physics.OnCollision -= Physics_OnCollision; 
        base.Remove(nullifyMe);
    }
    public override void Destroy(ITankHurtContext context, bool netSend) {
        // might not account for level testing via the level editor?
        OnDestroy?.Invoke();

        const string tankDestroySound1 = "Assets/sounds/tnk_destroy_enemy.ogg";
        SoundPlayer.PlaySoundInstance(tankDestroySound1, SoundContext.Effect, 0.3f);

        var aiDeathMark = new TankDeathMark(TankDeathMark.CheckColor.White) {
            Position = Position3D + new Vector3(0, 0.1f, 0),
        };

        aiDeathMark.StoredTank = new TankTemplate {
            AiTier = AiTankType,
            IsPlayer = false,
            Position = aiDeathMark.Position.FlattenZ(),
            Rotation = ChassisRotation,
            Team = Team,
        };

        base.Destroy(context, netSend);

        if (MainMenuUI.IsActive) return;
        if (LevelEditorUI.IsActive) return;
        if (LevelEditorUI.IsEditing) return;
        if (context.Source is null && Client.IsConnected()) return;

        // count enemy team-kills only in single player
        if (context.Source is not PlayerTank && Client.IsConnected()) return;

        // var player = context.Source as PlayerTank;

        if (!PlayerTank.TankKills.TryGetValue(AiTankType, out int value1))
            PlayerTank.TankKills.Add(AiTankType, 1);
        else
            PlayerTank.TankKills[AiTankType] = ++value1;

        // this code lowkey hurts to read
        // check if less than certain values for different value coins

        if (context is TankHurtContextShell cxtShell) {
            TankGame.SaveFile.BulletKills++;

            // if the ricochets remaining is less than the ricochets the bullet has, it has bounced at least once.
            if (cxtShell.Shell.RicochetsRemaining < cxtShell.Shell.Ricochets)
                TankGame.SaveFile.BounceKills++;
        }
        if (context is TankHurtContextExplosion) {
            TankGame.SaveFile.MineKills++;
        }

        // pretty sure != null isn't necessary because if Source is null it can't convert
        // it knows the owner is not me but increments my kill count anyway
        if (context.Source is PlayerTank p) {
            var myId = NetPlay.GetMyClientId();

            if (Client.IsConnected()) {
                bool isMe = p.PlayerId == myId;
                if (isMe)
                    PlayerTank.KillCounts[myId]++;
            }
            else {
                PlayerTank.KillCounts[p.PlayerId]++;

                TankGame.SaveFile.TotalKills++;
            }

            KeyDropLogic(PlayerID.PlayerTankColors[p.PlayerId]);
        }

        if (TankGame.SaveFile.TankKills.TryGetValue(AiTankType, out uint value))
            TankGame.SaveFile.TankKills[AiTankType] = ++value;

        GiveXP();
        // check if player id matches client id, if so, increment that player's kill count, then sync to the server
        // TODO: convert TankHurtContext into a struct and use it here
        // Will be used to track the reason of death and who caused the death, if any tank owns a shell or mine
        //
        // if (context.PlayerId == Client.PlayerId)
        // {
        //    PlayerTank.KillCount++;
        //    Client.Send(new TankKillCountUpdateMessage(PlayerTank.KillCount)); // not a bad idea actually
    }
    // TODO: kepe things better
    void KeyDropLogic(Color color) {
        if (GameLauncher.IsConsoleAllocated) return;

        if (TankGame.SaveFile.CollectedKeys >= 10) {
            SoundPlayer.SoundError();
            var str = TankGame.GameLanguage.KeysWarning;
            var p = GameHandler.Particles.MakeParticle(Position3D + new Vector3(0, 30, 0), str);

            p.Scale = new(0.65f);
            p.Pitch = MathHelper.Pi;
            p.FaceTowardsMe = CameraGlobals.IsUsingFirstPersonCamera;
            p.Origin2D = FontGlobals.RebirthFont.MeasureString(str) / 2;
            p.Color = Color.Red;

            p.UniqueBehavior = (p) => {
                p.Position.Y += 0.35f * RuntimeData.DeltaTime;

                if (p.LifeTime > 30)
                    p.Alpha -= 0.02f * RuntimeData.DeltaTime;

                if (p.Alpha <= 0)
                    p.Destroy();
            };
            return;
        }

        // do logic on the client for now
        var rand = Client.ClientRandom.Next(101);

        if (rand != 0) return;

        TankGame.SaveFile.CollectedKeys++;

        var tex = GameResources.GetGameResource<Texture2D>("Assets/textures/chest/key");
        var sound = SoundPlayer.PlaySoundInstance("Assets/sounds/shiny_get.ogg", SoundContext.Effect);

        // TextureGlobals.Pixels[color]

        var keyPart = GameHandler.Particles.MakeParticle(Position3D + new Vector3(0, 15, 0), ModelGlobals.Key.Asset, tex);

        keyPart.Alpha = 1f;
        // maybe this is retarded
        keyPart.Scale = Vector3.One * 40;
        keyPart.HasAdditiveBlending = false;
        keyPart.Pitch = MathHelper.PiOver2;
        keyPart.Color = color;

        float t = 0;
        float t2 = 0;

        float velY = 4f;
        float gravY = 0.05f;

        float lifeTime = 240;

        keyPart.UniqueBehavior = (p) => {
            p.Position.Y += velY * RuntimeData.DeltaTime;
            velY -= gravY;
            if (velY < 0) velY = 0;

            t += 0.01f * RuntimeData.DeltaTime;

            if (t > 1) t = 1;

            float ease = Easings.ComputeEase(EasingFunction.InOutSine, t);

            p.Roll = ease * MathHelper.TwoPi * 6 + MathHelper.PiOver2;

            if (p.LifeTime > lifeTime) {
                t2 += 0.01f * RuntimeData.DeltaTime;

                if (t2 > 1) t2 = 1;

                float ease2 = Easings.ComputeEase(EasingFunction.InOutSine, t2);

                p.Position.Y += ease2;

                if (p.Position.Y > 500f)
                    p.Destroy();
            }

            float randX = Client.ClientRandom.NextFloat(-10, 20);
            float randY = Client.ClientRandom.NextFloat(-5, 5);

            if (RuntimeData.UpdateCount % 10 == 0) {
                GameHandler.Particles.MakeShineSpot(p.Position + new Vector3(randX, randY, 0),
                    Color.Yellow, Client.ClientRandom.NextFloat(0.3f, 0.5f));
            }
        };
    }
    void GiveXP() {
        if (LevelEditorUI.IsEditing) return;
        var rand = Client.ClientRandom.NextFloat(0.75f, 1.25f);
        var gain = Parameters.BaseXP * rand;
        // i will keep this commented if anything else happens.
        //var gain = (BaseExpValue + rand) * GameData.UniversalExpMultiplier;
        GameHandler.ExpBar.GainExperience(gain);

        var str = $"+{gain * 100:0.00} XP";
        var p = GameHandler.Particles.MakeParticle(Position3D + new Vector3(0, 30, 0), str);

        p.Scale = new(0.5f);
        p.Pitch = MathHelper.Pi;
        p.FaceTowardsMe = CameraGlobals.IsUsingFirstPersonCamera;
        p.Origin2D = FontGlobals.RebirthFont.MeasureString(str) / 2;

        p.UniqueBehavior = (p) => {
            p.Position.Y += 0.1f * RuntimeData.DeltaTime;

            p.Alpha -= 0.01f * RuntimeData.DeltaTime;

            if (p.Alpha <= 0)
                p.Destroy();
        };
    }
    /// <summary>Sets various meta-data things about this <see cref="AITank"/>.
    /// <br></br>Sets <see cref="TargetTank"/>, <see cref="NearbyDangers"/>, and <see cref="ClosestDanger"/>.</summary>
    public void HandleTankMetaData() {
        TanksNearMineAwareness.Clear();
        BlocksNear.Clear();

        // get ai to target a player's ping
        TargetTank = TryOverrideTarget(out bool wasOverwritten);

        if (!wasOverwritten)
            TargetTank = GetAppropriateTarget();

        // measure the biggest WarinessRadius, player or ai, then check the larger, then do manual calculations.
        var radii = new float[] { Parameters.AwarenessFriendlyMine, Parameters.AwarenessHostileMine, Parameters.AwarenessFriendlyShell, Parameters.AwarenessHostileShell };
        var biggest = radii.Max();

        NearbyDangers = GetEvasionData();
        ClosestDanger = NearbyDangers.Closest(Position);

        if (NearbyDangers.Count > 0) {
            WhileDangerDetected?.Invoke(this, ClosestDanger!);
            ModdedData?.DangerDetected();
        }
    }
    /// <summary>The main AI loop of this <see cref="AITank"/>.</summary>
    public void DoAI() {
        if (!MainMenuUI.IsActive && !CampaignGlobals.InMission) return;

        TurretRotationMultiplier = 1f;

        Array.ForEach(Behaviors, x => x.Value += RuntimeData.DeltaTime);

        #region HandleTanksNear
        TanksNearMineAwareness.Clear();
        TanksNearShootAwareness.Clear();

        Span<Tank?> allTanks = GameHandler.AllTanks;
        ref var search = ref MemoryMarshal.GetReference(allTanks);

        for (int i = 0; i < allTanks.Length; i++) {
            var tank = Unsafe.Add(ref search, i);
            if (tank is null || tank == this || tank.IsDestroyed)
                continue;

            float distToBody = GameUtils.Distance_WiiTanksUnits(Position, tank.Position);
            float distToTurret = GameUtils.Distance_WiiTanksUnits(TurretPosition, tank.Position);

            if (distToBody <= Parameters.TankAwarenessMine)
                TanksNearMineAwareness.Add(tank);

            if (distToTurret <= Parameters.TankAwarenessShoot)
                TanksNearShootAwareness.Add(tank);
        }

        if (ModdedData is not null) {
            if (!ModdedData.CustomAI())
                return;
        }
        #endregion

        var isShellNear = NearbyDangers.Count > 0 && ClosestDanger is Shell;

        // only use if checking the respective boolean!
        var shell = (ClosestDanger as Shell)!;

        if (Parameters.DeflectsBullets) {
            if (isShellNear) {
                DoDeflection(shell);
            }
        }

        HandleTurret();
        if (DoMovements) {
            if (Properties.Stationary)
                return;

            // facing down = 0 radians/2pi radians

            // "DoMovement" handles danger avoidance.
            // IsSurviving is only set every movement opportunity
            DoMovement();

            // checks if it is entirely unable to lay mines first
            TryMineLay();
        }

        #region TankRotation

        // i really hope to remove this hardcode.
        if (DoMoveTowards) {
            var dir = Vector2.UnitY.RotatedBy(ChassisRotation);
            Velocity = Vector2.Normalize(dir);

            Velocity *= Speed;
            ChassisRotation = MathUtils.RoughStep(ChassisRotation, DesiredChassisRotation, Properties.TurningSpeed * RuntimeData.DeltaTime);
        }

        #endregion
    }

    public BasicEffect TankBasicEffectHandler = new(TankGame.Instance.GraphicsDevice);
    public override void Render() {
        base.Render();
        if (IsDestroyed) return;
        // find out why i put this here lmao
        TankGame.Instance.GraphicsDevice.BlendState = BlendState.AlphaBlend;
        DrawExtras();

        if (Properties.Invisible && (MainMenuUI.IsActive || CampaignGlobals.InMission)) return;

        foreach (ModelMesh mesh in DrawParamsTank.Model.Meshes) {
            foreach (BasicEffect effect in mesh.Effects) {
                effect.World = boneTransforms[mesh.ParentBone.Index];
                effect.View = DrawParams.View;
                effect.Projection = DrawParams.Projection;

                effect.TextureEnabled = true;

                if (!Properties.HasTurret)
                    if (mesh.Name == "Cannon")
                        return;

                if (mesh.Name == "Shadow") {
                    if (!CommandGlobals.DrawMeshShadows) continue;
                    effect.Texture = DrawParamsTank.ShadowTexture;
                    effect.Alpha = DrawParamsTank.ShadowAlpha;
                    mesh.Draw();
                    continue;
                }

                /*if (mesh.Name is "Cannon" or "Chassis") {
                    effect.Texture = GameResources.GetGameResource<Texture2D>("Assets/models/rebirth_tanks/tank_necro_tank");
                }
                else if (mesh.Name is "Cloak" or "Jewel") {
                    effect.Texture = GameResources.GetGameResource<Texture2D>("Assets/models/rebirth_tanks/tank_necro_extras");
                }
                else if (mesh.Name is "Skulls") {
                    effect.Texture = GameResources.GetGameResource<Texture2D>("Assets/models/rebirth_tanks/tank_necro_skulls");
                }*/
                // ^ old testing stuff

                effect.Alpha = DrawParamsTank.TankAlpha;
                effect.Texture = DrawParamsTank.TankTexture;

                effect.SetDefaultGameLighting_IngameEntities(DrawParams.LightPower, DrawParams.AmbientPower, DrawParams.UsePhong, DrawParams.LightDirection);
                mesh.Draw();
            }
        }
    }
    public void DrawAwarenessCircle(BasicEffect effect, float awareness, Color color, Vector2? posOverride = null) {
        if (awareness <= 0)
            return;

        const int circleResolution = 64;

        float radius = GameUtils.Value_WiiTanksUnits(awareness + TNK_WIDTH) / 2f;

        // slightly above the tank Y to prevent z-fighting
        float heightOffset = 0.2f;

        VertexPositionColor[] vertices = new VertexPositionColor[circleResolution + 1];

        var pos = posOverride ?? Position;

        for (int i = 0; i <= circleResolution; i++) {
            float angle = MathHelper.TwoPi * i / circleResolution;
            float x = MathF.Cos(angle) * radius;
            float z = MathF.Sin(angle) * radius;

            var worldPos = new Vector3(pos.X + x, heightOffset, pos.Y + z);
            vertices[i] = new VertexPositionColor(worldPos, color);
        }

        effect.World = Matrix.Identity;
        effect.View = DrawParams.View;
        effect.Projection = DrawParams.Projection;

        effect.VertexColorEnabled = true;

        foreach (EffectPass pass in effect.CurrentTechnique.Passes) {
            pass.Apply();
            effect.GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineStrip, vertices, 0, circleResolution);
        }
    }
    public void DrawAwarenessLine(BasicEffect effect, float distance, Color color, Vector3 offset = default, float forwardOffset = default) {
        float heightOffset = 0.2f;

        var start = new Vector3(Position.X, heightOffset, Position.Y) + offset;

        var forward = Vector2.UnitY.RotatedBy(ChassisRotation + forwardOffset);

        // not to game units...?
        var gameUnits = GameUtils.Value_WiiTanksUnits(distance + TNK_WIDTH);
        var end2D = Position + forward * gameUnits;
        var end = new Vector3(end2D.X, heightOffset, end2D.Y) + offset;

        var lineVerts = new VertexPositionColor[] {
            new(start, color),
            new(end, color * 0.75f)
        };

        effect.World = Matrix.Identity;
        effect.View = CameraGlobals.GameView;
        effect.Projection = CameraGlobals.GameProjection;
        effect.VertexColorEnabled = true;

        foreach (var pass in effect.CurrentTechnique.Passes) {
            pass.Apply();
            effect.GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, lineVerts, 0, 1);
        }
    }
    void DrawExtras() {
        if (IsDestroyed || IgnoreRegister)
            return;

        // did i ever make any good programming choices before this past year or so?
        // this code looks like it was written by a 12 year old with a broken arm - GitHub Copilot
        // even ai hates my code.
        if (DebugManager.DebugLevel == DebugManager.Id.AIData) {
            float calculation = 0f;

            var drawInfo = new Dictionary<(string Name, float Value, bool TrackTurret), Color>() {
                [(nameof(Parameters.ObstacleAwarenessMine), ObstacleAwarenessMineReal / 2, false)] = Color.Yellow,

                [(nameof(Parameters.ObstacleAwarenessMovement), Parameters.ObstacleAwarenessMovement * 2, false)] = Color.Purple,

                [(nameof(Parameters.AwarenessFriendlyShell), Parameters.AwarenessFriendlyShell, false)] = Color.Green,
                [(nameof(Parameters.AwarenessFriendlyMine), Parameters.AwarenessFriendlyMine, false)] = Color.LimeGreen,

                [(nameof(Parameters.AwarenessHostileShell), Parameters.AwarenessHostileShell, false)] = Color.DarkRed,
                [(nameof(Parameters.AwarenessHostileMine), Parameters.AwarenessHostileMine, false)] = Color.Red,

                [(nameof(Parameters.TankAwarenessShoot), Parameters.TankAwarenessShoot, true)] = Color.Blue,
                [(nameof(Parameters.TankAwarenessMine), Parameters.TankAwarenessMine, false)] = Color.Cyan,

                [(nameof(BlocksNear), BlocksNear.Count, false)] = Color.Orange,
                [(nameof(TanksNearMineAwareness), TanksNearMineAwareness.Count, false)] = Color.IndianRed,
            };

            // NOTE: cross-product with target rotation vector and rotation vector gives you whether or not (negative or positive)
            // the tank needs to rotate clockwise or counter-clockwise
            var realI = 0;
            for (int i = 0; i < drawInfo.Count; i++) {
                var info = drawInfo.ElementAt(i);

                if (info.Key.Value <= 0) continue;

                realI++;

                var pos = MatrixUtils.ConvertWorldToScreen(Vector3.Up * 20, DrawParams.World, DrawParams.View, DrawParams.Projection) - new Vector2(0, realI * 20);
                DrawUtils.DrawStringWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFont,
                    $"{info.Key.Name}: {info.Key.Value} ({GameUtils.Value_WiiTanksUnits(info.Key.Value)})", pos, info.Value, Color.White,
                    Vector2.One * 0.5f, 0f, borderThickness: 0.25f);
                DrawAwarenessCircle(TankBasicEffectHandler, info.Key.Value, info.Value, info.Key.TrackTurret ? TurretPosition : null);
            }

            DrawAwarenessLine(TankBasicEffectHandler, Parameters.ObstacleAwarenessMovement / 2 * Speed, Color.Black);
            // DrawAwarenessLine(TankBasicEffectHandler, Parameters.ObstacleAwarenessMovement / 2 * Speed, Color.Magenta, forwardOffset: ChassisRotation - DesiredChassisRotation);

            drawInfo.Clear();

            if (Parameters.PredictsPositions && TargetTank is not null)
                calculation = Position.Distance(TargetTank.Position) / (float)(Properties.ShellSpeed * 1.2f);

            if (Parameters.SmartRicochets)
                GetTanksInPath(Vector2.UnitY.RotatedBy(_seekRotation), out var ricP1, out var tnkCol1, true, missDist: Parameters.DetectionForgivenessHostile, doBounceReset: Parameters.BounceReset);
            // maybe not necessary. store from the cpu, draw on the gpu.
            var poo = GetTanksInPath(Vector2.UnitY.RotatedBy(TurretRotation - MathHelper.Pi), out var ricP2, out var tnkCol2, true, offset: Vector2.UnitY * 20, pattern: x => x.Properties.IsSolid | x.Type == BlockID.Teleporter, missDist: Parameters.DetectionForgivenessHostile, doBounceReset: Parameters.BounceReset);
            if (Parameters.PredictsPositions) {
                float rot = -Position.DirectionTo(TargetTank is not null ?
                    GeometryUtils.PredictFuturePosition(TargetTank.Position, TargetTank.Velocity, calculation) :
                    AimTarget).ToRotation() - MathHelper.PiOver2;
                GetTanksInPath(Vector2.UnitY.RotatedBy(rot), out var ricP3, out var tnkCol3, true, Vector2.Zero, pattern: x => x.Properties.IsSolid | x.Type == BlockID.Teleporter, missDist: Parameters.DetectionForgivenessHostile, doBounceReset: Parameters.BounceReset);
            }
            for (int i = 0; i < ricP2.Length; i++) {
                DebugManager.DrawDebugString(TankGame.SpriteRenderer, $"ric{i}", MatrixUtils.ConvertWorldToScreen(new Vector3(0, 11, 0),
                    Matrix.CreateTranslation(ricP2[i].X, 0, ricP2[i].Y), DrawParams.View, DrawParams.Projection), 1, centered: true);
            }
            for (int i = 0; i < tnkCol2.Length; i++) {
                DebugManager.DrawDebugString(TankGame.SpriteRenderer, $"col{i}", MatrixUtils.ConvertWorldToScreen(new Vector3(0, 11, 0),
                    Matrix.CreateTranslation(tnkCol2[i].X, 0, tnkCol2[i].Y), DrawParams.View, DrawParams.Projection), 1, centered: true);
            }

            /*for (int i = 0; i < info.Length; i++) {
                var pos = MatrixUtils.ConvertWorldToScreen(Vector3.Up * 20, World, View, Projection) -
                    new Vector2(0, (i * 20));
                DrawUtils.DrawTextWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFont, info[i], pos,
                    Color.Aqua, Color.Black, new Vector2(0.5f).ToResolution(), 0f, Anchor.TopCenter, 0.6f);
            }*/
        }
        /*if (DebugManager.DebugLevel == DebugManager.Id.AIData && !Properties.Stationary) {
            // magical numbers too lazy, look at update method to define
            IsObstacleInWay(AiParams.ObstacleAwarenessMovement / 2, Vector2.UnitY.RotatedBy(TargetTankRotation), out var travelPos, out var refPoints, TankPathCheckSize, draw: true);
            DebugManager.DrawDebugString(TankGame.SpriteRenderer, "TEP", MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(travelPos.X, 11, travelPos.Y), View, Projection), 6, centered: true);
            foreach (var pt in refPoints)
                DebugManager.DrawDebugString(TankGame.SpriteRenderer, "pt", MatrixUtils.ConvertWorldToScreen(new Vector3(0, 11, 0), Matrix.CreateTranslation(pt.ReflectionPoint.X, 0, pt.ReflectionPoint.Y), View, Projection), 6, centered: true);


            //DebugUtils.DrawDebugString(TankGame.SpriteRenderer, "end", MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(MathUtils.DirectionOf(travelPos, Position).X, 0, MathUtils.DirectionOf(travelPos, Position).Y), View, Projection), 6, centered: true);
        }*/

        if (Properties.Invisible && (CampaignGlobals.InMission || MainMenuUI.IsActive))
            return;

        Properties.Armor?.Render();
    }

    // strictly call this on the server.
    public static int PickRandomTier() => Server.ServerRandom.Next(0, TankID.Collection.Count);

    static readonly object _rayCastLock = new();
    /// <summary>Performs a raycast in all 4 cardinal directions of the tank (rotation-agnostic).</summary>
    /// <param name="distance">The length of the raycast.</param>
    /// <param name="callback">Code-callback for performing special actions based on the raycast.</param>
    /// <param name="offset">The angle offset.</param>
    /// <param name="ignoreDirs">Which directions to not cast a ray towards.</param>
    /// <returns>A direction with the corresponding direction vector.</returns>
    public (CollisionDirection Direction, Vector2 Vec)[] RayCastCardinals(float distance, RayCastReportFixtureDelegate? callback = null, float offset = 0f, params CollisionDirection[] ignoreDirs) {
        // directions denoted by their real directions are good directions
        var goodDirs = new (CollisionDirection, Vector2)[4];

        for (int i = 0; i < 4; i++) {
            // add 1 because we don't want "None"
            var collDir = (CollisionDirection)(i + 1);
            if (ignoreDirs.Contains(collDir))
                continue;

            // start raycast going down (positive Y axis)
            var dir = Vector2.UnitY.RotatedBy(i * MathHelper.PiOver2 + offset);
            goodDirs[i] = (collDir, dir);

            // Value_ToWiiTanksUnits...?
            var gameUnits = GameUtils.Value_WiiTanksUnits(TNK_WIDTH + distance);
            var endpoint = Physics.Position + dir * gameUnits / UNITS_PER_METER;

            lock (_rayCastLock) {
                // check in the 4 cardinal directions if mine-laying is ok.
                CollisionsWorld.RayCast((fixture, point, normal, fraction) => {
                    callback?.Invoke(fixture, point, normal, fraction);

                    if (fixture.Body.Tag is Block or GameScene.BoundsRenderer.BOUNDARY_TAG) {
                        // Console.WriteLine("hit along ray: " + fraction + "(" + collDir + ")");
                        goodDirs[i] = (CollisionDirection.None, Vector2.Zero);
                    }
                    else {
                        //Console.WriteLine($"ignoring... not implemented. ({fixture.Body.Tag})");
                        return -1f;
                    }
                    //Console.WriteLine("Hit fixture: " + fixture.Body.Tag);
                    //Console.WriteLine("At point: " + point);
                    //Console.WriteLine("With normal: " + normal);
                    //Console.WriteLine("Fraction along ray: " + fraction);
                    // Console.WriteLine();

                    return fraction;
                    // divide by 2 because it's a radius, i think
                }, Physics.Position, endpoint);
            }
        }

        return goodDirs;
    }
}