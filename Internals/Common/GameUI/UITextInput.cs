using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;
using Microsoft.Xna.Framework.Input;
using TextCopy;

namespace TanksRebirth.Internals.Common.GameUI
{
    public class UITextInput : UITextButton {
        public readonly int Id;

        public static int currentActiveBox = -1;

        public int MaxLength;

        public string DefaultString;

        public bool ActiveHandle;

        public static event EventHandler OnConfirmContents;

        /// <summary>Disfunctional right now.</summary>
        public bool UseDefaultStringWithText;

        public Func<SpriteBatch, (SpriteFontBase Font, Vector2 Position, Vector2 Origin, Vector2 Scale)>? CursorDrawInfo;

        public UITextInput(SpriteFontBase font, Color color, float scale, int maxLength) : base("", font, color, scale) {
            MaxLength = maxLength;
            Id = AllUIElements.IndexOf(AllUIElements.Find(x => x == this));
        }
        public string GetRealText() => IsEmpty() ? string.Empty : Text.Trim();
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
                if ((InputUtils.KeyboardMouse.CurrentKey.IsKeyDown(Keys.LeftControl) || InputUtils.KeyboardMouse.CurrentKey.IsKeyDown(Keys.RightControl)) && InputUtils.KeyJustPressed(Keys.V))
                    Text += ClipboardService.GetText();

            if (InputUtils.Click()) {
                if (MouseHovering) {
                    Activate();
                }
                else if (IsSelected()) {
                    Deactivate();
                }
            }
            if (string.IsNullOrEmpty(Text) && !IsSelected())
                Text = DefaultString;

            base.Draw(spriteBatch);
            UniqueDraw?.Invoke(this, spriteBatch);
        }

        private void Activate()
        {
            if (currentActiveBox == Id)
                return;

            DeactivateCurrent();

            currentActiveBox = Id;
            ActiveHandle = true;
            TankGame.Instance.Window.TextInput += HandleText;

            if (Text == DefaultString)
                Text = string.Empty;
        }

        private void Deactivate()
        {
            if (!ActiveHandle)
                return;

            TankGame.Instance.Window.TextInput -= HandleText;
            ActiveHandle = false;

            if (currentActiveBox == Id)
                currentActiveBox = -1;

            OnConfirmContents?.Invoke(this, new());
        }

        private static void DeactivateCurrent()
        {
            var selected = AllUIElements.OfType<UITextInput>().FirstOrDefault(x => x.IsSelected());
            selected?.Deactivate();
        }

        private static void SwitchFocus(bool reverse)
        {
            var boxes = AllUIElements.OfType<UITextInput>().OrderBy(x => x.Id).ToList();
            if (boxes.Count == 0)
                return;

            int selectedIndex = boxes.FindIndex(x => x.IsSelected());
            int nextIndex = selectedIndex;

            if (selectedIndex == -1)
                nextIndex = reverse ? boxes.Count - 1 : 0;
            else
                nextIndex = (selectedIndex + (reverse ? -1 : 1) + boxes.Count) % boxes.Count;

            boxes[nextIndex].Activate();
        }

        public bool IsEmpty() 
            => Text == DefaultString || string.IsNullOrEmpty(Text);
        public bool IsSelected()
            => currentActiveBox == Id;
        private void HandleText(object sender, TextInputEventArgs e)
        {
            if (!IsSelected())
            {
                // if another box is clicked, confirm contents
                if (ActiveHandle)
                    OnConfirmContents?.Invoke(this, new());

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
                    OnConfirmContents?.Invoke(this, new());
                }
                else if (e.Key == Keys.Tab)
                {
                    var reverse = InputUtils.CurrentKeySnapshot.IsKeyDown(Keys.LeftShift) || InputUtils.CurrentKeySnapshot.IsKeyDown(Keys.RightShift);
                    SwitchFocus(reverse);
                }
                else if (e.Key == Keys.Enter)
                {
                    TankGame.Instance.Window.TextInput -= HandleText;
                    ActiveHandle = false;
                    currentActiveBox = -1;
                    OnConfirmContents?.Invoke(this, new());
                }
                else
                {
                    //if (!FontGlobals.RebirthFont.Characters.Contains(args.Character))
                    //return;

                    if (Text.Length < MaxLength)
                        Text += e.Character;
                }
            }
        }
    }
}
