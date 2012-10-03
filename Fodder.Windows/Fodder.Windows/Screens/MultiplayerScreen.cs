#region File Description
//-----------------------------------------------------------------------------
// GameplayScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Fodder.GameState;
using Fodder.Core;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using System.IO.IsolatedStorage;
using XConsoleApp;
#endregion

namespace Fodder.Windows.GameState
{
    /// <summary>
    /// This screen implements the actual game logic. It is just a
    /// placeholder to get the idea across: you'll probably want to
    /// put some more interesting gameplay in here!
    /// </summary>
    public class MultiplayerScreen : GameScreen
    {
        #region Fields

        ContentManager content;

        Scenario gameScenario;    

        SpriteFont font;

        NetworkControllerWindows Net;

        BackgroundWorker bgw = new BackgroundWorker();
        bool upnpTried = false;
        bool upnpDone = false;

        #endregion

        #region Initialization


        /// <summary>
        /// Constructor.
        /// </summary>
        public MultiplayerScreen(int scenarioNum, ScenarioResult result)
        {
            TransitionOnTime = TimeSpan.FromSeconds(0.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);

            EnabledGestures = GestureType.Tap;

            List<Function> funcs = new List<Function>();
            funcs.Add(new Function("boost", 1000, true));
            funcs.Add(new Function("shield", 15000, true));
            funcs.Add(new Function("pistol", 4000, true));
            funcs.Add(new Function("shotgun", 6000, true));
            funcs.Add(new Function("smg", 8000, true));
            funcs.Add(new Function("sniper", 30000, true));
            funcs.Add(new Function("machinegun", 30000, true));
            funcs.Add(new Function("mortar", 30000, true));
            funcs.Add(new Function("haste", 20, true));
            funcs.Add(new Function("meteors", 20, true));
            funcs.Add(new Function("elite", 20, true));

            gameScenario = new Scenario("Multiplayer", "campaign11", funcs, 5000, 100, 100, 2000, 2000);

            
        }

        void bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            upnpDone = Net.UPNP();
            upnpTried = true;
        }


        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public override void LoadContent()
        {
            if (content == null)
                content = new ContentManager(ScreenManager.Game.Services, "Fodder.Content");

            font = content.Load<SpriteFont>("font");

            Net = new NetworkControllerWindows();
            Net.Initialize(0);

            XConsole.Instance.SelectedObjects.Add(Net);

            bgw.DoWork += new DoWorkEventHandler(bgw_DoWork);
            bgw.RunWorkerAsync();

            ScreenManager.Game.ResetElapsedTime();
        }


        /// <summary>
        /// Unload graphics content used by the game.
        /// </summary>
        public override void UnloadContent()
        {
            
            content.Unload();
        }


        #endregion

        #region Update and Draw


        /// <summary>
        /// Updates the state of the game. This method checks the GameScreen.IsActive
        /// property, so the game will stop updating when the pause menu is active,
        /// or if you tab away to a different application.
        /// </summary>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                                       bool coveredByOtherScreen)
        {
            Net.Update(gameTime);

            if (Net.RemoteState == RemoteClientState.Connected)
            {
                Net.RemoteState = RemoteClientState.ReadyToStart;
                LoadingScreen.Load(ScreenManager, false, null, new GameplayScreen(gameScenario, Net));
            }

            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            
        }


        /// <summary>
        /// Lets the game respond to player input. Unlike the Update method,
        /// this will only be called when the gameplay screen is active.
        /// </summary>
        public override void HandleInput(InputState input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            PlayerIndex pi;

            if (input.IsMenuCancel(ControllingPlayer, out pi))
            {
                Net.CloseConn();
                this.ExitScreen();
            }
            
        }


        /// <summary>
        /// Draws the gameplay screen.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

            spriteBatch.Begin();

            if (!upnpTried)
                spriteBatch.DrawString(font, "Attempting UPnP Port Forward", new Vector2(spriteBatch.GraphicsDevice.Viewport.Width / 2, (spriteBatch.GraphicsDevice.Viewport.Height / 2) - 30), Color.White * TransitionAlpha, 0f, font.MeasureString("Attempting UPnP Port Forward") / 2, 1f, SpriteEffects.None, 1);
            else
            {
                if(upnpDone)
                    spriteBatch.DrawString(font, "UPnP Port Forward Successful!", new Vector2(spriteBatch.GraphicsDevice.Viewport.Width / 2, (spriteBatch.GraphicsDevice.Viewport.Height / 2) - 30), Color.White * TransitionAlpha, 0f, font.MeasureString("UPnP Port Forward Successful!") / 2, 1f, SpriteEffects.None, 1);
                else
                    spriteBatch.DrawString(font, "UPnP forwarding failed - please forward port " + Net.ClientPort + " UDP", new Vector2(spriteBatch.GraphicsDevice.Viewport.Width / 2, (spriteBatch.GraphicsDevice.Viewport.Height / 2) - 30), Color.White * TransitionAlpha, 0f, font.MeasureString("UPnP forwarding failed - please forward port " + Net.ClientPort + " UDP") / 2, 1f, SpriteEffects.None, 1);

            }
                    

                if(Net.RemoteState== RemoteClientState.NotConnected)
                    spriteBatch.DrawString(font, "Waiting for connection with " + Net.HostName, new Vector2(spriteBatch.GraphicsDevice.Viewport.Width / 2, (spriteBatch.GraphicsDevice.Viewport.Height / 2)), Color.White * TransitionAlpha, 0f, font.MeasureString("Waiting for connection with " + Net.HostName) / 2, 1f, SpriteEffects.None, 1);
                if (Net.RemoteState == RemoteClientState.Connected)
                    spriteBatch.DrawString(font, "Connected to " + Net.HostName, new Vector2(spriteBatch.GraphicsDevice.Viewport.Width / 2, (spriteBatch.GraphicsDevice.Viewport.Height / 2)), Color.White * TransitionAlpha, 0f, font.MeasureString("Connected to " + Net.HostName) / 2, 1f, SpriteEffects.None, 1);
               
                spriteBatch.End();
         

            

        }

       


        #endregion
    }
}
