﻿using Archipelago.MultiClient.Net.Converters;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using Oculus.Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using WebSocketSharp;

namespace Archipelago.MultiClient.Net
{
    public class ArchipelagoSession
    {
        public delegate void PacketReceivedHandler(ArchipelagoPacketBase packet);
        public event PacketReceivedHandler PacketReceived;

        public delegate void ErrorReceivedHandler(Exception e, string message);
        public event ErrorReceivedHandler ErrorReceived;

        public string Url { get; private set; }
        public bool Connected { get => Socket.IsAlive; }

        private WebSocket Socket;

        public ArchipelagoSession(string urlToHost)
        {
            this.Url = urlToHost;
            this.Socket = new WebSocket(urlToHost);
            this.Socket.OnMessage += OnMessageReceived;
            this.Socket.OnError += OnError;
        }

        public void Connect()
        {
            if (!Socket.IsAlive)
            {
                Socket.ConnectAsync();
            }
        }

        public void Disconnect()
        {
            if (Socket.IsAlive)
            {
                Socket.Close();
            }
        }

        private void OnMessageReceived(object sender, MessageEventArgs e)
        {
            if (e.IsText)
            {
                var packets = JsonConvert.DeserializeObject<List<ArchipelagoPacketBase>>(e.Data, new ArchipelagoPacketConverter());
                foreach (var packet in packets)
                {
                    PacketReceived(packet);
                }
            }
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            if (ErrorReceived != null)
            {
                ErrorReceived(e.Exception, e.Message);
            }
        }

        public void SendPacket(ArchipelagoPacketBase packet)
        {
            SendMultiplePackets(new List<ArchipelagoPacketBase> { packet });
        }

        public void SendMultiplePackets(List<ArchipelagoPacketBase> packets)
        {
            SendMultiplePackets(packets.ToArray());
        }

        public void SendMultiplePackets(params ArchipelagoPacketBase[] packets)
        {
            if (Socket.IsAlive)
            {
                var packetAsJson = JsonConvert.SerializeObject(packets);
                Socket.Send(packetAsJson);
            }
        }
    }
}
