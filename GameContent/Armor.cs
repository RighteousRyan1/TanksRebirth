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
using WiiPlayTanksRemake.GameContent.Systems.Coordinates;

namespace WiiPlayTanksRemake.GameContent
{
    public class Armor
    {


        public Tank Host;

        public int HitPoints;

        private Texture2D _maskingTexture;

        private Model _model;

        public Armor(Tank host, int hitPoints) {
            _model = GameResources.GetGameResource<Model>("Assets/armor");
            Host = host;
            HitPoints = hitPoints;
            _maskingTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/armor");
        }

        public void Render()
        {
            if (HitPoints < 0) // so armor point amount is clamped to be greater than 0 at all times.
                HitPoints = 0;

            Vector2[] offset = { Vector2.Zero, Vector2.Zero, Vector2.Zero };
            bool[] render = { false, false, false }; // whether or not to render each.
            switch (HitPoints) {
                case 0:
                    // we dont really want to render anything since there isn't any armor present, so call return.
                    return;
                case 1:
                    render[1] = true; // make the middle armor render.
                    break;
                case 2:
                    offset[0] = new Vector2(0, 5);
                    offset[2] = new Vector2(0, -5);

                    render[0] = true; // make left hand armor render.
                    render[2] = true; // make right hand armor render.
                    break;
                default: // for any case > 2
                    offset[0] = new Vector2(0, 5);
                    offset[2] = new Vector2(0, -5);

                    render[0] = true; // make left hand armor render.
                    render[1] = true; // make the middle armor render.
                    render[2] = true; // make right hand armor render.
                    break;
            }

            float scale = 100f;

            for (int i = 0; i < HitPoints; i++)
            {
                foreach (ModelMesh mesh in _model.Meshes)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        //if (render[i])
                        //{
                            effect.World = Matrix.CreateRotationX(-MathHelper.PiOver2)
                                 * Matrix.CreateRotationY(-Host.TankRotation)
                                 * Matrix.CreateScale(scale)
                                 * Matrix.CreateTranslation(Host.Position3D + offset[i].RotatedByRadians(Host.TankRotation).ExpandZ());
                        //}
                        effect.View = Host.View;
                        effect.Projection = Host.Projection;

                        effect.SetDefaultGameLighting_IngameEntities();

                        effect.TextureEnabled = true;

                        effect.Texture = _maskingTexture;
                    }
                    mesh.Draw();
                }
            }
        }
    }
}
