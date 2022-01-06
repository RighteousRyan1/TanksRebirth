using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using WiiPlayTanksRemake.Internals.Common;
using WiiPlayTanksRemake.Internals.Common.Utilities;
using WiiPlayTanksRemake.Internals.Core;

namespace WiiPlayTanksRemake.Internals.UI
{
    public abstract partial class UIElement
    {
		/// <summary>
		/// Gets a <see cref="UIElement"/> at the specified position.
		/// </summary>
		/// <param name="position">The position to get the <see cref="UIElement"/> at.</param>
		/// <param name="getHighest">Whether or not to get the highest <see cref="UIElement"/> as opposed to the lowest.</param>
		/// <returns>The <see cref="UIElement"/> at the specified position, if one exists; otherwise, returns <see langword="null"/>.</returns>
		public static UIElement GetElementAt(Vector2 position, bool getHighest = false)
		{
			UIElement focusedElement = null;

			for (int iterator = AllUIElements.Count - 1; iterator >= 0; iterator--)
			{
				UIElement currentElement = AllUIElements[iterator];
				if (!currentElement.IgnoreMouseInteractions && currentElement.Visible && currentElement.Hitbox.Contains(position))
				{
					focusedElement = currentElement;
					break;
				}
			}

			return getHighest ? focusedElement : focusedElement?.GetElementAt(position);
		}

		protected bool CanRegisterInput(Func<bool> uniqueInput)
		{
			if (!TankGame.Instance.IsActive)
			{
				return false;
			}

			if (Parent is null || Parent.Hitbox.Contains(GameUtils.MousePosition))
			{
				if (uniqueInput is null || uniqueInput.Invoke())
				{
					if (Hitbox.Contains(GameUtils.MousePosition))
					{
						return true;
					}
				}
			}

			return false;
		}

		public event Action<UIElement> OnLeftClick;

		public virtual void LeftClick()
		{
			if (CanRegisterInput(() => Input.MouseLeft && !Input.OldMouseLeft))
			{
				OnLeftClick?.Invoke(this);
			}
		}

		public event Action<UIElement> OnLeftDown;

		public virtual void LeftDown()
		{
			if (CanRegisterInput(() => Input.MouseLeft))
			{
				OnLeftDown?.Invoke(this);
			}
		}

		public event Action<UIElement> OnLeftUp;

		public virtual void LeftUp()
		{
			if (CanRegisterInput(() => !Input.MouseLeft))
			{
				OnLeftUp?.Invoke(this);
			}
		}

		//---------------------------------------------------------

		public event Action<UIElement> OnRightClick;

		public virtual void RightClick()
		{
			if (CanRegisterInput(() => Input.MouseRight && !Input.OldMouseRight))
			{
				OnRightClick?.Invoke(this);
			}
		}

		public event Action<UIElement> OnRightDown;

		public virtual void RightDown()
		{
			if (CanRegisterInput(() => Input.MouseRight))
			{
				OnRightDown?.Invoke(this);
			}
		}

		public event Action<UIElement> OnRightUp;

		public virtual void RightUp()
		{
			if (CanRegisterInput(() => !Input.MouseRight))
			{
				OnRightUp?.Invoke(this);
			}
		}

		//--------------------------------------------------------

		public event Action<UIElement> OnMiddleClick;

		public virtual void MiddleClick()
		{
			if (CanRegisterInput(() => Input.MouseMiddle && !Input.OldMouseMiddle))
			{
				OnMiddleClick?.Invoke(this);
			}
		}

		public event Action<UIElement> OnMiddleDown;

		public virtual void MiddleDown()
		{
			if (CanRegisterInput(() => Input.MouseMiddle))
			{
				OnMiddleDown?.Invoke(this);
			}
		}

		public event Action<UIElement> OnMiddleUp;

		public virtual void MiddleUp()
		{
			if (CanRegisterInput(() => !Input.MouseMiddle))
			{
				OnMiddleUp?.Invoke(this);
			}
		}

		//--------------------------------------------------------

		public event Action<UIElement> OnMouseOver;

		public virtual void MouseOver()
		{
			if (!TankGame.Instance.IsActive)
			{
				return;
			}

			if (Parent is null || Parent.Hitbox.Contains(GameUtils.MousePosition))
			{
				if (Hitbox.Contains(GameUtils.MousePosition))
				{
					OnMouseOver?.Invoke(this);
					MouseHovering = true;
				}
			}
		}

		public event Action<UIElement> OnMouseOut;

		public virtual void MouseOut()
		{
			if (!TankGame.Instance.IsActive)
			{
				return;
			}

			if (!Hitbox.Contains(GameUtils.MousePosition))
			{
				OnMouseOut?.Invoke(this);
				MouseHovering = false;
			}
		}
	}
}