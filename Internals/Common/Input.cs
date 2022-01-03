using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Linq;
using WiiPlayTanksRemake.Internals.Common.Utilities;

namespace WiiPlayTanksRemake.Internals.Common
{
    public static class Input
    {
        public static KeyboardState CurrentKeySnapshot { get; internal set; }

        public static KeyboardState OldKeySnapshot { get; internal set; }

        public static MouseState CurrentMouseSnapshot { get; internal set; }

        public static MouseState OldMouseSnapshot { get; internal set; }

        public static GamePadState CurrentGamePadSnapshot { get; internal set; }

        public static GamePadState OldGamePadSnapshot { get; internal set; }

        public static void HandleInput(PlayerIndex pIndex = PlayerIndex.One) {
            OldKeySnapshot = CurrentKeySnapshot;
            OldMouseSnapshot = CurrentMouseSnapshot;
            OldGamePadSnapshot = CurrentGamePadSnapshot;
            CurrentKeySnapshot = Keyboard.GetState();
            CurrentMouseSnapshot = Mouse.GetState();
            CurrentGamePadSnapshot = GamePad.GetState(pIndex);
        }
        public static bool KeyJustPressed(Keys key) {
            bool pressed = CurrentKeySnapshot.IsKeyDown(key) && OldKeySnapshot.IsKeyUp(key);
            return pressed;
        }
        public static bool AreKeysPressed(params Keys[] keys)
        {
            return keys.All(key => CurrentKeySnapshot.IsKeyDown(key));
        }
        public static bool AreKeysJustPressed(params Keys[] keys)
        {
            var justPressed = AreKeysPressed(keys);

            return justPressed && keys.All(key => OldKeySnapshot.IsKeyUp(key));
        }
        public static bool MouseLeft => CurrentMouseSnapshot.LeftButton == ButtonState.Pressed;
        public static bool MouseMiddle => CurrentMouseSnapshot.MiddleButton == ButtonState.Pressed;
        public static bool MouseRight => CurrentMouseSnapshot.RightButton == ButtonState.Pressed;
        public static bool OldMouseLeft => OldMouseSnapshot.LeftButton == ButtonState.Pressed;
        public static bool OldMouseMiddle => OldMouseSnapshot.MiddleButton == ButtonState.Pressed;
        public static bool OldMouseRight => OldMouseSnapshot.RightButton == ButtonState.Pressed;
        public static bool CanDetectClick(bool rightClick = false) {
            bool clicked = !rightClick ? (CurrentMouseSnapshot.LeftButton == ButtonState.Pressed && OldMouseSnapshot.LeftButton == ButtonState.Released)
                : (CurrentMouseSnapshot.RightButton == ButtonState.Pressed && OldMouseSnapshot.RightButton == ButtonState.Released);
            return GameUtils.WindowActive ? clicked : false;
        }
        public static bool CanDetectClickRelease(bool rightClick = false) {
            bool released = !rightClick ? (CurrentMouseSnapshot.LeftButton != ButtonState.Pressed && OldMouseSnapshot.LeftButton != ButtonState.Released)
                : (CurrentMouseSnapshot.RightButton != ButtonState.Pressed && OldMouseSnapshot.RightButton != ButtonState.Released);
            return GameUtils.WindowActive ? released : false;
        }
        public static Keys FirstPressedKey
        {
            get
            {
                if (CurrentKeySnapshot.GetPressedKeys().Length > 0)
                    return CurrentKeySnapshot.GetPressedKeys()[CurrentKeySnapshot.GetPressedKeys().Length - 1];
                return Keys.None;
            }
        }
        public static Buttons[] GetPressedButtons(GamePadButtons buttons, bool excludeSystemButtons = false)
        {
            Buttons[] pressedButtons = new Buttons[10];
            if (buttons.A == ButtonState.Pressed)
            {
                pressedButtons.Append(Buttons.A);
            }
            if (buttons.B == ButtonState.Pressed)
            {
                pressedButtons.Append(Buttons.B);
            }
            if (buttons.Back == ButtonState.Pressed)
            {
                pressedButtons.Append(Buttons.Back);
            }
            if (buttons.BigButton == ButtonState.Pressed && !excludeSystemButtons)
            {
                pressedButtons.Append(Buttons.BigButton);
            }
            if (buttons.LeftShoulder == ButtonState.Pressed)
            {
                pressedButtons.Append(Buttons.LeftShoulder);
            }
            if (buttons.LeftStick == ButtonState.Pressed)
            {
                pressedButtons.Append(Buttons.LeftStick);
            }
            if (buttons.RightShoulder == ButtonState.Pressed)
            {
                pressedButtons.Append(Buttons.RightShoulder);
            }
            if (buttons.RightStick == ButtonState.Pressed)
            {
                pressedButtons.Append(Buttons.RightStick);
            }
            if (buttons.Start == ButtonState.Pressed && !excludeSystemButtons)
            {
                pressedButtons.Append(Buttons.Start);
            }
            if (buttons.X == ButtonState.Pressed)
            {
                pressedButtons.Append(Buttons.X);
            }
            if (buttons.Y == ButtonState.Pressed)
            {
                pressedButtons.Append(Buttons.Y);
            }
            return pressedButtons;
        }
        public static bool ButtonJustPressed(Buttons button)
        {
            bool pressed = CurrentGamePadSnapshot.IsButtonDown(button) && OldGamePadSnapshot.IsButtonUp(button);
            return pressed;
        }
        public static int DeltaScrollWheel => CurrentMouseSnapshot.ScrollWheelValue / 120;
    }
}