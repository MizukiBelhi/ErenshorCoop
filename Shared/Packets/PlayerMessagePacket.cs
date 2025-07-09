using LiteNetLib;
using LiteNetLib.Utils;

namespace ErenshorCoop.Shared.Packets
{
	public class PlayerMessagePacket : EntityBasePacket
	{

		public MessageType messageType;
		public string message;
		public string target;
		public PlayerMessagePacket() : base(DeliveryMethod.ReliableOrdered) { }

		public override void Write(NetDataWriter writer)
		{
			writer.Put((byte)PacketType.PLAYER_MESSAGE);
			writer.Put(entityID);
			writer.Put((byte)messageType);
			writer.Put(message);
			if (messageType == MessageType.WHISPER)
				writer.Put(target);
		}

		public override void Read(NetDataReader reader)
		{
			entityID = reader.GetShort();
			messageType = (MessageType)reader.GetByte();
			message = reader.GetString().Sanitize();
			if(messageType == MessageType.WHISPER)
				target = reader.GetString().Sanitize();
		}
	}
}
