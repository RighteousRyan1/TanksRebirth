using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using WiiPlayTanksRemake.Internals.Common.Utilities;

namespace WiiPlayTanksRemake.Internals.Common
{
    public static class Input
    {
        public static KeyboardState CurrentKeySnapshot
        {
            get; internal set;
        }
        public static KeyboardState OldKeySnapshot
        {
            get; internal set;
        }
        public static MouseState CurrentMouseSnapshot
        {
            get; internal set;
        }
        public static MouseState OldMouseSnapshot
        {
            get; internal set;
        }

        public static GamePadState CurrentGamePadSnapshot
        {
            get; internal set;
        }
        public static GamePadState OldGamePadSnapshot
        {
            get; internal set;
        }

        public static void HandleInput(PlayerIndex pIndex = PlayerIndex.One) {
            CurrentKeySnapshot = Keyboard.GetState();
            CurrentMouseSnapshot = Mouse.GetState();
            CurrentGamePadSnapshot = GamePad.GetState(pIndex);
        }
        public static bool KeyJustPressed(Keys key) {
            bool pressed = CurrentKeySnapshot.IsKeyDown(key) && OldKeySnapshot.IsKeyUp(key);
            return pressed;
        }
        public static bool MouseLeft => CurrentMouseSnapshot.LeftButton == ButtonState.Pressed;
        public static bool MouseMiddle => CurrentMouseSnapshot.MiddleButton == ButtonState.Pressed;
        public static bool MouseRight => CurrentMouseSnapshot.RightButton == ButtonState.Pressed;
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
        public static int DeltaScrollWheel => CurrentMouseSnapshot.ScrollWheelValue / 120;
    }
}