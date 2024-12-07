using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.Core;

namespace TanksRebirth.Internals.UI {
    public abstract partial class UIElement {
        private bool _wasHovered;
        /// <summary>Whether or not the user is able to interact with this <see cref="UIElement"/>.</summary>
        public bool IsInteractable { get; set; } = true;

        private static List<UIElement> TraverseChildrenFallThroughInputs(UIElement element) {
            if (!element.FallThroughInputs) return [];

            var nList = new List<UIElement>();
            var children = element.Children;

            /*
             *  We must collect all the children of the UIElement which allow to fall through input (and the children of such. Meaning we will have to do some GOOD recursion).
             */

            foreach (var child in children) {
                if (child.FallThroughInputs)
                    nList.AddRange(TraverseChildrenFallThroughInputs(child));
                else
                    nList.Add(child);
            }

            return nList;
        }

        private static List<UIElement> TraverseFocusedElementsList(List<UIElement> enumerable) {
            var finalListOfElements_0 = enumerable.ToList(); // Clone
            foreach (var element in enumerable)
                finalListOfElements_0.AddRange(TraverseChildrenFallThroughInputs(element));

            // Try to discard duplicates.
            List<UIElement> finalListOfElements_1 = [];

            foreach (var element in finalListOfElements_0.Where(element => !finalListOfElements_1.Contains(element)))
                finalListOfElements_1.Add(element);

            return finalListOfElements_1;
        }

        /// <summary>
        /// Gets a <see cref="UIElement"/> at the specified position.
        /// </summary>
        /// <param name="position">The position to get the <see cref="UIElement"/> at.</param>
        /// <param name="getHighest">Whether or not to get the highest <see cref="UIElement"/> as opposed to the lowest.</param>
        /// <returns>The <see cref="UIElement"/> at the specified position, if one exists; otherwise, returns <see langword="null"/>.</returns>
        public static List<UIElement> GetElementsAt(Vector2 position, bool getHighest = false) {
            List<UIElement> focusedElements = new(AllUIElements.Count / 8);

            if (!getHighest) {
                for (var iterator = AllUIElements.Count; iterator >= 0; iterator--) {
                    var currentElement = AllUIElements[iterator];
                    if (currentElement.IgnoreMouseInteractions || !currentElement.IsVisible || !currentElement.Hitbox.Contains(position))
                        continue;
                    if (!currentElement.FallThroughInputs)
                        break;
                    focusedElements.Add(currentElement);
                }
            }
            else {
                for (var iterator = 0; iterator < AllUIElements.Count; iterator++) {
                    var currentElement = AllUIElements[iterator];
                    if (currentElement.IgnoreMouseInteractions || !currentElement.IsVisible || !currentElement.Hitbox.Contains(position))
                        continue;

                    if (!focusedElements.Contains(currentElement))
                        focusedElements.Add(currentElement);

                    if (iterator + 1 >= AllUIElements.Count)
                        continue;

                    if (!currentElement.FallThroughInputs)
                        break;

                    focusedElements.Add(AllUIElements[iterator + 1]);
                }
            }

            /*
             *  Assuming this function is EXCLUSIVELY used for inputs... (BECAUSE IT IS)
             */

            var finalElementsList = TraverseFocusedElementsList(focusedElements);
            return finalElementsList;
        }

        private bool IsInputValid(bool? requiredInput) {
            if (!TankGame.Instance.IsActive || !IsInteractable)
                return false;

            if (Parent != null && !Parent.Hitbox.Contains(MouseUtils.MousePosition))
                return false;

            if (requiredInput.HasValue && !requiredInput.Value)
                return false;

            if (!Hitbox.Contains(MouseUtils.MousePosition))
                return false;

            return (HasScissor && Scissor.Invoke().Contains(MouseUtils.MousePosition)) || !HasScissor;
        }

        public Action<UIElement> OnLeftClick;

        public void LeftClick() {
            if (!IsInputValid(InputUtils.MouseLeft && !InputUtils.OldMouseLeft))
                return;

            if (delay <= 0)
                OnLeftClick?.Invoke(this);
            delay = 2;
        }

        public Action<UIElement> OnLeftDown;

        public void LeftDown() {
            if (!IsInputValid(InputUtils.MouseLeft))
                return;

            OnLeftDown?.Invoke(this);
        }

        public Action<UIElement> OnLeftUp;

        public void LeftUp() {
            if (!IsInputValid(!InputUtils.MouseLeft))
                return;

            OnLeftUp?.Invoke(this);
        }

        //---------------------------------------------------------

        public Action<UIElement> OnRightClick;

        public void RightClick() {
            if (!IsInputValid(InputUtils.MouseRight && !InputUtils.OldMouseRight))
                return;

            if (delay <= 0)
                OnRightClick?.Invoke(this);

            delay = 2;
        }

        public Action<UIElement> OnRightDown;

        public void RightDown() {
            if (!IsInputValid(InputUtils.MouseRight))
                return;

            OnRightDown?.Invoke(this);
        }

        public Action<UIElement> OnRightUp;

        public void RightUp() {
            if (!IsInputValid(!InputUtils.MouseRight))
                return;
            OnRightUp?.Invoke(this);
        }

        //--------------------------------------------------------

        public Action<UIElement> OnMiddleClick;

        public void MiddleClick() {
            if (!IsInputValid(InputUtils.MouseMiddle && !InputUtils.OldMouseMiddle))
                return;

            if (delay <= 0)
                OnMiddleClick?.Invoke(this);
            delay = 2;
        }

        public Action<UIElement> OnMiddleDown;

        public void MiddleDown() {
            if (!IsInputValid(InputUtils.MouseMiddle))
                return;

            OnMiddleDown?.Invoke(this);
        }

        public Action<UIElement> OnMiddleUp;

        public void MiddleUp() {
            if (!IsInputValid(!InputUtils.MouseMiddle))
                return;

            OnMiddleUp?.Invoke(this);
        }

        //--------------------------------------------------------

        public Action<UIElement> OnMouseOver;

        public void MouseOver() {
            if (!TankGame.Instance.IsActive)
                return;

            _wasHovered = MouseHovering;

            if (Parent is not null && !Parent.Hitbox.Contains(MouseUtils.MousePosition))
                return;

            if (!Hitbox.Contains(MouseUtils.MousePosition) || _wasHovered)
                return;

            if ((!HasScissor || !Scissor.Invoke().Contains(MouseUtils.MousePosition)) && HasScissor)
                return;

            OnMouseOver?.Invoke(this);
            MouseHovering = true;
            _wasHovered = MouseHovering;
        }

        public Action<UIElement> OnMouseOut;

        public void MouseOut() {
            if (!TankGame.Instance.IsActive)
                return;

            if (Hitbox.Contains(MouseUtils.MousePosition))
                return;

            OnMouseOut?.Invoke(this);
            MouseHovering = false;
        }
    }
}