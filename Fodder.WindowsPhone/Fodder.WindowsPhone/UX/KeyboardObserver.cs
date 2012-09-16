using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;

namespace Fodder.Windows.UX
{
    /// <summary>
    /// Defines a simple way for objects to look at keyboard interaction without making
    /// calls to the static instance object in the XNA framework directly.
    /// </summary>
    public class KeyboardObserver
    {
        public Boolean IsKeyPressed(Keys key)
        {
            return Keyboard.GetState().IsKeyDown(key);
        }
    }
}
