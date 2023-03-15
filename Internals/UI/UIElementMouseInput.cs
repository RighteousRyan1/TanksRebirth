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

namespace TanksRebirth.Internals.UI
{
    public abstract partial class UIElement
    {
		private bool _wasHovered;

		/// <summary>
		/// Gets a <see cref="UIElement"/> at the specified position.
		/// </summary>
		/// <param name="position">The position to get the <see cref="UIElement"/> at.</param>
		/// <param name="getHighest">Whether or not to get the highest <see cref="UIElement"/> as opposed to the lowest.</param>
		/// <returns>The <see cref="UIElement"/> at the specified position, if one exists; otherwise, returns <see langword="null"/>.</returns>
		public static List<UIElement> GetElementsAt(Vector2 position, bool getHighest = false)
		{
			List<UIElement> focusedElements = new();

			if (!getHighest)
            {
				for (var i = AllUIElements.Count - 1; i >= 0; i--) {
					var currentElement = AllUIElements[i];
					if (currentElement is not { IgnoreMouseInteractions: false, IsVisible: true } ||
					    !currentElement.Hitbox.Contains(position)) continue;
					focusedElements.Add(currentElement);
					if (!currentElement.FallThroughInputs)
						break;
				}
				return focusedElements;
            }
			for (var i = 0; i < AllUIElements.Count - 1; i++) {
				var currentElement = AllUIElements[i];
				if (currentElement is not { IgnoreMouseInteractions: false, IsVisible: true } ||
				    !currentElement.Hitbox.Contains(position)) continue;
				
				focusedElements.Add(currentElement);
				
				if (i + 1 > AllUIElements.Count) continue;
				
				if (currentElement.FallThroughInputs)
					focusedElements.Add(AllUIElements[i + 1]);
				else
					break;
			}

			return focusedElements;
		}

		protected bool CanRegisterInput(Func<bool> uniqueInput) {
			if (!TankGame.Instance.IsActive)
				return false;

			if (Parent is not null && !Parent.Hitbox.Contains(MouseUtils.MousePosition)) return false;
			
			if (uniqueInput is not null && !uniqueInput.Invoke()) return false;
			
			if (!Hitbox.Contains(MouseUtils.MousePosition)) return false;
			
			return (HasScissor && Scissor.Invoke().Contains(MouseUtils.MousePosition)) || !HasScissor;
		}

		public Action<UIElement> OnLeftClick;

		public virtual void LeftClick() {
			if (!CanRegisterInput(() => InputUtils.MouseLeft && !InputUtils.OldMouseLeft)) return;
			
			if (delay <= 0)
				OnLeftClick?.Invoke(this);
			delay = 2;
		}

		public Action<UIElement> OnLeftDown;

		public virtual void LeftDown() {
            if (CanRegisterInput(() => InputUtils.MouseLeft))
                OnLeftDown?.Invoke(this);
		}

		public Action<UIElement> OnLeftUp;

		public virtual void LeftUp() {
            if (CanRegisterInput(() => !InputUtils.MouseLeft))
				OnLeftUp?.Invoke(this);
		}

		//---------------------------------------------------------

		public Action<UIElement> OnRightClick;

		public virtual void RightClick() {
			if (!CanRegisterInput(() => InputUtils.MouseRight && !InputUtils.OldMouseRight)) return;
			
			if (delay <= 0)
				OnRightClick?.Invoke(this);
			delay = 2;
		}

		public Action<UIElement> OnRightDown;

		public virtual void RightDown() {
			if (CanRegisterInput(() => InputUtils.MouseRight))
				OnRightDown?.Invoke(this);
		}

		public Action<UIElement> OnRightUp;

		public virtual void RightUp() {
			if (CanRegisterInput(() => !InputUtils.MouseRight))
				OnRightUp?.Invoke(this);
		}

		//--------------------------------------------------------

		public Action<UIElement> OnMiddleClick;

		public virtual void MiddleClick() {
			if (!CanRegisterInput(() => InputUtils.MouseMiddle && !InputUtils.OldMouseMiddle)) return;
			
			if (delay <= 0)
				OnMiddleClick?.Invoke(this);
			delay = 2;
		}

		public Action<UIElement> OnMiddleDown;

		public virtual void MiddleDown() {
			if (CanRegisterInput(() => InputUtils.MouseMiddle))
				OnMiddleDown?.Invoke(this);
		}

		public Action<UIElement> OnMiddleUp;

		public virtual void MiddleUp() {
			if (CanRegisterInput(() => !InputUtils.MouseMiddle))
				OnMiddleUp?.Invoke(this);
		}

		//--------------------------------------------------------

		public Action<UIElement> OnMouseOver;

		public virtual void MouseOver() {
			if (!TankGame.Instance.IsActive)
				return;

			if (Parent is not null && !Parent.Hitbox.Contains(MouseUtils.MousePosition)) return;
			
			if (!Hitbox.Contains(MouseUtils.MousePosition) || _wasHovered) return;
			
			if ((!HasScissor || !Scissor.Invoke().Contains(MouseUtils.MousePosition)) && HasScissor) return;
			
			OnMouseOver?.Invoke(this);
			MouseHovering = true;

			_wasHovered = MouseHovering;
		}

		public Action<UIElement> OnMouseOut;

		public virtual void MouseOut() {
			if (!TankGame.Instance.IsActive)
				return;

			if (Hitbox.Contains(MouseUtils.MousePosition)) return;

			OnMouseOut?.Invoke(this);
			MouseHovering = false;
			_wasHovered = MouseHovering;
		}
	}
}