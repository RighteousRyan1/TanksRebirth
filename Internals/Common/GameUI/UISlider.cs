using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Internals.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Internals.Common.GameUI {
    public class UISlider : UIElement {
        /// <summary>The value of the <see cref="UISlider"/>. Will only ever range from 0 to 1.</summary>
        public float Value {
            get => InternalValue;
            set => InternalValue = MathHelper.Clamp(value, 0f, 1f);
        }

        private UIImage _slider;

        /// <summary>The width of the <see cref="UISlider"/>'s bar.</summary>
        public int BarWidth = 5;

        /// <summary>The color of the <see cref="UISlider"/>'s bar.</summary>
        public Color BarColor = Color.Black;

        /// <summary>The color of the <see cref="UISlider"/>.</summary>
        public Color SliderColor = Color.LightBlue;

        private float InternalValue;

        public override void OnInitialize() {
            UIImage interactable = new(TankGame.WhitePixel, new(1f), (image, spriteBatch) => spriteBatch.Draw(image.Texture, image.Hitbox, Color.Transparent));
            interactable.Tooltip = Tooltip;
            interactable.FallThroughInputs = false;
            interactable.SetDimensions((int)Position.X + 2, (int)Position.Y + 2, (int)Size.X - 1, (int)Size.Y - 1);

            var mouseXLastDown = 0;
            interactable.OnLeftDown = _ => {
                if (mouseXLastDown < 0) {
                    var mouseXRelative = (int)Math.Round(MouseUtils.MouseX - this.Position.X);
                    var nValue = mouseXRelative / this.Size.X;
                    InternalValue = MathHelper.Clamp(nValue != 0f ? nValue - 0.01f : nValue, 0, 1);
                }
                else {
                    var mouseXRelative = (int)Math.Round(MouseUtils.MouseX - this.Position.X);
                    var nValue = mouseXRelative / this.Size.X;
                    InternalValue = MathHelper.Clamp(nValue != 0f ? nValue + 0.01f : nValue, 0, 1);
                }

                mouseXLastDown = MouseUtils.MouseX;
            };
            Append(interactable);
            _slider = interactable;
            this.FallThroughInputs = true;

            base.OnInitialize();
        }

        public override void DrawSelf(SpriteBatch spriteBatch) {
            spriteBatch.Draw(TankGame.WhitePixel, Hitbox, SliderColor);
        }

        private const int DEAD_ZONE = 16;

        public override void DrawChildren(SpriteBatch spriteBatch) {
            spriteBatch.Draw(TankGame.WhitePixel, new Rectangle((int)Position.X + (int)(InternalValue * Size.X) - (InternalValue > 0.5 ? BarWidth / 2 : 0), (int)Position.Y - 2, BarWidth, (int)Size.Y + 4), BarColor);
            _slider.OverrideHitbox(new Rectangle(Hitbox.X, Hitbox.Y, Hitbox.Width + DEAD_ZONE, Hitbox.Height));
        }
    }
}