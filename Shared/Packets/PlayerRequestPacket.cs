using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;

namespace ErenshorCoop.Shared.Packets
{
	public class PlayerRequestPacket : BasePacket
	{
		public HashSet<Request> dataTypes = new();
		public List<EntityType> requestEntityType;

		public PlayerRequestPacket() :base(DeliveryMethod.ReliableOrdered) { }

		public override void Write(NetDataWriter writer)
		{
			writer.Put((byte)PacketType.PLAYER_REQUEST);
			writer.Put(entityID);

			writer.Put(Extensions.GetSubTypeFlag(dataTypes));

			if (dataTypes.Contains(Request.ENTITY_ID))
			{
				writer.Put(requestEntityType.Count);
				foreach (var entityType in requestEntityType)
					writer.Put((byte)entityType);
			}

		}

		public override void Read(NetPacketReader reader)
		{
			entityID = reader.GetShort();
			dataTypes = Extensions.ReadSubTypeFlag<Request>(reader.GetUShort());

			if (dataTypes.Contains(Request.ENTITY_ID))
			{
				requestEntityType = new();
				var c = reader.GetInt();
				for(int i = 0; i < c; i++)
					requestEntityType.Add((EntityType)reader.GetByte());
			}

		}
	}
}
