using LiteNetLib;
using LiteNetLib.Utils;

namespace ErenshorCoop.Shared.Packets
{
	public class ServerConnectPacket : EntityBasePacket
	{
		public ServerConnectPacket() : base(DeliveryMethod.ReliableOrdered) { }

		public override void Write(NetDataWriter writer)
		{
			writer.Put((byte)PacketType.SERVER_CONNECT);
			writer.Put(entityID);
		}

		public override void Read(NetDataReader reader)
		{
			entityID = reader.GetShort();
		}

	}
}
