using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input.Touch;

namespace Fodder.WindowsPhone.UX
{
    public class TouchObserver
    {
        public Int32 X { get { return this.GetSingleTouch(TouchPanel.GetState(), TouchAxis.X); } }
        public Int32 Y { get { return this.GetSingleTouch(TouchPanel.GetState(), TouchAxis.Y); } }

        private Int32 GetSingleTouch(TouchCollection touchCollection, TouchAxis axis)
        {
            // fail fast
            if (!touchCollection.Any())
                return 0;

            var location = 0;

            if (axis == TouchAxis.X)
                location = (Int32)touchCollection[0].Position.X;
            else
                location = (Int32)touchCollection[0].Position.Y;

            return location;
        }
    }
}
