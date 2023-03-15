using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Internals.Common.Framework.Input
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
        public bool JustPressed => InputUtils.KeyJustPressed(AssignedKey) && !PendKeyReassign;
        public bool IsPressed => InputUtils.CurrentKeySnapshot.IsKeyDown(AssignedKey) && !PendKeyReassign;
        public bool PendKeyReassign { get; set; } = false;

        public Action<Keys> OnKeyReassigned;

        public Keys AssignedKey { get; private set; } = Keys.None;
        public string Name { get; set; } = "Not Named";

        public Action<Keybind> KeybindPressAction { get; set; } = null;

        public void ForceReassign(Keys newKey) {
            AssignedKey = newKey;

            JustReassigned = true;
        }

        private void PollReassign() {
            var currentlyPressedKeys = InputUtils.CurrentKeySnapshot.GetPressedKeys();
            if (currentlyPressedKeys.Length <= 0) return;
            var firstKey = currentlyPressedKeys[0];
            if (InputUtils.KeyJustPressed(firstKey) && firstKey == AssignedKey) {
                OnKeyReassigned?.Invoke(AssignedKey);
                PendKeyReassign = false;
                return;
            }
            
            if (InputUtils.KeyJustPressed(firstKey) && firstKey == Keys.Escape) {
                AssignedKey = Keys.None;
                OnKeyReassigned?.Invoke(AssignedKey);
                PendKeyReassign = false;
                return;
            }

            AssignedKey = firstKey;
            OnKeyReassigned?.Invoke(AssignedKey);
            PendKeyReassign = false;
        }

        public void Fire() { KeybindPressAction?.Invoke(this); }

        internal void Update()
        {
            if (PendKeyReassign)
                PollReassign();

            JustReassigned = false;

            if (JustPressed)
                KeybindPressAction?.Invoke(this);
        }

        public override string ToString() {
            return Name + " = {" + $"Key: {AssignedKey.ParseKey()} | Pressed: {IsPressed} | ReassignPending: {PendKeyReassign} " + "}";
        }
    }
}