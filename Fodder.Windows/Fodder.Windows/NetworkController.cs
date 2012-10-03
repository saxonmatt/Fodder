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
using Lidgren.Network;
using Fodder.Core;

namespace Fodder.Windows
{
    class NetworkController : INetworkController
    {
        NetPeer peer;
        NetConnection conn;

        int ListenPort = 12345;
        int ClientPort = 12346;
        
        double currentUpdateTime = 0;

        double UPDATE_TIME = 100;

        public int Team;

        public void Initialize(int team)
        {
            Team = team;

            NetPeerConfiguration Config = new NetPeerConfiguration("fodder");
            Config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
            Config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
            Config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            Config.EnableMessageType(NetIncomingMessageType.UnconnectedData);
            Config.Port = 12345;
            Config.LocalAddress = NetUtility.Resolve("localhost");

            peer = new NetPeer(Config);
            peer.Start();
        }

        public void LoadContent(ContentManager content)
        {
            
        }

        public void Update(GameTime gameTime)
        {

            currentUpdateTime += gameTime.ElapsedGameTime.TotalMilliseconds;

            if (currentUpdateTime >= UPDATE_TIME)
            {
                currentUpdateTime = 0;

                if (peer.Connections.Count == 0)
                {
                    peer.DiscoverKnownPeer("localhost", ClientPort);
                }

                if (peer.Connections.Count > 0)
                {
                    NetOutgoingMessage outmsg = peer.CreateMessage();
                    outmsg.Write((Int32)PacketTypes.DUDES);
                    foreach (Dude d in GameSession.Instance.DudeController.Dudes)
                    {
                        if (d.Active && d.Team == Team)
                        {
                            DudeNetPacket dnp = new DudeNetPacket();
                            dnp.WriteTo(d);
                            outmsg.WriteAllProperties(dnp);
                        }
                    }
                    peer.SendMessage(outmsg, peer.Connections, NetDeliveryMethod.ReliableOrdered, 0);
                }
            }

            NetIncomingMessage msg;
            while ((msg = peer.ReadMessage()) != null)
            {
                switch (msg.MessageType)
                {
                    case NetIncomingMessageType.DiscoveryRequest:
                        //Console.WriteLine("ReceivePeersData DiscoveryRequest");
                        peer.SendDiscoveryResponse(null, msg.SenderEndpoint);
                        break;
                    case NetIncomingMessageType.DiscoveryResponse:
                        // just connect to first server discovered
                        //Console.WriteLine("ReceivePeersData DiscoveryResponse CONNECT");
                        peer.Connect(msg.SenderEndpoint);
                        break;
                    case NetIncomingMessageType.ConnectionApproval:
                        //Console.WriteLine("ReceivePeersData ConnectionApproval");
                        msg.SenderConnection.Approve();
                        //broadcast this to all connected clients
                        //msg.SenderEndpoint.Address, msg.SenderEndpoint.Port
                        //netManager.SendPeerInfo(msg.SenderEndpoint.Address, msg.SenderEndpoint.Port);
                        break;
                    case NetIncomingMessageType.Data:
                        //another client sent us data
                        //Console.WriteLine("BEGIN ReceivePeersData Data");
                        PacketTypes mType = (PacketTypes)msg.ReadInt32();
                        if (mType == PacketTypes.DUDES)
                        {
                            foreach (Dude d in GameSession.Instance.DudeController.Dudes)
                                if (d.Team == Team) d.Active = false;
                            while (msg.Position < msg.LengthBits)
                            {
                                try
                                {
                                    DudeNetPacket dnp = new DudeNetPacket();
                                    msg.ReadAllProperties(dnp);
                                    dnp.Team = Team;
                                    foreach (Dude d in GameSession.Instance.DudeController.Dudes)
                                    {
                                        if (!d.Active)
                                        {
                                            d.ReadFromPacket(dnp);
                                            break;
                                        }
                                    }
                                }
                                catch (Exception ex) { }
                            }
                        }
                        break;
                    case NetIncomingMessageType.UnconnectedData:
                        string orphanData = msg.ReadString();
                        //Console.WriteLine("UnconnectedData: " + orphanData);
                        break;
                    default:
                        //Console.WriteLine("ReceivePeersData Unknown type: " + msg.MessageType.ToString());
                        try
                        {
                            //Console.WriteLine(msg.ReadString());
                        }
                        catch
                        {
                            //Console.WriteLine("Couldn't parse unknown to string.");
                        }
                        break;
                }
            }
        }

        public void HandleInput(MouseState ms)
        {
           
        }

        public void Draw(SpriteBatch sb)
        {
           
        }

       
    }
}
