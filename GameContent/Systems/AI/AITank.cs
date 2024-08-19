using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals;
using TanksRebirth.GameContent.GameMechanics;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Graphics;
using TanksRebirth.GameContent.Systems.Coordinates;
using TanksRebirth.Internals.Common.Framework;
using TanksRebirth.Net;
using TanksRebirth.GameContent.Properties;
using TanksRebirth.GameContent.Cosmetics;
using TanksRebirth.GameContent.UI;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.ModSupport;
using TanksRebirth.GameContent.Systems.AI;
using System.Threading.Tasks;

namespace TanksRebirth.GameContent;
public partial class AITank : Tank  {
    // TODO: Make smoke bombs!
    /// <summary>A list of all active dangers on the map to <see cref="AITank"/>s. Includes <see cref="Shell"/>s, <see cref="Mine"/>s,
    /// and <see cref="Explosion"/>s by default. To make an AI Tank behave towards any thing you would like, make it inherit from <see cref="IAITankDanger"/>
    /// and change the tank's behavior when running away by hooking into <see cref="WhileDangerDetected"/>.</summary>
    public static List<IAITankDanger> Dangers = new();

    public delegate void PostExecuteAI(AITank tank);
    public static event PostExecuteAI? OnPostUpdateAI;

    public delegate void DangerDetected(AITank tank, IAITankDanger danger);
    public static event DangerDetected? WhileDangerDetected;

