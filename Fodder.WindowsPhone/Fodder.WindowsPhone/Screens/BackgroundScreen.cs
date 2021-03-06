#region File Description
//-----------------------------------------------------------------------------
// BackgroundScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Fodder.Core;
using Fodder.GameState;
using System.Collections.Generic;
#endregion

namespace Fodder.Phone.GameState
{
    /// <summary>
    /// The background screen sits behind all the other menu screens.
    /// It draws a background image that remains fixed in place regardless
    /// of whatever transitions the screens on top of it may be doing.
    /// </summary>
    public class BackgroundScreen : GameScreen
    {
        #region Fields

        ContentManager content;
        Texture2D texBG;
        Texture2D texLogo;

        GameSession attractSession;

        float logoAlpha = 1f;

        #endregion

        #region Initialization


        /// <summary>
        /// Constructor.
        /// </summary>
        public BackgroundScreen()
        {
            TransitionOnTime = TimeSpan.FromSeconds(0.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
        }


        /// <summary>
        /// Loads graphics content for this screen. The background texture is quite
        /// big, so we use our own local ContentManager to load it. This allows us
        /// to unload before going from the menus into the game itself, wheras if we
        /// used the shared ContentManager provided by the Game class, the content
        /// would remain loaded forever.
        /// </summary>
        public override void LoadContent()
        {
            if (content == null)
                content = new ContentManager(ScreenManager.Game.Services, "Fodder.Content");

            texBG = content.Load<Texture2D>("blank");
            texLogo = content.Load<Texture2D>("logo");

            List<Function> funcs = new List<Function>();
            funcs.Add(new Function("boost", 1000, true));
            funcs.Add(new Function("shield", 10000, true));
            funcs.Add(new Function("pistol", 4000, true));
            funcs.Add(new Function("shotgun", 6000, true));
            funcs.Add(new Function("smg", 8000, true));
            funcs.Add(new Function("sniper", 30000, true));
            funcs.Add(new Function("machinegun", 30000, true));
            funcs.Add(new Function("mortar", 30000, true));
            funcs.Add(new Function("haste", 20, true));
            funcs.Add(new Function("meteors", 20, true));
            funcs.Add(new Function("elite", 20, true));

            Scenario scenario = new Scenario("Attract", "attract", funcs, 3000, 100, 100, 1000, 1000);

            attractSession = new GameSession(GameClientType.AI, GameClientType.AI, scenario, ScreenManager.GraphicsDevice.Viewport, true);
            attractSession.LoadContent(content);
        }


        /// <summary>
        /// Unloads graphics content for this screen.
        /// </summary>
        public override void UnloadContent()
        {
            content.Unload();
        }


        #endregion

        #region Update and Draw


        /// <summary>
        /// Updates the background screen. Unlike most screens, this should not
        /// transition off even if it has been covered by another screen: it is
        /// supposed to be covered, after all! This overload forces the
        /// coveredByOtherScreen parameter to false in order to stop the base
        /// Update method wanting to transition off.
        /// </summary>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                                       bool coveredByOtherScreen)
        {
            attractSession.Update(gameTime);

            bool found = false;
            foreach (GameScreen screen in ScreenManager.GetScreens())
            {
                if (screen.GetType() == typeof(CampaignScreen))
                    found = true;
            }
            if (found)
                logoAlpha -= 0.05f;
            else
                logoAlpha += 0.05f;

            logoAlpha = MathHelper.Clamp(logoAlpha, 0f, 1f);

            base.Update(gameTime, otherScreenHasFocus, false);
        }


        /// <summary>
        /// Draws the background screen.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            Viewport viewport = ScreenManager.GraphicsDevice.Viewport;
            Rectangle fullscreen = new Rectangle(0, 0, viewport.Width, viewport.Height);

            attractSession.Draw(gameTime, spriteBatch);

            spriteBatch.Begin();
            spriteBatch.Draw(texBG, fullscreen,
                             Color.White * (0.5f + (0.5f * TransitionPosition)));

            spriteBatch.Draw(texLogo, new Vector2(viewport.Width / 2, viewport.Height / 4), null,
                             Color.White * TransitionAlpha * logoAlpha, 0f, new Vector2(texLogo.Width / 2, texLogo.Height / 2), 1f, SpriteEffects.None, 1);
            spriteBatch.End();
        }


        #endregion
    }
}
