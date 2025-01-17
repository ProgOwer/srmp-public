﻿using Lidgren.Network;
using SRMultiplayer;
using SRMultiplayer.Networking;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SRMultiplayer
{
    public static class Extensions
    {
        public static void Rebuild(this RefineryUI ui)
        {
            foreach(Transform child in ui.inventoryGridPanel.transform)
            {
                GameObject.Destroy(child.gameObject);
            }

            GadgetDirector gadgetDirector = SRSingleton<SceneContext>.Instance.GadgetDirector;
            PediaDirector pediaDirector = SRSingleton<SceneContext>.Instance.PediaDirector;
            int num = 0;
            foreach (Identifiable.Id id in ui.listedItems)
            {
                int refineryCount = gadgetDirector.GetRefineryCount(id);
                Identifiable.Id id2 = id;
                PediaDirector.Id? pediaId = pediaDirector.GetPediaId(Identifiable.IsPlort(id) ? ui.PlortToSlime(id) : id);
                if (refineryCount == 0 && pediaId != null && !pediaDirector.IsUnlocked(pediaId.Value))
                {
                    id2 = Identifiable.Id.NONE;
                }
                ui.AddInventory(id2, refineryCount);
                num++;
            }
            for (int j = num; j < 15; j++)
            {
                ui.AddEmptyInventory();
            }
        }

        public static void WriteAmmoSlot(this NetOutgoingMessage om, Ammo.Slot slot)
        {
            om.Write(slot != null);
            if (slot != null)
            {
                om.Write((ushort)slot.id);
                om.Write(slot.count);
                om.Write(slot.emotions != null);
                if (slot.emotions != null)
                {
                    om.Write(slot.emotions.Count);
                    foreach (var emotion in slot.emotions)
                    {
                        om.Write((byte)emotion.Key);
                        om.Write(emotion.Value);
                    }
                }
            }
        }

        public static Ammo.Slot ReadAmmoSlot(this NetIncomingMessage im)
        {
            if (im.ReadBoolean())
            {
                var slot = new Ammo.Slot((Identifiable.Id)im.ReadUInt16(), im.ReadInt32());
                if (im.ReadBoolean())
                {
                    int emotionCount = im.ReadInt32();
                    slot.emotions = new SlimeEmotionData();
                    for (int l = 0; l < emotionCount; l++)
                    {
                        slot.emotions.Add((SlimeEmotions.Emotion)im.ReadByte(), im.ReadFloat());
                    }
                }
                return slot;
            }
            return null;
        }

        public static void Send(this Packet packet, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered, int sequence = 0)
        {
            if(!Globals.IsClient)
            {
                NetworkServer.Instance.SendToAll(packet, method, sequence);
                return;
            }
            NetworkClient.Instance.Send(packet, method, sequence);
        }

        public static void Send(this Packet packet, NetworkPlayer player, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered, int sequence = 0)
        {
            if (!Globals.IsServer)
            {
                SRMP.Log("Trying to send packet as server while not server");
                return;
            }

            NetworkServer.Instance.Send(player.Connection, packet, method, sequence);
        }

        public static void SendToAll(this Packet packet, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered, int sequence = 0)
        {
            if (!Globals.IsServer)
            {
                SRMP.Log("Trying to send packet as server while not server");
                return;
            }

            List<NetConnection> cons = new List<NetConnection>();
            foreach (var p in Globals.Players.Values)
            {
                if (p.Connection != null)
                {
                    cons.Add(p.Connection);
                }
            }
            NetworkServer.Instance.SendTo(packet, cons, method, sequence);
        }

        public static void SendToAllInRegions(this Packet packet, NetworkPlayer player, bool includeSelf, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered, int sequence = 0)
        {
            if (!Globals.IsServer)
            {
                SRMP.Log("Trying to send packet as server while not server");
                return;
            }

            List<NetConnection> cons = new List<NetConnection>();
            foreach(var netRegion in player.Regions)
            {
                foreach(var p in netRegion.Players)
                {
                    if(p.ID != player.ID && p.Connection != null && !cons.Contains(p.Connection))
                    {
                        cons.Add(p.Connection);
                    }
                }
            }
            NetworkServer.Instance.SendTo(packet, cons, method, sequence);
        }

        public static void SendToAllExcept(this Packet packet, NetworkPlayer player, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered, int sequence = 0)
        {
            if (!Globals.IsServer)
            {
                SRMP.Log("Trying to send packet as server while not server");
                return;
            }

            List<NetConnection> cons = new List<NetConnection>();
            foreach (var p in Globals.Players.Values)
            {
                if (p.ID != player.ID && p.Connection != null)
                {
                    cons.Add(p.Connection);
                }
            }
            NetworkServer.Instance.SendTo(packet, cons, method, sequence);
        }

        public static T CopyComponent<T>(this T original, GameObject destination) where T : Component
        {
            System.Type type = original.GetType();
            Component copy = destination.AddComponent(type);
            System.Reflection.FieldInfo[] fields = type.GetFields();
            foreach (System.Reflection.FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }
            return copy as T;
        }

        public static T GetOrAddComponent<T>(this GameObject obj) where T : Component
        {
            var comp = obj.GetComponent<T>();
            if(comp == null)
            {
                return obj.AddComponent<T>();
            }
            return comp;
        }

        public static T GetInParent<T>(this GameObject obj) where T : Component
        {
            var cmp = obj.GetComponent<T>();
            if (cmp != null)
            {
                return cmp;
            }
            if (obj.transform.parent != null)
            {
                return GetInParent<T>(obj.transform.parent.gameObject);
            }
            return default(T);
        }

        public static string GetGameObjectPath(this Transform transform, bool withID = true)
        {
            string path = transform.name + (withID ?  transform.GetSiblingIndex().ToString() : "");
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + (withID ? transform.GetSiblingIndex().ToString() : "") + "/" + path;
            }
            return path;
        }

        public static string GetGameObjectPath(this GameObject transform, bool withID = true)
        {
            return GetGameObjectPath(transform.transform, withID);
        }

        public static bool GetBit(this byte b, int bitNumber)
        {
            return (b & (1 << bitNumber)) != 0;
        }

        public static byte SetBit(this byte b, int position, bool value)
        {
            if(value)
            {
                return (byte)(b | (1 << position));
            }
            else
            {
                return (byte)(b & ~(1 << position));
            }
        }

        public static Transform FindDisabled(this Transform transform, string name)
        {
            if(transform.name.Equals(name, StringComparison.CurrentCulture))
            {
                return transform;
            }
            foreach(Transform child in transform)
            {
                var found = FindDisabled(child, name);
                if(found != null)
                {
                    return found;
                }
            }
            return null;
        }
    }
}
