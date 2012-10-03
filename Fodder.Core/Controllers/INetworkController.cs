using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Fodder.Core
{
    public enum PacketTypes
    {
        INIT,
        DUDES,
        PROJECTILES,
        FLAGS
    }

    public interface INetworkController
    {
        void Initialize(int team);

        void Update(GameTime gameTime);

    }
}
