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
        READY,
        DUDES,
        PROJECTILES,
        FLAGS
    }

    public enum RemoteClientState
    {
        NotConnected,
        Connected,
        ReadyToStart,
        InGame
    }

    public interface INetworkController
    {
        void Initialize(int team);

        void Update(GameTime gameTime);

        RemoteClientState GetState();
        int GetTeam();
        void SendReady();

    }
}
