using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;

namespace Fodder.Windows.UX
{
    /// <summary>
    /// Defines a simple way for objects to look at mouse interaction without making
    /// calls to the static instance object in the XNA framework directly.
    /// </summary>
    public class MouseObserver
    {
        public Int32 X { get { return Mouse.GetState().X; } }
        public Int32 Y { get { return Mouse.GetState().Y; } }
        public Boolean LeftButtonClicked { get { return Mouse.GetState().LeftButton == ButtonState.Pressed; } }
        public Boolean RightButtonClicked { get { return Mouse.GetState().RightButton == ButtonState.Pressed; } }
        public Int32 MouseScrollWheelValue { get { return Mouse.GetState().ScrollWheelValue; } }
    }
}
