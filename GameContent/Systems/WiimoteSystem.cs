using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Utilities;
using WiimoteLib;

namespace TanksRebirth.GameContent.Systems;

#pragma warning disable
public static class WiimoteSystem {
    static Wiimote _wm;
    public static bool IsConnected => _wm != null;
    public static bool IsNunchukConnected => IsConnected && _wm.WiimoteState.ExtensionType == ExtensionType.Nunchuk;

    public static Vector2 PointerLocation {
        get {
            if (!IsConnected) return Vector2.Zero;
            var state = _wm.WiimoteState;
            return new Vector2(state.IRState.IRSensors[0].Position.X, state.IRState.IRSensors[0].Position.Y);
        }
    }
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

            TankGame.ClientLog.Write("Wiimote connected and mapped successfully.", LogType.Info);
            return true;
        } catch (Exception e) {
            TankGame.ClientLog.Write($"Failed to connect Wiimote: {e.Message} (did you set-up your wiimote with your OS?)", LogType.ErrorFatal);
            return false;
        }
    }
    public static void TryDisconnect() {
        if (!IsConnected) return;

        _wm.SetLEDs(0);
        _wm.Disconnect();
        _wm.WiimoteChanged -= UpdateWiimoteState;
        _wm.WiimoteExtensionChanged -= WiimoteExtChanged;

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
        var state = e.WiimoteState;
        var nunState = state.NunchukState;

        const float min_any = -0.5f;
        const float max_any = 0.5f;

        MinDeadzone = PlayerTank.StickDeadzone;
        MaxDeadzone = PlayerTank.StickAntiDeadzone;

        NunchukAxis = new Vector2(
            InputUtils.ApplyDeadzone(nunState.Joystick.X, MinDeadzone, MaxDeadzone, min_any, max_any),
            InputUtils.ApplyDeadzone(nunState.Joystick.Y, MinDeadzone, MaxDeadzone, min_any, max_any)
        );

        // calculates the cursor position in screen coordinates
        float screenX = 1 - state.IRState.Midpoint.X;
        float screenY = state.IRState.Midpoint.Y;
        var mousePos = Vector2.Zero;


        //Mouse.SetPosition(
        var realPos = new Vector2(
            MathHelper.Clamp(screenX, 0.15f, 0.85f) * WindowUtils.WindowWidth,
            MathHelper.Clamp(screenY, 0.15f, 0.85f) * WindowUtils.WindowHeight);

        if (InputUtils.CurrentKeySnapshot.IsKeyDown(Keys.LeftShift))
            Mouse.SetPosition((int)realPos.X, (int)realPos.Y);

        Console.WriteLine(realPos);

        /*float rate = 0.4f;
        mousePos = new((int)MathHelper.Lerp(MouseUtils.MousePosition.X, newScreenX, rate),
            (int)MathHelper.Lerp(cursorY, newScreenY, rate));*/


        //Mouse.SetPosition((int)mousePos.X, (int)mousePos.Y);

        // Console.WriteLine(NunchukAxis);
        // Console.WriteLine($"{screenX}, {screenY}");
        // Console.WriteLine(Motion);
    }
}
