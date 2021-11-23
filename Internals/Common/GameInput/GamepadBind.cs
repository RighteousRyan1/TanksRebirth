using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace WiiPlayTanksRemake.Internals.Common.GameInput
{
    public class GamepadBind
    {
        public static List<GamepadBind> AllGamepadBinds { get; internal set; } = new();

        public GamepadBind(string name, Buttons defaultKey = 0) {
            Name = name;
            AssignedKey = defaultKey;
            AllGamepadBinds.Add(this);
        }

        public bool JustReassigned
        {
            get; private set;
        }

        public bool JustPressed => !Input.OldGamePadSnapshot.IsButtonDown(AssignedKey) && Input.CurrentGamePadSnapshot.IsButtonDown(AssignedKey) && _bindingWait <= 0;
        public bool IsPressed => Input.CurrentGamePadSnapshot.IsButtonDown(AssignedKey) && _bindingWait <= 0;
        public bool IsReassignPending
        {
            get; private set;
        }

        public Buttons AssignedKey { get; internal set; } = 0;
        public string Name { get; set; } = "Not Named";

        /// <summary>
        /// This is an extra tool for people who use this. Used for displaying messages after a key is recently rebound.
        /// </summary>
        public bool RecentlyBound => _rebindAlertTime > 0;
        private int _rebindAlertTime;

        public Action<GamepadBind> GamepadBindPressAction { get; set; } = null;
        private int _bindingWait;

        public bool PendKeyReassign() {
            _bindingWait = 5;
            bool isOtherBindAssigning() {
                int reassignCounts = 0;
                for (int i = 0; i < AllGamepadBinds.Count; i++) {
                    var kBind = AllGamepadBinds[i];

                    if (kBind.IsReassignPending)
                        reassignCounts++;

                    if (reassignCounts >= 1)
                        return true;
                }
                return false;
            }
            if (!IsReassignPending && !isOtherBindAssigning()) {
                Console.WriteLine($"Reassigning '{Name}'... (Current: {AssignedKey})\nPress {AssignedKey} to stop binding, press Escape to unbind.");
                IsReassignPending = true;
            }
            else
                Console.WriteLine($"Tried reassigning '{Name}' but cannot.");
            return true;
        }

        private bool TryAcceptReassign() {
            if (_bindingWait <= 0) {
                if (Input.CurrentKeySnapshot.GetPressedKeys().Length > 0) {
                    var firstButton = Input.GetPressedButtons(Input.CurrentGamePadSnapshot.Buttons)[0];
                    if (Input.ButtonJustPressed(firstButton) && firstButton == AssignedKey) {
                        Console.WriteLine($"Stopped the assigning of '{Name}'");
                        IsReassignPending = false;
                        return false;
                    }
                    if (Input.ButtonJustPressed(firstButton) && firstButton == Buttons.Back) {
                        Console.WriteLine($"Unassigned '{Name}'");
                        AssignedKey = 0;
                        IsReassignPending = false;
                        return false;
                    }
                    Console.WriteLine($"Keybind of name '{Name}' key assigned from {AssignedKey} to '{firstButton}'");
                    AssignedKey = firstButton;

                    _rebindAlertTime = 45;
                    IsReassignPending = false;
                    JustReassigned = true;
                    _bindingWait = 5;
                    return true;
                }
            }
            return false;
        }

        internal void Update() {

            if (_bindingWait > 0)
                _bindingWait--;

            if (IsReassignPending)
                TryAcceptReassign();

            if (_rebindAlertTime > 0)
                _rebindAlertTime--;

            JustReassigned = false;

            if (IsPressed)
                GamepadBindPressAction?.Invoke(this);
        }

        public override string ToString() {
            return Name + " = {" + $"Button: {AssignedKey} | Pressed: {IsPressed} | ReassignPending: {IsReassignPending} " + "}";
        }
    }
} 