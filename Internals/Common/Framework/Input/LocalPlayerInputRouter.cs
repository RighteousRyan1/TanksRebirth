using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace TanksRebirth.Internals.Common.Framework.Input;

public sealed class LocalPlayerInputRouter {
    private readonly LocalPlayerInputFrame[] _frames = [
        new(Vector2.Zero, Vector2.Zero, Vector2.Zero, LocalAimSource.Mouse, false, false, false),
        new(Vector2.Zero, -Vector2.UnitY, Vector2.Zero, LocalAimSource.Direction, false, false, false),
    ];

    private Vector2 _playerTwoAim = -Vector2.UnitY;

    public static LocalPlayerInputRouter Runtime { get; } = new();

    public void Update(
        KeyboardState currentKeyboard,
        KeyboardState previousKeyboard,
        MouseState currentMouse,
        MouseState previousMouse) {
        _frames[0] = new LocalPlayerInputFrame(
            ReadDirection(currentKeyboard, Keys.W, Keys.S, Keys.A, Keys.D),
            new Vector2(currentMouse.X, currentMouse.Y),
            new Vector2(currentMouse.X, currentMouse.Y),
            LocalAimSource.Mouse,
            currentMouse.LeftButton == ButtonState.Pressed && previousMouse.LeftButton == ButtonState.Released,
            JustPressed(currentKeyboard, previousKeyboard, Keys.Space),
            currentKeyboard.IsKeyDown(Keys.Q));

        var playerTwoAim = ReadDirection(currentKeyboard, Keys.Up, Keys.Down, Keys.Left, Keys.Right);
        if (playerTwoAim != Vector2.Zero)
            _playerTwoAim = playerTwoAim;

        _frames[1] = new LocalPlayerInputFrame(
            ReadDirection(currentKeyboard, Keys.I, Keys.K, Keys.J, Keys.L),
            _playerTwoAim,
            playerTwoAim,
            LocalAimSource.Direction,
            JustPressed(currentKeyboard, previousKeyboard, Keys.Enter),
            JustPressed(currentKeyboard, previousKeyboard, Keys.RightShift),
            currentKeyboard.IsKeyDown(Keys.P));
    }

    public LocalPlayerInputFrame GetFrame(int playerId) {
        if ((uint)playerId >= _frames.Length)
            throw new ArgumentOutOfRangeException(nameof(playerId), playerId, "Only local players 0 and 1 are supported.");

        return _frames[playerId];
    }

    private static Vector2 ReadDirection(KeyboardState state, Keys up, Keys down, Keys left, Keys right) {
        var direction = Vector2.Zero;
        if (state.IsKeyDown(up)) direction.Y -= 1;
        if (state.IsKeyDown(down)) direction.Y += 1;
        if (state.IsKeyDown(left)) direction.X -= 1;
        if (state.IsKeyDown(right)) direction.X += 1;
        return direction == Vector2.Zero ? direction : Vector2.Normalize(direction);
    }

    private static bool JustPressed(KeyboardState current, KeyboardState previous, Keys key) =>
        current.IsKeyDown(key) && previous.IsKeyUp(key);
}
