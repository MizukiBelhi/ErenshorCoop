﻿using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using static ErenshorCoop.ErenshorCoopMod;

namespace ErenshorCoop.Shared.Packets
{
	public class ServerInfoPacket : BasePacket
	{
		public HashSet<ServerInfoType> dataTypes = new();
		public ServerInfoPacket() : base(DeliveryMethod.ReliableOrdered) { }

		public bool pvpMode = false;
		public short zoneOwner = 0;
		public List<short> playerList;
		public string zone;

		public ServerSettings serverSettings;
		public List<PluginData> plugins;
		public List<Steam.Networking.PlayerData> playerInfoList;
		public override void Write(NetDataWriter writer)
		{
			writer.Put((byte)PacketType.SERVER_INFO);


			ushort flag = Extensions.GetSubTypeFlag(dataTypes);
			writer.Put(flag);

			if (dataTypes.Contains(ServerInfoType.PVP_MODE))
			{
				writer.Put(pvpMode);
			}
			if(dataTypes.Contains(ServerInfoType.SERVER_SETTINGS))
			{
				writer.Put(serverSettings.hpMod);
				writer.Put(serverSettings.xpMod);
				writer.Put(serverSettings.dmgMod);
				writer.Put(serverSettings.lootMod);
			}
			if(dataTypes.Contains(ServerInfoType.HOST_MODS))
			{
				writer.Put(plugins.Count);
				foreach(var plugin in plugins)
				{
					writer.Put(plugin.name);
					writer.Put(plugin.version.ToString());
				}
			}
			if (dataTypes.Contains(ServerInfoType.ZONE_OWNERSHIP))
			{
				writer.Put(zoneOwner);
				writer.Put(zone);
				writer.Put(playerList.Count);
				foreach(short p in playerList)
					writer.Put(p);
			}
			if(dataTypes.Contains(ServerInfoType.PLAYER_LIST))
			{
				writer.Put(playerInfoList.Count);
				foreach(var p in playerInfoList)
				{
					writer.Put(p.playerID);
					writer.Put(p.ping);
					writer.Put(p.isMod);
					writer.Put(p.isHost);
					writer.Put(p.isDev);
				}
			}
		}

		public override void Read(NetDataReader reader)
		{
			dataTypes = Extensions.ReadSubTypeFlag<ServerInfoType>(reader.GetUShort());

			if (dataTypes.Contains(ServerInfoType.PVP_MODE))
			{
				pvpMode = reader.GetBool();
			}
			if (dataTypes.Contains(ServerInfoType.SERVER_SETTINGS))
			{
				serverSettings = new()
				{
					hpMod = reader.GetFloat(),
					xpMod = reader.GetFloat(),
					dmgMod = reader.GetFloat(),
					lootMod = reader.GetFloat()
				};
			}
			if (dataTypes.Contains(ServerInfoType.HOST_MODS))
			{
				plugins = new();
				var c = reader.GetInt();
				for(var i=0;i<c;i++)
				{
					plugins.Add(new()
					{
						name = reader.GetString(),
						version = new System.Version(reader.GetString())
					});
				}
			}
			if (dataTypes.Contains(ServerInfoType.ZONE_OWNERSHIP))
			{
				zoneOwner = reader.GetShort();
				zone = reader.GetString().Sanitize();
				playerList = new();
				int count = reader.GetInt();
				for(var i=0;i<count;i++)
					playerList.Add(reader.GetShort());
			}
			if (dataTypes.Contains(ServerInfoType.PLAYER_LIST))
			{
				playerInfoList = new();
				var c = reader.GetInt();
				for(int i=0;i<c;i++)
				{
					var plID = reader.GetShort();
					var ping = reader.GetInt();
					var isMod = reader.GetBool();
					var isHost = reader.GetBool();
					var isDev = reader.GetBool();

					playerInfoList.Add(new() { playerID = plID, ping = ping, isMod = isMod, isHost = isHost, isDev = isDev });
				}
			}
		}

	}

	public class ServerSettings
	{
		public float xpMod;
		public float hpMod;
		public float lootMod;
		public float dmgMod;
	}
}
