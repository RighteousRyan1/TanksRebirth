using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using WiiPlayTanksRemake.Enums;
using System.Linq;
using WiiPlayTanksRemake.Internals.Common.GameInput;
using Microsoft.Xna.Framework.Input;
using WiiPlayTanksRemake.Internals.Common.Utilities;

namespace WiiPlayTanksRemake.GameContent
{
    public class Tank
    {
        public static List<Tank> AllTanks { get; } = new();

        public Vector2 position;
        public Vector2 approachVelocity;
        public Vector2 velocity;
        public float speed;
        public float bulletShootSpeed;
        public float barrelRotation; // do remember this is in radians
        public float tankRotation;
        public int maxLayableMines;

        public bool IsAI { get; }

        public TankTier tier;

        public int TierHierarchy => (int)tier;

        public Action<Tank> behavior;

        public static TankTier GetHighestTierActive()
        {
            TankTier highest = TankTier.None;

            foreach (Tank tank in AllTanks)
            {
                if (tank.tier > highest)
                    highest = tank.tier;
            }
            return highest;
        }

        private Texture2D _texture;

        public Keybind cUp = new("Up", Keys.W);
        public Keybind cDown = new("Down", Keys.S);
        public Keybind cLeft = new("Left", Keys.A);
        public Keybind cRight = new("Right", Keys.D);

        public Tank(Vector2 beginPos, bool ai = false, TankTier tier = TankTier.None)
        {
            position = beginPos;
            IsAI = ai;
            this.tier = tier;
            _texture = TankGame.Instance.Content.Load<Texture2D>(ai ? "Assets/textures/e_template" : "Assets/textures/p_blue");
            cUp.KeybindPressAction = (cUp) =>
            {
                approachVelocity.Y -= 20f;
            };
            cDown.KeybindPressAction = (cDown) =>
            {
                approachVelocity.Y += 20f;
            };
            cLeft.KeybindPressAction = (cLeft) =>
            {
                approachVelocity.X -= 20f;
            };
            cRight.KeybindPressAction = (cRight) =>
            {
                approachVelocity.X += 20f;
            };
            AllTanks.Add(this);
        }

        internal void Update()
        {
            position += velocity;
            GetAIBehavior();
            if (IsAI)
            {
                behavior?.Invoke(this);
            }
            else
            {
                UpdatePlayerMovement();
            }
        }

        public void UpdatePlayerMovement()
        {
            velocity += approachVelocity / 10;
            barrelRotation = GameUtils.DirectionOf(GameUtils.MousePosition, position).ToRotation();
            approachVelocity = Vector2.Zero;
        }

        public void GetAIBehavior()
        {
            if (tier == TankTier.Ash)
            {
                behavior = (tank) => {
                    if (TryGetBulletNear(tank, 50f, out var bullet))
                    {
                        tank.velocity = tank.position - bullet.position;
                    }
                };
            }
        }

        internal void DrawBody()
        {
            GameUtils.DrawStringAtMouse(this);
            TankGame.spriteBatch.Draw(_texture, position, new(0, 0, 32, 32), Color.White, tankRotation, _texture.Size() / 2, 1f, SpriteEffects.None, 0f);
            DrawBarrel();
        }
        private void DrawBarrel()
        {
            TankGame.spriteBatch.Draw(_texture, position, new(31, 0, 12, 3), Color.White, barrelRotation, _texture.Size() / 2, 1f, SpriteEffects.None, 0f);
        }

        public static bool TryGetBulletNear(Tank tank, float distance, out Bullet bullet)
        {
            foreach (var blet in Bullet.AllBullets)
            {
                if (Vector2.Distance(tank.position, blet.position) < distance)
                {
                    bullet = blet;
                    return true;
                }
            }
            bullet = null;
            return false;
        }
        public static bool TryGetMineNear(Tank tank, float distance, out Mine mine)
        {
            foreach (var mne in Mine.AllMines)
            {
                if (Vector2.Distance(tank.position, mne.position) < distance)
                {
                    mine = mne;
                    return true;
                }
            }
            mine = null;
            return false;
        }

        public override string ToString()
            => $"tier: {tier} | velocity/achievable: {velocity}/{approachVelocity}";
    }
}