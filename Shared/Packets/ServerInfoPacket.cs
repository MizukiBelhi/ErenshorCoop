using System.Collections.Generic;
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
		}

		public override void Read(NetPacketReader reader)
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
				zone = reader.GetString();
				playerList = new();
				int count = reader.GetInt();
				for(var i=0;i<count;i++)
					playerList.Add(reader.GetShort());
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
