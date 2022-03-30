using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WiiPlayTanksRemake.Internals.UI;
using FontStashSharp;
using Microsoft.Xna.Framework.Input;
using TextCopy;

namespace WiiPlayTanksRemake.Internals.Common.GameUI
{
    public class UITextInput : UITextButton
    {
        public readonly int Id;

        public static int currentActiveBox = -1;

        public int MaxLength;

        public string DefaultString;

        public bool ActiveHandle;

        public UITextInput(SpriteFontBase font, Color color, float scale, int maxLength) : base("", font, color, scale)
        {
            MaxLength = maxLength;
            Id = AllUIElements.IndexOf(AllUIElements.Find(x => x == this));
        }
        public string GetRealText() => IsEmpty() ? "" : Text;
        public override void Draw(SpriteBatch spriteBatch)
        {
            // spriteBatch.DrawString(Font, Text, Position, Color, new Vector2(Scale));

            // orig: 17
            // max: 20
            // paste: 5
            // new (orig + paste): 22

            // new - max (22 - 20) = 2

            if (IsSelected() && Text.Length > 20)
                Text = Text.Remove(20);

            if (ActiveHandle)
                if ((Input.CurrentKeySnapshot.IsKeyDown(Keys.LeftControl) || Input.CurrentKeySnapshot.IsKeyDown(Keys.RightControl)) && Input.KeyJustPressed(Keys.V))
                {
                    Text += ClipboardService.GetText();
                }

            if (MouseHovering)
            {
                if (Input.CanDetectClick())
                {
                    if (currentActiveBox != Id)
                    {
                        ActiveHandle = true;
                        TankGame.Instance.Window.TextInput += HandleText;
                    }
                    if (Text == DefaultString)
                        Text = "";
                    currentActiveBox = Id;
                }
            }
            if (string.IsNullOrEmpty(Text) && !IsSelected())
                Text = DefaultString;

            base.Draw(spriteBatch);
        }

        public bool IsEmpty() 
            => Text == DefaultString || string.IsNullOrEmpty(Text);
        public bool IsSelected()
            => currentActiveBox == Id;
        private void HandleText(object sender, TextInputEventArgs e)
        {
            if (!IsSelected())
            {
                ActiveHandle = false;
                TankGame.Instance.Window.TextInput -= HandleText;
                return;
            }
            if (TankGame.Instance.IsActive)
            {
                if (e.Key == Keys.Back)
                {
                    if (Text.Length > 0)
                        Text = Text.Remove(Text.Length - 1);
                }
                else if (e.Key == Keys.Escape)
                {
                    Text = string.Empty;
                    TankGame.Instance.Window.TextInput -= HandleText;
                    ActiveHandle = false;
                    currentActiveBox = -1;
                }
                else if (e.Key == Keys.Tab)
                    Text += "   ";
                else if (e.Key == Keys.Enter)
                {
                    TankGame.Instance.Window.TextInput -= HandleText;
                    ActiveHandle = false;
                    currentActiveBox = -1;
                }
                else
                {
                    //if (!TankGame.TextFont.Characters.Contains(args.Character))
                    //return;

                    if (Text.Length < MaxLength)
                        Text += e.Character;
                }
            }
        }
    }
}
