using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace XConsoleApp
{
    /// <summary>
    /// A singleton game component that provides a debug console with the ability to both 
    /// write text and register new commands.
    /// </summary>
    public class XConsole : IGameComponent, IUpdateable, IDrawable
    {
        /// <summary>
        /// Gets the singleton instance of XConsole; only valid after the constructor is called.
        /// </summary>
        public static XConsole Instance { get; private set; }

        // services the console requires
        private IKeyboardInputService keyboard;
        private IGraphicsDeviceService graphics;

        // graphics objects necessary for drawing
        private ContentManager content;
        private SpriteBatch spriteBatch;
        private Texture2D blank;
        private string fontName;
        private SpriteFont font;

        // information about the height of the console
        private float currentHeight = 0;
        private int heightInLines = 15;
        private int totalHeight;

        // extra padding at the bottom of the console
        private const int bottomPadding = 2;

        // data about animation
        private float animationTime = .25f;
        private float heightSpeed;

        // the prompt information
        private const string prompt = "> ";
        private int promptWidth;

        // cursor information
        private const string cursor = "_";
        private int cursorSize;
        private int cursorPosition = 0;
        private float cursorBlinkTimer = 0f;
        private const float cursorBlinkRate = .5f;
        private bool cursorVisible = true;

        // key repeat information
        private Keys pressedKey;
        private float keyRepeatTimer = 0f;
        private const float keyRepeatStartDuration = .25f;
        private const float keyRepeatDuration = .05f;

        // the list of lines for the console output
        private List<OutputLine> output = new List<OutputLine>();

        // an offset (in lines) to shift for drawing update when using ctrl+up/down to scroll the output
        private int outputShift = 0;

        // the current input text
        private StringBuilder inputText = new StringBuilder();

        // the history of console input
        private List<string> inputHistory = new List<string>();

        // current position in the inputHistory list when using the up/down arrows
        private int inputHistoryPosition = 0;
        
        // the location in the inputText where the tab completion should be inserted
        private int tabCompletePosition;

        // a list of suggestions for tab completion
        private IList<string> tabCompletionSuggestions;

        // the index into our suggestions list for the current suggestion
        private int tabCompleteIndex;

        // a suggestion provider for command names, used by our initial tab behavior for commands as well as for
        // suggesting command names for the 'help' command
        private readonly CommandSuggestionProvider commandSuggestionProvider = new CommandSuggestionProvider();

        // the list of selected objects for use with the 'info', 'get', 'set', and 'invoke' commands
        private List<object> selectedObjects = new List<object>();

        // all of the currently registered commands
        private Dictionary<string, CommandInfo> commands = new Dictionary<string, CommandInfo>();

        /// <summary>
        /// Gets the current state of the console.
        /// </summary>
        public State CurrentState { get; private set; }

        /// <summary>
        /// Gets or sets the key to use to toggle the console.
        /// </summary>
        public Keys ToggleKey { get; set; }

        /// <summary>
        /// Gets or sets the height of the console in lines.
        /// </summary>
        public int Height
        {
            get { return heightInLines; }
            set
            {
                heightInLines = value;
                if (font != null)
                    totalHeight = value * font.LineSpacing + bottomPadding;
            }
        }

        /// <summary>
        /// Gets or sets the length of time it should take the console to go from fully closed to fully open.
        /// </summary>
        public float AnimationLength
        {
            get { return animationTime; }
            set
            {
                animationTime = value;
                if (totalHeight > 0)
                    heightSpeed = totalHeight / animationTime;
            }
        }

        /// <summary>
        /// Gets or sets the opacity of the background of the console.
        /// </summary>
        public float BackgroundOpacity { get; set; }

        /// <summary>
        /// Gets or sets the delegate that converts a key into a character. 
        /// </summary>
        /// <remarks>
        /// The default is generally "good enough" for standard US keyboards, but if you have
        /// a non-standard US keyboard or a non-US keyboard you'll likely want to replace this
        /// with a function that more properly maps the XNA Framework Keys values to characters
        /// for your particular language.
        /// </remarks>
        public KeyToCharacterDelegate KeyToCharacter { get; set; }

        /// <summary>
        /// Gets or sets an object to handle parsing strings for the "set" and "invoke" commands.
        /// </summary>
        public IStringParser StringParser { get; set; }

        /// <summary>
        /// Gets the list of selected objects.
        /// </summary>
        public List<object> SelectedObjects { get { return selectedObjects; } }

        /// <summary>
        /// Initializes a new XConsole.
        /// </summary>
        /// <param name="services">A service provider where the console can find the IKeyboardInputService and IGraphicsDeviceService.</param>
        /// <param name="fontName">The name of a font to use for the console. Must be relative path from executable.</param>
        public XConsole(IServiceProvider services, string fontName)
        {
            // only allow one instance at a time
            if (Instance != null)
                throw new InvalidOperationException("Only one XConsole can exist. Use XConsole.Instance to access it.");
            Instance = this;

            // attempt to get the input service
            try { keyboard = services.GetService(typeof(IKeyboardInputService)) as IKeyboardInputService; }
            catch { keyboard = null; }
            finally { if (keyboard == null) throw new InvalidOperationException("Could not find IKeyboardInputService in services."); }

            // attempt to get the graphics service
            try { graphics = services.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService; }
            catch { graphics = null; }
            finally { if (graphics == null) throw new InvalidOperationException("Could not find IGraphicsDeviceService in services."); }

            // create our content manager
            content = new ContentManager(services);

            // initialize some state
            CurrentState = State.Closed;
            ToggleKey = Keys.F12;
            this.fontName = fontName;
            KeyToCharacter = KeyString.KeyToString;
            StringParser = new DefaultStringParser();
            BackgroundOpacity = .6f;

            // register all the default console commands
            RegisterDefaultCommands();
        }

        /// <summary>
        /// Opens the console.
        /// </summary>
        public void Open()
        {
            Open(true);
        }

        /// <summary>
        /// Opens the console.
        /// </summary>
        /// <param name="animated">Whether or not to animate the console opening.</param>
        public void Open(bool animated)
        {
            if (animated)
            {
                if (CurrentState != State.Open)
                    CurrentState = State.Opening;
            }
            else
            {
                currentHeight = totalHeight;
                CurrentState = State.Open;
            }
        }

        /// <summary>
        /// Closes the console.
        /// </summary>
        public void Close()
        {
            Close(true);
        }

        /// <summary>
        /// Closes the console.
        /// </summary>
        /// <param name="animated">Whether or not to animate the console closing.</param>
        public void Close(bool animated)
        {
            if (animated)
            {
                if (CurrentState != State.Closed)
                    CurrentState = State.Closing;
            }
            else
            {
                currentHeight = 0;
                CurrentState = State.Closed;
            }
        }

        /// <summary>
        /// Toggles the console, opening it if it's closed/closing or closing it if it's open/opening.
        /// </summary>
        public void Toggle()
        {
            Toggle(true);
        }

        /// <summary>
        /// Toggles the console, opening it if it's closed/closing or closing it if it's open/opening.
        /// </summary>
        /// <param name="animated">Whether or not to animate the console.</param>
        public void Toggle(bool animated)
        {
            if (CurrentState == State.Closed || CurrentState == State.Closing)
                Open(animated);
            else if (CurrentState == State.Open || CurrentState == State.Opening)
                Close(animated);
        }

        /// <summary>
        /// Allows a way to invoke commands programmatically.
        /// </summary>
        /// <remarks>
        /// Make sure you quote arguments with spaces, just like you have to manually.
        /// </remarks>
        /// <param name="command">The full command to invoke including arguments.</param>
        public void InvokeCommand(string input)
        {
            // make sure we didn't have a blank line
            if (string.IsNullOrEmpty(input.Trim()))
                return;

            // parse the input to find the command and arguments
            string command;
            List<string> arguments;
            ParseInput(input, out command, out arguments);

            // write out the prompt and text
            WriteLine(string.Format("{0}{1}", prompt, input));

            // record the input in the history
            inputHistory.Add(input);
            inputHistoryPosition = inputHistory.Count;

            // get the action for the command
            CommandInfo info;
            if (commands.TryGetValue(command, out info))
            {
                // invoke the action with the arguments
                try { info.Action(command, arguments); }
                catch (Exception e) { WriteError(string.Format("'{0}' threw an exception: {1}", command, e.Message)); }
            }
            else
            {
                // if there is no registered command, write an error
                WriteError(string.Format("'{0}' is not a recognized command.", command));
            }

            // jump to the end of the history so we can see this output
            outputShift = 0;
        }

        /// <summary>
        /// Registers a new console command with help text and a tab completion provider.
        /// </summary>
        /// <param name="command">The name of the command.</param>
        /// <param name="helpText">Text to display for this command when used as an argument to the 'help' command.</param>
        /// <param name="action">The delegate to invoke for the command.</param>
        public void RegisterCommand(string command, string helpText, CommandActionDelegate action)
        {
            RegisterCommand(command, helpText, action, null);
        }

        /// <summary>
        /// Registers a new console command with help text and a tab completion provider.
        /// </summary>
        /// <param name="command">The name of the command.</param>
        /// <param name="helpText">Text to display for this command when used as an argument to the 'help' command.</param>
        /// <param name="action">The delegate to invoke for the command.</param>
        /// <param name="tabCompletionProvider">The tab completion provider to use for the command arguments. Pass null to disable tab completion of arguments.</param>
        public void RegisterCommand(string command, string helpText, CommandActionDelegate action, ISuggestionProvider tabCompletionProvider)
        {
            if (string.IsNullOrEmpty(command)) throw new ArgumentNullException("command");
            if (action == null) throw new ArgumentNullException("action");
            if (string.IsNullOrEmpty(helpText)) throw new ArgumentNullException("helpText");

            commands.Add(command, new CommandInfo(command, action, helpText, tabCompletionProvider));
        }

        /// <summary>
        /// Unregisters a console command.
        /// </summary>
        /// <param name="command">The name of the command.</param>
        public void UnregisterCommand(string command)
        {
            commands.Remove(command);
        }

        /// <summary>
        /// Writes a line of text to the console.
        /// </summary>
        /// <param name="text">The text to write.</param>
        /// <param name="color">The color in which to display the text.</param>
        public void WriteLine(string text, Color color)
        {
            if (text.Contains('\n'))
            {
                // If the text already has line breaks, split by line and invoke WriteLine for each line individually
                string[] lines = text.Split('\n');
                foreach (var l in lines)
                    WriteLine(l, color);
            }
            else
            {
                // First we want to do some word wrapping on the text
                List<string> lines = new List<string>();

                // Split the text into words
                string[] words = text.Split(' ');

                // Now go through the words trying to build up lines until we run out of horizontal space
                StringBuilder line = new StringBuilder();
                int lineSize = 0;
                int maxSize = graphics.GraphicsDevice.Viewport.Width;
                for (int i = 0; i < words.Length; i++)
                {
                    // Get the word
                    string word = words[i];

                    // Empty words are really spaces that got trimmed by the Split method
                    if (word == string.Empty)
                        word = " ";
                    // If this isn't the first word on a line, add a space before it
                    else if (line.Length > 0)
                        word = " " + word;

                    // Measure the size of the word
                    int wordSize = (int)font.MeasureString(word).X;

                    // If the line is too long with this word, write out the line
                    if (lineSize + wordSize >= maxSize && line.Length > 0)
                    {
                        lines.Add(line.ToString());
                        line.Clear();
                        lineSize = 0;

                        // Remove the space from the word and remeasure it
                        word = words[i];
                        wordSize = (int)font.MeasureString(word).X;
                    }

                    // Add the word to the line
                    line.Append(word);
                    lineSize += wordSize;
                }

                // Make sure to add the last line if anything is left in our builder
                if (line.Length > 0)
                    lines.Add(line.ToString());

                // Now we can write out each line into our output
                foreach (var l in lines)
                    output.Add(new OutputLine { Text = l, Color = color });
            }
        }

        /// <summary>
        /// Writes a line of text to the console.
        /// </summary>
        /// <param name="text">The text to write.</param>
        public void WriteLine(string text)
        {
            WriteLine(text, Color.White);
        }

        /// <summary>
        /// Writes a warning to the console.
        /// </summary>
        /// <param name="text">The warning text.</param>
        public void WriteWarning(string text)
        {
            WriteLine(string.Format("WARNING: {0}", text), Color.Orange);
        }

        /// <summary>
        /// Writes an error to the console.
        /// </summary>
        /// <param name="text">The error text.</param>
        public void WriteError(string text)
        {
            WriteLine(string.Format("ERROR: {0}", text), Color.Red);
        }

        /// <summary>
        /// Prints the help text of a command.
        /// </summary>
        /// <param name="command">The command name for which help should be displayed.</param>
        public void WriteCommandHelp(string command)
        {
            // Try to find our command info
            CommandInfo info;
            if (!commands.TryGetValue(command, out info))
            {
                // Write an error if the command isn't registered
                WriteError(string.Format("'{0}' is not a recognized command.", command));
            }
            else
            {
                // Write out the help text in gold
                WriteLine(info.HelpText, Color.Gold);
            }

            // jump to the end of the history so we can see this output
            outputShift = 0;
        }

        // We explicitly implement the three methods because while they are important, user code shouldn't
        // ever need to call them. By making them explicit, they won't show up in Intellisense when using
        // XConsole unless you explicitly cast it to one of the interfaces.

        void IGameComponent.Initialize()
        {
            spriteBatch = new SpriteBatch(graphics.GraphicsDevice);
            (blank = new Texture2D(graphics.GraphicsDevice, 1, 1)).SetData(new[] { Color.White });
            font = content.Load<SpriteFont>(fontName);

            // set these values to themselves as properties as they generate other values using the font size
            Height = heightInLines;
            AnimationLength = animationTime;

            // measure some strings
            promptWidth = (int)font.MeasureString(prompt).X;
            cursorSize = (int)font.MeasureString(cursor).X;
        }

        void IUpdateable.Update(GameTime gameTime)
        {
            // check for the toggle key to toggle our state
            if (keyboard.IsJustPressed(ToggleKey))
                Toggle();

            // if we're closed, don't do anything
            if (CurrentState == State.Closed)
                return;

            // animate our opening or closing
            if (CurrentState == State.Opening)
            {
                currentHeight += (float)gameTime.ElapsedGameTime.TotalSeconds * heightSpeed;
                if (currentHeight >= Height * font.LineSpacing)
                {
                    currentHeight = totalHeight;
                    CurrentState = State.Open;
                }
            }
            else if (CurrentState == State.Closing)
            {
                currentHeight -= (float)gameTime.ElapsedGameTime.TotalSeconds * heightSpeed;
                if (currentHeight <= 0)
                {
                    currentHeight = 0;
                    CurrentState = State.Closed;
                }
            }

            else
            {
                // apply the cursor blinking animation
                cursorBlinkTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (cursorBlinkTimer <= 0f)
                {
                    cursorVisible = !cursorVisible;
                    cursorBlinkTimer = cursorBlinkRate;
                }

                // handle user input
                HandleInput(gameTime);
            }
        }

        void IDrawable.Draw(GameTime gameTime)
        {
            if (CurrentState == State.Closed)
                return;

            // use a matrix for the translation animation so that the rest of our drawing code doesn't 
            // have to concern itself with the math to put things in the right place
            spriteBatch.Begin(
                SpriteSortMode.Deferred, null, null, null, null, null,
                Matrix.CreateTranslation(0f, -totalHeight + currentHeight, 0f));

            // draw the background
            spriteBatch.Draw(blank, new Rectangle(0, 0, graphics.GraphicsDevice.Viewport.Width, totalHeight), Color.Black * BackgroundOpacity);

            int promptY = totalHeight - font.LineSpacing - bottomPadding;

            // draw the prompt at the bottom
            spriteBatch.DrawString(font, prompt, new Vector2(0, promptY), Color.Lime);

            // draw the input string next to the prompt
            spriteBatch.DrawString(font, inputText, new Vector2(promptWidth, promptY), Color.Lime);

            // draw the cursor
            if (cursorVisible)
            {
                spriteBatch.DrawString(font, cursor, new Vector2(promptWidth + cursorSize * cursorPosition, promptY), Color.Lime);
            }

            // draw the log
            if (output.Count > 0)
            {
                int start = Math.Max(output.Count - heightInLines + 1 - outputShift, 0);
                int end = Math.Min(output.Count, start + heightInLines - 1);

                for (int i = start; i < end; i++)
                {
                    OutputLine l = output[i];
                    spriteBatch.DrawString(font, l.Text, new Vector2(0, (i - start) * font.LineSpacing), l.Color);
                }
            }

            spriteBatch.End();
        }

        // These members are all explicitly defined because they're not really useful to anyone but the Game
        // class since they all are either Get-only properties or events for the properties which will never
        // fire since the property values never change.

        bool IUpdateable.Enabled { get { return true; } }
        int IUpdateable.UpdateOrder { get { return int.MaxValue; } }
        bool IDrawable.Visible { get { return true; } }
        int IDrawable.DrawOrder { get { return int.MaxValue; } }
        event EventHandler<EventArgs> IUpdateable.EnabledChanged { add { } remove { } }
        event EventHandler<EventArgs> IUpdateable.UpdateOrderChanged { add { } remove { } }
        event EventHandler<EventArgs> IDrawable.DrawOrderChanged { add { } remove { } }
        event EventHandler<EventArgs> IDrawable.VisibleChanged { add { } remove { } }

        private void HandleInput(GameTime gameTime)
        {
            bool control = keyboard.IsDown(Keys.LeftControl) || keyboard.IsDown(Keys.RightControl);
            bool shift = keyboard.IsDown(Keys.LeftShift) || keyboard.IsDown(Keys.RightShift);

            // check for backspace which deletes the character behind the
            // cursor and moves the cursor back
            if (IsKeyPressed(gameTime, Keys.Back))
            {
                if (cursorPosition > 0)
                {
                    inputText.Remove(cursorPosition - 1, 1);
                    cursorPosition--;
                    InvalidateTabCompletion();
                }
            }
            // check for delete which deletes the character in front of the cursor
            else if (IsKeyPressed(gameTime, Keys.Delete))
            {
                if (cursorPosition < inputText.Length)
                {
                    inputText.Remove(cursorPosition, 1); 
                    InvalidateTabCompletion();
                }
            }
            // check for the left/right arrow keys which move the cursor
            else if (IsKeyPressed(gameTime, Keys.Left))
            {
                cursorPosition = Math.Max(cursorPosition - 1, 0);
            }
            else if (IsKeyPressed(gameTime, Keys.Right))
            {
                cursorPosition = Math.Min(cursorPosition + 1, inputText.Length);
            }
            // Ctrl+Up/Ctrl+Down scroll the output up and down
            else if (control && IsKeyPressed(gameTime, Keys.Up))
            {
                outputShift = Math.Min(outputShift + 1, output.Count - heightInLines + 1);
            }
            else if (control && IsKeyPressed(gameTime, Keys.Down))
            {
                outputShift = Math.Max(outputShift - 1, 0);
            }
            // Up/Down without Ctrl goes back through the input history
            else if (IsKeyPressed(gameTime, Keys.Up))
            {
                if (inputHistory.Count > 0)
                {
                    inputHistoryPosition = Math.Max(inputHistoryPosition - 1, 0);
                    inputText.Clear();
                    inputText.Append(inputHistory[inputHistoryPosition]);
                    cursorPosition = inputText.Length;
                    InvalidateTabCompletion();
                }
            }
            else if (IsKeyPressed(gameTime, Keys.Down))
            {
                if (inputHistory.Count > 0)
                {
                    inputHistoryPosition = Math.Min(inputHistoryPosition + 1, inputHistory.Count - 1);
                    inputText.Clear();
                    inputText.Append(inputHistory[inputHistoryPosition]);
                    cursorPosition = inputText.Length;
                    InvalidateTabCompletion();
                }
            }
            // Tab does suggestion completion for commands and arguments
            else if (keyboard.IsJustPressed(Keys.Tab))
            {
                PerformTabCompletion();
            }
            // Esc clears the input
            else if (keyboard.IsJustPressed(Keys.Escape))
            {
                inputText.Clear();
                cursorPosition = 0;
                InvalidateTabCompletion();
            }
            // Enter submits the input
            else if (keyboard.IsJustPressed(Keys.Enter))
            {
                SubmitInput();
            }
            else
            {
                // If our pressed key is no longer down, reset it
                if (!keyboard.IsDown(pressedKey))
                    pressedKey = Keys.None;

                // get all the pressed keys so we can do some typing
                Keys[] keys = keyboard.GetPressedKeys();

                foreach (Keys key in keys)
                {
                    // skip our toggle key
                    if (key == ToggleKey) continue;

                    // convert the key to a character with our delegate.
                    // we also use the IsKeyPressed method to track our repeat delay if we have a key with a valid character.
                    char ch;
                    if (KeyToCharacter(key, shift, out ch) && IsKeyPressed(gameTime, key))
                    {
                        inputText = inputText.Insert(cursorPosition, ch);
                        cursorPosition++;
                        InvalidateTabCompletion();
                    }
                }
            }
        }

        private void PerformTabCompletion()
        {
            // if tab completion is invalid
            if (tabCompletionSuggestions == null)
            {
                // parse the current input to get the command and the arguments
                string command;
                List<string> arguments;
                ParseInput(inputText.ToString(), out command, out arguments);

                if (arguments.Count == 0)
                {
                    // if there are no arguments, we are doing tab completion using the command as our root
                    tabCompletionSuggestions = commandSuggestionProvider.GetSuggestions(command, 0, command, arguments);

                    // set our completion position to the start of the input since we'll replace the entire input string with our suggestions
                    tabCompletePosition = 0;
                }
                else
                {
                    // otherwise we're doing argument completion so we need to figure out if we can do that for the current command
                    CommandInfo info;
                    if (commands.TryGetValue(command, out info) && info.TabCompletionProvider != null)
                    {
                        // get the index of the argument and the argument string
                        int argIndex = arguments.Count - 1;
                        string currentArgument = arguments[argIndex];

                        // remove the current argument so arguments is just the previous arguments
                        arguments.RemoveAt(argIndex);

                        // now use the command's tab completion provider to get the suggestions for the command based on the last argument
                        tabCompletionSuggestions = info.TabCompletionProvider.GetSuggestions(command, argIndex, currentArgument, arguments);

                        // we need to set our completion position to the start of this argument so that suggestions are properly inserted
                        tabCompletePosition = inputText.ToString().LastIndexOf(currentArgument);
                    }
                }
            }

            if (tabCompletionSuggestions != null && tabCompletionSuggestions.Count > 0)
            {
                // wrap the tab completion index
                if (tabCompleteIndex >= tabCompletionSuggestions.Count)
                    tabCompleteIndex = 0;

                // find the suggestion at our index
                string suggestion = tabCompletionSuggestions[tabCompleteIndex];

                // clear the input text from our tab completion position forward
                inputText.Remove(tabCompletePosition, inputText.Length - tabCompletePosition);

                // append the suggestion to the input text
                inputText.Append(suggestion);

                // and now fix the cursor position
                cursorPosition = inputText.Length;

                // increment the index
                tabCompleteIndex++;
            }
        }

        private void InvalidateTabCompletion()
        {
            tabCompletePosition = 0;
            tabCompleteIndex = 0;
            tabCompletionSuggestions = null;
        }
        
        /// <summary>
        /// Parses a string for the command name (first "argument") and the rest of the arguments of the command.
        /// </summary>
        private void ParseInput(string input, out string command, out List<string> arguments)
        {
            arguments = new List<string>();

            // the first argument is the command
            GetNextArgument(input, 0, out command);
            if (command == null)
                return;

            // then we can parse out the rest of the arguments
            int start = command.Length;
            string argument;
            while ((start = GetNextArgument(input, start, out argument)) > 0)
                arguments.Add(argument);
        }

        /// <summary>
        /// Helper decides whether a key is pressed given our delay repeat timing.
        /// </summary>
        private bool IsKeyPressed(GameTime gameTime, Keys key)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // if the key was just pressed and isn't the key we think is being pressed,
            // reset the repeat timer to the starting duration, store this key, and return
            // true
            if (keyboard.IsJustPressed(key) && pressedKey != key)
            {
                keyRepeatTimer = keyRepeatStartDuration;
                pressedKey = key;
                return true;
            }

            // if the key is down and is the key we think is pressed, update the timer and
            // if we're down to 0, add in the repeat duration and return true.
            else if (keyboard.IsDown(key) && key == pressedKey)
            {
                keyRepeatTimer -= dt;
                if (keyRepeatTimer <= 0f)
                {
                    keyRepeatTimer += keyRepeatDuration;
                    return true;
                }
            }

            return false;
        }

        private void SubmitInput()
        {
            // Simply send the input text to our InvokeCommand method
            InvokeCommand(inputText.ToString());

            // clear out our input and invalidate tab completion
            inputText.Clear();
            cursorPosition = 0;
            InvalidateTabCompletion();
        }

        /// <summary>
        /// Helper for parsing input strings to find arguments separated by spaces while respecting quotes.
        /// </summary>
        private int GetNextArgument(string input, int start, out string argument)
        {
            // make sure we are in the bounds of the input
            if (start >= input.Length)
            {
                argument = null;
                return -1;
            }

            // move forward to the first non-space character
            while (start < input.Length && input[start] == ' ')
                start++;

            // if we're at the end, return null
            if (start == input.Length)
            {
                argument = null;
                return -1;
            }

            // move forward until we're done with this argument (we've found a space and
            // are outside of a pair of quotes)
            bool inQuotes = false;
            int end;
            for (end = start; end < input.Length; end++)
            {
                if (input[end] == ' ' && !inQuotes)
                    break;
                if (input[end] == '"' && (end - 1 < 0 || input[end - 1] != '\\'))
                    inQuotes = !inQuotes;
            }

            // return the substring, without quotes, trimmed
            argument = input.Substring(start, end - start).Replace("\"", "");
            return end;
        }

        private void RegisterDefaultCommands()
        {
            StringBuilder helpTextBuilder = new StringBuilder();

            // suggestion provider that will handle our get, set, and invoke commands to help with autocompletion of member names
            SelectionMemberSuggestionProvider memberSuggestionProvider = new SelectionMemberSuggestionProvider();

            // Register our 'commands' command to list all registered commands
            helpTextBuilder.Append("Lists all commands currently registered with the console.");
            RegisterCommand("commands", helpTextBuilder.ToString(), CommandsCommandAction);

            // Register 'help' to display help text for commands
            helpTextBuilder.Clear();
            helpTextBuilder.Append("Displays help information for registered commands.");
            RegisterCommand("help", helpTextBuilder.ToString(), HelpCommandAction, commandSuggestionProvider);

            // Register 'info' to display all public fields, properties, and methods on the selected objects
            helpTextBuilder.Clear();
            helpTextBuilder.Append("Displays public fields, properties, and methods that exist on the selected object(s).");
            RegisterCommand("info", helpTextBuilder.ToString(), InfoCommandAction, null);

            // Register 'get' for retrieving values from the selected objects
            helpTextBuilder.Clear();
            helpTextBuilder.AppendLine("Gets the value of a field or property on the selected object(s).");
            helpTextBuilder.AppendLine("Usage: get [propertyOrFieldName]");
            helpTextBuilder.Append("propertyOrFieldName  The name of a field or property to get from the selected object(s).");
            RegisterCommand("get", helpTextBuilder.ToString(), GetCommandAction, memberSuggestionProvider);

            // Register 'set' for setting values on the selected objects
            helpTextBuilder.Clear();
            helpTextBuilder.AppendLine("Sets the value of a field or property on the selected object(s).");
            helpTextBuilder.AppendLine("Usage: set [propertyOrFieldName] [newPropertyValue]");
            helpTextBuilder.AppendLine("propertyOrFieldName  The name of a field or property to set on the selected object(s).");
            helpTextBuilder.Append("newPropertyValue     A string representation of the new value to assign to the field or property.");
            RegisterCommand("set", helpTextBuilder.ToString(), SetCommandAction, memberSuggestionProvider);

            // Register 'invoke' for calling methods on the selected objects
            helpTextBuilder.Clear();
            helpTextBuilder.AppendLine("Invokes a method on the selected object(s).");
            helpTextBuilder.AppendLine("Usage: invoke [methodName] (arg1 arg2 arg3 etc)");
            helpTextBuilder.AppendLine("methodName   The name of a method to invoke on the selected object(s).");
            helpTextBuilder.Append("arg1 etc     (Optional) The arguments to parse and pass to the method.");
            RegisterCommand("invoke", helpTextBuilder.ToString(), InvokeCommandAction, memberSuggestionProvider);
        }

        private void CommandsCommandAction(string command, IList<string> args)
        {
            StringBuilder temp = new StringBuilder();
            foreach (var c in commands.Keys)
                temp.AppendFormat("{0}, ", c);
            temp.Remove(temp.Length - 2, 2);
            WriteLine(temp.ToString());
        }

        private void HelpCommandAction(string command, IList<string> args)
        {
            // Validate arguments
            if (args.Count == 0)
            {
                WriteError("help requires at least one argument.");
                WriteCommandHelp(command);
                return;
            }

            // Write out the help for the command
            foreach (var a in args)
            {
                // If we have multiple commands, write the command as well for clarity
                if (args.Count > 1)
                    WriteLine(string.Format("Command: {0}", a), Color.Gold);
                WriteCommandHelp(a);
            }
        }

        private void InfoCommandAction(string command, IList<string> args)
        {
            // Make sure we have a selection
            if (selectedObjects.Count == 0)
            {
                WriteError("Nothing is selected. Add objects to the SelectedObjects list before using 'info'.");
                return;
            }

            // get all the base types of the selected objects
            var baseTypes = FindCommonBaseTypesOfSelection();

            // First write out all fields and properties
            WriteLine("Fields/Properties:");
            foreach (var t in baseTypes)
            {
                foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    WriteLine(string.Format(" {0} {1}", f.FieldType.Name, f.Name));
                foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    // Build up the accessors string for the property
                    StringBuilder accessors = new StringBuilder();

                    // Check each accessor and append them to our string
                    if (p.GetGetMethod() != null && p.GetGetMethod().IsPublic)
                        accessors.Append(" get;");
                    if (p.GetSetMethod() != null && p.GetSetMethod().IsPublic)
                        accessors.Append(" set;");

                    // Write out the property type, name, and accessors
                    WriteLine(string.Format(" {0} {1} {{{2} }}", p.PropertyType.Name, p.Name, accessors));
                }
            }

            // Then write out all methods
            WriteLine("Methods:");
            foreach (var t in baseTypes)
            {
                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    // Special name methods are property accessors so skip those
                    if (m.IsSpecialName) continue;

                    // Build up the argument list for this method
                    StringBuilder argumentList = new StringBuilder("(");
                    foreach (var p in m.GetParameters())
                        argumentList.AppendFormat("{0}, ", p.ParameterType.Name);
                    if (argumentList.Length > 1)
                        argumentList.Remove(argumentList.Length - 2, 2);
                    argumentList.Append(")");

                    // Output the method with the return type and argument list
                    WriteLine(string.Format(" {0} {1}{2}", m.ReturnType.Name, m.Name, argumentList));
                }
            }
        }

        private void GetCommandAction(string command, IList<string> args)
        {
            // Basic validation of arguments
            if (args.Count != 1)
            {
                // Write an error and display the help for the command
                WriteError("get requires a single argument");
                WriteCommandHelp(command);
                return;
            }

            // Make sure we have a selection
            if (selectedObjects.Count == 0)
            {
                WriteError("Nothing is selected. Add objects to the SelectedObjects list before using 'get'.");
                return;
            }

            // Get the value for each selected object
            foreach (var o in selectedObjects)
            {
                // Try to find a field or property with the given name
                FieldInfo f = o.GetType().GetField(args[0]);
                PropertyInfo p = o.GetType().GetProperty(args[0]);

                // Error if neither are found
                if (f == null && p == null)
                {
                    WriteError(string.Format("{0} doesn't have a field or property called {1}.", o, args[0]));
                }
                else
                {
                    // Get the value of the field or property from the object and write out the value
                    try
                    {
                        object value = f != null ? f.GetValue(o) : p.GetValue(o, null);
                        WriteLine(string.Format("{0}.{1} = {2}", o, args[0], value == null ? "(null)" : StringParser.ToString(value)));
                    }
                    catch (Exception e)
                    {
                        WriteError(string.Format("Failed to get {0}.{1}: {2}", o, args[0], e.Message));
                    }
                }
            }
        }

        private void SetCommandAction(string command, IList<string> args)
        {
            // Basic validation of arguments
            if (args.Count != 2)
            {
                WriteError("set requires two arguments");
                WriteCommandHelp(command);
                return;
            }

            // Make sure we have a selection
            if (selectedObjects.Count == 0)
            {
                WriteError("Nothing is selected. Add objects to the SelectedObjects list before using 'set'.");
                return;
            }

            // Validate the string parsing delegate
            if (StringParser == null)
            {
                WriteError("No string parsing delegate was given to XConsole; no way to parse argument types.");
                return;
            }

            // Set the value for each selected object
            foreach (var o in selectedObjects)
            {
                // Try to find a field or property with the given name
                FieldInfo f = o.GetType().GetField(args[0]);
                PropertyInfo p = o.GetType().GetProperty(args[0]);

                // Error if neither are found
                if (f == null && p == null)
                {
                    WriteError(string.Format("{0} doesn't have a field or property called {1}.", o, args[0]));
                }
                else
                {
                    // Try to parse the value argument using our delegate
                    Type valueType = f != null ? f.FieldType : p.PropertyType;
                    object value;
                    try { value = StringParser.Parse(valueType, args[1]); }
                    catch (Exception e)
                    {
                        WriteError(string.Format("Failed to parse '{0}' as {1}: {2}", args[1], valueType, e.Message));
                        continue;
                    }

                    // Now set the value on the field or property
                    try
                    {
                        if (f != null)
                            f.SetValue(o, value);
                        else
                            p.SetValue(o, value, null);
                    }
                    catch (Exception e)
                    {
                        WriteError(string.Format("Failed to set {0}.{1}: {2}", o, args[0], e.Message));
                        continue;
                    }

                    // And log the new value
                    WriteLine(string.Format("{0}.{1} = {2}", o, args[0], value == null ? "(null)" : StringParser.ToString(value)));
                }
            }
        }

        private void InvokeCommandAction(string command, IList<string> args)
        {
            // Basic validation of arguments
            if (args.Count < 1)
            {
                WriteError("invoke requires at least one argument");
                WriteCommandHelp(command);
                return;
            }

            // Make sure we have a selection
            if (selectedObjects.Count == 0)
            {
                WriteError("Nothing is selected. Add objects to the SelectedObjects list before using 'invoke'.");
                return;
            }

            // Validate the string parsing delegate
            if (StringParser == null)
            {
                WriteError("No string parsing delegate was given to XConsole; no way to parse argument types.");
                return;
            }

            // Invoke the method for each selected object
            foreach (var o in selectedObjects)
            {
                // Try to get the method
                MethodInfo method = o.GetType().GetMethod(args[0]);

                // Error if no method was found
                if (method == null)
                {
                    WriteError(string.Format("{0} doesn't have a method called {1}.", o, args[0]));
                }
                // Error if we have a mismatch of parameter counts (make sure to -1 on a.Count since a[0] is the method name)
                else if (method.GetParameters().Length != args.Count - 1)
                {
                    WriteError(string.Format("{0}.{1} requires {2} arguments, {3} were given to 'invoke'.", o, args[0], method.GetParameters().Length, args.Count - 1));
                }
                else
                {
                    // Parse out the parameters for the method
                    ParameterInfo[] methodParams = method.GetParameters();
                    object[] methodArguments = new object[methodParams.Length];
                    try
                    {
                        for (int i = 0; i < methodParams.Length; i++)
                            methodArguments[i] = StringParser.Parse(methodParams[i].ParameterType, args[i + 1]);
                    }
                    catch (Exception e)
                    {
                        WriteError(string.Format("Error while parsing arguments: {0}", e.Message));
                        return;
                    }

                    // Invoke the method on the object
                    object output = method.Invoke(o, methodArguments);

                    // If the method isn't a void return type, write out the result
                    if (method.ReturnType != typeof(void))
                        WriteLine(string.Format("Result: {0}", output == null ? "(null)" : StringParser.ToString(output)));
                }
            }
        }

        // Finds all of the common base classes and interfaces of the selection
        private IEnumerable<Type> FindCommonBaseTypesOfSelection()
        {
            List<Type> baseTypes = new List<Type>();

            foreach (var o in selectedObjects)
            {
                Type t = o.GetType();

                // add in all the interfaces from this type
                foreach (var i in t.GetInterfaces())
                {
                    if (!baseTypes.Contains(i))
                        baseTypes.Add(i);
                }

                // add in all base classes from this type
                while (t != null)
                {
                    if (!baseTypes.Contains(t))
                        baseTypes.Add(t);
                    t = t.BaseType;
                }
            }

            // now we can return the types from this collection that are assignable from
            // all of the selected object types
            return baseTypes.Where(t =>
                {
                    foreach (var o in selectedObjects)
                        if (!t.IsAssignableFrom(o.GetType()))
                            return false;
                    return true;
                }).ToList();
        }

        #region Public Support Types

        /// <summary>
        /// Defines the possible states of the XConsole.
        /// </summary>
        public enum State
        {
            /// <summary>
            /// The XConsole is completely closed.
            /// </summary>
            Closed,

            /// <summary>
            /// The XConsole is animating to the Open state.
            /// </summary>
            Opening,

            /// <summary>
            /// The XConsole is completely open.
            /// </summary>
            Open,

            /// <summary>
            /// The XConsole is animating to the Closed state.
            /// </summary>
            Closing
        }

        /// <summary>
        /// The delegate takes in a key as well as whether or not the shift key is down and
        /// uses that to return a character which is then added to the current input string.
        /// The function needs to return true if there is a valid character for the key or 
        /// false if the input should be ignored.
        /// </summary>
        /// <param name="key">The key that was pressed on the keyboard.</param>
        /// <param name="useAlternate">Whether or not to use an alternate value for the key, e.g. when shift is held on the keyboard.</param>
        /// <param name="character">The character mapped to this key.</param>
        /// <returns>True if there was a valid character for the key, false otherwise.</returns>
        public delegate bool KeyToCharacterDelegate(Keys key, bool useAlternate, out char character);

        /// <summary>
        /// Delegate invoked for XConsole commands.
        /// </summary>
        /// <param name="command">The name of the command that was invoked.</param>
        /// <param name="args">A list of arguments passed to the command.</param>
        public delegate void CommandActionDelegate(string command, IList<string> args);

        /// <summary>
        /// An input service the XConsole relies on for keyboard input.
        /// </summary>
        public interface IKeyboardInputService
        {
            /// <summary>
            /// Determines if a key was just pressed this frame.
            /// </summary>
            /// <param name="key">The key to test.</param>
            /// <returns>True if the key was just pressed this frame, false otherwise.</returns>
            bool IsJustPressed(Keys key);

            /// <summary>
            /// Determines if a key is down.
            /// </summary>
            /// <param name="key">The key to test.</param>
            /// <returns>True if the key is down, false otherwise.</returns>
            bool IsDown(Keys key);

            /// <summary>
            /// Returns an array of currently pressed keys.
            /// </summary>
            /// <returns>An array of keys currently pressed.</returns>
            Keys[] GetPressedKeys();
        }

        /// <summary>
        /// Interface for an object that can convert values to and from strings for the console.
        /// </summary>
        public interface IStringParser
        {
            object Parse(Type type, string value);
            string ToString(object value);
        }

        /// <summary>
        /// The interface for an object that provides tab completion for command arguments.
        /// </summary>
        public interface ISuggestionProvider
        {
            /// <summary>
            /// Given an argument index and the partial argument given to the console, this method
            /// returns a list of suggestions for completing the argument.
            /// </summary>
            /// <param name="command">The command for which suggestions should be provided.</param>
            /// <param name="argumentIndex">The index of the current argument. This is 0-based.</param>
            /// <param name="argument">The partial argument as entered by the user.</param>
            /// <param name="previousArguments">The arguments prior to the current argument, for context.</param>
            /// <returns>An list of suggestions for completing the argument.</returns>
            IList<string> GetSuggestions(string command, int argumentIndex, string argument, IList<string> previousArguments);
        }

        /// <summary>
        /// The default string parser for XConsole. Can be subclassed to add additional type support.
        /// </summary>
        /// <remarks>
        /// DefaultStringParser supports a large number of default C# types:
        ///   byte
        ///   sbyte
        ///   short
        ///   ushort
        ///   int
        ///   uint
        ///   long
        ///   ulong
        ///   float
        ///   double
        ///   decimal
        ///   bool
        ///   string
        /// 
        /// If you want more type support, you can either add it here or subclass this type as the DemoStringParser does
        /// since both Parse and ToString are made virtual and thus overridable in subclasses.
        /// </remarks>
        public class DefaultStringParser : IStringParser
        {
            public virtual object Parse(Type type, string value)
            {
                // We support all the basic C# numeric types as well as bool and string. 
                // Anything more complex requires a custom string parser.
                if (type == typeof(byte))
                    return byte.Parse(value, CultureInfo.InvariantCulture);
                if (type == typeof(sbyte))
                    return sbyte.Parse(value, CultureInfo.InvariantCulture);
                if (type == typeof(short))
                    return short.Parse(value, CultureInfo.InvariantCulture);
                if (type == typeof(ushort))
                    return ushort.Parse(value, CultureInfo.InvariantCulture);
                if (type == typeof(int))
                    return int.Parse(value, CultureInfo.InvariantCulture);
                if (type == typeof(uint))
                    return uint.Parse(value, CultureInfo.InvariantCulture);
                if (type == typeof(long))
                    return long.Parse(value, CultureInfo.InvariantCulture);
                if (type == typeof(ulong))
                    return ulong.Parse(value, CultureInfo.InvariantCulture);
                if (type == typeof(float))
                    return float.Parse(value, CultureInfo.InvariantCulture);
                if (type == typeof(double))
                    return double.Parse(value, CultureInfo.InvariantCulture);
                if (type == typeof(decimal))
                    return decimal.Parse(value, CultureInfo.InvariantCulture);
                if (type == typeof(bool))
                    return bool.Parse(value);
                if (type == typeof(string))
                    return value;

                throw new InvalidOperationException(string.Format("{0} is not supported by default string parser.", type));
            }

            public virtual string ToString(object value)
            {
                Type type = value.GetType();

                // Support good ToString calls for all the basic C# math types, falling back to plain ToString() for any other types.
                if (type == typeof(byte))
                    return ((byte)value).ToString(CultureInfo.InvariantCulture);
                if (type == typeof(sbyte))
                    return ((sbyte)value).ToString(CultureInfo.InvariantCulture);
                if (type == typeof(short))
                    return ((short)value).ToString(CultureInfo.InvariantCulture);
                if (type == typeof(ushort))
                    return ((ushort)value).ToString(CultureInfo.InvariantCulture);
                if (type == typeof(int))
                    return ((int)value).ToString(CultureInfo.InvariantCulture);
                if (type == typeof(uint))
                    return ((uint)value).ToString(CultureInfo.InvariantCulture);
                if (type == typeof(long))
                    return ((long)value).ToString(CultureInfo.InvariantCulture);
                if (type == typeof(ulong))
                    return ((ulong)value).ToString(CultureInfo.InvariantCulture);
                if (type == typeof(float))
                    return ((float)value).ToString(CultureInfo.InvariantCulture);
                if (type == typeof(double))
                    return ((double)value).ToString(CultureInfo.InvariantCulture);
                if (type == typeof(decimal))
                    return ((decimal)value).ToString(CultureInfo.InvariantCulture);
                if (type == typeof(bool))
                    return ((bool)value).ToString(CultureInfo.InvariantCulture);
                if (type == typeof(string))
                    return (string)value;

                return value.ToString();
            }
        }

        #endregion

        #region Private Support Types

        /// <summary>
        /// Represents one line of our output.
        /// </summary>
        struct OutputLine
        {
            public string Text;
            public Color Color;
        }

        /// <summary>
        /// Provides our default KeyToCharacterDelegate.
        /// </summary>
        static class KeyString
        {
            class CharPair
            {
                public char NormalChar;
                public char? ShiftChar;

                public CharPair(char normalChar, char? shiftChar)
                {
                    this.NormalChar = normalChar;
                    this.ShiftChar = shiftChar;
                }
            }

            private static Dictionary<Keys, CharPair> keyMap = new Dictionary<Keys, CharPair>();

            public static bool KeyToString(Keys key, bool shiftKeyPressed, out char character)
            {
                bool result = false;
                character = ' ';
                CharPair charPair;

                if ((Keys.A <= key && key <= Keys.Z) || key == Keys.Space)
                {
                    // Use as is if it is A～Z, or Space key.
                    character = (shiftKeyPressed) ? (char)key : Char.ToLower((char)key);
                    result = true;
                }
                else if (keyMap.TryGetValue(key, out charPair))
                {
                    // Otherwise, convert by key map.
                    if (!shiftKeyPressed)
                    {
                        character = charPair.NormalChar;
                        result = true;
                    }
                    else if (charPair.ShiftChar.HasValue)
                    {
                        character = charPair.ShiftChar.Value;
                        result = true;
                    }
                }

                return result;
            }

            static KeyString()
            {
                // First row of US keyboard.
                AddKeyMap(Keys.OemTilde, "`~");
                AddKeyMap(Keys.D1, "1!");
                AddKeyMap(Keys.D2, "2@");
                AddKeyMap(Keys.D3, "3#");
                AddKeyMap(Keys.D4, "4$");
                AddKeyMap(Keys.D5, "5%");
                AddKeyMap(Keys.D6, "6^");
                AddKeyMap(Keys.D7, "7&");
                AddKeyMap(Keys.D8, "8*");
                AddKeyMap(Keys.D9, "9(");
                AddKeyMap(Keys.D0, "0)");
                AddKeyMap(Keys.OemMinus, "-_");
                AddKeyMap(Keys.OemPlus, "=+");

                // Second row of US keyboard.
                AddKeyMap(Keys.OemOpenBrackets, "[{");
                AddKeyMap(Keys.OemCloseBrackets, "]}");
                AddKeyMap(Keys.OemPipe, "\\|");

                // Third row of US keyboard.
                AddKeyMap(Keys.OemSemicolon, ";:");
                AddKeyMap(Keys.OemQuotes, "'\"");
                AddKeyMap(Keys.OemComma, ",<");
                AddKeyMap(Keys.OemPeriod, ".>");
                AddKeyMap(Keys.OemQuestion, "/?");

                // Keypad keys of US keyboard.
                AddKeyMap(Keys.NumPad1, "1");
                AddKeyMap(Keys.NumPad2, "2");
                AddKeyMap(Keys.NumPad3, "3");
                AddKeyMap(Keys.NumPad4, "4");
                AddKeyMap(Keys.NumPad5, "5");
                AddKeyMap(Keys.NumPad6, "6");
                AddKeyMap(Keys.NumPad7, "7");
                AddKeyMap(Keys.NumPad8, "8");
                AddKeyMap(Keys.NumPad9, "9");
                AddKeyMap(Keys.NumPad0, "0");
                AddKeyMap(Keys.Add, "+");
                AddKeyMap(Keys.Divide, "/");
                AddKeyMap(Keys.Multiply, "*");
                AddKeyMap(Keys.Subtract, "-");
                AddKeyMap(Keys.Decimal, ".");
            }

            static void AddKeyMap(Keys key, string charPair)
            {
                char char1 = charPair[0];
                Nullable<char> char2 = null;
                if (charPair.Length > 1)
                    char2 = charPair[1];

                keyMap.Add(key, new CharPair(char1, char2));
            }
        }

        /// <summary>
        /// Stores all information about a given command.
        /// </summary>
        class CommandInfo
        {
            public readonly string Command;
            public readonly CommandActionDelegate Action;
            public readonly string HelpText;
            public readonly ISuggestionProvider TabCompletionProvider;

            public CommandInfo(string command, CommandActionDelegate action, string helpText, ISuggestionProvider tabCompletionProvider)
            {
                Command = command;
                Action = action;
                HelpText = helpText;
                TabCompletionProvider = tabCompletionProvider;
            }
        }

        /// <summary>
        /// Provides suggestions based on registered commands. Used by our default command tab completion as well
        /// as for the help command to provide tab completion of command names as arguments.
        /// </summary>
        class CommandSuggestionProvider : ISuggestionProvider
        {
            public IList<string> GetSuggestions(string command, int argumentIndex, string argument, IList<string> previousArguments)
            {
                // simply find all keys in our command dictionary that start with the argument
                var list = XConsole.Instance.commands.Keys.Where(c => c.StartsWith(argument ?? string.Empty)).ToList();

                // sort the list so the results are alphabetical. not required, but kind of nice
                list.Sort();

                return list;
            }
        }

        /// <summary>
        /// A suggestion provider for fields, properties, and methods on the selected objects.
        /// </summary>
        class SelectionMemberSuggestionProvider : ISuggestionProvider
        {
            // we'll cache off the names for each type to make repeated uses of the suggestion a little faster
            Dictionary<Type, List<string>> fieldAndPropertyNameCache = new Dictionary<Type, List<string>>();
            Dictionary<Type, List<string>> methodNameCache = new Dictionary<Type, List<string>>();

            public IList<string> GetSuggestions(string command, int argumentIndex, string argument, IList<string> previousArguments)
            {
                var types = XConsole.Instance.FindCommonBaseTypesOfSelection();

                // get and set will be looking for field or property names for argument 0
                if ((command == "get" || command == "set") && argumentIndex == 0)
                {
                    List<string> names = new List<string>();

                    // find all fields and property names that start with the argument bit
                    foreach (var t in types)
                    {
                        // first try to get our names out of the cache
                        List<string> temp;
                        if (!fieldAndPropertyNameCache.TryGetValue(t, out temp))
                        {
                            // if that fails we iterate all fields and properties to build up a list of names
                            temp = new List<string>();
                            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                                temp.Add(f.Name);
                            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                                temp.Add(p.Name);

                            // and add it to the cache
                            fieldAndPropertyNameCache.Add(t, temp);
                        }

                        // now select any from that list that start with our argument value
                        names.AddRange(temp.Where(n => n.StartsWith(argument)));
                    }

                    // sort the names alphabetically
                    names.Sort();

                    return names;
                }

                // invoke is going to be looking for method names for argument 0
                else if (command == "invoke" && argumentIndex == 0)
                {
                    List<string> names = new List<string>();

                    // find all method names that start with the argument bit
                    foreach (var t in types)
                    {
                        // first try to get our names out of the cache
                        List<string> temp;
                        if (!methodNameCache.TryGetValue(t, out temp))
                        {
                            // if that fails we iterate all methods to build up a list of names
                            temp = new List<string>();
                            foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                                temp.Add(m.Name);

                            // and add it to the cache
                            methodNameCache.Add(t, temp);
                        }

                        // now select any from that list that start with our argument value
                        names.AddRange(temp.Where(n => n.StartsWith(argument)));
                    }

                    // sort the names alphabetically
                    names.Sort();

                    return names;
                }

                // we can't help with any other arguments or commands
                return null;
            }
        }
        
        #endregion
    }

#if !WINDOWS
    /// <summary>
    /// Extension methods for StringBuilder to replace functionality that exists on Windows for non-Windows platforms.
    /// Extension method classes cannot be nested which is why this isn't housed inside the XConsole class itself.
    /// </summary>
    static class StringBuilderExtensions
    {
        public static StringBuilder Clear(this StringBuilder builder)
        {
            return builder.Remove(0, builder.Length);
        }

        public static StringBuilder Insert(this StringBuilder builder, int index, char ch)
        {
            return builder.Insert(index, ch.ToString());
        }
    }
#endif
}
