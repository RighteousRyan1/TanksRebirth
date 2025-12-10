using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Systems.CommandsSystem;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Systems;

public class GameConsole {
    // general stuff
    const int MAX_LINE_COUNT = 100;
    const int HEADER_HEIGHT = 30;
    const int RESIZE_HANDLE_HEIGHT = 20;
    const float BG_ALPHA = 0.9f;
    const int PADDING = 5;

    string _lastCmd = string.Empty;
    public Color UserInputColor = Color.LightGreen;
    public Color ErrorColor = Color.Red;
    public float LogScale = 1f; // scale of the text in the log

    // suggestion stuff
    const int SUGGESTION_MAX_ITEMS = 8;
    const int SUGGESTION_ITEM_HEIGHT = 35;

    // window state
    public bool IsOpen { get; private set; } = false;
    Rectangle _windowRect = new(50, 50, 600, 300);

    // input/drag state
    string _currentInput = "";
    bool _isDragging = false;
    bool _isResizing = false;
    Point _dragOffset;
    readonly Keys _toggleKey;

    // suggestion state
    List<string> _currentSuggestions = [];
    int _selectedSuggestionIndex = 0;

    // console data
    readonly Queue<(string Text, Color Color)> _logLines = [];

    readonly Game _game;

    public GameConsole(Game game, Keys toggleKey = Keys.OemTilde) {
        _game = game;
        _toggleKey = toggleKey;
        _game.Window.TextInput += OnTextInput;
    }

    public void Log(string message, Color color) {
        _logLines.Enqueue(($"[{DateTime.Now:HH:mm:ss}] {message}", color));

        // i mean this works fine
        while (_logLines.Count > MAX_LINE_COUNT) {
            _logLines.Dequeue();
        }
    }

    public void Update(GameTime gameTime) {
        var mousePos = MouseUtils.MousePosition.ToPoint();

        if (InputUtils.KeyJustPressed(_toggleKey)) {
            IsOpen = !IsOpen;
        }

        if (!IsOpen) return;

        TankGame.MouseUIHover = true;
        // navigation of suggestions
        if (_currentSuggestions.Count > 0) {
            if (InputUtils.KeyJustPressed(Keys.Up)) {
                _selectedSuggestionIndex = Math.Max(0, _selectedSuggestionIndex - 1);
            }
            if (InputUtils.KeyJustPressed(Keys.Down)) {
                _selectedSuggestionIndex = Math.Min(_currentSuggestions.Count - 1, _selectedSuggestionIndex + 1);
            }
            // press tab for autocomplete
            if (InputUtils.KeyJustPressed(Keys.Tab)) {
                var selected = _currentSuggestions[_selectedSuggestionIndex];

                // preserves "help" in case the user is asking for help with the command
                if (_currentInput.StartsWith("help ", StringComparison.OrdinalIgnoreCase)) {
                    _currentInput = "help " + selected;
                }
                else {
                    _currentInput = selected;
                }

                // autocomplete clears suggestions
                _currentSuggestions.Clear();
                _selectedSuggestionIndex = 0;
            }
        }
        else {
            if (InputUtils.KeyJustPressed(Keys.Up))
                _currentInput = _lastCmd;
        }

        // header drawing
        Rectangle headerRect = new(_windowRect.X, _windowRect.Y, _windowRect.Width, HEADER_HEIGHT);

        if (InputUtils.MouseLeft) {
            if (!_isDragging && !_isResizing) {
                Rectangle resizeRect = new(_windowRect.Right - RESIZE_HANDLE_HEIGHT, _windowRect.Bottom - RESIZE_HANDLE_HEIGHT, RESIZE_HANDLE_HEIGHT, RESIZE_HANDLE_HEIGHT);

                if (resizeRect.Contains(mousePos)) {
                    _isResizing = true;
                    _dragOffset = mousePos - new Point(_windowRect.Width, _windowRect.Height);
                }
                else if (headerRect.Contains(mousePos)) {
                    _isDragging = true;
                    _dragOffset = mousePos - _windowRect.Location;
                }
            }

            if (_isResizing) {
                int newWidth = Math.Max(200, mousePos.X - _dragOffset.X);
                int newHeight = Math.Max(100, mousePos.Y - _dragOffset.Y);
                _windowRect.Width = newWidth;
                _windowRect.Height = newHeight;
            }
            else if (_isDragging) {
                _windowRect.Location = mousePos - _dragOffset;
            }
        }
        else {
            _isDragging = false;
            _isResizing = false;
        }
    }

