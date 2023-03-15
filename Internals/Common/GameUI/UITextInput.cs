using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.Internals.UI;
using FontStashSharp;
using Microsoft.Xna.Framework.Input;
using TextCopy;

namespace TanksRebirth.Internals.Common.GameUI
{
    public class UITextInput : UITextButton {
        public readonly int Id;

        public static int currentActiveBox = -1;

        public readonly int MaxLength;

        public string DefaultString;

        public bool ActiveHandle;

        public static event EventHandler OnConfirmContents;

        /// <summary>Dysfunctional right now.</summary>
        public bool UseDefaultStringWithText;

        public UITextInput(SpriteFontBase font, Color color, float scale, int maxLength) : base("", font, color, scale) {
            MaxLength = maxLength;
            Id = AllUIElements.IndexOf(AllUIElements.Find(x => x == this));
        }
        public string GetRealText() => IsEmpty() ? "" : Text;
        public override void Draw(SpriteBatch spriteBatch) {
            // spriteBatch.DrawString(Font, Text, Position, Color, new Vector2(Scale));

            // orig: 17
            // max: 20
            // paste: 5
            // new (orig + paste): 22

            // new - max (22 - 20) = 2

            if (IsSelected() && Text.Length > MaxLength)
                Text = Text.Remove(MaxLength);

            if (ActiveHandle)
                if ((InputUtils.CurrentKeySnapshot.IsKeyDown(Keys.LeftControl) || InputUtils.CurrentKeySnapshot.IsKeyDown(Keys.RightControl)) && InputUtils.KeyJustPressed(Keys.V))
                    Text += ClipboardService.GetText();

            if (InputUtils.CanDetectClick()) {
                if (MouseHovering) {
                    if (currentActiveBox != Id) {
                        ActiveHandle = true;
                        TankGame.Instance.Window.TextInput += HandleText;
                    }
                    if (Text == DefaultString)
                        Text = string.Empty;
                    currentActiveBox = Id;
                }
                /*else {
                    ActiveHandle = false;
                    TankGame.Instance.Window.TextInput -= HandleText;
                    if (Text == DefaultString)
                        Text = string.Empty;
                    currentActiveBox = -1;
                }*/
            }
            if (string.IsNullOrEmpty(Text) && !IsSelected())
                Text = DefaultString;

            base.Draw(spriteBatch);
            UniqueDraw?.Invoke(this, spriteBatch);
        }

        public bool IsEmpty() 
            => Text == DefaultString || string.IsNullOrEmpty(Text);
        public bool IsSelected()
            => currentActiveBox == Id;
        private void HandleText(object sender, TextInputEventArgs e) {
            if (!IsSelected()) {
                // if another box is clicked, confirm contents
                if (ActiveHandle)
                    OnConfirmContents?.Invoke(this, EventArgs.Empty);

                ActiveHandle = false;
                TankGame.Instance.Window.TextInput -= HandleText;
                return;
            }

            if (!TankGame.Instance.IsActive) return;
            
            // e == TextInputEventArguments
            switch (e) {
                case { Key: Keys.Back }: {
                    if (Text.Length > 0)
                        Text = Text.Remove(Text.Length - 1);
                    break;
                }
                case { Key: Keys.Escape }:
                    Text = string.Empty;
                    TankGame.Instance.Window.TextInput -= HandleText;
                    ActiveHandle = false;
                    currentActiveBox = -1;
                    OnConfirmContents?.Invoke(this, EventArgs.Empty);
                    break;
                case { Key: Keys.Tab }:
                    Text += "   ";
                    break;
                case { Key: Keys.Enter }:
                    TankGame.Instance.Window.TextInput -= HandleText;
                    ActiveHandle = false;
                    currentActiveBox = -1;
                    OnConfirmContents?.Invoke(this, EventArgs.Empty);
                    break;
                default: {
                    //if (!TankGame.TextFont.Characters.Contains(args.Character))
                    //return;

                    if (Text.Length < MaxLength)
                        Text += e.Character;
                    break;
                }
            }
        }
    }
}
