using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using WiiPlayTanksRemake.Internals.Common.GameUI;
using WiiPlayTanksRemake.Internals.Common.Utilities;
using WiiPlayTanksRemake.Internals.Core;

namespace WiiPlayTanksRemake.Internals.UI
{
    public abstract partial class UIElement
    {
        public bool HasScissor { get; set; }

        public Rectangle Scissor = new(-int.MaxValue, -int.MaxValue, 0, 0);

        public delegate void MouseEvent(UIElement affectedElement);

        private Vector2 InternalPosition;

        private Vector2 InternalSize;

        internal string Tooltip;

        /// <summary>A list of all <see cref="UIElement"/>s.</summary>
        public static List<UIElement> AllUIElements { get; internal set; } = new();

        /// <summary>The parent of this <see cref="UIElement"/>.</summary>
        public UIElement Parent { get; private set; }

        /// <summary>This <see cref="UIElement"/>'s children.</summary>
        protected IList<UIElement> Children { get; set; } = new List<UIElement>();

        /// <summary>The hitbox of this <see cref="UIElement"/>.</summary>
        public Rectangle Hitbox => new((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y);

        /// <summary>The position of this <see cref="UIElement"/>.</summary>
        public Vector2 Position { get; set; }

        /// <summary>The size of this <see cref="UIElement"/>.</summary>
        public Vector2 Size { get; set; }

        public Vector2 ScaleOrigin = new Vector2(0.5f);

        /// <summary>Whether or not this <see cref="UIElement"/> is visible. If set to <see langword="false"/>, the <see cref="UIElement"/> will not accept mouse input.</summary>
        public bool Visible = true;

        /// <summary>Whether or not the mouse is currently hovering over this <see cref="UIElement"/>.</summary>
        public bool MouseHovering;

        /// <summary>Whether or not <see cref="Initialize"/> has been called yet on this <see cref="UIElement"/>.</summary>
        public bool Initialized;

        /// <summary>Whether or not to handle mouse interactions for this <see cref="UIElement"/>.</summary>
        public bool IgnoreMouseInteractions;

        /// <summary>Whether or not to have the <see cref="UIElement"/> under this one fire mouse input events.</summary>
        public bool FallThroughInputs;

        /// <summary>The rotation of this <see cref="UIElement"/>.</summary>
        public float Rotation { get; set; } = 0;

        internal UIElement() {
            AllUIElements.Add(this);
        }

        /// <summary>
        /// Sets the dimensions of this <see cref="UIElement"/>.
        /// </summary>
        /// <param name="x">The x-coordinate of the top left corner of the created <see cref="UIElement"/>.</param>
        /// <param name="y">The y-coordinate of the top left corner of the created <see cref="UIElement"/>.</param>
        /// <param name="width">The width of the created <see cref="UIElement"/>.</param>
        /// <param name="height">The height of the created <see cref="UIElement"/>.</param>
        public void SetDimensions(float x, float y, float width, float height)
        {
            InternalPosition = new Vector2(x, y);
            InternalSize = new Vector2(width, height);
            Recalculate();
        }

        /// <summary>
        /// Sets the dimensions of this <see cref="UIElement"/>.
        /// </summary>
        /// <param name="rect">The <see cref="Rectangle"/> dictating the created <see cref="UIElement"/>'s dimensions.</param>
        public void SetDimensions(Rectangle rect)
        {
            InternalPosition = new Vector2(rect.X, rect.Y);
            InternalSize = new Vector2(rect.Width, rect.Height);
            Recalculate();
        }

        /// <summary>
        /// Recalculates the position and size of this <see cref="UIElement"/>. Called automatically after <see cref="SetDimensions"/>. Generally does not need to be called.
        /// </summary>
        public void Recalculate()
        {
            Position = InternalPosition;
            Size = InternalSize;
        }

        /// <summary>
        /// Draws the <see cref="UIElement"/> and associated content.
        /// </summary>
        /// <param name="spriteBatch">The <see cref="SpriteBatch"/> being used to draw this <see cref="UIElement"/> with.</param>
        public virtual void Draw(SpriteBatch spriteBatch)
        {
            if (!Visible)
                return;

            if (!HasScissor)
            {
                DrawSelf(spriteBatch);
                DrawChildren(spriteBatch);

                if (Tooltip is not null && Hitbox.Contains(GameUtils.MousePosition))
                {
                    QuickIndicator(spriteBatch, Color.White);
                }
            }
            else
            {
                var rastState = new RasterizerState
                {
                    ScissorTestEnable = true
                };

                TankGame.Instance.GraphicsDevice.RasterizerState = rastState;

                TankGame.Instance.GraphicsDevice.ScissorRectangle = Scissor;

                spriteBatch.Begin(rasterizerState: rastState);

                DrawSelf(spriteBatch);
                DrawChildren(spriteBatch);

                if (Tooltip is not null && Hitbox.Contains(GameUtils.MousePosition))
                {
                    QuickIndicator(spriteBatch, Color.White);
                }

                spriteBatch.End();
                // draw with schissor
            }
        }

        /// <summary>
        /// Initializes this <see cref="UIElement">.
        /// </summary>
        public void Initialize()
        {
            if (Initialized)
                return;

            OnInitialize();
            Initialized = true;
        }

        /// <summary>
        /// Runs directly after <see cref="Initialize"/>. Call the base value if overriding to call <see cref="Initialize"/> for all this <see cref="UIElement"/>'s children.
        /// </summary>
        public virtual void OnInitialize()
        {
            foreach (UIElement child in Children)
            {
                child.Initialize();
            }
        }

        /// <summary>
        /// Draws the <see cref="UIElement"/>. Called in <see cref="Draw(SpriteBatch)"/>.
        /// </summary>
        /// <param name="spriteBatch">The <see cref="SpriteBatch"/> being used to draw this <see cref="UIElement"/> with.</param>
        public virtual void DrawSelf(SpriteBatch spriteBatch) { }

        /// <summary>
        /// Draws the <see cref="UIElement"/>'s children.
        /// </summary>
        /// <param name="spriteBatch">The <see cref="SpriteBatch"/> being used to draw this <see cref="UIElement"/>'s children with.</param>
        public virtual void DrawChildren(SpriteBatch spriteBatch)
        {
            foreach (UIElement child in Children) {
                child.Draw(spriteBatch);
            }
        }

        /// <summary>
        /// Safely retrieves the first found child of this <see cref="UIElement"/> based on a predicate.
        /// </summary>
        /// <param name="predicate">The method used to find the child of this <see cref="UIElement"/>.</param>
        /// <param name="child">The child of the <see cref="UIElement"/>, if found.</param>
        /// <returns><see langword="true"/> if a child was found; otherwise, returns <see langword="false"/>.</returns>
        public virtual bool GetChildSafely(Func<UIElement, bool> predicate, out UIElement child)
        {
            child = Children.FirstOrDefault(predicate);
            return child != null;
        }

        /// <summary>
        /// Adds a child to this <see cref="UIElement"/>.
        /// </summary>
        /// <param name="element">The <see cref="UIElement"/> to add as a child.</param>
        public virtual void Append(UIElement element)
        {
            element.Remove();
            element.Parent = this;
            Children.Add(element);
        }

        /// <summary>
        /// Removes this <see cref="UIElement"/>.
        /// </summary>
        public virtual void Remove()
        {
            Parent?.Children.Remove(this);
            AllUIElements.Remove(this);
            Parent = null;
        }

        /// <summary>
        /// Removes a child from this <see cref="UIElement"/>.
        /// </summary>
        /// <param name="child">The <see cref="UIElement"/> to remove.</param>
        public virtual void Remove(UIElement child)
        {
            Children.Remove(child);
            child.Parent = null;
        }

        /// <summary>
        /// Gets a <see cref="UIElement"/> at the specified position.
        /// </summary>
        /// <param name="position">The position to get the <see cref="UIElement"/> at.</param>
        /// <returns>The <see cref="UIElement"/> at the specified position, if one exists; otherwise, returns <see langword="null"/>.</returns>
        public virtual UIElement GetElementAt(Vector2 position)
        {
            UIElement focusedElement = null;
            for (int iterator = Children.Count - 1; iterator >= 0; iterator--)
            {
                UIElement currentElement = Children[iterator];
                if (!currentElement.IgnoreMouseInteractions && currentElement.Visible && currentElement.Hitbox.Contains(position))
                {
                    focusedElement = currentElement;
                    break;
                }
            }

            if (focusedElement != null)
            {
                return focusedElement.GetElementAt(position);
            }

            if (IgnoreMouseInteractions)
            {
                return null;
            }

            return Hitbox.Contains(position) ? this : null;
        }

        internal void QuickIndicator(SpriteBatch spriteBatch, Color color)
        {
            SpriteFont font = TankGame.Fonts.Default;
            Vector2 scaleFont = font.MeasureString(Tooltip);
            Rectangle Hitbox = new Rectangle(GameUtils.MouseX + 5, GameUtils.MouseY + 5, (int)scaleFont.X + 30, (int)scaleFont.Y + 30);
            Texture2D texture = TankGame.UITextures.UIPanelBackground;

            int border = 12;

            int middleX = Hitbox.X + border;
            int rightX = Hitbox.Right - border;

            int middleY = Hitbox.Y + border;
            int bottomY = Hitbox.Bottom - border;

            spriteBatch.Draw(texture, new Rectangle(Hitbox.X, Hitbox.Y, border, border), new Rectangle(0, 0, border, border), color);
            spriteBatch.Draw(texture, new Rectangle(middleX, Hitbox.Y, Hitbox.Width - border * 2, border), new Rectangle(border, 0, texture.Width - border * 2, border), color);
            spriteBatch.Draw(texture, new Rectangle(rightX, Hitbox.Y, border, border), new Rectangle(texture.Width - border, 0, border, border), color);

            spriteBatch.Draw(texture, new Rectangle(Hitbox.X, middleY, border, Hitbox.Height - border * 2), new Rectangle(0, border, border, texture.Height - border * 2), color);
            spriteBatch.Draw(texture, new Rectangle(middleX, middleY, Hitbox.Width - border * 2, Hitbox.Height - border * 2), new Rectangle(border, border, texture.Width - border * 2, texture.Height - border * 2), color);
            spriteBatch.Draw(texture, new Rectangle(rightX, middleY, border, Hitbox.Height - border * 2), new Rectangle(texture.Width - border, border, border, texture.Height - border * 2), color);

            spriteBatch.Draw(texture, new Rectangle(Hitbox.X, bottomY, border, border), new Rectangle(0, texture.Height - border, border, border), color);
            spriteBatch.Draw(texture, new Rectangle(middleX, bottomY, Hitbox.Width - border * 2, border), new Rectangle(border, texture.Height - border, texture.Width - border * 2, border), color);
            spriteBatch.Draw(texture, new Rectangle(rightX, bottomY, border, border), new Rectangle(texture.Width - border, texture.Height - border, border, border), color);

            spriteBatch.DrawString(font, Tooltip, Hitbox.Center.ToVector2(), Color.Black, 0, scaleFont / 2, 1, SpriteEffects.None, 1f);
        }
    }
}