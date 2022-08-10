using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.Core;

namespace TanksRebirth.Internals.UI
{
    // make position stored as a Func<Vector2> so said position is always set, which will make UI adjustment easier.
    public abstract partial class UIElement
    {
        public static int delay;

        public static Texture2D UIPanelBackground;
        public bool HasScissor { get; set; }

        public Rectangle Scissor = new(-int.MaxValue, -int.MaxValue, 0, 0);

        public delegate void MouseEvent(UIElement affectedElement);

        private Vector2 InternalPosition;

        private Vector2 InternalSize;

        public string Tooltip;

        /// <summary>Only use TopLeft, BottomLeft, TopRight, and BottomRight</summary>
        public Anchor TooltipAnchor;

        /// <summary>A list of all <see cref="UIElement"/>s.</summary>
        public static List<UIElement> AllUIElements { get; internal set; } = new();

        /// <summary>The parent of this <see cref="UIElement"/>.</summary>
        public UIElement Parent { get; private set; }

        /// <summary>This <see cref="UIElement"/>'s children.</summary>
        protected IList<UIElement> Children { get; set; } = new List<UIElement>();

        /// <summary>The hitbox of this <see cref="UIElement"/>.</summary>
        public Rectangle Hitbox => new((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y);

        /// <summary>The position of this <see cref="UIElement"/>.</summary>
        public Vector2 Position;

        /// <summary>The size of this <see cref="UIElement"/>.</summary>
        public Vector2 Size;

        public Vector2 ScaleOrigin = new Vector2(0.5f);

        /// <summary>Whether or not this <see cref="UIElement"/> is visible. If set to <see langword="false"/>, the <see cref="UIElement"/> will not accept mouse input.</summary>
        public bool IsVisible = true;

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
        /// <summary>The anchor of this <see cref="UIElement"/>. 
        /// <para>WARNING: Does not work graphically.</para></summary>
        public Anchor Anchor { get; set; } = Anchor.TopLeft;


        /// <summary>Whether or not the <see cref="UIElement"/> should draw its children before itself.</summary>
        public bool ReverseDrawOrder { get; set; }

        internal static UIPanel cunoSucksElement;

        internal UIElement()
        {
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
            _doUpdating = false;
            Recalculate();
        }

        private Func<Vector2> _updatedPos;
        private Func<Vector2> _updatedSize;
        private bool _doUpdating;

        public void SetDimensions(Func<Vector2> position, Func<Vector2> dimensions)
        {
            _updatedPos = position;
            _updatedSize = dimensions;
            _doUpdating = true;
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
            cunoSucksElement = new() { IsVisible = false };
            cunoSucksElement.Remove();
            cunoSucksElement = new();
            cunoSucksElement.SetDimensions(-1000789342, -783218, 0, 0);
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
            if (!IsVisible)
                return;

            if (!HasScissor)
            {
                if (!ReverseDrawOrder)
                {
                    DrawSelf(spriteBatch);
                    DrawChildren(spriteBatch);
                }
                else
                {
                    DrawChildren(spriteBatch);
                    DrawSelf(spriteBatch);
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

                if (!ReverseDrawOrder)
                {
                    DrawSelf(spriteBatch);
                    DrawChildren(spriteBatch);
                }
                else
                {
                    DrawChildren(spriteBatch);
                    DrawSelf(spriteBatch);
                }

                /*if (Tooltip is not null && Hitbox.Contains(GameUtils.MousePosition))
                {
                    QuickIndicator(spriteBatch, Color.White);
                }*/

                spriteBatch.End();
                // draw with schissor
            }
        }
        public virtual void DrawTooltips(SpriteBatch spriteBatch)
        {
            if (!IsVisible)
                return;

            if (Tooltip is not null && Hitbox.Contains(GameUtils.MousePosition))
            {
                DrawTooltipBox(spriteBatch, Color.White);
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
                if (child.HasScissor)
                    spriteBatch.End();

                child?.Draw(spriteBatch);

                if (child.HasScissor)
                    spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
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
                if (!currentElement.IgnoreMouseInteractions && currentElement.IsVisible && currentElement.Hitbox.Contains(position))
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

        // TODO: tooltip anchors
        internal void DrawTooltipBox(SpriteBatch spriteBatch, Color color)
        {
            SpriteFontBase font = TankGame.TextFont;
            Vector2 scaleFont = font.MeasureString(Tooltip);
            Rectangle Hitbox = new(GameUtils.MouseX + 5, GameUtils.MouseY + 5, (int)scaleFont.X + 30, (int)scaleFont.Y + 30);
            Texture2D texture = UIPanelBackground;

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

            spriteBatch.DrawString(font, Tooltip, Hitbox.Center.ToVector2(), Color.Black, new Vector2(1f), 0f, scaleFont / 2, 0);
        }

        public static void UpdateElements()
        {
            var focusedElements = GetElementsAt(GameUtils.MousePosition, true);

            foreach (UIElement el in focusedElements)
            {
                if (el is not null)
                {
                    el.LeftClick();
                    el.LeftDown();
                    el.LeftUp();

                    el.RightClick();
                    el.RightDown();
                    el.RightUp();

                    el.MiddleClick();
                    el.MiddleDown();
                    el.MiddleUp();

                    el.MouseOver();

                }
            }

            foreach (var element in AllUIElements)
            {
                if (element != null)
                {
                    if (element._doUpdating)
                    {
                        element.Position = element._updatedPos.Invoke();
                        element.Size = element._updatedSize.Invoke();
                    }
                }
            }

            var trySlider = GetElementsAt(GameUtils.MousePosition);

            if (trySlider.Count > 0)
            {
                if (trySlider[0] != null)
                {
                    UIElement elementWeWant = trySlider[0].GetElementAt(GameUtils.MousePosition);
                    if (elementWeWant.GetType() != typeof(UIImage))
                        return;

                    elementWeWant.LeftClick();
                    elementWeWant.LeftDown();
                    elementWeWant.LeftUp();

                    elementWeWant.RightClick();
                    elementWeWant.RightDown();
                    elementWeWant.RightUp();

                    elementWeWant.MiddleClick();
                    elementWeWant.MiddleDown();
                    elementWeWant.MiddleUp();

                    elementWeWant.MouseOver();
                }
            }

            foreach (UIElement element in AllUIElements)
            {
                if (element.MouseHovering)
                    element.MouseOut();
            }
        }
    }
}