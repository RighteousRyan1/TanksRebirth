using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace WiiPlayTanksRemake.Internals.Common.GameInput
{
    public class Keybind
    {
        public static List<Keybind> AllKeybinds { get; internal set; } = new();

        public Keybind(string name, Keys defaultKey = Keys.None) {
            Name = name;
            AssignedKey = defaultKey;
            AllKeybinds.Add(this);
        }

        public bool JustReassigned { get; private set; }

        public bool JustPressed => Input.KeyJustPressed(AssignedKey) && _bindingWait <= 0;
        public bool IsPressed => Input.CurrentKeySnapshot.IsKeyDown(AssignedKey) && _bindingWait <= 0;
        public bool IsReassignPending { get; private set; }

        public Keys AssignedKey { get; internal set; } = Keys.None;
        public string Name { get; set; } = "Not Named";

        /// <summary>
        /// This is an extra tool for people who use this. Used for displaying messages after a key is recently rebound.
        /// </summary>
        public bool RecentlyBound => _rebindAlertTime > 0;
        private int _rebindAlertTime;

        public Action<Keybind> KeybindPressAction { get; set; } = null;
        private int _bindingWait;

        public bool PendKeyReassign() {
            _bindingWait = 5;
            bool isOtherBindAssigning() {
                int reassignCounts = 0;
                for (int i = 0; i < AllKeybinds.Count; i++) {
                    var kBind = AllKeybinds[i];

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
                    var firstKey = Input.CurrentKeySnapshot.GetPressedKeys()[0];
                    if (Input.KeyJustPressed(firstKey) && firstKey == AssignedKey) {
                        Console.WriteLine($"Stopped the assigning of '{Name}'");
                        IsReassignPending = false;
                        return false;
                    }
                    if (Input.KeyJustPressed(firstKey) && firstKey == Keys.Escape) {
                        Console.WriteLine($"Unassigned '{Name}'");
                        AssignedKey = Keys.None;
                        IsReassignPending = false;
                        return false;
                    }
                    Console.WriteLine($"Keybind of name '{Name}' key assigned from {AssignedKey} to '{firstKey.ParseKey()}'");
                    AssignedKey = firstKey;

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

            if (JustPressed)
                KeybindPressAction?.Invoke(this);
        }

        public override string ToString() {
            return Name + " = {" + $"Key: {AssignedKey.ParseKey()} | Pressed: {IsPressed} | ReassignPending: {IsReassignPending} " + "}";
        }
    }
}