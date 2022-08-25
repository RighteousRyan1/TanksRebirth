using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace TanksRebirth.GameContent.Systems
{
    public class DecalSystem
    {
        private struct DecalInfo
        {
            public Texture2D texture;
            public Vector2 position;
            public Rectangle? srcRect;
            public Color color;
            public float rotation;

            public DecalInfo(Texture2D texture, Vector2 position, Rectangle? srcRect, Color color, float rotation)
            {
                this.texture = texture;
                this.position = position;
                this.srcRect = srcRect;
                this.color = color;
                this.rotation = rotation;
            }
        }

        private Dictionary<BlendState, List<DecalInfo>> _decalsToAdd;
        private RenderTarget2D _target;

        private SpriteBatch _spriteBatch;
        private GraphicsDevice _device;

        public BasicEffect Effect;

        public DecalSystem(SpriteBatch batch, GraphicsDevice device) => Initialize(batch, device);

        public void Initialize(SpriteBatch batch, GraphicsDevice device)
        {
            _device = device;
            _spriteBatch = batch;
            _target = new RenderTarget2D(device, 2048, 1500, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _decalsToAdd = new Dictionary<BlendState, List<DecalInfo>>();
        }

        public void UpdateRenderTarget()
        {
            if (_decalsToAdd is null || _decalsToAdd.Count == 0)
                return;

            var oldtargets = _device.GetRenderTargets();
            _device.SetRenderTarget(_target);

            foreach (var pair in _decalsToAdd)
            {
                var blendState = pair.Key;
                var drawList = pair.Value;

                _spriteBatch.Begin(SpriteSortMode.Deferred, blendState, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, Effect);

                foreach (var info in drawList)
                    _spriteBatch.Draw(info.texture, info.position, info.srcRect, info.color);

                _spriteBatch.End();
            }

            _device.SetRenderTargets(oldtargets);

            _decalsToAdd.Clear();
        } 

        public void Dispose()
        {
            if (_target != null)
            {
                _target.Dispose();
            }
        }

        public void AddDecal(Texture2D texture, Vector2 localDestRect, Rectangle? srcRect, Color color, float rotation, BlendState blendState)
        {
            if (!_decalsToAdd.TryGetValue(blendState, out var list))
            {
                _decalsToAdd[blendState] = list = new List<DecalInfo>();
            }

            list.Add(new DecalInfo(texture, localDestRect, srcRect, color, rotation));
        }

        public void CleanRenderTarget(GraphicsDevice device)
        {
            device.SetRenderTarget(_target);
            device.Clear(Color.Black);
            device.SetRenderTarget(null);
        }
    }
}