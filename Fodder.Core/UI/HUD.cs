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
    class HUD
    {
        static Texture2D _texHud;
        static SpriteFont _font;

        static Vector2 _team1Bar;
        static Vector2 _team2Bar;

        public HUD() { }

        public void LoadContent(ContentManager content)
        {
            _texHud = content.Load<Texture2D>("hud");
            _font = content.Load<SpriteFont>("font");

            _team1Bar = new Vector2(20, 50);
            _team2Bar = new Vector2(GameSession.Instance.Viewport.Width - 20 - 200, 50);
        }

        public void Update(GameTime gameTime)
        {
            
        }

        public void Draw(SpriteBatch sb)
        {
            sb.Begin();

            int reinforcementswidth = (int)((200f / (float)GameSession.Instance.Team1StartReinforcements) * (float)GameSession.Instance.Team1Reinforcements);
            int activewidth = (int)((200f / (float)GameSession.Instance.Team1StartReinforcements) * (float)GameSession.Instance.Team1ActiveCount);
            sb.Draw(_texHud, _team1Bar, new Rectangle(0, 0, 200, 30), Color.White);
            sb.Draw(_texHud, _team1Bar + new Vector2(reinforcementswidth,0), new Rectangle(reinforcementswidth, 30, activewidth, 30), Color.White);
            sb.Draw(_texHud, _team1Bar, new Rectangle(0, 60, reinforcementswidth, 30), Color.White);

            reinforcementswidth = (int)((200f / (float)GameSession.Instance.Team2StartReinforcements) * (float)GameSession.Instance.Team2Reinforcements);
            activewidth = (int)((200f / (float)GameSession.Instance.Team2StartReinforcements) * (float)GameSession.Instance.Team2ActiveCount);
            sb.Draw(_texHud, _team2Bar, new Rectangle(0, 0, 200, 30), Color.White);
            sb.Draw(_texHud, _team2Bar + new Vector2(200 - reinforcementswidth - activewidth, 0), new Rectangle(200-reinforcementswidth-activewidth, 30, activewidth, 30), Color.White);
            sb.Draw(_texHud, _team2Bar + new Vector2(200 - reinforcementswidth, 0), new Rectangle(200-reinforcementswidth, 60, reinforcementswidth, 30), Color.White);

            if (GameSession.Instance.Team1Win && GameSession.Instance.Team2Win) sb.DrawString(_font, "Nobody wins", new Vector2(GameSession.Instance.Viewport.Width, GameSession.Instance.Viewport.Height) / 2, Color.White, 0f, _font.MeasureString("Nobody wins") / 2, 1f, SpriteEffects.None, 1);
            else
            {
                if (GameSession.Instance.Team1Win) sb.DrawString(_font, "Reds Win", new Vector2(GameSession.Instance.Viewport.Width, GameSession.Instance.Viewport.Height) / 2, Color.White, 0f, _font.MeasureString("Reds Win") / 2, 1f, SpriteEffects.None, 1);
                if (GameSession.Instance.Team2Win) sb.DrawString(_font, "Blues Win", new Vector2(GameSession.Instance.Viewport.Width, GameSession.Instance.Viewport.Height) / 2, Color.White, 0f, _font.MeasureString("Blues Win") / 2, 1f, SpriteEffects.None, 1);
            }

            sb.End();
        }


        
    }
}
