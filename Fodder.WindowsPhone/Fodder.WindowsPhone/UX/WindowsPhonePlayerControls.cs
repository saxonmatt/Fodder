using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fodder.Core.UX;
using Microsoft.Xna.Framework.Input;

namespace Fodder.WindowsPhone.UX
{
    public class WindowsPhonePlayerControls : IHumanPlayerControls
    {
        #region Fields

        private TouchObserver _TouchObserver;
        private ButtonObserver _ButtonObserver;

        #endregion

        public WindowsPhonePlayerControls(TouchObserver touchObserver, ButtonObserver buttonObserver)
        {
            if (touchObserver == null)
                throw new ArgumentException("Cannot construct a WindowsPhonePlayerControls without a TouchObserver");
            if (buttonObserver == null)
                throw new ArgumentException("Cannot construct a WindowsPhonePlayerControls without a ButtonObserver");

            this._TouchObserver = touchObserver;
            this._ButtonObserver = buttonObserver;
        }

        #region IHumanPlayerControls members

        public ZoomDirection Zoom
        {
            get { return ZoomDirection.None; }
        }

        public ScrollDirection Scroll
        {
            get { return ScrollDirection.None; }
        }

        public Int32 X
        {
            get { return this._TouchObserver.X; }
        }

        public Int32 Y
        {
            get { return this._TouchObserver.Y; }
        }

        public Boolean Select
        {
            get { return true; }
        }

        public Boolean Reset
        {
            get { return false; }
        }

        public Boolean IsButtonShortcutKeyPressed(Keys key)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
