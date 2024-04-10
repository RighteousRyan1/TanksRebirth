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

namespace TanksRebirth.GameContent;
public partial class AITank : Tank  {
    // TODO: Make smoke bombs!

    public static List<IAITankDanger> Dangers = new();

    public delegate void PostExecuteAI(AITank tank);
    public static event PostExecuteAI OnPostUpdateAI;
    public AiBehavior[] Behaviors { get; private set; } // each of these should keep track of an action the tank performs
    public AiBehavior[] SpecialBehaviors { get; private set; }
    public int AiTankType;
    private Texture2D _tankTexture;
    private static Texture2D _shadowTexture;
    public Action enactBehavior;
    public int AITankId { get; private set; }

    public void ReassignId(int newId) => AITankId = newId;

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
    public void SwapTankTexture(Texture2D texture) => _tankTexture = texture;
    #region AiTankParams

    /// <summary>The AI parameter collection of this AI Tank.</summary>
    public AiParameters AiParams { get; set; } = new();

    public Vector2 AimTarget;

    /// <summary>Whether or not this tank sees its target. Generally should not be set, but the tank will shoot if able when this is true.</summary>
    public bool SeesTarget { get; set; }

    /// <summary>The target rotation for this tank's turret. <see cref="Tank.TurretRotation"/> will move towards this value at a rate of <see cref="TurretSpeed"/>.</summary>
    public float TargetTurretRotation;

    public float BaseExpValue { get; set; }

    #endregion
    
    public void Swap(int tier, bool setDefaults = true) {
        AiTankType = tier;

        var tierName = TankID.Collection.GetKey(tier).ToLower();

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
    /// <param name="setTankDefaults">Whether or not to give this <see cref="AITank"/> the default values.</param>
    /// /// <param name="isIngame">Whether or not this <see cref="AITank"/> is a gameplay tank or a cosmetic tank (i.e: display models on menus, etc).</param>
    public AITank(int tier, Range<int> tankRange = default, bool setTankDefaults = true, bool isIngame = true, bool isRandomizable = true) {
        IsIngame = isIngame;
        // TargetTankRotation = MathHelper.Pi;
        if (IsIngame)
        {
            if (Difficulties.Types["BumpUp"])
                tier++;
            if (Difficulties.Types["MeanGreens"])
                tier = TankID.Green;
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

                var tnkAsset = Assets[$"tank_" + TankID.Collection.GetKey(tier).ToLower()];

                var t = new Texture2D(TankGame.Instance.GraphicsDevice, tnkAsset.Width, tnkAsset.Height);

                var colors = new Color[tnkAsset.Width * tnkAsset.Height];

                tnkAsset.GetData(colors);

                t.SetData(colors);

                _tankTexture = t;
            }
        }
        else
            _tankTexture = Assets[$"tank_" + TankID.Collection.GetKey(tier).ToLower()];

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
        properties.TreadVolume = 0.05f;
        base.ApplyDefaults(ref properties);
    }
    public override void Update()
    {
        if (ModLoader.Status != LoadStatus.Complete)
            return;

        base.Update();

        //CannonMesh.ParentBone.Transform = Matrix.CreateRotationY(TurretRotation + TankRotation + (Flip ? MathHelper.Pi : 0));

        if (Dead || !MapRenderer.ShouldRender)
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

    public bool pathBlocked;

    public bool isEnemySpotted;

    private bool seeks;
    private float seekRotation = 0;

    public bool nearDestructibleObstacle;

    // make a new method for just any rectangle

    // TODO: literally fix everything about these turret rotation values.
    private List<Tank> GetTanksInPath(Vector2 pathDir, out Vector2 rayEndpoint, bool draw = false, Vector2 offset = default, float missDist = 0f, Func<Block, bool> pattern = null, bool doBounceReset = true)
    {
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
                            tpos = otherTp.Position;
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
                var pathPosScreen = MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(pathPos.X, 11, pathPos.Y), TankGame.GameView, TankGame.GameProjection);
                TankGame.SpriteRenderer.Draw(whitePixel, pathPosScreen, null, Color.White * 0.5f, 0, whitePixel.Size() / 2, /*2 + (float)Math.Sin(i * Math.PI / 5 - TankGame.GameUpdateTime * 0.1f) * */realMiss, default, default);
                // DebugUtils.DrawDebugString(TankGame.spriteBatch, $"{goneThroughTeleporter}:{(block is not null ? $"{block.Type}" : "N/A")}", MatrixUtils.ConvertWorldToScreen(new Vector3(0, 11, 0), Matrix.CreateTranslation(pathPos.X, 0, pathPos.Y), View, Projection), 1, centered: true);
            }

