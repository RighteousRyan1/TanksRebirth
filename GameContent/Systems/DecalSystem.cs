using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace TanksRebirth.GameContent.Systems
{
    public sealed class DecalSystem
    {
        private struct DecalInfo
        {
            public Texture2D texture;
            public Rectangle destRect;
            public Rectangle? srcRect;
            public Color color;

            public DecalInfo(Texture2D texture, Rectangle destRect, Rectangle? srcRect, Color color)
            {
                this.texture = texture;
                this.destRect = destRect;
                this.srcRect = srcRect;
                this.color = color;
            }
        }

        private static Dictionary<BlendState, List<DecalInfo>> _decalsToAdd;
        private static RenderTarget2D _target;

        private static SpriteBatch _spriteBatch;
        private static GraphicsDevice _device;

        public static void Initialize(SpriteBatch batch, GraphicsDevice device)
        {
            _device = device;
            _spriteBatch = batch;
            _target = new RenderTarget2D(device, 2048, 1500, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _decalsToAdd = new Dictionary<BlendState, List<DecalInfo>>();
        }

        public static void UpdateRenderTarget()
        {
            if (_decalsToAdd is null || _decalsToAdd.Count == 0)
            {
                return;
            }

            _device.SetRenderTarget(_target);

            foreach (var pair in _decalsToAdd)
            {
                var blendState = pair.Key;
                var drawList = pair.Value;

                _spriteBatch.Begin(SpriteSortMode.Deferred, blendState, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);

                foreach (var info in drawList)
                {
                    _spriteBatch.Draw(info.texture, info.destRect, info.srcRect, info.color);
                }

                _spriteBatch.End();
            }

            _device.SetRenderTarget(null);

            _decalsToAdd.Clear();
        } 

        public static void Dispose()
        {
            if (_target != null)
            {
                _target.Dispose();
            }
        }

        public static void AddDecal(Texture2D texture, Rectangle localDestRect, Rectangle? srcRect, Color color, BlendState blendState)
        {
            if (!_decalsToAdd.TryGetValue(blendState, out var list))
            {
                _decalsToAdd[blendState] = list = new List<DecalInfo>();
            }

            list.Add(new DecalInfo(texture, localDestRect, srcRect, color));
        }
    }
}