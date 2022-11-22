using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.Systems
{
    // TODO: finish
    public class Popup
    {
        public enum PopupAnchor {
            Top, 
            Bottom
        }

        public TimeSpan Duration = TimeSpan.FromSeconds(5);

        public PopupAnchor Anchor = PopupAnchor.Bottom;

        public string Text;

        private Vector2 _curPos;

        public float Easing;

        public Popup(PopupAnchor anchor, float easing) {
            Anchor = anchor;
            Easing = easing;

            _curPos = new(-9999, 9999);
        }

        public void Start() {
            var measure = TankGame.TextFont.MeasureString(Text);

            _curPos = Anchor switch {
                PopupAnchor.Top => new(WindowUtils.WindowWidth / 2, -measure.Y * 1.2f),
                PopupAnchor.Bottom => new(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight + measure.Y * 1.2f),
                _ => new()
            };
            float destination = Anchor switch {
                PopupAnchor.Top => measure.Y * 1.2f,
                PopupAnchor.Bottom => WindowUtils.WindowHeight - measure.Y * 1.2f,
                _ => 0f
            };
            // from here we start the popup appearance, then end it later
            Task.Run(async () => {
                switch (Anchor)
                {
                    case PopupAnchor.Top:
                        if (_curPos.Y < destination)
                            _curPos.Y += Easing;
                        break;
                    case PopupAnchor.Bottom:
                        if (_curPos.Y > destination)
                            _curPos.Y -= Easing;
                        break;
                }
                await Task.Delay(Duration).ConfigureAwait(false);
            });
        }
    }
}
