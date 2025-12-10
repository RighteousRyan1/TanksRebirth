using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Framework.Input;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Systems;

public enum ChatMessageCorner {
    TopLeft = 0,
    TopRight = 1,
    BottomLeft = 2,
    BottomRight = 3
}

/// <summary>A system for handling chat.</summary>
public sealed record ChatSystem {
    // constants
    const int MAX_LENGTH = 250;
    const int MAX_MESSAGES_AT_ONCE = 10;
    const float DEFAULT_SCALE = 0.8f;
    const int BOX_WIDTH_BASE = 750;
    const int PADDING_BASE = 8;
    const int MESSAGE_OFFSET_BASE = 20;


    public static List<ChatMessage> ChatMessages = [];
    public static int UnreadMessageCount;
    public static bool IsOpen;
    public static ChatMessageCorner Corner = ChatMessageCorner.TopLeft;
    public static string CurTyping = string.Empty;
    public static bool ActiveHandle;
    public static bool ChatBoxHover;
    public static Texture2D ChatAlert;

    static Vector2 _openOrigin = new(8, 8);

    static float _openProgress = 0f;
    static Color _backgroundColor = new(20, 20, 20); // Dark sleek background
    static Color _borderColor = new(60, 60, 60);

    // events
    public delegate void OnMessageAddedDelegate(string message);
    public static event OnMessageAddedDelegate? OnMessageAdded;

    // properties
    public static Keybind ToggleChat { get; } = new("Toggle Chat", Keys.F2) {
        OnPress = () => IsOpen = !IsOpen
    };

    // struct(s)
    public struct TextSection(string text, Color color) {
        public string Text = text;
        public Color Color = color;
    }

    // yes, terraria inspired :rolling_eyes:
    /// <summary>
    /// Parses input text for chat tags (e.g., [c/FF0000:Text]).
    /// </summary>
    static List<TextSection> ParseText(string text, Color defaultColor) {
        var snippets = new List<TextSection>();
        var buffer = new StringBuilder();
        var currentColor = defaultColor;

        for (int i = 0; i < text.Length; i++) {
            // tag start
            if (text[i] == '[' && i + 3 < text.Length && text[i + 1] == 'c' && text[i + 2] == '/') {
                if (buffer.Length > 0) {
                    snippets.Add(new TextSection(buffer.ToString(), currentColor));
                    buffer.Clear();
                }

                // parses hex
                int endHex = text.IndexOf(':', i);
                if (endHex != -1 && endHex - (i + 3) == 6) { //
                    string hexStr = text.Substring(i + 3, 6);
                    if (int.TryParse(hexStr, NumberStyles.HexNumber, null, out int hexVal)) {
                        // find tag endpoint
                        int endTag = text.IndexOf(']', endHex);
                        if (endTag != -1) {
                            // Extract content inside tag
                            string content = text.Substring(endHex + 1, endTag - (endHex + 1));

                            // converts hex to a color
                            var tagColor = new Color(
                                (hexVal >> 16) & 0xFF,
                                (hexVal >> 8) & 0xFF,
                                hexVal & 0xFF
                            );

                            snippets.Add(new TextSection(content, tagColor));

                            // move to the end of the tag to continue parsing
                            i = endTag;
                            continue;
                        }
                    }
                }
            }

            buffer.Append(text[i]);
        }

        // remove buffer after parsing
        if (buffer.Length > 0) {
            snippets.Add(new TextSection(buffer.ToString(), currentColor));
        }

        return snippets;
    }

    public static void Initialize() {
        ChatAlert = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/chatalert");
    }

    public static void SendMessage(object contents, Color color, string? sender = null, bool netSend = false) {
        var message = contents.ToString()!;

        if (sender is not null) {
            SoundPlayer.PlaySoundInstance("Assets/sounds/menu/menu_tick.ogg", SoundContext.Effect);
            if (Client.IsConnected() && !netSend)
                Client.SendMessage(message, color, sender);
        }

        // split message by newlines
        var lines = message.Split('\n');
        foreach (var line in lines) {
            // Prepare the full raw string
            string fullText = (sender is not null && line == lines[0])
                ? $"<{sender}> {line}"
                : line;

            // tag parsing
            var snippets = ParseText(fullText, color);
            ChatMessages.Add(new ChatMessage(snippets, fullText));
        }

        UnreadMessageCount++;
        OnMessageAdded?.Invoke(message);
    }

    public static void SendMessage(object contents, string? sender = null, bool netSend = false) {
        SendMessage(contents, Color.White, sender, netSend);
    }

