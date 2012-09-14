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
    class Button
    {
        public Rectangle UIRect;
        public string Function;
        public bool IsSelected = false;
        public Keys ShortcutKey;
        public bool IsEnabled = true;

        Texture2D _texButtons;
        double _currentCDTime = 0;
        double _actualCDTime = 0;
        Rectangle _sourceRectFull;
        Rectangle _sourceRectEmpty;

        bool _buttonDown = false;

        public Button(Texture2D texture, int buttonNum, double coolDown, string func, Vector2 screenPosition, Keys shortcutKey, bool enabled)
        {
            _texButtons = texture;
            _sourceRectFull = new Rectangle(buttonNum * 100, 0, 100, 100);
            _sourceRectEmpty = new Rectangle(buttonNum * 100, 100, 100, 100);
            _actualCDTime = coolDown;

            Function = func;
            UIRect = new Rectangle((int)screenPosition.X, (int)screenPosition.Y, 100, 100);
            ShortcutKey = shortcutKey;
            IsEnabled = enabled;
        }

        public void Update(GameTime gameTime)
        {
            if (_currentCDTime > 0)
            {
                _currentCDTime -= gameTime.ElapsedGameTime.TotalMilliseconds;
            }
        }

        public void MouseOver()
        {
            
        }
        public void MouseOut()
        {
            _buttonDown = false;
        }
        public void MouseDown()
        {
            _buttonDown = true;
        }
        public void MouseUp()
        {
            if (_buttonDown == true && IsEnabled)
            {
                GameSession.Instance.ButtonController.Deselect();
                IsSelected = true;

                // Button has been clicked
                _buttonDown = false;
            }
        }

        public void ActivateFunction(Dude d)
        {
            if (_currentCDTime <= 0)
            {
                switch (Function)
                {
                    case "boost":
                        d.BoostTime = 5000;
                        _currentCDTime = _actualCDTime;
                        break;
                    case "shield":
                        d.ShieldTime = 10000;
                        _currentCDTime = _actualCDTime;
                        break;
                    default:
                        if (d.Weapon.GetType() == typeof(Weapons.Sword))
                        {
                            d.GiveWeapon(Function);
                            _currentCDTime = _actualCDTime;
                        }
                        break;
                }
            }
        }

        public void Draw(SpriteBatch sb)
        {
            int coolDownToHeight = 100 - (int)((100/_actualCDTime) * _currentCDTime);
            
            sb.Draw(_texButtons, UIRect, _sourceRectEmpty, IsSelected?Color.Red:Color.White);
            sb.Draw(_texButtons,
                    new Rectangle(UIRect.Left, UIRect.Top + (100 - coolDownToHeight), 100, coolDownToHeight),
                    new Rectangle(_sourceRectFull.Left, _sourceRectFull.Top + (100 - coolDownToHeight), 100, coolDownToHeight),
                    !IsEnabled ? Color.DarkGray : (IsSelected ? Color.Red : Color.White));
        }



        internal void Reset()
        {
            _currentCDTime = 0;
        }
    }
}
