using LiteNetLib;
using LiteNetLib.Utils;

namespace ErenshorCoop.Shared.Packets
{
	public class ServerDisonnectPacket : EntityBasePacket
	{
		public ServerDisonnectPacket() : base(DeliveryMethod.ReliableOrdered) { }
		public override void Write(NetDataWriter writer)
		{
			writer.Put((byte)PacketType.DISCONNECT);
			writer.Put(entityID);
		}

		public override void Read(NetPacketReader reader)
		{
			entityID = reader.GetShort();
		}
	}
}
