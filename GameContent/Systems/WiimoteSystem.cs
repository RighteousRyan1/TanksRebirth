using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using TanksRebirth.GameContent.Tanks;
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
    // pretty sure this doesn't even work. and the original game doesn't even use it. why is it here? probably removing
    public static Vector3 Motion => new Vector3(_wm.WiimoteState.AccelState.Values.X, _wm.WiimoteState.AccelState.Values.Y, _wm.WiimoteState.AccelState.Values.Z);

    // this needs to be fully tested. pushing anyway xd
    /// <summary>Higher is snappier, lower is smoother.</summary>
    public static float Smoothing = 25f;
    static bool _hasTarget;
    // where the wiimote points as normalized coordinates
    static Vector2 _target, _pointer;

    // used so the wiimote points instead of gliding in from the corner
    static bool _pointerStarted;

    public static bool TryConnect() {
        try {
            _wm = new();
            _wm.Connect();

            _wm.SetLEDs(1);
            _wm.SetReportType(InputReport.IRExtensionAccel, true);
            // _wm.SetRumble(true);

            _wm.WiimoteChanged += UpdateWiimoteState;
            _wm.WiimoteExtensionChanged += WiimoteExtChanged;

            IsConnected = true;
            TankGame.ClientLog.Write("Wiimote connected and mapped successfully.", RebirthLogLevel.Info);
            return true;
        } catch (Exception e) {
            TankGame.ClientLog.Write($"Failed to connect Wiimote: {e.Message} (did you setup your wiimote with your OS?)", RebirthLogLevel.Warn);
            return false;
        }
    }
    public static bool TryDisconnect() {
        if (!IsConnected) return false;

        _wm?.SetLEDs(0);
        _wm.WiimoteChanged -= UpdateWiimoteState;
        _wm.WiimoteExtensionChanged -= WiimoteExtChanged;
        _wm?.Disconnect();
        _wm = null;

        IsConnected = false;
        _hasTarget = false;
        _pointerStarted = false;

        TankGame.ClientLog.Write("Wiimote disconnected successfully.", RebirthLogLevel.Info);

        return true;
    }

    static void WiimoteExtChanged(object? sender, WiimoteExtensionChangedEventArgs e) {
        if (!e.Inserted) {
            TankGame.ClientLog.Write($"Wiimote Extension '{_wm.WiimoteState.ExtensionType}' disconnected.", RebirthLogLevel.Info);
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

        // i think the wii did something like this in case a wiimote was aiming too far off of the ir sensors?
        var ir = state.IRState.IRSensors;
        Vector2? point = (ir[0].Found, ir[1].Found) switch {
            (true, true) => new Vector2((ir[0].Position.X + ir[1].Position.X) / 2f, (ir[0].Position.Y + ir[1].Position.Y) / 2f),
            (true, false) => new Vector2(ir[0].Position.X, ir[0].Position.Y),
            (false, true) => new Vector2(ir[1].Position.X, ir[1].Position.Y),
            _ => null,
        };
        if (point is Vector2 p) {
            _target = new Vector2(MathHelper.Clamp(1f - p.X, 0f, 1f), MathHelper.Clamp(p.Y, 0f, 1f));
            _hasTarget = true;
        }

        HandleInputs(state);

        // Console.WriteLine($"{realPos}\nnormalized: ({screenX}, {screenY})");
        //Console.WriteLine("Sensors:\n\n" + string.Join("\n", state.IRState.IRSensors[..2].Select(x => x.Position)));

        // Console.WriteLine($"\nAverage of sensors: ({realPos.X}, {realPos.Y})\n");

        //Console.WriteLine($"PixelPos: {realPos}\n");
        //Console.WriteLine("IR 1: " + state.IRState.IRSensors[1]);

        PreviousButtons = state.ButtonState;
    }

    public static void UpdatePointer() {
        if (!IsConnected || !_hasTarget || !TankGame.Instance.IsActive) return;

        var target = _target * new Vector2(WindowUtils.WindowWidth, WindowUtils.WindowHeight);

        if (!_pointerStarted) {
            _pointer = target;
            _pointerStarted = true;
        }

        // this should hopefully emulate the feeling that the original wiimote had as opposed to the jerkiness that existed prior
        var t = 1f - MathF.Exp(-Smoothing * (float)TankGame.LastGameTime.ElapsedGameTime.TotalSeconds);
        _pointer = Vector2.Lerp(_pointer, target, t);

        // love how wiimotelib has a Mouse
        // i INITIALLY rounded prior to int cast here, but cast by itself is probably fine
        Microsoft.Xna.Framework.Input.Mouse.SetPosition((int)_pointer.X, (int)_pointer.Y);
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

        var statColor = new StatisticalColor<float>(Color.Red, Color.Lime, 0f, BatteryPercent, 100f);
        DrawUtils.DrawStringWithBorder(spriteBatch, font, $"Battery Life: {BatteryPercent}%", WindowUtils.WindowBottomLeft,
            statColor.FinalColor, ColorUtils.ChangeColorBrightness(statColor.FinalColor, -0.75f), new Vector2(0.8f).ToResolution(), 0f, Anchor.BottomLeft, 0.75f);
    }
}