    private void OnTextInput(object? sender, TextInputEventArgs e) {
        if (!IsOpen) return;

        // ignores the toggle key to prevent it from being typed
        if (e.Key == _toggleKey) return;

        // tab is the autocomplete key so ignore
        if (e.Key == Keys.Tab) return;

        if (e.Key == Keys.Back) {
            if (_currentInput.Length > 0)
                _currentInput = _currentInput[..^1];

            UpdateCommandSuggestions();
            return;
        }

        if (e.Key == Keys.Enter) {
            ProcessCommand(_currentInput);
            _lastCmd = _currentInput;
            _currentInput = string.Empty;
            _currentSuggestions.Clear();
            return;
        }

        if (!char.IsControl(e.Character)) {
            _currentInput += e.Character;
            UpdateCommandSuggestions();
        }
    }

    public void ProcessCommand(string commandInput, bool log = true) {
        if (log)
            Log($"> {commandInput}", UserInputColor);

        if (string.IsNullOrWhiteSpace(commandInput)) return;

        // hard-coded command
        if (commandInput.Equals("clear", StringComparison.OrdinalIgnoreCase)) {
            _logLines.Clear();
            return;
        }


        // do help command specific shit
        if (commandInput.StartsWith("help ", StringComparison.OrdinalIgnoreCase)) {
            var targetCmdName = commandInput[5..].Trim(); // removes "help "

            // find matching command
            var cmdMatch = CommandGlobals.Commands.FirstOrDefault(c => c.Key.Name.Equals(targetCmdName, StringComparison.OrdinalIgnoreCase));

            // check if the command is even real
            if (cmdMatch.Key.Name != null) {
                var desc = cmdMatch.Key.Description ?? "No description available.";
                Log($"Help for '{targetCmdName}': {desc}", UserInputColor);
            }
            else {
                Log($"Command '{targetCmdName}' not found.", ErrorColor);
            }
            return;
        }

        if (commandInput.Equals("help", StringComparison.OrdinalIgnoreCase)) {
            Log("Type 'help <command>' to see a description.", UserInputColor);
            Log("Type 'clear' to wipe console text.", UserInputColor);
            return;
        }

        // splits command by whitespaces for arguments
        var cmdSplit = commandInput.Split(' ');
        var cmdName = cmdSplit[0];

        // attempts to find a command to execute
        var cmdPair = CommandGlobals.Commands.FirstOrDefault(c => c.Key.Name.Equals(cmdName, StringComparison.OrdinalIgnoreCase));

        // checks if the key is valid
        if (cmdPair.Key.Name != null) {
            try {
                var value = cmdPair.Value;
                var args = cmdSplit.Length <= 1 ? [] : cmdSplit[1..];

                if (value.NetSync && Client.IsConnected()) {
                    if (!Client.IsHost()) {
                        Log("Error: You cannot use this command as you are not the host.", ErrorColor);
                        return;
                    }
                    if (Client.IsHost())
                        Client.SendCommandUsage(commandInput);
                }

                if (value.RequireCheats) {
                    if (CommandGlobals.AreCheatsEnabled)
                        value.ActionToPerform?.Invoke(args);
                    else
                        Log("Error: Cheats must be enabled to use this command.", ErrorColor);
                }
                else {
                    value.ActionToPerform?.Invoke(args);
                }
            } catch (Exception e) {
                TankGame.ReportError(e);
                Log($"Error executing command: {e.Message}", ErrorColor);
            }
        }
        else {
            Log($"Unknown command: '{cmdName}'.", ErrorColor);
        }
    }

