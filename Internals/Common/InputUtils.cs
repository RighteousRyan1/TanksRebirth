﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Internals.Common;

public static class InputUtils
{
    public static KeyboardState CurrentKeySnapshot { get; internal set; }

    public static KeyboardState OldKeySnapshot { get; internal set; }

    public static MouseState CurrentMouseSnapshot { get; internal set; }

    public static MouseState OldMouseSnapshot { get; internal set; }

    public static GamePadState CurrentGamePadSnapshot { get; internal set; }

    public static GamePadState OldGamePadSnapshot { get; internal set; }

    public static void PollEvents(PlayerIndex pIndex = PlayerIndex.One) {
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
    public static bool AreKeysDown(params Keys[] keys)
    {
        return keys.All(key => CurrentKeySnapshot.IsKeyDown(key));
    }
    public static bool AreKeysJustPressed(params Keys[] keys)
    {
        bool allAreDown = keys.All(key => CurrentKeySnapshot.IsKeyDown(key));
        bool notAllUp = keys.Any(key => OldKeySnapshot.IsKeyUp(key));
        
        return allAreDown && notAllUp;
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
        return WindowUtils.WindowActive && clicked;
    }
    public static bool CanDetectClickRelease(bool rightClick = false) {
        bool released = !rightClick ? (CurrentMouseSnapshot.LeftButton != ButtonState.Pressed && OldMouseSnapshot.LeftButton != ButtonState.Released)
            : (CurrentMouseSnapshot.RightButton != ButtonState.Pressed && OldMouseSnapshot.RightButton != ButtonState.Released);
        return WindowUtils.WindowActive && released;
    }
    public static Keys FirstPressedKey
    {
        get
        {
            if (CurrentKeySnapshot.GetPressedKeys().Length > 0)
                return CurrentKeySnapshot.GetPressedKeys()[^1];
            return Keys.None;
        }
    }
    /// <summary>
    /// Returns true if the user has used the gamepad this frame (i.e: pressed buttons, moved stick, pulled trigger)
    /// </summary>
    public static bool IsGamepadBeingUsed(PlayerIndex player = PlayerIndex.One) {
        var state = GamePad.GetState(player);

        if (!state.IsConnected)
            return false;

        bool isUsed =
            state.Buttons.A == ButtonState.Pressed ||
            state.Buttons.B == ButtonState.Pressed ||
            state.Buttons.X == ButtonState.Pressed ||
            state.Buttons.Y == ButtonState.Pressed ||
            state.Buttons.Start == ButtonState.Pressed ||
            state.Buttons.Back == ButtonState.Pressed ||
            state.Buttons.LeftShoulder == ButtonState.Pressed ||
            state.Buttons.RightShoulder == ButtonState.Pressed ||
            state.DPad.IsPressed() ||
            Math.Abs(state.ThumbSticks.Left.X) > 0.1f ||
            Math.Abs(state.ThumbSticks.Left.Y) > 0.1f ||
            Math.Abs(state.ThumbSticks.Right.X) > 0.1f ||
            Math.Abs(state.ThumbSticks.Right.Y) > 0.1f ||
            state.Triggers.Left > 0.05f ||
            state.Triggers.Right > 0.05f;

        return isUsed;
    }

    private static bool IsPressed(this GamePadDPad dpad) {
        return dpad.Up == ButtonState.Pressed ||
               dpad.Down == ButtonState.Pressed ||
               dpad.Left == ButtonState.Pressed ||
               dpad.Right == ButtonState.Pressed;
    }
    public static Buttons[] GetPressedButtons(GamePadButtons buttons, bool excludeSystemButtons = false)
    {
        var pressedButtons = new Buttons[10];
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
    public static int OldDeltaScrollWheel => OldMouseSnapshot.ScrollWheelValue / 120;

    public static int GetScrollWheelChange() => DeltaScrollWheel == OldDeltaScrollWheel ? 0 : DeltaScrollWheel - OldDeltaScrollWheel;
}