    public delegate void InstancedDestroy();
    public event InstancedDestroy? OnDestroy;
    /// <summary>Each of these keep track of certain behaviors that take place during the AI Cycle, including, but not limited to:<para></para>
    /// Navigation, Shell/Mine avoidance, Mine Laying, Shell Shooting</summary>
    public AiBehavior[] Behaviors { get; private set; }
    /// <summary>Each of these are for super special tanks (by default). The currently unimplemented tanks have special abilities that
    /// use this.</summary>
    public AiBehavior[] SpecialBehaviors { get; private set; }
    /// <summary>The AI Tank Tier/Type of this <see cref="AITank"/>. For instance, a Brown tank would be <see cref="TankID.Brown"/>.</summary>
    public int AiTankType;
    private Texture2D? _tankTexture;
    private static Texture2D? _shadowTexture;
    /// <summary>The invoked method for performing the actions of the tank's AI.</summary>
    public Action? AIBehaviorAction;
    /// <summary>The position of this <see cref="AITank"/> in the <see cref="GameHandler.AllAITanks"/> array.</summary>
    public int AITankId { get; private set; }
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
        [TankID.Granite] = new(152, 96, 26),
        [TankID.Bubblegum] = Color.LightPink,
        [TankID.Water] = Color.LightBlue,
        [TankID.Crimson] = Color.Crimson,
        [TankID.Tiger] = Color.Yellow,
        [TankID.Creeper] = Color.Green,
        [TankID.Fade] = Color.Beige,
        [TankID.Gamma] = Color.DarkGreen,
        [TankID.Marble] = Color.Red,
        [TankID.Cherry] = Color.DarkRed,
        [TankID.Assassin] = Color.DarkGray,
        [TankID.Commando] = Color.Olive,
        [TankID.RocketDefender] = Color.DarkGray,
        [TankID.Electro] = Color.Blue,
        [TankID.Explosive] = Color.DarkGray,
    };
    /// <summary>Change the texture of this <see cref="AITank"/>.</summary>
    /// <param name="texture">The new texture.</param>
    public void SwapTankTexture(Texture2D texture) => _tankTexture = texture;
    #region AiTankParams
    /// <summary>The AI parameter collection of this AI Tank.</summary>
    public AiParameters AiParams { get; set; } = new();
    /// <summary>The position of the target this <see cref="AITank"/> is currently attempting to aim at.</summary>
    public Vector2 AimTarget;
    /// <summary>Whether or not this tank sees its target. Generally should not be set, but the tank will shoot if able when this is true.</summary>
    public bool SeesTarget { get; set; }
    /// <summary>The target rotation for this tank's turret. <see cref="Tank.TurretRotation"/> will move towards this value at a rate of <see cref="AiParameters.TurretSpeed"/>.</summary>
    public float TargetTurretRotation;
    /// <summary>The default XP value to award to players if a player destroys this <see cref="AITank"/>.</summary>
    public float BaseExpValue { get; set; }
    #endregion
    /// <summary>Changes this <see cref="AITank"/> to a completely different type of tank. Should only be used in special cases.</summary>
    /// <param name="tier">The new tier that this tank will be.</param>
    /// <param name="setDefaults">Whether or not to set the associated defaults of this tank in accordance to <paramref name="tier"/>.</param>
    public void Swap(int tier, bool setDefaults = true) {
        AiTankType = tier;

        var tierName = TankID.Collection.GetKey(tier)!.ToLower();

        if (tier <= TankID.Marble)
            SwapTankTexture(Assets[$"tank_" + tierName]);
        #region Special

        if (tier == TankID.Commando)
        {
            Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_commando");

            SwapTankTexture(GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_commando"));

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
        else if (tier == TankID.Assassin)
        {
            Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_assassin");

            SwapTankTexture(GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_assassin"));
        }
        else if (tier == TankID.RocketDefender)
        {
            Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_rocket");

            SwapTankTexture(GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_rocket"));
        }
        else if (tier == TankID.Electro)
        {
            Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_electro");

            SwapTankTexture(GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_electro"));
        }
        else if (tier == TankID.Explosive)
        {
            Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_explosive");

            SwapTankTexture(GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_explosive"));
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
    /// <param name="tier">The tier of this <see cref="AITank"/>. If '<see cref="TankID.Random"/>', it will be randomly chosen.</param>
    /// <param name="tankRange">The randomization range that this <see cref="AITank"/> can become.</param>
    /// <param name="setTankDefaults">Whether or not to give this <see cref="AITank"/> the default values.</param>
    /// <param name="isIngame">Whether or not this <see cref="AITank"/> is a gameplay tank or a cosmetic tank (i.e: display models on menus, etc).</param>
    /// <param name="isRandomizable">Whether or not this <see cref="AITank"/> can be randomized or not using <paramref name="tankRange"/>.</param>
    public AITank(int tier, Range<int> tankRange = default, bool setTankDefaults = true, bool isIngame = true, bool isRandomizable = true) {
        IsIngame = isIngame;
        // TargetTankRotation = MathHelper.Pi;
        if (IsIngame)
        {
            if (Difficulties.Types["BumpUp"])
                tier++;
            if (Difficulties.Types["Monochrome"])
                tier = MainMenu.MonochromeValue;
            if (Difficulties.Types["MasterModBuff"] && !Difficulties.Types["MarbleModBuff"])
                tier += 9;
            if (Difficulties.Types["MarbleModBuff"] && !Difficulties.Types["MasterModBuff"])
                tier += 18;
            if (Difficulties.Types["RandomizedTanks"])
            {
                tier = TankID.Random;
                tankRange = new Range<int>(TankID.Brown, TankID.Marble); // set to commando when the time comes
            }
        }
        if (isRandomizable)
            if (tier == TankID.Random)
                tier = (short)GameHandler.GameRand.Next(tankRange.Min, tankRange.Max + 1); // guh?? an overload exists???

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

        if (TankGame.IsMainThread) {
            if (tier < TankID.Cherry) {

                var tnkAsset = Assets[$"tank_" + TankID.Collection.GetKey(tier)!.ToLower()];

                var t = new Texture2D(TankGame.Instance.GraphicsDevice, tnkAsset.Width, tnkAsset.Height);

                var colors = new Color[tnkAsset.Width * tnkAsset.Height];

                tnkAsset.GetData(colors);

                t.SetData(colors);

                _tankTexture = t;
            }
        }
        else
            _tankTexture = Assets[$"tank_" + TankID.Collection.GetKey(tier)!.ToLower()];
        #region Special

        if (tier == TankID.Commando)
        {
            Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_commando");

            _tankTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_commando");
            // fix?
        }
        else if (tier == TankID.Assassin)
        {
            Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_assassin");

            _tankTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_assassin");
        }
        else if (tier == TankID.RocketDefender)
        {
            Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_rocket");

            _tankTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_rocket");
        }
        else if (tier == TankID.Electro)
        {
            Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_electro");

            _tankTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_electro");
        }
        else if (tier == TankID.Explosive)
        {
            Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_explosive");

            _tankTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_explosive");
        }
        else
        {
            Model = GameResources.GetGameResource<Model>("Assets/tank_e");
        }

        #endregion

        //CannonMesh = Model.Meshes["Cannon"];

        //boneTransforms = new Matrix[Model.Bones.Count];

        _shadowTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank_shadow");

        AiTankType = tier;

        if (setTankDefaults)
            ApplyDefaults(ref Properties);

        int index = Array.IndexOf(GameHandler.AllAITanks, null);

        if (index < 0) {
            return;
        }

        AITankId = index;

        GameHandler.AllAITanks[index] = this;

        int index2 = Array.IndexOf(GameHandler.AllTanks, null);

        if (index2 < 0) {
            WorldId = -1;
            GC.Collect(); // guh?
            return;
        }

        // so not all tanks pursuit at the same time (it looks mad weird if they do)
        if (AiParams.PursuitFrequency > 0)
            AiParams.PursuitFrequency += GameHandler.GameRand.Next(-AiParams.PursuitFrequency / 4, AiParams.PursuitFrequency / 4 + 1);

        WorldId = index2;

        GameHandler.AllTanks[index2] = this;

        base.Initialize();
    }
    public override void ApplyDefaults(ref TankProperties properties) {
        properties.DestructionColor = TankDestructionColors[AiTankType];
        AiParams = this.GetAiDefaults(properties, AiTankType);

        if (properties.Stationary) {
            properties.Deceleration = 0.2f;
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
        properties.TreadVolume = 0.05f;
        base.ApplyDefaults(ref properties);
    }
    public override void Update()
    {
        if (ModLoader.Status != LoadStatus.Complete)
            return;

        base.Update();

        //CannonMesh.ParentBone.Transform = Matrix.CreateRotationY(TurretRotation + TankRotation + (Flip ? MathHelper.Pi : 0));

        if (Dead || !MapRenderer.ShouldRenderAll)
            return;
        if (AiTankType == TankID.Commando)
        {
            Model.Meshes["Laser_Beam"].ParentBone.Transform = Matrix.CreateRotationY(TurretRotation + TankRotation + (Flip ? MathHelper.Pi : 0));
            Model.Meshes["Barrel_Laser"].ParentBone.Transform = Matrix.CreateRotationY(TurretRotation + TankRotation + (Flip ? MathHelper.Pi : 0));
            Model.Meshes["Dish"].ParentBone.Transform = Matrix.CreateRotationY(TurretRotation + TankRotation + (Flip ? MathHelper.Pi : 0));
        }
        if (TankGame.SuperSecretDevOption) {
            var tnkGet = Array.FindIndex(GameHandler.AllAITanks, x => x is not null && !x.Dead && !x.Properties.Stationary);
            if (tnkGet > -1)
                if (AITankId == GameHandler.AllAITanks[tnkGet].AITankId)
                    TargetTankRotation = (MatrixUtils.ConvertWorldToScreen(Vector3.Zero, World, View, Projection) - MouseUtils.MousePosition).ToRotation() + MathHelper.PiOver2;
        }
        if ((Server.serverNetManager is not null && Client.IsConnected()) || (!Client.IsConnected() && !Dead) || MainMenu.Active)
        {
            timeSinceLastAction++;

            if (!MainMenu.Active)
                if (!GameProperties.InMission || IntermissionSystem.IsAwaitingNewMission || LevelEditor.Active)
                    Velocity = Vector2.Zero;
            DoAi(true, true, true);

            if (IsIngame)
                Client.SyncAITank(this);
        }
    }
    public override void Remove(bool nullifyMe)
    {
        if (nullifyMe) {
            GameHandler.AllAITanks[AITankId] = null;
            GameHandler.AllTanks[WorldId] = null;
        }
        base.Remove(nullifyMe);
    }
    public override void Destroy(ITankHurtContext context)
    {
        // might not account for level testing via the level editor?
        OnDestroy?.Invoke();
        if (!MainMenu.Active && !LevelEditor.Active && !Client.IsConnected()) // goofy ahh...
        {
            PlayerTank.KillCount++;

            if (!PlayerTank.TankKills.ContainsKey(AiTankType))
                PlayerTank.TankKills.Add(AiTankType, 1);
            else
                PlayerTank.TankKills[AiTankType]++;

            if (context.IsPlayer)
            {
                var rnd = GameHandler.GameRand.NextFloat(0, 1);

                // check if less than certain values for different value coins

                if (context is TankHurtContextShell cxt1)
                {
                    TankGame.GameData.BulletKills++;
                    TankGame.GameData.TotalKills++;

                    if (cxt1.Bounces > 0)
                        TankGame.GameData.BounceKills++;
                }
                if (context is TankHurtContextMine cxt2)
                {
                    TankGame.GameData.MineKills++;
                    TankGame.GameData.TotalKills++;
                }

                if (TankGame.GameData.TankKills.ContainsKey(AiTankType))
                    TankGame.GameData.TankKills[AiTankType]++;

                // haaaaaaarddddddcode
                if (!LevelEditor.Editing)
                {
                    var rand = GameHandler.GameRand.NextFloat(-(BaseExpValue * 0.25f), BaseExpValue * 0.25f);
                    var gain = BaseExpValue + rand;
                    // i will keep this commented if anything else happens.
                    //var gain = (BaseExpValue + rand) * GameData.UniversalExpMultiplier;
                    TankGame.GameData.ExpLevel += gain;

                    var p = GameHandler.ParticleSystem.MakeParticle(Position3D + new Vector3(0, 30, 0), $"+{gain * 100:0.00} XP");

                    p.Scale = new(0.5f);
                    p.Roll = MathHelper.Pi;
                    p.Origin2D = TankGame.TextFont.MeasureString($"+{gain * 100:0.00} XP") / 2;

                    p.UniqueBehavior = (p) =>
                    {
                        p.Position.Y += 0.1f * TankGame.DeltaTime;

                        p.Alpha -= 0.01f * TankGame.DeltaTime;

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
        base.Destroy(context);
    }

    public bool IsPathBlocked;

    public bool IsEnemySpotted;

    private bool isSeeking;
    private float seekRotation;

    public bool IsNearDestructibleObstacle;

    // make a new method for just any rectangle

    // TODO: literally fix everything about these turret rotation values.
    private List<Tank> GetTanksInPath(Vector2 pathDir, out Vector2 rayEndpoint, bool draw = false, Vector2 offset = default, float missDist = 0f, Func<Block, bool> pattern = null, bool doBounceReset = true) {
        rayEndpoint = new(-999999, -999999);
        List<Tank> tanks = new();
        pattern ??= (c) => c.IsSolid || c.Type == BlockID.Teleporter;

        const int MAX_PATH_UNITS = 1000;
        const int PATH_UNIT_LENGTH = 8;

        // 20, 30

        var whitePixel = GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel");
        var pathPos = Position + offset.RotatedByRadians(-TurretRotation);

        pathDir.Y *= -1; // this may be a culprit and i hate it.
        pathDir *= PATH_UNIT_LENGTH;
        int pathRicochetCount = 0;

        int uninterruptedIterations = 0;

        bool goneThroughTeleporter = false;
        int tpidx = -1;
        Vector2 tpos = Vector2.Zero;

        for (int i = 0; i < MAX_PATH_UNITS; i++) {
            var dummyPos = Vector2.Zero;

            uninterruptedIterations++;

            if (pathPos.X < MapRenderer.MIN_X || pathPos.X > MapRenderer.MAX_X) {
                pathDir.X *= -1;
                pathRicochetCount++;
                resetIterations();
            }
            else if (pathPos.Y < MapRenderer.MIN_Y || pathPos.Y > MapRenderer.MAX_Y) {
                pathDir.Y *= -1;
                pathRicochetCount++;
                resetIterations();
            }

            var pathHitbox = new Rectangle((int)pathPos.X - 5, (int)pathPos.Y - 5, 8, 8);

            // Why is velocity passed by reference here lol
            Collision.HandleCollisionSimple_ForBlocks(pathHitbox, pathDir, ref dummyPos, out var dir, out var block, out bool corner, false, pattern);
            if (corner)
                return tanks;

            if (block is not null) {
                if (block.Type == BlockID.Teleporter) {
                    if (!goneThroughTeleporter) {
                        var otherTp = Block.AllBlocks.FirstOrDefault(bl => bl != null && bl != block && bl.TpLink == block.TpLink);

                        if (Array.IndexOf(Block.AllBlocks, otherTp) > -1) {
                            //pathPos = otherTp.Position;
                            tpos = otherTp!.Position;
                            goneThroughTeleporter = true;
                            tpidx = i + 1;
                        }
                    }
                }
                else {
                    if (block.AllowShotPathBounce) {
                        switch (dir) {
                            case CollisionDirection.Up:
                            case CollisionDirection.Down:
                                pathDir.Y *= -1;
                                pathRicochetCount += block.PathBounceCount;
                                resetIterations();
                                break;
                            case CollisionDirection.Left:
                            case CollisionDirection.Right:
                                pathDir.X *= -1;
                                pathRicochetCount += block.PathBounceCount;
                                resetIterations();
                                break;
                        }
                    }
                }
            }

            if (goneThroughTeleporter && i == tpidx)
                pathPos = tpos;

            void resetIterations() { if (doBounceReset) uninterruptedIterations = 0; }

            if (i == 0 && Block.AllBlocks.Any(x => x is not null && x.Hitbox.Intersects(pathHitbox) && pattern is not null && pattern.Invoke(x))) {
                rayEndpoint = pathPos;
                return tanks;
            }

            if (i < (int)Properties.ShellSpeed / 2 && pathRicochetCount > 0) {
                rayEndpoint = pathPos;
                return tanks;
            }

            if (pathRicochetCount > Properties.RicochetCount) {
                rayEndpoint = pathPos;
                return tanks;
            }

            pathPos += pathDir;
            var realMiss = 1f + (missDist * uninterruptedIterations);
            if (draw) {
                var pathPosScreen = MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(pathPos.X, 11, pathPos.Y), TankGame.GameView, TankGame.GameProjection);
                TankGame.SpriteRenderer.Draw(whitePixel, pathPosScreen, null, Color.White * 0.5f, 0, whitePixel.Size() / 2, /*2 + (float)Math.Sin(i * Math.PI / 5 - TankGame.GameUpdateTime * 0.1f) * */realMiss, default, default);
                // DebugUtils.DrawDebugString(TankGame.spriteBatch, $"{goneThroughTeleporter}:{(block is not null ? $"{block.Type}" : "N/A")}", MatrixUtils.ConvertWorldToScreen(new Vector3(0, 11, 0), Matrix.CreateTranslation(pathPos.X, 0, pathPos.Y), View, Projection), 1, centered: true);
            }

            foreach (var enemy in GameHandler.AllTanks)
                if (enemy is not null) {
                    if (!enemy.Dead) {
                        if (!tanks.Contains(enemy)) {
                            if (i > 15) {
                                if (GameUtils.Distance_WiiTanksUnits(enemy.Position, pathPos) <= realMiss)
                                    tanks.Add(enemy);
                            }
                            else if (enemy.CollisionBox.Intersects(pathHitbox)) {
                                tanks.Add(enemy);
                            }
                        }
                    }
                }

        }
        return tanks;
    }

    public const int PATH_UNIT_LENGTH = 8;
    public int PathHitMax = 10;
    private bool IsObstacleInWay(int checkDist, Vector2 pathDir, out Vector2 endpoint, out Vector2[] reflectPoints, int size = 1, bool draw = false)
    {
        bool hasCollided = false;

        var list = new List<Vector2>();

        var whitePixel = GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel");
        var pathPos = Position;

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

            var pathHitbox = new Rectangle((int)pathPos.X, (int)pathPos.Y, size, size);

            // Why is velocity passed by reference here lol
            Collision.HandleCollisionSimple_ForBlocks(pathHitbox, pathDir, ref dummyPos, out var dir, out var block, out bool corner, false);

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
                //var sin = 2 + MathF.Sin(i * MathF.PI / 5 - TankGame.UpdateCount * 0.3f);
                var pathPosScreen = MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(pathPos.X, 11, pathPos.Y), TankGame.GameView, TankGame.GameProjection);
                TankGame.SpriteRenderer.Draw(whitePixel, pathPosScreen, null, Color.White, 0, whitePixel.Size() / 2, new Vector2(size, size), default, default);
            }
        }
        reflectPoints = [.. list];
        endpoint = pathPos;
        return hasCollided;
    }

    private bool _predicts;

    // TODO: make view distance, and make tanks in path public
    public void UpdateAim(List<Tank> tanksNear, bool fireWhen) {
        _predicts = false;
        SeesTarget = false;

        bool tooCloseToExplosiveShell = false;

        List<Tank> tanksDef;

        if (Properties.ShellType == ShellID.Explosive)
        {
            tanksDef = GetTanksInPath(Vector2.UnitY.RotatedByRadians(TurretRotation - MathHelper.Pi), out var rayEndpoint, offset: Vector2.UnitY * 20, pattern: x => (!x.IsDestructible && x.IsSolid) || x.Type == BlockID.Teleporter, missDist: AiParams.Inaccuracy, doBounceReset: AiParams.BounceReset);
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
                float rot = -MathUtils.DirectionOf(Position,
                    GeometryUtils.PredictFuturePosition(TargetTank.Position, TargetTank.Velocity, calculation))
                    .ToRotation() - MathHelper.PiOver2;

                tanksDef = GetTanksInPath(
                Vector2.UnitY.RotatedByRadians(-MathUtils.DirectionOf(Position, TargetTank.Position).ToRotation() - MathHelper.PiOver2),
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
        var findsEnemy = tanksDef.Any(tnk => tnk is not null && (tnk.Team != Team || tnk.Team == TeamID.NoTeam) && tnk != this);
        var findsSelf = tanksDef.Any(tnk => tnk is not null && tnk == this);
        var findsFriendly = tanksDef.Any(tnk => tnk is not null && (tnk.Team == Team && tnk.Team != TeamID.NoTeam));

        if (findsEnemy && !tooCloseToExplosiveShell)
            SeesTarget = true;

        // ChatSystem.SendMessage($"tier: {tier} | enemy: {findsEnemy} | self: {findsSelf} | friendly: {findsFriendly} | Count: {tanksDef.Count}", Color.White);

        if (AiParams.SmartRicochets)
        {
            //if (!seeks)
            seekRotation += AiParams.TurretSpeed * 0.5f;
            var canShoot = !(CurShootCooldown > 0 || OwnedShellCount >= Properties.ShellLimit);
            if (canShoot)
            {
                var tanks = GetTanksInPath(Vector2.UnitY.RotatedByRadians(seekRotation), out var rayEndpoint, false, default, AiParams.Inaccuracy, doBounceReset: AiParams.BounceReset);

                var findsEnemy2 = tanks.Any(tnk => tnk is not null && (tnk.Team != Team || tnk.Team == TeamID.NoTeam) && tnk != this);
                // var findsSelf2 = tanks.Any(tnk => tnk is not null && tnk == this);
                // var findsFriendly2 = tanks.Any(tnk => tnk is not null && (tnk.Team == Team && tnk.Team != TeamID.NoTeam));
                // ChatSystem.SendMessage($"{findsEnemy2} {findsFriendly2} | seek: {seeks}", Color.White);
                if (findsEnemy2/* && !findsFriendly2*/)
                {
                    isSeeking = true;
                    TurretRotationMultiplier = 3f;
                    TargetTurretRotation = seekRotation - MathHelper.Pi;
                }
            }

            if (TurretRotation == TargetTurretRotation || !canShoot)
                isSeeking = false;
        }

        bool checkNoTeam = Team == TeamID.NoTeam || !tanksNear.Any(x => x.Team == Team);

        if (AiParams.PredictsPositions)
        {
            if (SeesTarget && checkNoTeam && fireWhen)
                if (CurShootCooldown <= 0)
                    Shoot(false);
        }
        else
        {
            if (SeesTarget && checkNoTeam && !findsSelf && !findsFriendly && fireWhen)
                if (CurShootCooldown <= 0)
                    Shoot(false);
        }
    }

    public Tank? TargetTank;

    public float TurretRotationMultiplier = 1f;

    private bool _oldPathBlocked;
    private int _pathHitCount;
    public bool AutoEnactAIBehavior = true;

    public static int TankPathCheckSize = 3;
    public void DoAi(bool doMoveTowards = true, bool doMovements = true, bool doFire = true) {
        if (!MainMenu.Active && !GameProperties.InMission)
            return;

        TurretRotationMultiplier = 1f;
        // AiParams.DeflectsBullets = true;
        for (int i = 0; i < Behaviors.Length; i++)
            Behaviors[i].Value += TankGame.DeltaTime;

        // defining an Action isn't that intensive, right?
        AIBehaviorAction = () => {
            TargetTank = GameHandler.AllTanks.FirstOrDefault(tnk => tnk is not null && !tnk.Dead && (tnk.Team != Team || tnk.Team == TeamID.NoTeam) && tnk != this)!;

            foreach (var tank in GameHandler.AllTanks) {
                if (tank is not null && !tank.Dead && (tank.Team != Team || tank.Team == TeamID.NoTeam) && tank != this)
                    if (GameUtils.Distance_WiiTanksUnits(tank.Position, Position) < GameUtils.Distance_WiiTanksUnits(TargetTank.Position, Position))
                        if ((tank.Properties.Invisible && tank.timeSinceLastAction < 60) || !tank.Properties.Invisible)
                            TargetTank = tank;
            }

            // measure the biggest WarinessRadius, player or ai, then check the larger, then do manual calculations.
            var radii = new float[] { AiParams.MineWarinessRadius_AILaid, AiParams.MineWarinessRadius_PlayerLaid, AiParams.ProjectileWarinessRadius_AIShot, AiParams.ProjectileWarinessRadius_PlayerShot };
            var biggest = radii.Max();
            bool isThereDanger = TryGetDangerNear(biggest, out var danger);

            if (isThereDanger) {
                WhileDangerDetected?.Invoke(this, danger!);
                for (int i = 0; i < ModLoader.ModTanks.Length; i++)
                    if (AiTankType == ModLoader.ModTanks[i].Type)
                        ModLoader.ModTanks[i].DangerDetected(this, danger!);
            }

            var isShellNear = isThereDanger && danger is Shell;
            var isMineNear = isThereDanger && danger is Mine;
            var isExplNear = isThereDanger && danger is Explosion;

            // only use if checking the respective boolean!
            var shell = (danger as Shell)!;
            var mine = (danger as Mine)!;
            var explosion = (danger as Explosion)!;

            var tanksNearMe = new List<Tank>();
            var blocksNearMe = new List<Block>();

            foreach (var tank in GameHandler.AllTanks)
                if (tank != this && tank is not null && !tank.Dead && GameUtils.Distance_WiiTanksUnits(tank.Position, Position) <= AiParams.TankWarinessRadius)
                    tanksNearMe.Add(tank);

            foreach (var block in Block.AllBlocks)
                if (block is not null && GameUtils.Distance_WiiTanksUnits(Position, block.Position) < AiParams.BlockWarinessDistance)
                    blocksNearMe.Add(block);

            if (AiParams.DeflectsBullets) {
                if (isThereDanger && isShellNear) {
                    if (shell.LifeTime > 60) {
                        var dir = MathUtils.DirectionOf(Position, shell.Position);
                        var rotation = dir.ToRotation();
                        var calculation = (Position.Distance(shell.Position) - 20f) / (float)(Properties.ShellSpeed * 1.2f);
                        float rot = -MathUtils.DirectionOf(Position,
                            GeometryUtils.PredictFuturePosition(shell.Position, shell.Velocity, calculation))
                            .ToRotation() + MathHelper.PiOver2;

                        TargetTurretRotation = rot;

                        TurretRotationMultiplier = 4f;

                        rot %= MathHelper.Tau;

                        //if ((-TurretRotation + MathHelper.PiOver2).IsInRangeOf(TargetTurretRotation, 0.15f))
                        Shoot(false);
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
            bool targetExists = Array.IndexOf(GameHandler.AllTanks, TargetTank) > -1 && TargetTank is not null;
            if (targetExists) {
                if (!isSeeking && !_predicts) {
                    if (Behaviors[1].IsModOf(AiParams.TurretMeanderFrequency)) {
                        IsEnemySpotted = false;
                        if (TargetTank!.Properties.Invisible && TargetTank.timeSinceLastAction < 60) {
                            AimTarget = TargetTank.Position;
                            IsEnemySpotted = true;
                        }

                        if (!TargetTank.Properties.Invisible) {
                            AimTarget = TargetTank.Position;
                            IsEnemySpotted = true;
                        }

                        var dirVec = Position - AimTarget;
                        TargetTurretRotation = -dirVec.ToRotation() - MathHelper.PiOver2 + GameHandler.GameRand.NextFloat(-AiParams.AimOffset, AiParams.AimOffset);
                    }
                }

                if (doFire)
                    UpdateAim(tanksNearMe, !isMineNear);

                TurretRotation = MathUtils.RoughStep(TurretRotation, TargetTurretRotation, AiParams.TurretSpeed * TurretRotationMultiplier * TankGame.DeltaTime);
            }

            #endregion
            if (doMovements) {
                if (Properties.Stationary)
                    return;

                // fix the floatiness
                #region ShellAvoidance

                var indif = 3;

                if (isShellNear) {
                    if (Behaviors[6].IsModOf(indif)) {
                        // with .IsHeadingTowards we don't need a lifetime check anymore lol.

                        // TODO: add all shells that may or may not be near, average their position and make the tank go away from that position
                        if (CurMineStun <= 0 && CurShootStun <= 0) {
                            var dist = shell.IsPlayerSourced ? AiParams.ProjectileWarinessRadius_PlayerShot : AiParams.ProjectileWarinessRadius_AIShot;
                            isShellNear = shell.LifeTime > 60; //shell.IsHeadingTowards(Position, dist, MathHelper.PiOver2);
                            if (isShellNear) {
                                var direction = -Vector2.UnitY.RotatedByRadians(shell.Position.DirectionOf(Position, false).ToRotation());
                                TargetTankRotation = direction.ToRotation();
                            }
                        }
                    }
                }

                #endregion


                // facing down = 0 radians/2pi radians
                #region BlockNav
                if (Behaviors[2].IsModOf(AiParams.BlockReadTime) && !isMineNear && !isShellNear) {
                    IsPathBlocked = IsObstacleInWay(AiParams.BlockWarinessDistance / PATH_UNIT_LENGTH, Vector2.UnitY.RotatedByRadians(TargetTankRotation), out var travelPath, out var refPoints, TankPathCheckSize);
                    if (IsPathBlocked) {
                        if (refPoints.Length > 0) {
                            // up = PiOver2
                            var dirOf = MathUtils.DirectionOf(travelPath, Position).ToRotation();
                            var refAngle = dirOf + MathHelper.PiOver2;

                            if (refAngle % MathHelper.PiOver2 == 0) {
                                refAngle += GameHandler.GameRand.NextFloat(0, MathHelper.TwoPi);
                            }
                            TargetTankRotation = refAngle;
                        }
                    }
                    // FIXME: experimental.
                    if (IsPathBlocked && !_oldPathBlocked) {
                        //if (GameHandler.GameRand.NextFloat(0f, 1f) <= 0.25f)
                        _pathHitCount++;
                        // this check probably isn't doing what it should.
                        //if (_pathHitCount % PathHitMax == 0)
                            //TargetTankRotation = -TargetTankRotation + MathHelper.PiOver2;
                    }

                    _oldPathBlocked = IsPathBlocked;

                    // TODO: i literally do not understand this
                }

                #endregion

                #region GeneralMovement

                if (!isMineNear && !isShellNear && !IsTurning && CurMineStun <= 0 && CurShootStun <= 0) {
                    if (!IsPathBlocked) {
                        if (Behaviors[0].IsModOf(AiParams.MeanderFrequency)) {
                            var random = GameHandler.GameRand.NextFloat(-AiParams.MeanderAngle / 2, AiParams.MeanderAngle / 2);

                            TargetTankRotation += random;
                        }
                        // disabling aggression for now.
                        /*if (targetExists) {
                            if (AiParams.PursuitFrequency != 0) {
                                if (Behaviors[0].IsModOf(AiParams.PursuitFrequency)) {
                                    float angleToTarget = MathUtils.DirectionOf(Position, TargetTank!.Position).ToRotation() - MathHelper.PiOver2;

                                    // we want to go AWAY from the target.
                                    if (AiParams.PursuitLevel < 0)
                                        angleToTarget += MathHelper.PiOver2;

                                    var targetAngle = (angleToTarget) * AiParams.PursuitLevel;

                                    TargetTankRotation = targetAngle;
                                }
                            }
                        }*/
                    }
                }
                #endregion

                #region MineHandle / MineAvoidance
                if (!isMineNear && !isShellNear) {
                    if (Properties.MineLimit > 0) {
                        // why is 60 hardcoded xd
                        if (Behaviors[4].IsModOf(60)) {
                            if (!tanksNearMe.Any(x => x.Team == Team)) {
                                IsNearDestructibleObstacle = blocksNearMe.Any(c => c.IsDestructible);
                                if (AiParams.SmartMineLaying) {
                                    if (IsNearDestructibleObstacle) {
                                        // (100, 100)? why?
                                        TargetTankRotation = new Vector2(100, 100).RotatedByRadians(GameHandler.GameRand.NextFloat(0, MathHelper.TwoPi)).ToRotation();
                                        LayMine();
                                    }
                                }
                                else {
                                    if (GameHandler.GameRand.NextFloat(0, 1) <= AiParams.MinePlacementChance) {
                                        TargetTankRotation = new Vector2(100, 100).RotatedByRadians(GameHandler.GameRand.NextFloat(0, MathHelper.TwoPi)).ToRotation();
                                        LayMine();
                                    }
                                }
                            }
                        }
                    }
                }
                if (isMineNear && !isShellNear && !isExplNear) {
                    // why is 10 hardcoded xd
                    if (Behaviors[5].IsModOf(10)) {
                        var dist = mine.IsPlayerSourced ? AiParams.MineWarinessRadius_PlayerLaid : AiParams.MineWarinessRadius_AILaid;
                        var direction = -Vector2.UnitY.RotatedByRadians(mine.Position.DirectionOf(Position, false).ToRotation());

                        TargetTankRotation = direction.ToRotation();
                    }
                }
                #endregion

                #region ExplosionAvoidance

                // FIXME?: is the modulus check more sensible to come first or not? only time will tell.
                if (isExplNear) {
                    if (Behaviors[5].IsModOf(10)) {
                        var dist = explosion.IsPlayerSourced ? AiParams.MineWarinessRadius_PlayerLaid : AiParams.MineWarinessRadius_AILaid;
                        var direction = -Vector2.UnitY.RotatedByRadians(explosion.Position.DirectionOf(Position, false).ToRotation());
                        TargetTankRotation = direction.ToRotation();
                    }
                }

                #endregion
            }

            #region Special Tank Behavior

            // TODO: just use inheritance?

            if (AiTankType == TankID.Creeper) {
                if (Array.IndexOf(GameHandler.AllTanks, TargetTank) > -1 && TargetTank is not null) {
                    float explosionDist = 90f;
                    if (GameUtils.Distance_WiiTanksUnits(TargetTank.Position, Position) < explosionDist) {
                        Destroy(new TankHurtContextOther("CreeperTankExplosion"));

                        new Explosion(Position, 10f, this, 0.2f);
                    }
                }
            }
            else if (AiTankType == TankID.Cherry)
            {
                if (SeesTarget)
                {
                    if (SpecialBehaviors[0].Value <= 0)
                    {
                        SoundPlayer.PlaySoundInstance("Assets/sounds/tnk_event/alert.ogg", SoundContext.Effect, 0.6f, gameplaySound: true);
                        Add2DCosmetic(CosmeticChest.Anger, () => SpecialBehaviors[0].Value <= 0);
                    }
                    SpecialBehaviors[0].Value = 300;
                }
                else
                    if (SpecialBehaviors[0].Value > 0)
                    SpecialBehaviors[0].Value -= TankGame.DeltaTime;
                if (SpecialBehaviors[0].Value > 0)
                    Properties.MaxSpeed = 2.2f;
                else
                    Properties.MaxSpeed = 1.35f;
            }
            else if (AiTankType == TankID.Electro) {
                SpecialBehaviors[0].Value += TankGame.DeltaTime;

                if (SpecialBehaviors[1].Value == 0)
                    SpecialBehaviors[1].Value = GameHandler.GameRand.NextFloat(180, 360);

                if (SpecialBehaviors[0].Value > SpecialBehaviors[1].Value) {
                    SpecialBehaviors[2].Value += TankGame.DeltaTime;

                    GameHandler.ParticleSystem.MakeShineSpot(Position3D, Color.Blue, 1f);

                    if (SpecialBehaviors[2].Value > 180f) {

                        SpecialBehaviors[2].Value = 0f;
                        SpecialBehaviors[0].Value = 0f;
                        SpecialBehaviors[1].Value = 0f;

                        var r = RandomUtils.PickRandom(PlacementSquare.Placements.Where(x => x.BlockId == -1).ToArray());

                        Body.Position = r.Position.FlattenZ() / UNITS_PER_METER;
                    }
                }
            }
            else if (AiTankType == TankID.Commando)
            {
                SpecialBehaviors[0].Value += TankGame.DeltaTime;
                if (SpecialBehaviors[1].Value == 0)
                    SpecialBehaviors[1].Value = GameHandler.GameRand.NextFloat(400, 600);

                if (SpecialBehaviors[0].Value > SpecialBehaviors[1].Value) {
                    SpecialBehaviors[1].Value = 0f;
                    SpecialBehaviors[0].Value = 0f;

                    var r = RandomUtils.PickRandom(PlacementSquare.Placements.Where(x => x.BlockId == -1).ToArray());

                    var crate = Crate.SpawnCrate(r.Position + new Vector3(0, 500, 0), 2f);
                    crate.TankToSpawn = new TankTemplate() {
                        AiTier = PickRandomTier(),
                        IsPlayer = false,
                        Team = Team
                    };
                }
            }

            #endregion

            #region TankRotation

            // i really hope to remove this hardcode.
            if (doMoveTowards)
            {
                // this is repeated in AITank for less obfuscation.
                var negDif = TargetTankRotation - Properties.MaximalTurn - MathHelper.ToRadians(5);
                var posDif = TargetTankRotation + Properties.MaximalTurn + MathHelper.ToRadians(5);

                IsTurning = !(TankRotation > negDif && TankRotation < posDif);

                if (!IsTurning) {
                    Speed += Properties.Acceleration * TankGame.DeltaTime;
                    if (Speed > Properties.MaxSpeed)
                        Speed = Properties.MaxSpeed;
                }
                else {
                    Speed -= Properties.Deceleration * TankGame.DeltaTime;
                    if (Speed < 0)
                        Speed = 0;
                }
                // TODO: fix this pls.
                /*if (TankRotation > MathHelper.Tau)
                    TankRotation -= MathHelper.Tau;
                if (TankRotation < 0)
                    TankRotation += MathHelper.Tau;*/
                // %=?
                if (TargetTankRotation > MathHelper.Tau)
                    TargetTankRotation -= MathHelper.Tau;
                if (TargetTankRotation < 0)
                    TargetTankRotation += MathHelper.Tau;

                var dir = Vector2.UnitY.RotatedByRadians(TankRotation);
                Velocity.X = dir.X;
                Velocity.Y = dir.Y;

                Velocity.Normalize();

                Velocity *= Speed;
                TankRotation = MathUtils.RoughStep(TankRotation, TargetTankRotation, Properties.TurningSpeed * TankGame.DeltaTime);
            }

            #endregion
        };
        if (AutoEnactAIBehavior)
            AIBehaviorAction?.Invoke();
        OnPostUpdateAI?.Invoke(this);
        for (int i = 0; i < ModLoader.ModTanks.Length; i++)
            if (AiTankType == ModLoader.ModTanks[i].Type)
                ModLoader.ModTanks[i].PostUpdate(this);
    }
    public override void Render() {
        base.Render();
        if (Dead || !MapRenderer.ShouldRenderAll)
            return;
        TankGame.Instance.GraphicsDevice.BlendState = BlendState.AlphaBlend;
        DrawExtras();
        if ((MainMenu.Active && Properties.Invisible) || (Properties.Invisible && GameProperties.InMission))
            return;

        for (int i = 0; i < (Lighting.AccurateShadows ? 2 : 1); i++) {
            foreach (ModelMesh mesh in Model.Meshes) {
                foreach (BasicEffect effect in mesh.Effects) {
                    effect.World = i == 0 ? _boneTransforms[mesh.ParentBone.Index] : _boneTransforms[mesh.ParentBone.Index] * Matrix.CreateShadow(Lighting.AccurateLightingDirection, new(Vector3.UnitY, 0)) * Matrix.CreateTranslation(0, 0.2f, 0);
                    effect.View = View;
                    effect.Projection = Projection;

                    effect.TextureEnabled = true;

                    if (!Properties.HasTurret)
                        if (mesh.Name == "Cannon")
                            return;

                    if (mesh.Name == "Shadow") {
                        if (!Lighting.AccurateShadows) {
                            effect.Texture = _shadowTexture;
                            effect.Alpha = 0.5f;
                            mesh.Draw();
                        }
                    }
                    else {
                        if (IsHoveredByMouse)
                            effect.EmissiveColor = Color.White.ToVector3();
                        else
                            effect.EmissiveColor = Color.Black.ToVector3();

                        effect.Texture = _tankTexture;
                        effect.Alpha = 1;
                        mesh.Draw();

                        // TODO: uncomment code when disabling implementation is re-implemented.

                        if (ShowTeamVisuals) {
                            if (Team != TeamID.NoTeam) {
                                //var ex = new Color[1024];

                                //Array.Fill(ex, new Color(GameHandler.GameRand.Next(0, 256), GameHandler.GameRand.Next(0, 256), GameHandler.GameRand.Next(0, 256)));

                                //effect.Texture.SetData(0, new Rectangle(0, 8, 32, 15), ex, 0, 480);
                                var ex = new Color[1024];

                                Array.Fill(ex, TeamID.TeamColors[Team]);

                                effect.Texture.SetData(0, new Rectangle(0, 0, 32, 9), ex, 0, 288);
                                effect.Texture.SetData(0, new Rectangle(0, 23, 32, 9), ex, 0, 288);
                            }
                        }
                    }

                    effect.SetDefaultGameLighting_IngameEntities(0.9f);
                }
            }
        }
    }
    private void DrawExtras()
    {
        if (Dead)
            return;

        if (DebugUtils.DebugLevel == DebugUtils.Id.EntityData)
        {
            float calculation = 0f;

            if (AiParams.PredictsPositions && TargetTank is not null)
                calculation = Position.Distance(TargetTank.Position) / (float)(Properties.ShellSpeed * 1.2f);

            if (AiParams.SmartRicochets)
                GetTanksInPath(Vector2.UnitY.RotatedByRadians(seekRotation), out var rayEndpoint, true, missDist: AiParams.Inaccuracy, doBounceReset: AiParams.BounceReset);
            var poo = GetTanksInPath(Vector2.UnitY.RotatedByRadians(TurretRotation - MathHelper.Pi), out var rayEnd, true, offset: Vector2.UnitY * 20, pattern: x => x.IsSolid | x.Type == BlockID.Teleporter, missDist: AiParams.Inaccuracy, doBounceReset: AiParams.BounceReset);
            if (AiParams.PredictsPositions)
            {
                float rot = -MathUtils.DirectionOf(Position, TargetTank is not null ?
                    GeometryUtils.PredictFuturePosition(TargetTank.Position, TargetTank.Velocity, calculation) :
                    AimTarget).ToRotation() - MathHelper.PiOver2;
                GetTanksInPath(Vector2.UnitY.RotatedByRadians(rot), out var rayEnd2, true, Vector2.Zero, pattern: x => x.IsSolid | x.Type == BlockID.Teleporter, missDist: AiParams.Inaccuracy, doBounceReset: AiParams.BounceReset);
            }
            DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"{poo.Count} tank(s) spotted | pathC: {_pathHitCount % PathHitMax} / 10", MatrixUtils.ConvertWorldToScreen(new Vector3(0, 11, 0), World, View, Projection), 1, centered: true);

        }
        if (DebugUtils.DebugLevel == 6 && !Properties.Stationary) {
            IsObstacleInWay(AiParams.BlockWarinessDistance / PATH_UNIT_LENGTH, Vector2.UnitY.RotatedByRadians(TargetTankRotation), out var travelPos, out var refPoints, TankPathCheckSize, draw: true);
            DebugUtils.DrawDebugString(TankGame.SpriteRenderer, "TEP", MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(travelPos.X, 11, travelPos.Y), View, Projection), 6, centered: true);
            foreach (var pt in refPoints)
                DebugUtils.DrawDebugString(TankGame.SpriteRenderer, "pt", MatrixUtils.ConvertWorldToScreen(new Vector3(0, 11, 0), Matrix.CreateTranslation(pt.X, 0, pt.Y), View, Projection), 6, centered: true);


            //DebugUtils.DrawDebugString(TankGame.SpriteRenderer, "end", MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(MathUtils.DirectionOf(travelPos, Position).X, 0, MathUtils.DirectionOf(travelPos, Position).Y), View, Projection), 6, centered: true);
        }

        if (Properties.Invisible && (GameProperties.InMission || MainMenu.Active))
            return;

        Properties.Armor?.Render();
    }
    public bool TryGetDangerNear(float distance, out IAITankDanger? danger) {
        IAITankDanger? closest = null;

        Span<IAITankDanger> dangers = Dangers.ToArray();

        ref var dangersSearchSpace = ref MemoryMarshal.GetReference(dangers);

        for (var i = 0; i < Dangers.Count; i++) {
            var currentDanger = Unsafe.Add(ref dangersSearchSpace, i);

            if (currentDanger is null) continue;

            var distanceFromMineToSelf = GameUtils.Distance_WiiTanksUnits(Position, currentDanger.Position);

            if (!(distanceFromMineToSelf < distance)) continue;

            if (closest == null || distanceFromMineToSelf <
                GameUtils.Distance_WiiTanksUnits(Position, closest.Position)) {
                closest = currentDanger;
            }
        }

        danger = closest;
        return closest != null;
    }

    private static readonly int[] workingTiers =
    {
        TankID.Brown, TankID.Marine, TankID.Yellow, TankID.Black, TankID.White, TankID.Pink, TankID.Violet, TankID.Green, TankID.Ash,
        TankID.Bronze, TankID.Silver, TankID.Sapphire, TankID.Ruby, TankID.Citrine, TankID.Amethyst, TankID.Emerald, TankID.Gold, TankID.Obsidian,
        TankID.Granite, TankID.Bubblegum, TankID.Water, TankID.Crimson, TankID.Tiger, TankID.Creeper, TankID.Gamma, TankID.Marble,
    };
    public static int PickRandomTier()
        => workingTiers[GameHandler.GameRand.Next(0, workingTiers.Length)];
}
