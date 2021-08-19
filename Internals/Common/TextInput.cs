using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using WiiPlayTanksRemake.Internals.Common.GameInput;

namespace WiiPlayTanksRemake.Internals.Common
{
    public class TextInput
    {
        private TextInput() {
        }

        public static bool trackingInput;
        private static Keys _lastInputKey = Keys.None;
        private static int _timeBeforeRepitition;

        public static int pressDelay = 0;

        public static string InputtedText { get; private set; } = string.Empty;

        // starts the tracking of keys, only when input is not already being tracked
        public static void BeginInput() {
            trackingInput = true;
            pressDelay = 20;
        }

        // stops tracking keys
        public static void EndInput() {
            trackingInput = false;
            InputtedText = string.Empty;
        }

        public static void TrackInputKeys() {
            if (trackingInput) {
                bool isBack = Input.FirstPressedKey == Keys.Back;
                bool isSpace = Input.FirstPressedKey == Keys.Space;
                bool ignoreable = Input.FirstPressedKey == Keys.Enter || (int)Input.FirstPressedKey >= 91;

                if (ignoreable)
                    return;

                if (isSpace) {
                    InputtedText += " ";
                    _lastInputKey = Keys.Space;
                }

                if (isBack && InputtedText.Length > 0) {
                    InputtedText.Remove(InputtedText.Length - 1);
                }

                if ((Input.FirstPressedKey != _lastInputKey || _timeBeforeRepitition <= 0) && Input.FirstPressedKey != Keys.None && !isSpace && !isBack) {
                    InputtedText += Input.FirstPressedKey.ParseKey();
                    _lastInputKey = Input.FirstPressedKey;
                    _timeBeforeRepitition = 10;
                }
            }
            if (_timeBeforeRepitition > 0)
                _timeBeforeRepitition--;
        }
    }
}