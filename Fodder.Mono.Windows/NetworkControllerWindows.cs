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
using System.Net;
using System.ComponentModel;

namespace Fodder.Windows
{
    

    class NetworkControllerWindows : INetworkController
    {
        NetPeer peer;
        NetConnection conn;

        public int ListenPort = 12345;
        public int ClientPort = 12345;

        public string HostName = "localhost";
        
        double currentUpdateTime = 0;

        double UPDATE_TIME = 100;

        public int Team;
        public RemoteClientState RemoteState;

        

        

        public void Initialize(int team)
        {
            Team = team;
            RemoteState = RemoteClientState.NotConnected;

            

            NetPeerConfiguration Config = new NetPeerConfiguration("fodder");
            Config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
            Config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
            Config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            Config.EnableMessageType(NetIncomingMessageType.UnconnectedData);
            Config.Port = ListenPort;
            Config.EnableUPnP = true;
            //Config.LocalAddress = NetUtility.Resolve("localhost");

            peer = new NetPeer(Config);
            peer.Start();
        }

        public bool UPNP()
        {
            NetUPnP pnp = new NetUPnP(peer);
            
            return pnp.ForwardPort(12345, "Fodder game");
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
                    try
                    {
                        peer.DiscoverKnownPeer(new System.Net.IPEndPoint(NetUtility.Resolve(HostName), ClientPort));
                    }
                    catch (Exception ex) { };
                }

                if (peer.Connections.Count > 0)
                {
                    if (RemoteState == RemoteClientState.NotConnected) RemoteState = RemoteClientState.Connected;
                    NetOutgoingMessage outmsg = peer.CreateMessage();
                    outmsg.Write((Int32)PacketTypes.DUDES);
                    int dudecount = 0;
                    foreach (Dude d in GameSession.Instance.DudeController.Dudes)
                        if (d.Active && d.Team != Team) dudecount++;
                    outmsg.Write(dudecount);
                    foreach (Dude d in GameSession.Instance.DudeController.Dudes)
                    {
                        if (d.Active && d.Team != Team)
                        {
                            DudeNetPacket dnp = new DudeNetPacket();
                            dnp.WriteTo(d);
                            outmsg.WriteAllProperties(dnp);
                        }
                    }
                    outmsg.WritePadBits();
                    peer.SendMessage(outmsg, peer.Connections, NetDeliveryMethod.ReliableOrdered, 0);

                    outmsg = peer.CreateMessage();
                    outmsg.Write((Int32)PacketTypes.PROJECTILES);
                    int pcount = 0;
                    foreach (Projectile p in GameSession.Instance.ProjectileController.Projectiles)
                        if (p.Active && p.Team != Team) pcount++;
                    outmsg.Write(pcount);
                    foreach (Projectile p in GameSession.Instance.ProjectileController.Projectiles)
                    {
                        if (p.Active && p.Team != Team)
                        {
                            ProjectileNetPacket pnp = new ProjectileNetPacket();
                            pnp.WriteTo(p);
                            outmsg.WriteAllProperties(pnp);
                        }
                    }
                    outmsg.WritePadBits();
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
                        //RemoteState = RemoteClientState.Connected;
                        break;
                    case NetIncomingMessageType.DiscoveryResponse:
                        // just connect to first server discovered
                        //Console.WriteLine("ReceivePeersData DiscoveryResponse CONNECT");
                        try
                        {
                            peer.Connect(msg.SenderEndpoint);
                        }
                        catch (Exception ex) { }
                        break;
                    case NetIncomingMessageType.ConnectionApproval:
                        //Console.WriteLine("ReceivePeersData ConnectionApproval");
                        msg.SenderConnection.Approve();
                        Team = 1;
                        //RemoteState = RemoteClientState.Connected;
                        //broadcast this to all connected clients
                        //msg.SenderEndpoint.Address, msg.SenderEndpoint.Port
                        //netManager.SendPeerInfo(msg.SenderEndpoint.Address, msg.SenderEndpoint.Port);
                        break;
                    case NetIncomingMessageType.Data:
                        //another client sent us data
                        //Console.WriteLine("BEGIN ReceivePeersData Data");
                        PacketTypes mType = (PacketTypes)msg.ReadInt32();
                        if (mType == PacketTypes.READY)
                        {
                            RemoteState = RemoteClientState.InGame;
                        }
                        if (mType == PacketTypes.DUDES)
                        {
                            foreach (Dude d in GameSession.Instance.DudeController.Dudes)
                                if (d.Team == Team) d.Active = false;
                            int dudecount = msg.ReadInt32();
                            for(int i=0;i<dudecount;i++)
                            {
                                try
                                {
                                    DudeNetPacket dnp = new DudeNetPacket();
                                    try { msg.ReadAllProperties(dnp); }
                                    catch (NetException e) { break; }
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
                        if (mType == PacketTypes.PROJECTILES)
                        {
                            foreach (Projectile p in GameSession.Instance.ProjectileController.Projectiles)
                                if (p.Team == Team) p.Active = false;
                            int pcount = msg.ReadInt32();
                            for (int i = 0; i < pcount; i++)
                            {
                                try
                                {
                                    ProjectileNetPacket pnp = new ProjectileNetPacket();
                                    try { msg.ReadAllProperties(pnp); }
                                    catch (NetException e) { break; }
                                    pnp.Team = Team;
                                    foreach (Projectile p in GameSession.Instance.ProjectileController.Projectiles)
                                    {
                                        if (!p.Active)
                                        {
                                            p.ReadFromPacket(pnp);
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

        public RemoteClientState GetState()
        {
            return RemoteState;
        }
        public int GetTeam()
        {
            return Team;
        }

        public void SendReady()
        {
            if (peer.Connections.Count > 0)
            {
                NetOutgoingMessage outmsg = peer.CreateMessage();
                outmsg.Write((Int32)PacketTypes.READY);
                peer.SendMessage(outmsg, peer.Connections, NetDeliveryMethod.ReliableOrdered, 0);
            }
        }

        public void CloseConn()
        {
            peer.Shutdown("bye");
        }
       
    }
}
