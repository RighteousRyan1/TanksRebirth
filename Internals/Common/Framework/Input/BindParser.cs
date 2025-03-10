using Microsoft.Xna.Framework.Input;
using System;
using System.Reflection.Metadata.Ecma335;
using static Microsoft.Xna.Framework.Input.Keys;

namespace TanksRebirth.Internals.Common.Framework.Input;

public static class BindParser
{
    public const Buttons None = (Buttons)255;
    private static int[] _numConverter = [1, 2, 3, 4, 5, 6, 7, 8, 9, 0];

    public static readonly int KeyCount = Enum.GetValues<Keys>().Length;
    public static readonly int ButtonCount = Enum.GetValues<Buttons>().Length;
    public static bool IsNum(this Keys key, out int num) {
        num = -1;
        foreach (var number in _numConverter) {
            if (key.ToString() == $"D{number}") {
                num = number;
                return true;
            }
        }
        return false;
    }

    public static string KeyAsString(this Keys key) {
        foreach (var num in _numConverter) {
            if (key.ToString() == $"D{num}") {
                return num.ToString();
            }
        }

        return key switch {
            OemPlus => "+",
            OemMinus => "-",
            OemCloseBrackets => "]",
            OemOpenBrackets => "[",
            OemSemicolon => ";",
            OemBackslash => "/",
            OemComma => ",",
            OemPeriod => ".",
            OemQuestion => "?",
            OemQuotes => "'",
            OemPipe => @"\",
            OemTilde => "`",
            _ => key.ToString(),
        };
    }

    // TODO: BIG, now that this is setup, go to the settings ui control binds and update the code to use this system.

    /// <summary>Gets a <see cref="Buttons"/> or a <see cref="Keys"/> and gets its unique identifier.</summary>
    /// <returns>The unique ID for the bind.</returns>
    public static int GetBindId(bool isKey, Keys key = Keys.None, Buttons button = None) {
        if (isKey)
            return (int)key;
        else
            return (int)button + KeyCount;
    }
    public static bool IsKey(int input) => input < KeyCount;
    public static Buttons ReadButton(int input) => Enum.GetValues<Buttons>()[input - KeyCount];
    public static Keys ReadKey(int input) => Enum.GetValues<Keys>()[input];
}