            foreach (var enemy in GameHandler.AllTanks)
                if (enemy is not null)
                {
                    if (!enemy.Dead)
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

        }
        return tanks;
    }

    public const int PATH_UNIT_LENGTH = 8;
    private bool IsObstacleInWay(int checkDist, Vector2 pathDir, out Vector2 endpoint, out Vector2[] reflectPoints, bool draw = false)
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
                var pathPosScreen = MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(pathPos.X, 11, pathPos.Y), TankGame.GameView, TankGame.GameProjection);
                TankGame.SpriteRenderer.Draw(whitePixel, pathPosScreen, null, Color.White, 0, whitePixel.Size() / 2, 2 + (float)Math.Sin(i * Math.PI / 5 - TankGame.UpdateCount * 0.3f), default, default);
            }
        }
        reflectPoints = list.ToArray();
        endpoint = pathDir;
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
                    seeks = true;
                    TurretRotationMultiplier = 3f;
                    TargetTurretRotation = seekRotation - MathHelper.Pi;
                }
            }

            if (TurretRotation == TargetTurretRotation || !canShoot)
                seeks = false;
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
    public void DoAi(bool doMoveTowards = true, bool doMovements = true, bool doFire = true) {
        if (!MainMenu.Active && !GameProperties.InMission)
            return;

        TurretRotationMultiplier = 1f;
        // AiParams.DeflectsBullets = true;
        for (int i = 0; i < Behaviors.Length; i++)
            Behaviors[i].Value += TankGame.DeltaTime;

        // defining an Action isn't that intensive, right?
        enactBehavior = () => {
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

            var isShellNear = isThereDanger && danger is Shell;
            var isMineNear = isThereDanger && danger is Mine;

            // only use if checking the respective boolean!
            var shell = (danger as Shell)!;
            var mine = (danger as Mine)!;

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
                if (!seeks && !_predicts) {
                    if (Behaviors[1].IsModOf(AiParams.TurretMeanderFrequency)) {
                        isEnemySpotted = false;
                        if (TargetTank!.Properties.Invisible && TargetTank.timeSinceLastAction < 60) {
                            AimTarget = TargetTank.Position;
                            isEnemySpotted = true;
                        }

                        if (!TargetTank.Properties.Invisible) {
                            AimTarget = TargetTank.Position;
                            isEnemySpotted = true;
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

                #region BlockNav
                if (Behaviors[2].IsModOf(AiParams.BlockReadTime) && !isMineNear && !isShellNear) {
                    pathBlocked = IsObstacleInWay(AiParams.BlockWarinessDistance / PATH_UNIT_LENGTH, Vector2.UnitY.RotatedByRadians(TargetTankRotation), out var travelPath, out var refPoints);
                    if (pathBlocked) {
                        if (refPoints.Length > 0) {
                            // float AngleSmoothStep(float angle, float target, float amount) => GameUtils.AngleLerp(angle, target, amount * amount * (3f - 2f * amount));
                            // why does this never work no matter what i do
                            // TODO: seek mental asylum
                            var refAngle = MathUtils.DirectionOf(Position, travelPath - new Vector2(0, 10000)).ToRotation();

                            // AngleSmoothStep(TargetTankRotation, refAngle, refAngle / 3);
                            // TargetTankRotation = -TargetTankRotation + MathHelper.PiOver2;
                            TargetTankRotation = MathUtils.RoughStep(TargetTankRotation, /*TargetTankRotation <= MathHelper.Pi ? -refAngle + MathHelper.PiOver2 : */refAngle, refAngle / 6);
                        }
                    }
                    // FIXME: experimental.
                    if (pathBlocked && !_oldPathBlocked) {
                        //if (GameHandler.GameRand.NextFloat(0f, 1f) <= 0.25f)
                        _pathHitCount++;
                        // this check probably isn't doing what it should.
                        if (_pathHitCount % 10 == 0)
                            TargetTankRotation = -TargetTankRotation + MathHelper.PiOver2;
                    }

                    _oldPathBlocked = pathBlocked;

                    // TODO: i literally do not understand this
                }

                #endregion

                #region GeneralMovement

                if (!isMineNear && !isShellNear && !IsTurning && CurMineStun <= 0 && CurShootStun <= 0) {
                    if (!pathBlocked) {
                        if (Behaviors[0].IsModOf(AiParams.MeanderFrequency)) {
                            float dir = -100;

                            if (targetExists)
                                dir = MathUtils.DirectionOf(Position, TargetTank!.Position).ToRotation();

                            var random = GameHandler.GameRand.NextFloat(-AiParams.MeanderAngle / 2, AiParams.MeanderAngle / 2);

                            TargetTankRotation += random;
                        }
                        if (targetExists) {
                            if (AiParams.PursuitFrequency != 0) {
                                if (Behaviors[0].IsModOf(AiParams.PursuitFrequency)) {
                                    float dir = -100;

                                    if (targetExists)
                                        dir = MathUtils.DirectionOf(Position, TargetTank!.Position).ToRotation();

                                    var random = GameHandler.GameRand.NextFloat(-AiParams.MeanderAngle / 2, AiParams.MeanderAngle / 2);

                                    var meanderRandom = dir != -100 ? random + (dir + MathHelper.PiOver2) + (0.2f * AiParams.PursuitLevel) : random;

                                    TargetTankRotation = meanderRandom;
                                }
                            }
                        }
                    }
                }
                #endregion

                // fix the floatiness
                #region ShellAvoidance

                var indif = 3;

                if (isShellNear) {
                    if (shell.LifeTime > 30) {
                        if (CurMineStun <= 0 && CurShootStun <= 0) {
                            if (Behaviors[6].IsModOf(indif)) {
                                var direction = -Vector2.UnitY.RotatedByRadians(shell.Position.DirectionOf(Position, false).ToRotation());
                                TargetTankRotation = direction.ToRotation();
                            }
                        }
                    }
                }

                #endregion

                #region MineHandle / MineAvoidance
                if (!isMineNear && !isShellNear) {
                    if (Properties.MineLimit > 0) {
                        if (Behaviors[4].IsModOf(60)) {
                            if (!tanksNearMe.Any(x => x.Team == Team)) {
                                nearDestructibleObstacle = blocksNearMe.Any(c => c.IsDestructible);
                                if (AiParams.SmartMineLaying) {
                                    if (nearDestructibleObstacle) {
                                        TargetTankRotation = new Vector2(100, 100).RotatedByRadians(GameHandler.GameRand.NextFloat(0, MathHelper.TwoPi))/*.ExpandZ()*/.ToRotation();
                                        LayMine();
                                    }
                                }
                                else {
                                    if (GameHandler.GameRand.NextFloat(0, 1) <= AiParams.MinePlacementChance) {
                                        TargetTankRotation = new Vector2(100, 100).RotatedByRadians(GameHandler.GameRand.NextFloat(0, MathHelper.TwoPi))/*.ExpandZ()*/.ToRotation();
                                        LayMine();
                                    }
                                }
                            }
                        }
                    }
                }
                if (isMineNear && !isShellNear) {
                    if (Behaviors[5].IsModOf(10)) {
                        var direction = -Vector2.UnitY.RotatedByRadians(mine.Position.DirectionOf(Position, false).ToRotation());

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
            enactBehavior?.Invoke();
        OnPostUpdateAI?.Invoke(this);
    }
    public override void Render() {
        base.Render();
        if (Dead || !MapRenderer.ShouldRender)
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

        if (DebugUtils.DebugLevel == 1)
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
            DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"{poo.Count} tank(s) spotted | pathC: {_pathHitCount % 10} / 10", MatrixUtils.ConvertWorldToScreen(new Vector3(0, 11, 0), World, View, Projection), 1, centered: true);
            if (!Properties.Stationary)
            {
                IsObstacleInWay(AiParams.BlockWarinessDistance / PATH_UNIT_LENGTH, Vector2.UnitY.RotatedByRadians(TargetTankRotation), out var travelPos, out var refPoints, true);
                DebugUtils.DrawDebugString(TankGame.SpriteRenderer, "TRAVELENDPOINT", MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(travelPos.X, 11, travelPos.Y), View, Projection), 1, centered: true);
                DebugUtils.DrawDebugString(TankGame.SpriteRenderer, "ENDPOINT", MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(rayEnd.X, 11, rayEnd.Y), View, Projection), 1, centered: true);

                /*var pos = MathUtils.DirectionOf(Position, travelPos);
                var rot = pos.ToRotation();
                var posNew = new Vector2(50, 0).RotatedByRadians(rot);
                DebugUtils.DrawDebugString(TankGame.spriteBatch, "here?",
                    MatrixUtils.ConvertWorldToScreen(new(posNew.X, 11, posNew.Y), 
                    Matrix.CreateTranslation(Position.X, 
                    0, Position.Y), View, Projection), 
                    1, centered: true);*/

                foreach (var pt in refPoints)
                    DebugUtils.DrawDebugString(TankGame.SpriteRenderer, "pt", MatrixUtils.ConvertWorldToScreen(new Vector3(0, 11, 0), Matrix.CreateTranslation(pt.X, 0, pt.Y), View, Projection), 1, centered: true);


                DebugUtils.DrawDebugString(TankGame.SpriteRenderer, "end", MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(MathUtils.DirectionOf(Position, travelPos).X, 0, MathUtils.DirectionOf(Position, travelPos).Y), View, Projection), 1, centered: true);
                DebugUtils.DrawDebugString(TankGame.SpriteRenderer, "me", MatrixUtils.ConvertWorldToScreen(Vector3.Zero, World, View, Projection/*Matrix.CreateTranslation(Position.X, 11, Position.Y), View, Projection)*/), 1, centered: true);
                //TankGame.spriteBatch.Draw(GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel"), new Rectangle((int)travelPos.X - 1, (int)travelPos.Y - 1, 20, 20), Color.White);

                // draw future
                // DebugUtils.DrawDebugString(TankGame.spriteBatch, "FUT", MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(GeometryUtils.PredictFuturePosition(TargetTank.Position, TargetTank.Velocity, calculation).ExpandZ() + new Vector3(0, 11, 0)), View, Projection), 1, centered: true);
            }

        }

        if (Properties.Invisible && (GameProperties.InMission || MainMenu.Active))
            return;

        Properties.Armor?.Render();
    }
    public bool TryGetDangerNear(float distance, out IAITankDanger? danger) {
        // do the stuff.
        danger = null;
        bool success = false;
        IAITankDanger? closest = null;
        for (int i = 0; i < Dangers.Count; i++) {
            var cDanger = Dangers[i];
            var dist = GameUtils.Distance_WiiTanksUnits(Position, cDanger.Position);
            if (dist < distance) {
                if (closest == null)
                    closest = cDanger;
                else if (GameUtils.Distance_WiiTanksUnits(Position, cDanger.Position) < GameUtils.Distance_WiiTanksUnits(Position, closest.Position))
                    closest = cDanger;
                success = true;
            }
        }
        danger = closest;
        return success;
    }
    /*public bool TryGetShellNear(float distance, out Shell? shell)
    {
        shell = null;

        Shell? closest = null;

        bool returned = false;

        foreach (var bullet in Shell.AllShells)
        {
            if (bullet is not null)
            {
                if (bullet.LifeTime > 30)
                {
                    var dist = GameUtils.Distance_WiiTanksUnits(Position, bullet.Position);
                    if (dist < distance)
                    {
                        if (closest == null)
                            closest = bullet;
                        else if (GameUtils.Distance_WiiTanksUnits(Position, bullet.Position) < GameUtils.Distance_WiiTanksUnits(Position, closest.Position))
                            closest = bullet;
                        returned = true;
                    }
                }
            }
        }

        shell = closest;
        return returned;
    }
    public bool TryGetMineNear(float distance, out Mine mine) {

        mine = null;

        Mine closest = null;
        
        Span<Mine> mines = Mine.AllMines;

        ref var minesSearchSpace = ref MemoryMarshal.GetReference(mines);
        
        for (var i = 0; i < Mine.AllMines.Length; i++) {
            var currentMine = Unsafe.Add(ref minesSearchSpace, i);

            if (currentMine is null) continue;

            var distanceFromMineToSelf = GameUtils.Distance_WiiTanksUnits(Position, currentMine.Position);
            
            if (!(distanceFromMineToSelf < distance)) continue;

            if (closest == null || distanceFromMineToSelf <
                GameUtils.Distance_WiiTanksUnits(Position, closest.Position)) {
                closest = currentMine;
            }
        }

        mine = closest;
        return closest != null;
    }*/

    private static readonly int[] workingTiers =
    {
        TankID.Brown, TankID.Marine, TankID.Yellow, TankID.Black, TankID.White, TankID.Pink, TankID.Violet, TankID.Green, TankID.Ash,
        TankID.Bronze, TankID.Silver, TankID.Sapphire, TankID.Ruby, TankID.Citrine, TankID.Amethyst, TankID.Emerald, TankID.Gold, TankID.Obsidian,
        TankID.Granite, TankID.Bubblegum, TankID.Water, TankID.Crimson, TankID.Tiger, TankID.Creeper, TankID.Gamma, TankID.Marble,
        // TankTier.Assassin
    };
    public static int PickRandomTier()
        => workingTiers[GameHandler.GameRand.Next(0, workingTiers.Length)];
}