    private static string WrapText(SpriteFontBase font, string text, float maxLineWidth, float scale = 1f) {
        if (string.IsNullOrEmpty(text)) return "";

        string[] words = text.Split(' ');
        StringBuilder sb = new StringBuilder();
        float lineWidth = 0f;
        float spaceWidth = font.MeasureString(" ").X * scale;

        foreach (var word in words) {
            Vector2 size = font.MeasureString(word) * scale;
            if (lineWidth + size.X < maxLineWidth) {
                sb.Append(word + " ");
                lineWidth += size.X + spaceWidth;
            }
            else {
                if (sb.Length > 0) sb.Append("\n");
                sb.Append(word + " ");
                lineWidth = size.X + spaceWidth;
            }
        }
        return sb.ToString();
    }

    private static List<List<TextSection>> WrapColoredText(List<TextSection> sections, SpriteFontBase font, float maxLineWidth, float scale) {
        var lines = new List<List<TextSection>>();
        var currentLine = new List<TextSection>();
        float currentLineWidth = 0f;
        float spaceWidth = font.MeasureString(" ").X * scale;

        foreach (var section in sections) {
            var words = section.Text.Split(' ');
            for (int i = 0; i < words.Length; i++) {
                string word = words[i];
                // preserve space unless it's the very last word of the section which might not have had one
                // simplified: just add space to all
                string wordWithSpace = word + " ";
                float wordWidth = font.MeasureString(wordWithSpace).X * scale;

                if (currentLineWidth + wordWidth < maxLineWidth) {
                    currentLine.Add(new TextSection(wordWithSpace, section.Color));
                    currentLineWidth += wordWidth;
                }
                else {
                    if (currentLine.Count > 0) lines.Add([.. currentLine]);
                    currentLine.Clear();
                    currentLine.Add(new TextSection(wordWithSpace, section.Color));
                    currentLineWidth = wordWidth;
                }
            }
        }
        if (currentLine.Count > 0) lines.Add(currentLine);
        return lines;
    }

    public static void DrawMessages() {
        var sb = TankGame.SpriteRenderer;

        if (_openProgress <= 0f && !IsOpen) {
            sb.Begin();
            DrawUnreadNotification(sb);
            sb.End();
            return;
        }

        var font = FontGlobals.RebirthFont;

        // applies easing
        float smoothOpen = Easings.InOutQuint(_openProgress);

        var resScale = new Vector2(DEFAULT_SCALE).ToResolution();
        var padding = new Vector2(PADDING_BASE).ToResolution();
        // var messageOffset = MESSAGE_OFFSET_BASE.ToResolutionY();

        // calculates wrapped text
        float maxInputWidth = BOX_WIDTH_BASE.ToResolutionX(); // approximately
        string wrappedInput = WrapText(font, CurTyping, maxInputWidth, resScale.X);
        Vector2 inputSize = font.MeasureString(wrappedInput) * resScale;

        sb.Begin();

        // Pass the smoothed value to calculate position
        DrawChatBox(smoothOpen, out var chatRect, out var typeRect, inputSize.Y);

        var mousePos = MouseUtils.MousePosition;
        var crc = chatRect.Contains(mousePos);
        var trc = typeRect.Contains(mousePos);
        ChatBoxHover = crc || trc;

        void DrawPanel(Rectangle rect, bool hover) {
            // bg of box
            sb.Draw(TextureGlobals.Pixels[Color.White], rect, _backgroundColor * 0.85f * smoothOpen);

            var border = hover ? Color.CornflowerBlue : _borderColor;

            DrawUtils.DrawBox(rect, border * smoothOpen, 3);
        }

        DrawPanel(chatRect, crc);
        DrawPanel(typeRect, trc || ActiveHandle);

        if (smoothOpen > 0.1f) {
            // Calculate text position
            var typePos = new Vector2(typeRect.X + padding.X, typeRect.Y + padding.Y);

            sb.DrawString(font, wrappedInput, typePos, Color.White * smoothOpen, resScale);

            // blinking caret
            if (ActiveHandle && RuntimeData.RunTime % 60 < 30) {
                var lines = wrappedInput.Split('\n');
                var lastLine = lines.LastOrDefault() ?? "";
                var lineCount = lines.Length;

                var caretX = font.MeasureString(lastLine).X * resScale.X;
                var caretY = (lineCount - 1) * font.LineHeight * resScale.Y;

                var caretPos = typePos + new Vector2(caretX + 2, caretY);
                sb.Draw(TextureGlobals.Pixels[Color.White], new Rectangle((int)caretPos.X, (int)caretPos.Y, 2, (int)(font.MeasureString("A").Y * resScale.Y)), Color.White * smoothOpen);
            }
        }

        sb.End();

        var boxRasterizer = new RasterizerState() { ScissorTestEnable = true };
        sb.Begin(rasterizerState: boxRasterizer);

        TankGame.Instance.GraphicsDevice.ScissorRectangle = chatRect;

        HandleTextInputState(trc, typeRect);

        // prunes old messages
        while (ChatMessages.Count > MAX_MESSAGES_AT_ONCE) ChatMessages.RemoveAt(0);

        float currentY = chatRect.Y + chatRect.Height - padding.Y;
        float maxMsgWidth = chatRect.Width - (padding.X * 2);

        for (int i = ChatMessages.Count - 1; i >= 0; i--) {
            var msg = ChatMessages[i];

            // wraps tags
            var lines = WrapColoredText(msg.Sections, font, maxMsgWidth, resScale.X);

            // total msg height
            float msgHeight = lines.Count * font.LineHeight * resScale.Y;

            // move cursor y
            currentY -= msgHeight;

            var drawPos = new Vector2(chatRect.X + padding.X, currentY);

            // draws each line
            foreach (var line in lines) {
                foreach (var section in line) {
                    sb.DrawString(font, section.Text, drawPos, section.Color * smoothOpen, resScale);
                    drawPos.X += font.MeasureString(section.Text).X * resScale.X;
                }
                // reset x + move y for next line
                drawPos.X = chatRect.X + padding.X;
                drawPos.Y += font.LineHeight * resScale.Y;
            }

            // adds spacing between messages
            currentY += 10f * resScale.Y;
        }

        sb.End();
    }

