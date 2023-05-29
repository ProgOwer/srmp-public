﻿using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.ActorEmotions)]
    public class PacketActorEmotions : Packet
    {
        public int ID;
        public float Fear;
        public float Agitation;
        public float Hunger;

        public PacketActorEmotions() { }
        public PacketActorEmotions(NetIncomingMessage im) { Deserialize(im); }
    }
}
