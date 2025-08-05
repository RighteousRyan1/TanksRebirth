using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Framework;
using TanksRebirth.Internals.Common.Utilities;
using WiimoteLib;

namespace TanksRebirth.GameContent.Systems;

#pragma warning disable
public enum WiimoteButton {
    A, B, 
    Minus, Plus, 
    One, Two, 
    Home, Up, Down, Left, Right,

    // nunchuk buttons
    C, Z
}
public static class WiimoteSystem {
    static Wiimote? _wm;

    public static WiimoteState State => _wm.WiimoteState;
    public static ButtonState PreviousButtons { get; private set; }
    public static bool IsConnected { get; private set; }
    public static bool IsNunchukConnected => IsConnected && _wm.WiimoteState.ExtensionType == ExtensionType.Nunchuk;
    public static float BatteryPercent => _wm.WiimoteState.Battery;

    public static float MinDeadzone;
    public static float MaxDeadzone;
    /// <summary>Will be Zero if disconnected.</summary>
    public static Vector2 NunchukAxis { get; private set; }
    public static Vector3 Motion => new Vector3(_wm.WiimoteState.AccelState.Values.X, _wm.WiimoteState.AccelState.Values.Y, _wm.WiimoteState.AccelState.Values.Z);

    public static bool TryConnect() {
        try {
            _wm = new();
            _wm.Connect();

            _wm.SetLEDs(1);
            _wm.SetReportType(InputReport.IRExtensionAccel, true);
            _wm.SetRumble(true);

            _wm.WiimoteChanged += UpdateWiimoteState;
            _wm.WiimoteExtensionChanged += WiimoteExtChanged;

            IsConnected = true;
            TankGame.ClientLog.Write("Wiimote connected and mapped successfully.", LogType.Info);
            return true;
        } catch (Exception e) {
            TankGame.ClientLog.Write($"Failed to connect Wiimote: {e.Message} (did you set-up your wiimote with your OS?)", LogType.ErrorFatal);
            return false;
        }
    }
    public static void TryDisconnect() {
        if (!IsConnected) return;

        _wm?.SetLEDs(0);
        _wm.WiimoteChanged -= UpdateWiimoteState;
        _wm.WiimoteExtensionChanged -= WiimoteExtChanged;
        _wm?.Disconnect();
        _wm = null;

        IsConnected = false;

        TankGame.ClientLog.Write("Wiimote disconnected successfully.", LogType.Info);
    }

    static void WiimoteExtChanged(object? sender, WiimoteExtensionChangedEventArgs e) {
        if (!e.Inserted) {
            TankGame.ClientLog.Write($"Wiimote Extension '{_wm.WiimoteState.ExtensionType}' disconnected.", LogType.Info);
            return;
        }
        if (e.ExtensionType == ExtensionType.Nunchuk) {
            // handle nunchuk connection
        }
    }

    static void UpdateWiimoteState(object? sender, WiimoteChangedEventArgs e) {
        if (!TankGame.Instance.IsActive) return;

        var state = e.WiimoteState;
        var nunState = state.NunchukState;

        const float min_any = -0.5f;
        const float max_any = 0.5f;

        MinDeadzone = PlayerTank.StickDeadzone;
        MaxDeadzone = PlayerTank.StickAntiDeadzone;

        NunchukAxis = new Vector2(
            InputUtils.ApplyDeadzone(nunState.Joystick.X, MinDeadzone, MaxDeadzone, min_any, max_any),
            -InputUtils.ApplyDeadzone(nunState.Joystick.Y, MinDeadzone, MaxDeadzone, min_any, max_any)
        );

        // calculates the cursor position in screen coordinates
        float screenX = 1 - state.IRState.IRSensors[1].Position.X;
        float screenY = state.IRState.IRSensors[1].Position.Y;

        state.IRState.Mode = IRMode.Extended;

        var realX = state.IRState.IRSensors[0].Position.X + state.IRState.IRSensors[1].Position.X / 2 - 0.5f;
        var realY = state.IRState.IRSensors[0].Position.Y + state.IRState.IRSensors[1].Position.Y / 2 - 0.5f;

        var realPos = new Vector2(
            MathHelper.Clamp((1f - realX), 0, 1) * WindowUtils.WindowWidth,
            MathHelper.Clamp(realY, 0, 1) * WindowUtils.WindowHeight
        );

        HandleInputs(state);

        // Console.WriteLine($"{realPos}\nnormalized: ({screenX}, {screenY})");
        //Console.WriteLine("Sensors:\n\n" + string.Join("\n", state.IRState.IRSensors[..2].Select(x => x.Position)));

        // Console.WriteLine($"\nAverage of sensors: ({realPos.X}, {realPos.Y})\n");

        //Console.WriteLine($"PixelPos: {realPos}\n");
        //Console.WriteLine("IR 1: " + state.IRState.IRSensors[1]);

        float smoothing = 0.4f;
        var mousePos = new Vector2(
            (int)MathHelper.Lerp(MouseUtils.MousePosition.X, realPos.X, smoothing),
            (int)MathHelper.Lerp(MouseUtils.MousePosition.Y, realPos.Y, smoothing)
        );

        Microsoft.Xna.Framework.Input.Mouse.SetPosition((int)mousePos.X, (int)mousePos.Y);


        PreviousButtons = state.ButtonState;
    }

    static void HandleInputs(WiimoteState state) {
        if (state.ButtonState.B && !PreviousButtons.B) {
            InputUtils.MouseForce();
        }
        else if (!PreviousButtons.B && !state.ButtonState.B) {
            InputUtils.MouseForce(false);
        }

        /*if (state.ButtonState.Up && !_prevUp) {
            InputUtils.KeyForce(Keys.W);
        }
        else if (_prevUp && !state.ButtonState.Up) {
            InputUtils.KeyForce(Keys.W, false);
        }

        if (state.ButtonState.Down && !_prevDown) {
            InputUtils.KeyForce(Keys.S);
        }
        else if (_prevDown && !state.ButtonState.Down) {
            InputUtils.KeyForce(Keys.S, false);
        }

        if (state.ButtonState.Right && !_prevRight) {
            InputUtils.KeyForce(Keys.D);
        }
        else if (_prevRight && !state.ButtonState.Right) {
            InputUtils.KeyForce(Keys.D, false);
        }

        if (state.ButtonState.Left && !_prevLeft) {
            InputUtils.KeyForce(Keys.A);
        }
        else if (_prevRight && !state.ButtonState.Left) {
            InputUtils.KeyForce(Keys.A, false);
        }*/
    }

    public static void DrawWiimoteBatteryLife(SpriteBatch spriteBatch, SpriteFontBase font) {
        if (!IsConnected) return;

        var statColor = new StatisticalColor<float>(Color.Red, Color.Lime, 0f, BatteryPercent, 1f);
        DrawUtils.DrawStringWithBorder(spriteBatch, font, $"Battery Life: {BatteryPercent}%", WindowUtils.WindowBottomLeft,
            statColor.FinalColor, ColorUtils.ChangeColorBrightness(statColor.FinalColor, -0.5f), new Vector2(0.8f).ToResolution(), 0f, Anchor.BottomLeft, 0.75f);
    }
}
