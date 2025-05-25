using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;

namespace ErenshorCoop.Shared.Packets
{
	public class ServerRequestPacket : BasePacket
	{
		public HashSet<Request> dataTypes = new();
		public short reqID;

		public ServerRequestPacket() : base(DeliveryMethod.ReliableOrdered) { }

		public override void Write(NetDataWriter writer)
		{
			writer.Put((byte)PacketType.SERVER_REQUEST);
			writer.Put(entityID);

			writer.Put(Extensions.GetSubTypeFlag(dataTypes));

			if (dataTypes.Contains(Request.ENTITY_ID))
				writer.Put(reqID);

		}

		public override void Read(NetPacketReader reader)
		{
			entityID = reader.GetShort();
			dataTypes = Extensions.ReadSubTypeFlag<Request>(reader.GetUShort());

			if (dataTypes.Contains(Request.ENTITY_ID))
				reqID = reader.GetShort();
		}
		
	}
}
