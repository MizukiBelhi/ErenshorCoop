using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;

namespace ErenshorCoop.Shared.Packets
{
	public class ServerRequestPacket : BasePacket
	{
		public HashSet<Request> dataTypes = new();
		public List<short> reqID;

		public ServerRequestPacket() : base(DeliveryMethod.ReliableOrdered) { }

		public override void Write(NetDataWriter writer)
		{
			writer.Put((byte)PacketType.SERVER_REQUEST);
			writer.Put(entityID);

			writer.Put(Extensions.GetSubTypeFlag(dataTypes));

			if (dataTypes.Contains(Request.ENTITY_ID))
			{
				writer.Put(reqID.Count);
				foreach(var r in reqID)
					writer.Put(r);
			}

		}

		public override void Read(NetPacketReader reader)
		{
			entityID = reader.GetShort();
			dataTypes = Extensions.ReadSubTypeFlag<Request>(reader.GetUShort());

			if (dataTypes.Contains(Request.ENTITY_ID))
			{
				reqID = new();
				var c = reader.GetInt();
				for (int i = 0; i < c; i++)
					reqID.Add(reader.GetShort());
			}
		}
		
	}
}
