using ErenshorCoop.Shared.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ErenshorCoop.Client;
using ErenshorCoop.Server;
using LiteNetLib;
using LiteNetLib.Utils;

namespace ErenshorCoop.Shared
{
	public static class PacketManager
	{
		public static Dictionary<short, Dictionary<PacketType, IPacket>> packets = new();
		public static List<PacketData> serverPackets = new();

		public struct PacketData
		{
			public PacketType packetType;
			public IPacket packet;
		}

		private static Queue<(PacketType packetType, IPacket packet)> packetQueue = new();

		public static T GetOrCreatePacket<T>(short entityID, PacketType type, bool dontSend=false) where T : BasePacket, new()
		{
			if(packets.TryGetValue(entityID, out var entityData))
			{
				if (entityData.TryGetValue(type, out var packet))
				{
					( (BasePacket)packet ).hasSend = dontSend;
					return (T)packet;
				}
				else
				{
					var pack = new T { entityID = entityID };
					entityData[type] = pack;
					((BasePacket)pack).hasSend = dontSend;
					return pack;
				}
			}
			else
			{
				var entData = new Dictionary<PacketType, IPacket>();
				var pack = new T { entityID = entityID };
				entData[type] = pack;
				packets.Add(entityID, entData);
				((BasePacket)pack).hasSend = dontSend;
				return pack;
			}
		}

		public static void ServerAddPacket(PacketType type, BasePacket packet)
		{
			serverPackets.Add(new(){packet = packet, packetType = type});
		}

		
		public static void ExtremePoolNoodleAction()
		{
			if (!ClientConnectionManager.Instance.IsRunning)
			{
				packets.Clear();
				serverPackets.Clear();
				return;
			}

			if (!ServerConnectionManager.Instance.IsRunning)
			{
				serverPackets.Clear();
			}

			foreach (var packetList in packets.Values)
			{
				foreach (var packetPair in packetList)
				{
					var packetType = packetPair.Key;
					var packet = packetPair.Value;

					if (( (BasePacket)packet ).hasSend && !( (BasePacket)packet ).canSend) continue;

					SendPacket(packetType, packet);

					//packetQueue.Enqueue((packetType, packet));
				}
			}
			foreach (var packet in serverPackets)
			{
				SendPacket(packet.packetType, packet.packet);
			}
			packets.Clear();
			serverPackets.Clear();
		}

		public static void ClearQueue()
		{
			packetQueue.Clear();
			packets.Clear();
		}

		private static byte GetChannel(PacketType packetType)
		{
			byte channel = 2;
			switch (packetType)
			{
				case PacketType.GROUP:
				case PacketType.SERVER_GROUP:
					channel = 9;
					break;
				case PacketType.PLAYER_MESSAGE:
				case PacketType.PLAYER_REQUEST:
					channel = 8;
					break;
				case PacketType.PLAYER_DATA:
				case PacketType.PLAYER_ACTION:
				case PacketType.PLAYER_CONNECT:
					channel = 3;
					break;
				case PacketType.ENTITY_ACTION:
				case PacketType.ENTITY_DATA:
				case PacketType.ENTITY_SPAWN:
					channel = 5;
					break;
				case PacketType.SERVER_CONNECT:
				case PacketType.DISCONNECT:
				case PacketType.SERVER_INFO:
				case PacketType.SERVER_REQUEST:
					channel = 0; //Server specific packets
					break;
				case PacketType.ENTITY_TRANSFORM: channel = 4; break;
				case PacketType.PLAYER_TRANSFORM: channel = 2; break;
			}

			return channel;
		}
		public static void SendPacket(PacketType packetType, IPacket packet)
		{
			//if (packetQueue.Count <= 0) return;

			//( PacketType packetType, IPacket packet ) = packetQueue.Dequeue();

			byte channel = GetChannel(packetType);

			bool isClientPacket = packetType == PacketType.GROUP ||
								packetType == PacketType.PLAYER_CONNECT ||
								packetType == PacketType.PLAYER_DATA ||
								packetType == PacketType.PLAYER_ACTION ||
								packetType == PacketType.PLAYER_TRANSFORM ||
								packetType == PacketType.PLAYER_MESSAGE ||
								packetType == PacketType.PLAYER_REQUEST;

			var basePacket = (BasePacket)packet;
			
			/*if (basePacket.isSim)
			{
				channel = (byte)(packetType == PacketType.ENTITY_TRANSFORM ? 6 : 7);
			}*/


			var writer = new NetDataWriter();
			try
			{
				basePacket.Write(writer);
			} catch (Exception e)
			{
				Logging.LogError($"{e.Message} \r\n {e.StackTrace}");
				return;
			}

			if (isClientPacket)
			{
				//Logging.Log($"writing {packetType}");
				if (ServerConnectionManager.Instance.IsRunning)
				{
					if (basePacket.singleTarget)
					{
						basePacket.peer.Send(writer, channel, basePacket.deliveryMethod);
					}else
						ServerBroadcast(writer, channel, basePacket.deliveryMethod, basePacket.exclusions);
				}
				else
					ClientConnectionManager.Instance.Server.Send(writer, channel, basePacket.deliveryMethod);
			}
			else
			{
				if (basePacket.singleTarget)
				{
					//Logging.Log($"writing {packetType}");
					basePacket.peer.Send(writer, channel, basePacket.deliveryMethod);
				}
				else
				{
					ServerBroadcast(writer, channel, basePacket.deliveryMethod, basePacket.exclusions);
				}
			}
		}

		private static void ServerBroadcast(NetDataWriter writer, byte channel, DeliveryMethod deliveryMethod, List<NetPeer> exclusions)
		{
			foreach (var client in ClientConnectionManager.Instance.Players)
			{
				if (client.Key == ClientConnectionManager.Instance.LocalPlayerID) continue;
				if (exclusions.Contains(client.Value.peer)) continue;

				client.Value.peer.Send(writer, channel, deliveryMethod);
			}
		}
	}
}
