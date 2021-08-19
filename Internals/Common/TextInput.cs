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

        public static string InputtedText { get; private set; } = string.Empty;

        // starts the tracking of keys, only when input is not already being tracked
        public static void BeginInput() {
            trackingInput = true;
        }

        // stops tracking keys
        public static void EndInput(Action onEnd) {
            onEnd?.Invoke();
            trackingInput = false;
            InputtedText = string.Empty;
        }

        // tracks what keys are pressed sequentially
        public static void TrackInputKeys() {
            int delay = 60;
            if (trackingInput) {
                bool isBack = Input.FirstPressedKey == Keys.Back;
                bool isSpace = Input.FirstPressedKey == Keys.Space;
                bool ignoreable = Input.FirstPressedKey == Keys.Enter || (int)Input.FirstPressedKey >= 91;


                // finish this junk tomorrow loser
                if (!ignoreable) {
                    if (!isBack) {
                        if (isSpace) {
                            InputtedText += " ";
                            _lastInputKey = Keys.Space;
                            _timeBeforeRepitition = delay;
                        }
                        /*if (isSpace && _timeBeforeRepitition <= 0)
                        {
                            InputtedText += " ";
                            _lastInputKey = Keys.Space;
                        }*/
                        if (Input.FirstPressedKey != _lastInputKey && _timeBeforeRepitition > 0) {
                            _timeBeforeRepitition = delay;
                            if (Input.FirstPressedKey != Keys.None && !isSpace) {
                                InputtedText += Input.FirstPressedKey.ParseKey();
                                _lastInputKey = Input.FirstPressedKey;
                                _timeBeforeRepitition = delay;
                            }
                        }
                        else if (_timeBeforeRepitition <= 0) {
                            if (Input.FirstPressedKey != Keys.None) {
                                InputtedText += Input.FirstPressedKey.ParseKey();
                                _lastInputKey = Input.FirstPressedKey;
                            }
                        }
                    }
                    else if (_timeBeforeRepitition > 0) {
                        if (InputtedText.Length > 0)
                            InputtedText = InputtedText.Remove(InputtedText.Length - 1);
                        _lastInputKey = Keys.Back;
                        _timeBeforeRepitition = delay;
                    }
                    else if (_timeBeforeRepitition <= 0) {
                        if (InputtedText.Length > 0)
                            InputtedText = InputtedText.Remove(InputtedText.Length - 1);
                        _lastInputKey = Keys.Back;
                    }
                }
            }
            if (_timeBeforeRepitition > 0)
                _timeBeforeRepitition--;
        }
    }
}