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
				for (int iterator = AllUIElements.Count - 1; iterator >= 0; iterator--)
				{
					UIElement currentElement = AllUIElements[iterator];
					if (!currentElement.IgnoreMouseInteractions && currentElement.IsVisible && currentElement.Hitbox.Contains(position))
					{
						focusedElements.Add(currentElement);
						if (!currentElement.FallThroughInputs)
							break;
					}
				}
			}
			else
            {
				for (int iterator = 0; iterator < AllUIElements.Count - 1; iterator++)
				{
					UIElement currentElement = AllUIElements[iterator];
					if (!currentElement.IgnoreMouseInteractions && currentElement.IsVisible && currentElement.Hitbox.Contains(position))
					{
						focusedElements.Add(currentElement);
						if (iterator + 1 <= AllUIElements.Count)
                        {
							if (currentElement.FallThroughInputs)
                            {
								focusedElements.Add(AllUIElements[iterator + 1]);
                            }
							else
                            {
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

		protected bool CanRegisterInput(Func<bool> uniqueInput)
		{
			if (!TankGame.Instance.IsActive)
				return false;

			if (Parent is null || Parent.Hitbox.Contains(MouseUtils.MousePosition))
			{
				if (uniqueInput is null || uniqueInput.Invoke())
				{
					if (Hitbox.Contains(MouseUtils.MousePosition))
					{
						if ((HasScissor && Scissor.Invoke().Contains(MouseUtils.MousePosition)) || !HasScissor)
							return true;
					}
				}
			}

			return false;
		}

		public Action<UIElement> OnLeftClick;

		public virtual void LeftClick()
		{
			if (CanRegisterInput(() => InputUtils.MouseLeft && !InputUtils.OldMouseLeft))
			{
				if (delay <= 0)
					OnLeftClick?.Invoke(this);
				delay = 2;
			}
		}

		public Action<UIElement> OnLeftDown;

		public virtual void LeftDown()
		{
            if (CanRegisterInput(() => InputUtils.MouseLeft))
			{
                // ChatSystem.SendMessage(this is UISlider, Color.White);
                OnLeftDown?.Invoke(this);
			}
		}

		public Action<UIElement> OnLeftUp;

		public virtual void LeftUp()
		{
            if (CanRegisterInput(() => !InputUtils.MouseLeft))
			{
				OnLeftUp?.Invoke(this);
			}
		}

		//---------------------------------------------------------

		public Action<UIElement> OnRightClick;

		public virtual void RightClick()
		{
			if (CanRegisterInput(() => InputUtils.MouseRight && !InputUtils.OldMouseRight))
			{
				if (delay <= 0)
					OnRightClick?.Invoke(this);
				delay = 2;
			}
		}

		public Action<UIElement> OnRightDown;

		public virtual void RightDown()
		{
			if (CanRegisterInput(() => InputUtils.MouseRight))
			{
				OnRightDown?.Invoke(this);
			}
		}

		public Action<UIElement> OnRightUp;

		public virtual void RightUp()
		{
			if (CanRegisterInput(() => !InputUtils.MouseRight))
			{
				OnRightUp?.Invoke(this);
			}
		}

		//--------------------------------------------------------

		public Action<UIElement> OnMiddleClick;

		public virtual void MiddleClick()
		{
			if (CanRegisterInput(() => InputUtils.MouseMiddle && !InputUtils.OldMouseMiddle))
			{
				if (delay <= 0)
					OnMiddleClick?.Invoke(this);
				delay = 2;
			}
		}

		public Action<UIElement> OnMiddleDown;

		public virtual void MiddleDown()
		{
			if (CanRegisterInput(() => InputUtils.MouseMiddle))
			{
				OnMiddleDown?.Invoke(this);
			}
		}

		public Action<UIElement> OnMiddleUp;

		public virtual void MiddleUp()
		{
			if (CanRegisterInput(() => !InputUtils.MouseMiddle))
			{
				OnMiddleUp?.Invoke(this);
			}
		}

		//--------------------------------------------------------

		public Action<UIElement> OnMouseOver;

		public virtual void MouseOver()
		{
			if (!TankGame.Instance.IsActive)
			{
				return;
			}

			if (Parent is null || Parent.Hitbox.Contains(MouseUtils.MousePosition))
			{
				if (Hitbox.Contains(MouseUtils.MousePosition) && !_wasHovered)
				{
					if ((HasScissor && Scissor.Invoke().Contains(MouseUtils.MousePosition)) || !HasScissor)
                    {
						OnMouseOver?.Invoke(this);
						MouseHovering = true;
					}
				}
			}

			_wasHovered = MouseHovering;
		}

		public Action<UIElement> OnMouseOut;

		public virtual void MouseOut()
		{
			if (!TankGame.Instance.IsActive)
			{
				return;
			}

			if (!Hitbox.Contains(MouseUtils.MousePosition))
			{
				OnMouseOut?.Invoke(this);
				MouseHovering = false;
			}
		}
	}
}