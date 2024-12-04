using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Gets a <see cref="UIElement"/> at the specified position.
        /// </summary>
        /// <param name="position">The position to get the <see cref="UIElement"/> at.</param>
        /// <param name="getHighest">Whether or not to get the highest <see cref="UIElement"/> as opposed to the lowest.</param>
        /// <returns>The <see cref="UIElement"/> at the specified position, if one exists; otherwise, returns <see langword="null"/>.</returns>
        public static List<UIElement> GetElementsAt(Vector2 position, bool getHighest = false) {
            List<UIElement> focusedElements = new(AllUIElements.Count / 8);

            if (!getHighest) {
                for (int iterator = AllUIElements.Count - 1; iterator >= 0; iterator--) {
                    UIElement currentElement = AllUIElements[iterator];
                    if (!currentElement.IgnoreMouseInteractions && currentElement.IsVisible && currentElement.Hitbox.Contains(position)) {
                        focusedElements.Add(currentElement);
                        if (!currentElement.FallThroughInputs)
                            break;
                    }
                }
            }
            else {
                for (int iterator = 0; iterator < AllUIElements.Count - 1; iterator++) {
                    UIElement currentElement = AllUIElements[iterator];
                    if (!currentElement.IgnoreMouseInteractions && currentElement.IsVisible && currentElement.Hitbox.Contains(position)) {
                        focusedElements.Add(currentElement);
                        if (iterator + 1 <= AllUIElements.Count) {
                            if (currentElement.FallThroughInputs) {
                                focusedElements.Add(AllUIElements[iterator + 1]);
                            }
                            else {
                                break;
                            }
                        }
                        //if (!currentElement.FallThroughInputs)
                        //break;
                    }
                }
            }

            return focusedElements;
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