    // i want to slowly start moving from RuntimeData.DeltaTime to just direct elapsed time calculations... soon.
    public static void Update(GameTime gameTime) {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (ChatBoxHover || ActiveHandle)
            TankGame.MouseUIHover = true;

        if (IsOpen) {
            UnreadMessageCount = 0;
            _openProgress += dt * 3f; // speed of opening
            if (_openProgress > 1f) _openProgress = 1f;
        }
        else {
            _openProgress -= dt * 3f; // speed of closing
            if (_openProgress < 0f) _openProgress = 0f;
        }
    }

    // non-api
    static void DrawChatBox(float animProgress, out Rectangle chatBox, out Rectangle typeBox, float inputContentHeight) {
        var fontY = FontGlobals.RebirthFont.MeasureString("X").Y;
        var scale = new Vector2(DEFAULT_SCALE).ToResolution();
        var margin = _openOrigin.ToResolution();
        var boxWidthRes = BOX_WIDTH_BASE.ToResolutionX();
        var paddingRes = PADDING_BASE.ToResolutionY();

        var chatHeight = (int)(fontY * scale.Y * MAX_MESSAGES_AT_ONCE);

        // Dynamic height for input box
        var typeBoxHeight = (int)Math.Max(32.ToResolutionY(), inputContentHeight + (paddingRes * 2));

        var totalHeight = chatHeight + paddingRes + typeBoxHeight;
        var viewport = TankGame.Instance.GraphicsDevice.Viewport;

        float x = 0, y = 0;

        // handle slide-in anim
        // invert progress cuz 1 = open, 0 = closed
        float slideOffset = (1f - animProgress) * 50f;

        switch (Corner) {
            case ChatMessageCorner.TopLeft:
                x = margin.X - slideOffset;
                y = margin.Y;
                break;

            case ChatMessageCorner.TopRight:
                x = (viewport.Width - boxWidthRes - margin.X) + slideOffset;
                y = margin.Y;
                break;

            case ChatMessageCorner.BottomLeft:
                x = margin.X - slideOffset;
                y = viewport.Height - totalHeight - margin.Y;
                break;

            case ChatMessageCorner.BottomRight:
                x = (viewport.Width - boxWidthRes - margin.X) + slideOffset;
                y = viewport.Height - totalHeight - margin.Y;
                break;
        }

        // ensures no mouse interaction
        if (animProgress <= 0.01f) y = -50000;

        // chat history
        chatBox = new Rectangle((int)x, (int)y, (int)boxWidthRes, chatHeight);

        // input box
        typeBox = new Rectangle(chatBox.X, chatBox.Y + chatBox.Height + (int)paddingRes, chatBox.Width, typeBoxHeight);
    }