    private void UpdateCommandSuggestions() {
        _currentSuggestions.Clear();

        if (string.IsNullOrWhiteSpace(_currentInput)) return;

        // determines the word to match with
        string wordToMatch;
        var split = _currentInput.Split(' ');

        // if typing "help <something>", suggest commands for the second word
        if (_currentInput.StartsWith("help ", StringComparison.OrdinalIgnoreCase))
            wordToMatch = split.Length > 1 ? split[1] : "";
        // regular suggestion
        else if (split.Length == 1) wordToMatch = split[0];
        // the command has been fully typed already
        else return;

        var allCommands = CommandGlobals.Commands.Keys.Select(k => k.Name).ToList();

        // check starting conditions
        var startsWith = allCommands
            .Where(c => c.StartsWith(wordToMatch, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Length) // shortest matches appear first
            .ToList();

        // "fuzzy" match
        var fuzzyMatches = allCommands
            .Where(c => !startsWith.Contains(c) && StringUtils.ComputeLevenshteinDistance(wordToMatch, c) <= 2)
            .OrderBy(c => StringUtils.ComputeLevenshteinDistance(wordToMatch, c)) // closest matches first
            .ToList();

        _currentSuggestions.AddRange(startsWith);
        _currentSuggestions.AddRange(fuzzyMatches);

        // caps the size
        if (_currentSuggestions.Count > SUGGESTION_MAX_ITEMS)
            _currentSuggestions = _currentSuggestions.GetRange(0, SUGGESTION_MAX_ITEMS);

        if (_selectedSuggestionIndex >= _currentSuggestions.Count)
            _selectedSuggestionIndex = 0;
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFontBase font) {
        if (!IsOpen) return;

        // logging draw
        spriteBatch.Begin();

        // bg
        spriteBatch.Draw(TextureGlobals.Pixels[Color.Black], _windowRect, Color.White * BG_ALPHA);
        Rectangle headerRect = new(_windowRect.X, _windowRect.Y, _windowRect.Width, HEADER_HEIGHT);
        spriteBatch.Draw(TextureGlobals.Pixels[Color.DarkGray], headerRect, Color.White);

        // header
        var devConsoleText = "Developer Console";
        var devMeasure = font.MeasureString(devConsoleText);
        spriteBatch.DrawString(font, devConsoleText, new Vector2(_windowRect.X + PADDING, _windowRect.Y + PADDING), Color.White, origin: new(0, devMeasure.Y / 2));

        // input section, handling overflow
        float maxInputWidth = _windowRect.Width - (PADDING * 2);
        string wrappedInput = StringUtils.WrapText(font, "$ " + _currentInput, maxInputWidth);
        Vector2 inputSize = font.MeasureString(wrappedInput);

        // calculate dynamic input height
        int inputHeight = (int)Math.Max(font.MeasureString("A").Y + (PADDING * 2), inputSize.Y + (PADDING * 2));
        Rectangle inputRect = new(_windowRect.X, _windowRect.Bottom - inputHeight, _windowRect.Width, inputHeight);

        // pixel-tall separation
        spriteBatch.Draw(TextureGlobals.Pixels[Color.Gray], new Rectangle(_windowRect.X, inputRect.Y, _windowRect.Width, 1), Color.White);

        // input display
        spriteBatch.DrawString(font, wrappedInput, new Vector2(inputRect.X + PADDING, inputRect.Y + PADDING), Color.Yellow);

        // caret animation
        if (RuntimeData.RunTime % 60 < 30) {
            // Find position of last line to place caret
            var lines = wrappedInput.Split('\n');
            var lastLine = lines.Last();
            var lastLineIdx = lines.Length - 1;

            // Y position is based on line count, X position is width of the last line
            float caretY = lastLineIdx * font.LineHeight;
            float caretX = font.MeasureString(lastLine).X;

            // Adjust for padding
            Vector2 caretPos = new Vector2(inputRect.X + PADDING + caretX, inputRect.Y + PADDING + caretY);
            spriteBatch.DrawString(font, "|", caretPos, Color.Yellow);
        }

        // handle resizing
        Rectangle handleRect = new(_windowRect.Right - 10, _windowRect.Bottom - 10, 10, 10);
        spriteBatch.Draw(TextureGlobals.Pixels[Color.Gray], handleRect, Color.White);

        if (_currentSuggestions.Count > 0) {
            DrawSuggestions(spriteBatch, font, inputRect);
        }

        spriteBatch.End();

        // logs text drawing
        int logAreaY = _windowRect.Y + HEADER_HEIGHT;
        int logAreaHeight = inputRect.Y - logAreaY;

        if (logAreaHeight > 0 && _windowRect.Width > 0) {
            Rectangle scissorRect = new(_windowRect.X, logAreaY, _windowRect.Width, logAreaHeight);
            Rectangle viewportRect = _game.GraphicsDevice.Viewport.Bounds;
            scissorRect = Rectangle.Intersect(scissorRect, viewportRect);

            if (scissorRect.Width > 0 && scissorRect.Height > 0) {
                RasterizerState rasterizer = new() { ScissorTestEnable = true };
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, rasterizer);

                Rectangle previousScissor = _game.GraphicsDevice.ScissorRectangle;
                _game.GraphicsDevice.ScissorRectangle = scissorRect;

                int currentY = inputRect.Y - PADDING;
                float maxLogWidth = _windowRect.Width - (PADDING * 2);

                foreach (var line in _logLines.Reverse()) {
                    // Wrap log text
                    string wrappedLog = StringUtils.WrapText(font, line.Text, maxLogWidth, LogScale);
                    string[] wrappedLogLines = wrappedLog.Split('\n');

                    // Draw each line of the wrapped log message (bottom-up)
                    for (int i = wrappedLogLines.Length - 1; i >= 0; i--) {
                        var logLine = wrappedLogLines[i];
                        Vector2 size = font.MeasureString(logLine) * LogScale;
                        currentY -= (int)size.Y;

                        if (currentY + size.Y < scissorRect.Y) break;
                        if (currentY < scissorRect.Bottom) {
                            spriteBatch.DrawString(font, logLine, new Vector2(_windowRect.X + PADDING, currentY), line.Color, new Vector2(LogScale));
                        }
                    }
                    if (currentY < scissorRect.Y) break;
                }

                spriteBatch.End();
                _game.GraphicsDevice.ScissorRectangle = previousScissor;
            }
        }
    }

