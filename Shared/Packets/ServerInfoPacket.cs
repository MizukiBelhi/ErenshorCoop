using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ErenshorCoop.Server;
using LiteNetLib;
using LiteNetLib.Utils;

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

		public override void Write(NetDataWriter writer)
		{
			writer.Put((byte)PacketType.SERVER_INFO);


			ushort flag = Extensions.GetSubTypeFlag(dataTypes);
			writer.Put(flag);

			if (dataTypes.Contains(ServerInfoType.PVP_MODE))
			{
				writer.Put(pvpMode);
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
}