    static void HandleTextInputState(bool isHoveringTypeRect, Rectangle typeRect) {
        // line overflow handling removed (handled by wrapping in draw)

        if (InputUtils.CanDetectClick()) {
            if (isHoveringTypeRect && !ActiveHandle) {
                TankGame.Instance.Window.TextInput += HandleInput;
                ActiveHandle = true;
            }
            else if (!isHoveringTypeRect && ActiveHandle) {
                TankGame.Instance.Window.TextInput -= HandleInput;
                ActiveHandle = false;
            }
        }

        if (ActiveHandle) {
            if (InputUtils.AreKeysJustPressed(Keys.LeftControl, Keys.V))
                CurTyping += TextCopy.ClipboardService.GetText();
            if (InputUtils.AreKeysJustPressed(Keys.LeftControl, Keys.C))
                TextCopy.ClipboardService.SetText(CurTyping);
            if (ToggleChat.JustPressed) {
                TankGame.Instance.Window.TextInput -= HandleInput;
                ActiveHandle = false;
            }
        }
    }

    static void DrawUnreadNotification(SpriteBatch sb) {
        var scale = new Vector2(DEFAULT_SCALE).ToResolution();
        var margin = _openOrigin.ToResolution();
        var font = FontGlobals.RebirthFont;
        var viewport = TankGame.Instance.GraphicsDevice.Viewport;

        // Helper to calculate size
        string textMsg = $"{TankGame.GameLanguage.Press} [{ToggleChat.Assigned}] {TankGame.GameLanguage.ToToggleChat}";
        var textSize = font.MeasureString(textMsg) * scale;
        var alertSize = ChatAlert.Size() * scale;

        var contentWidth = (UnreadMessageCount > 0)
            ? (alertSize.X + 10.ToResolutionX() + textSize.X)
            : textSize.X;

        var position = Vector2.Zero;

        switch (Corner) {
            case ChatMessageCorner.TopLeft:
                position = margin;
                break;
            case ChatMessageCorner.TopRight:
                position = new(viewport.Width - contentWidth - margin.X, margin.Y);
                break;
            case ChatMessageCorner.BottomLeft:
                position = new Vector2(margin.X, viewport.Height - textSize.Y - margin.Y);
                break;
            case ChatMessageCorner.BottomRight:
                position = new Vector2(viewport.Width - contentWidth - margin.X, viewport.Height - textSize.Y - margin.Y);
                break;
        }

        if (UnreadMessageCount > 0) {
            sb.Draw(ChatAlert, position, null, Color.White, 0f, Vector2.Zero, scale, default, default);

            var countPos = position + (ChatAlert.Size() * scale) - new Vector2(12, 12).ToResolution();
            sb.DrawString(font, UnreadMessageCount.ToString(), countPos, Color.White, scale);

            var textPos = position + new Vector2(ChatAlert.Size().X * scale.X + 10.ToResolutionX(), 0);
            sb.DrawString(font, textMsg, textPos, Color.White, scale);
        }
        else {
            sb.DrawString(font, textMsg, position, Color.White, scale);
        }
    }

    static void HandleInput(object? sender, TextInputEventArgs e) {
        if (!TankGame.Instance.IsActive) return;

        // ignores the toggle key to prevent it from being typed
        if (e.Key == ToggleChat.Assigned) return;

        if (e.Key == Keys.Back) {
            if (CurTyping.Length > 0) CurTyping = CurTyping[..^1];
        }
        else if (e.Key == Keys.Escape) {
            CurTyping = string.Empty;
            TankGame.Instance.Window.TextInput -= HandleInput;
            ActiveHandle = false;
        }
        else if (e.Key == Keys.Tab) {
            CurTyping += "   ";
        }
        else if (e.Key == Keys.Enter) {
            if (string.IsNullOrEmpty(CurTyping)) {
                TankGame.Instance.Window.TextInput -= HandleInput;
                ActiveHandle = false;
                return;
            }

            string? senderName = Client.IsConnected() ? NetPlay.CurrentClient.Name : null;
            SendMessage(CurTyping, Color.White, senderName);
            CurTyping = string.Empty;
        }
        else {
            if (CurTyping.Length < MAX_LENGTH)
                CurTyping += e.Character;
        }
    }
}

/// <summary>Represents a system used to store messages and their contents in use with the <see cref="ChatSystem"/>.</summary>
public struct ChatMessage(List<ChatSystem.TextSection> snippets, string rawContent) {
    /// <summary>The parsed segments of the message containing text and color data.</summary>
    public List<ChatSystem.TextSection> Sections = snippets;

    /// <summary>The raw text content.</summary>
    public string RawContent = rawContent;
}