    private void DrawSuggestions(SpriteBatch sb, SpriteFontBase font, Rectangle inputRect) {
        int totalHeight = _currentSuggestions.Count * SUGGESTION_ITEM_HEIGHT;

        // draws the box beneath text input
        Rectangle suggestionRect = new(inputRect.X, inputRect.Bottom, inputRect.Width, totalHeight);

        // bg
        sb.Draw(TextureGlobals.Pixels[Color.Black], suggestionRect, Color.Black * 0.9f);

        // border
        sb.Draw(TextureGlobals.Pixels[Color.Gray], new Rectangle(suggestionRect.X, suggestionRect.Y, suggestionRect.Width, 1), Color.White); // Top border (Separator)
        sb.Draw(TextureGlobals.Pixels[Color.Gray], new Rectangle(suggestionRect.X, suggestionRect.Y, 1, suggestionRect.Height), Color.White); // Left border
        sb.Draw(TextureGlobals.Pixels[Color.Gray], new Rectangle(suggestionRect.Right - 1, suggestionRect.Y, 1, suggestionRect.Height), Color.White); // Right border
        sb.Draw(TextureGlobals.Pixels[Color.Gray], new Rectangle(suggestionRect.X, suggestionRect.Bottom - 1, suggestionRect.Width, 1), Color.White); // Bottom border

        for (int i = 0; i < _currentSuggestions.Count; i++) {
            var itemText = _currentSuggestions[i];

            // highlights selected suggestion
            bool isSelected = i == _selectedSuggestionIndex;
            var textColor = isSelected ? Color.Yellow : Color.Gray;

            if (isSelected) {
                Rectangle itemRect = new(suggestionRect.X, suggestionRect.Y + (i * SUGGESTION_ITEM_HEIGHT), suggestionRect.Width, SUGGESTION_ITEM_HEIGHT);
                sb.Draw(TextureGlobals.Pixels[Color.White], itemRect, Color.White * 0.1f);
            }

            var pos = new Vector2(suggestionRect.X + PADDING, suggestionRect.Y + (i * SUGGESTION_ITEM_HEIGHT) + 2); // +2 for vertical centering adjustment
            sb.DrawString(font, itemText, pos, textColor);
        }
    }
}