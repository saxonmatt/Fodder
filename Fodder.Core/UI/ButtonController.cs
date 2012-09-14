using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace Fodder.Core
{
    class ButtonController
    {
        public List<Button> Buttons = new List<Button>();

        static Texture2D _texButtons;

        public ButtonController()
        {

        }

        public void LoadContent(ContentManager content)
        {
            _texButtons = content.Load<Texture2D>("buttonsheet");

            Vector2 screenPos = new Vector2((GameSession.Instance.Viewport.Width/2) - (700/2),20);
            Buttons.Add(new Button(_texButtons, 0, GameSession.Instance.AvailableFunctions[0].CoolDown, GameSession.Instance.AvailableFunctions[0].Name, screenPos + new Vector2(100 * Buttons.Count, 0), Keys.D1, GameSession.Instance.AvailableFunctions[0].IsEnabled));
            Buttons.Add(new Button(_texButtons, 1, GameSession.Instance.AvailableFunctions[1].CoolDown, GameSession.Instance.AvailableFunctions[1].Name, screenPos + new Vector2(100 * Buttons.Count, 0), Keys.D2, GameSession.Instance.AvailableFunctions[1].IsEnabled));
            Buttons.Add(new Button(_texButtons, 2, GameSession.Instance.AvailableFunctions[2].CoolDown, GameSession.Instance.AvailableFunctions[2].Name, screenPos + new Vector2(100 * Buttons.Count, 0), Keys.D3, GameSession.Instance.AvailableFunctions[2].IsEnabled));
            Buttons.Add(new Button(_texButtons, 3, GameSession.Instance.AvailableFunctions[3].CoolDown, GameSession.Instance.AvailableFunctions[3].Name, screenPos + new Vector2(100 * Buttons.Count, 0), Keys.D4, GameSession.Instance.AvailableFunctions[3].IsEnabled));
            Buttons.Add(new Button(_texButtons, 4, GameSession.Instance.AvailableFunctions[4].CoolDown, GameSession.Instance.AvailableFunctions[4].Name, screenPos + new Vector2(100 * Buttons.Count, 0), Keys.D5, GameSession.Instance.AvailableFunctions[4].IsEnabled));
            Buttons.Add(new Button(_texButtons, 5, GameSession.Instance.AvailableFunctions[5].CoolDown, GameSession.Instance.AvailableFunctions[5].Name, screenPos + new Vector2(100 * Buttons.Count, 0), Keys.D6, GameSession.Instance.AvailableFunctions[5].IsEnabled));
            Buttons.Add(new Button(_texButtons, 6, GameSession.Instance.AvailableFunctions[6].CoolDown, GameSession.Instance.AvailableFunctions[6].Name, screenPos + new Vector2(100 * Buttons.Count, 0), Keys.D7, GameSession.Instance.AvailableFunctions[6].IsEnabled));
        }

        public void Update(GameTime gameTime)
        {
            foreach (Button b in Buttons)
                b.Update(gameTime);
        }

        public void HandleInput(MouseState ms, KeyboardState ks)
        {
            if (GameSession.Instance.Team1Win || GameSession.Instance.Team2Win) return;
            if (GameSession.Instance.Team1ClientType != GameClientType.Human && GameSession.Instance.Team2ClientType != GameClientType.Human) return;

            foreach (Button b in Buttons)
            {
                if (b.UIRect.Contains((int)ms.X, (int)ms.Y))
                {
                    b.MouseOver();
                    if (ms.LeftButton == ButtonState.Pressed) b.MouseDown();
                    else b.MouseUp();
                }
                else b.MouseOut();

                if (ks.IsKeyDown(b.ShortcutKey) && b.IsEnabled)
                {
                    Deselect();
                    b.IsSelected = true;
                }
            }
        }

        public void DudeClicked(Dude d)
        {
            foreach (Button b in Buttons)
                if (b.IsSelected) b.ActivateFunction(d);
        }

        public void Deselect()
        {
            foreach (Button b in Buttons) b.IsSelected = false;
        }

        public void Draw(SpriteBatch sb)
        {
            if (GameSession.Instance.Team1ClientType != GameClientType.Human && GameSession.Instance.Team2ClientType != GameClientType.Human) return;

            sb.Begin();

            foreach (Button b in Buttons)
                b.Draw(sb);

            sb.End();
        }

        internal void Reset()
        {
            foreach (Button b in Buttons)
            {
                b.Reset();
            }
        }
    }
}
